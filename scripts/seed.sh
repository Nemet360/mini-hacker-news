#!/usr/bin/env bash
# Starts the configured store over with SEED_COUNT (default 100) real Hacker News
# stories, then checks that exactly that many posts are stored.
#
#   ConnectionStrings__Posts="Host=localhost;Database=minihn;Username=minihn;Password=..." bash scripts/seed.sh
set -euo pipefail

: "${ConnectionStrings__Posts:?seed: set ConnectionStrings__Posts to a PostgreSQL connection string}"
SEED_COUNT=${SEED_COUNT:-100}

output=$(dotnet run --project tools/MiniHn.Crawler -c Release -- --clear --count "$SEED_COUNT" | tee /dev/stderr)

stored=$(printf '%s\n' "$output" | sed -nE 's/.*store now holds ([0-9]+) post\(s\).*/\1/p' | tail -n 1)
if [ "$stored" != "$SEED_COUNT" ]; then
  echo "seed: expected $SEED_COUNT posts, the store holds ${stored:-unknown}" >&2
  exit 1
fi
echo "seed: OK, $stored posts"
