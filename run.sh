#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [ -z "${META_MODEL_API_KEY:-}" ]; then
  read -rsp "Enter META_MODEL_API_KEY: " META_MODEL_API_KEY
  echo
fi
export META_MODEL_API_KEY

if [ -z "${META_MODEL_API_KEY}" ]; then
  echo "META_MODEL_API_KEY is required." >&2
  exit 1
fi

cleanup() {
  echo "Stopping services..."
  [ -n "${BACKEND_PID:-}" ] && kill "$BACKEND_PID" 2>/dev/null || true
  [ -n "${FRONTEND_PID:-}" ] && kill "$FRONTEND_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

echo "Starting backend..."
(cd "$ROOT_DIR/backend" && dotnet run) &
BACKEND_PID=$!

echo "Starting frontend..."
(cd "$ROOT_DIR/frontend" && npm run dev) &
FRONTEND_PID=$!

wait "$BACKEND_PID" "$FRONTEND_PID"
