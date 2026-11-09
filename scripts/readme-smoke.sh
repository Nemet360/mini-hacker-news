#!/usr/bin/env bash
# Runs the commands the README tells a new contributor to run, so the docs cannot rot.
# BASE_URL can be overridden when port 5080 is taken on the machine running this.
set -euo pipefail

BASE_URL=${BASE_URL:-http://localhost:5080}

rm -rf src/MiniHn.Api/data
dotnet build -c Release --nologo >/dev/null

ASPNETCORE_URLS="$BASE_URL" dotnet run --no-build --no-launch-profile -c Release --project src/MiniHn.Api &
API_PID=$!
cleanup() { kill "$API_PID" 2>/dev/null || true; wait "$API_PID" 2>/dev/null || true; }
trap cleanup EXIT

for _ in $(seq 1 60); do
  if curl -fs "$BASE_URL/posts" >/dev/null 2>&1; then break; fi
  sleep 0.5
done

body=$(curl -fs "$BASE_URL/posts")
echo "GET $BASE_URL/posts -> $body"
test "$body" = "[]" || { echo "README smoke: expected [] on a fresh database, got $body"; exit 1; }
test -f src/MiniHn.Api/data/minihn.db || { echo "README smoke: the database file was not created"; exit 1; }
echo "README smoke: OK"
