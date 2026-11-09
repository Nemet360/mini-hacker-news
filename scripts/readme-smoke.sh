#!/usr/bin/env bash
# Runs the commands the README tells a new contributor to run, so the docs cannot rot.
set -euo pipefail

rm -rf src/MiniHn.Api/data
dotnet build -c Release --nologo >/dev/null

dotnet run --no-build -c Release --project src/MiniHn.Api &
API_PID=$!
trap 'kill "$API_PID" 2>/dev/null || true' EXIT

for _ in $(seq 1 40); do
  if curl -fs http://localhost:5080/posts >/dev/null 2>&1; then break; fi
  sleep 0.5
done

body=$(curl -fs http://localhost:5080/posts)
echo "GET /posts -> $body"
test "$body" = "[]" || { echo "README smoke: expected [] on a fresh database, got $body"; exit 1; }
test -f src/MiniHn.Api/data/minihn.db || { echo "README smoke: the database file was not created"; exit 1; }
echo "README smoke: OK"
