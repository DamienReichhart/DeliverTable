#!/bin/sh
set -eu

TEMPLATE=/usr/local/etc/redis/redis.conf
RUNTIME_CONF=/tmp/redis.conf

if [ -z "${REDIS_PASSWORD:-}" ]; then
  echo "ERROR: REDIS_PASSWORD is not set" >&2
  exit 1
fi

cp "$TEMPLATE" "$RUNTIME_CONF"
sed -i "s|REDIS_PASSWORD_PLACEHOLDER|${REDIS_PASSWORD}|" "$RUNTIME_CONF"

exec redis-server "$RUNTIME_CONF"
