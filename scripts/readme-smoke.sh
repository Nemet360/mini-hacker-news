#!/usr/bin/env bash
# Runs the commands the README tells a new contributor to run, so the docs cannot rot.
# BASE_URL can be overridden when port 5080 is taken on the machine running this.
# SMOKE_ADMIN_URL (a postgres:// URL to a maintenance database) lets it drop its
# throwaway database afterwards; CI sets it.
set -euo pipefail

BASE_URL=${BASE_URL:-http://localhost:5080}

: "${ConnectionStrings__Posts:?README smoke: set ConnectionStrings__Posts to a PostgreSQL connection string}"

# A fresh store is the point of the check, but the configured database may hold
# posts somebody crawled. So by default the API runs against a throwaway database
# beside it on the same server: it creates the database when it migrates, and the
# script drops it on exit when psql is available. KEEP_DATA=1 uses the configured
# database as it is and only asserts the route.
SMOKE_DB=""
if [ "${KEEP_DATA:-0}" != "1" ]; then
  SMOKE_DB="minihn_readme_smoke_$$"
  ConnectionStrings__Posts=$(printf '%s' "$ConnectionStrings__Posts" | sed -E "s/(^|;)[Dd]atabase=[^;]*/\1Database=$SMOKE_DB/")
  case "$ConnectionStrings__Posts" in
    *"Database=$SMOKE_DB"*) ;;
    *) ConnectionStrings__Posts="$ConnectionStrings__Posts;Database=$SMOKE_DB" ;;
  esac
  export ConnectionStrings__Posts
fi
dotnet build -c Release --nologo >/dev/null

ASPNETCORE_URLS="$BASE_URL" dotnet run --no-build --no-launch-profile -c Release --project src/MiniHn.Api &
API_PID=$!
cleanup() {
  kill "$API_PID" 2>/dev/null || true
  wait "$API_PID" 2>/dev/null || true
  if [ -n "$SMOKE_DB" ] && [ -n "${SMOKE_ADMIN_URL:-}" ] && command -v psql >/dev/null 2>&1; then
    psql "$SMOKE_ADMIN_URL" -qc "DROP DATABASE IF EXISTS \"$SMOKE_DB\" WITH (FORCE)" >/dev/null || true
  fi
}
trap cleanup EXIT

for _ in $(seq 1 60); do
  if curl -fs "$BASE_URL/posts" >/dev/null 2>&1; then break; fi
  sleep 0.5
done

body=$(curl -fs "$BASE_URL/posts")
echo "GET $BASE_URL/posts -> $(printf '%.80s' "$body")"
if [ "${KEEP_DATA:-0}" != "1" ]; then
  test "$body" = "[]" || { echo "README smoke: expected [] on a fresh database, got $body"; exit 1; }
fi
# The README also documents a coverage command; CI advertises this script as
# "the README's own commands", so that one is run here as well.
if [ "${SKIP_COVERAGE:-0}" != "1" ]; then
  echo "README smoke: coverage block"
  CONFIGURATION=Release EXTRA_TEST_ARGS=--no-build bash scripts/coverage.sh >/dev/null
  grep -E "Line coverage|Branch coverage" coverage/report/Summary.txt
fi

echo "README smoke: OK"
