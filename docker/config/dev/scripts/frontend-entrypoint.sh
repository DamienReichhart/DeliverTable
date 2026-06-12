#!/bin/bash
set -uo pipefail

PROJECT="DeliverTableClient"
SHARED_LIB="DeliverTableSharedLibrary"
APP_PID=""

kill_tree() {
    local pid=$1 sig=${2:-TERM}
    local children
    children=$(pgrep -P "$pid" 2>/dev/null) || true
    for child in $children; do
        kill_tree "$child" "$sig"
    done
    kill -"$sig" "$pid" 2>/dev/null || true
}

cleanup() {
    [ -n "$APP_PID" ] && kill_tree "$APP_PID" KILL
    exit 0
}
trap cleanup SIGTERM SIGINT

stop_app() {
    if [ -n "$APP_PID" ] && kill -0 "$APP_PID" 2>/dev/null; then
        kill_tree "$APP_PID" TERM
        local i=0
        while [ $i -lt 10 ] && kill -0 "$APP_PID" 2>/dev/null; do
            sleep 0.5
            i=$((i + 1))
        done
        kill_tree "$APP_PID" KILL 2>/dev/null || true
        wait "$APP_PID" 2>/dev/null || true
        APP_PID=""
    fi
}

start_app() {
    dotnet run --project "$PROJECT" --no-launch-profile &
    APP_PID=$!
}

start_app

# NOTE: the `.css`/`.css.map` exclusion is essential, not cosmetic. The Sass
# compiler writes its generated output (wwwroot/css/app.css and the scoped
# *.razor.css bundles) back inside the watched tree. Without excluding it, the
# compiler's own output would retrigger this loop, killing `dotnet run`
# mid-compile and leaving a truncated (0-byte) app.css — i.e. broken styling.
while inotifywait -r -q \
    -e modify -e create -e delete -e move \
    --exclude '(/(obj|bin|\.git|node_modules|\.vs)/|\.css(\.map)?$)' \
    "$PROJECT/" "$SHARED_LIB/"; do

    echo ""
    echo "── File change detected — rebuilding ──"
    echo ""

    stop_app
    start_app
done
