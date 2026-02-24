#!/bin/sh
set -eu

INIT_MARKER="/var/lib/garage/meta/.initialized"

cleanup() { kill "$GARAGE_PID" 2>/dev/null; wait "$GARAGE_PID" 2>/dev/null; }
trap cleanup TERM INT

garage server &
GARAGE_PID=$!

echo "Waiting for Garage to start..."
retries=0
until garage status >/dev/null 2>&1; do
  retries=$((retries + 1))
  if [ "$retries" -ge 30 ]; then
    echo "ERROR: Garage failed to start after 30s" >&2
    exit 1
  fi
  sleep 1
done
echo "Garage is running."

if [ ! -f "$INIT_MARKER" ]; then
  echo "First run – initializing cluster layout, bucket, and API key..."

  NODE_ID=$(garage node id 2>/dev/null | head -1 | cut -d@ -f1 | tr -d '[:space:]')
  if [ -z "$NODE_ID" ]; then
    echo "ERROR: Failed to retrieve Garage node ID" >&2
    exit 1
  fi

  garage layout assign "$NODE_ID" -z dc1 -c "${GARAGE_NODE_CAPACITY:-1G}" 2>&1 || true
  garage layout apply --version 1 2>&1 || true

  garage bucket create "${GARAGE_BUCKET_NAME}" 2>&1 || true
  garage key import --yes "${GARAGE_S3_ACCESS_KEY}" "${GARAGE_S3_SECRET_KEY}" 2>&1 || true
  garage bucket allow --read --write --owner "${GARAGE_BUCKET_NAME}" --key "${GARAGE_S3_ACCESS_KEY}" 2>&1 || true

  if ! garage key info "${GARAGE_S3_ACCESS_KEY}" >/dev/null 2>&1; then
    echo "ERROR: API key ${GARAGE_S3_ACCESS_KEY} was not created." >&2
    echo "Access key must be GK + 24 hex chars, secret key must be 64 hex chars." >&2
    exit 1
  fi

  touch "$INIT_MARKER"
  echo "Garage initialization complete."
fi

wait "$GARAGE_PID"
