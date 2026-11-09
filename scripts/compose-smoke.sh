#!/usr/bin/env bash
# Proves docker-compose.yml brings up a healthy PostgreSQL. It runs as its own
# compose project, on its own port and volume, with a throwaway password, and
# removes all of it on exit, so it never touches a developer's database.
set -euo pipefail

export COMPOSE_PROJECT_NAME=minihn-compose-smoke
export POSTGRES_PASSWORD="smoke-$$"
export POSTGRES_PORT=${SMOKE_PORT:-55432}

cleanup() { docker compose down -v --remove-orphans >/dev/null 2>&1 || true; }
trap cleanup EXIT

docker compose up -d --wait --wait-timeout 120
docker compose exec -T postgres sh -c 'pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB"'
echo "compose smoke: OK"
