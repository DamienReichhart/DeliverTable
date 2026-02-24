# ── Stage 1: Build Go tools ───────────────────────────────────
FROM golang:1.24-alpine AS tools

WORKDIR /src
COPY docker/images/tools/ ./

RUN CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/depcopier   ./depcopier   && \
    CGO_ENABLED=0 go build -trimpath -ldflags='-s -w' -o /out/healthcheck ./healthcheck

# ── Stage 2: Build Blazor WASM ────────────────────────────────
# Standard SDK (not Alpine) — the bundled Dart Sass in
# AspNetCore.SassCompiler requires glibc.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY DeliverTableClient/DeliverTableClient.csproj               DeliverTableClient/
COPY DeliverTableSharedLibrary/DeliverTableSharedLibrary.csproj DeliverTableSharedLibrary/

RUN dotnet restore DeliverTableClient/DeliverTableClient.csproj

COPY DeliverTableClient/       DeliverTableClient/
COPY DeliverTableSharedLibrary/ DeliverTableSharedLibrary/

RUN dotnet publish DeliverTableClient/DeliverTableClient.csproj \
    -c Release \
    -o /app/publish \
    --no-restore

# ── Stage 3: Assemble rootfs from nginx:alpine ───────────────
# We extract the nginx binary, its shared-library deps, and the
# default nginx.conf (which includes conf.d/*.conf), then layer in
# the Blazor WASM static output and our site-specific config.
FROM nginx:1.27-alpine AS rootfs

COPY --from=tools /out/depcopier   /tools/depcopier
COPY --from=tools /out/healthcheck /staging/healthcheck

COPY docker/config/prod/nginx/frontend.conf  /staging/frontend.conf
COPY --from=build /app/publish/wwwroot       /staging/html

RUN /tools/depcopier \
    --scan  /usr/sbin/nginx \
    --copy  /usr/sbin/nginx:/usr/sbin/nginx \
    --copy  /etc/nginx/nginx.conf:/etc/nginx/nginx.conf \
    --copy  /etc/nginx/mime.types:/etc/nginx/mime.types \
    --copy  /staging/frontend.conf:/etc/nginx/conf.d/default.conf \
    --copy  /staging/html:/usr/share/nginx/html \
    --copy  /staging/healthcheck:/healthcheck \
    --link  /dev/stdout:/var/log/nginx/access.log \
    --link  /dev/stderr:/var/log/nginx/error.log \
    --link  /var/run:/run \
    --user  101:101:nginx \
    --mkdir /var/cache/nginx \
    --mkdir /var/run \
    --mkdir /tmp \
    --out   /rootfs

# ── Stage 4: Scratch ─────────────────────────────────────────
FROM scratch

COPY --from=rootfs /rootfs /

EXPOSE 80
STOPSIGNAL SIGQUIT

ENTRYPOINT ["/usr/sbin/nginx", "-g", "daemon off;"]
