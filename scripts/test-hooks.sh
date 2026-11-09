#!/usr/bin/env bash
# Six valid and eight invalid commit messages through the commit-msg hook.
set -uo pipefail

HOOK=".githooks/commit-msg"
tmp=$(mktemp)
fails=0

check() { # check <expected-exit> <message>
  printf '%s\n' "$2" > "$tmp"
  bash "$HOOK" "$tmp" >/dev/null 2>&1
  got=$?
  if [ "$got" -ne "$1" ]; then
    echo "FAIL (expected exit $1, got $got): $2"
    fails=$((fails + 1))
  fi
}

check 0 "feat(api): add get posts endpoint"
check 0 "fix(data): apply pending migrations on start"
check 0 "docs: document how to run the api"
check 0 "ci: add pull request workflow"
check 0 "test(api): cover the empty list response"
check 0 "revert: feat(api): add get posts endpoint"
check 0 "Merge pull request #1 from origin/milestone/m01-list-posts"

check 1 "Added stuff"
check 1 "feat: Add Posts Endpoint"
check 1 "feat(api) add get posts endpoint"
check 1 "feature(api): add get posts endpoint"
check 1 "feat(api): add get posts endpoint."
check 1 "feat(api):add get posts endpoint"
check 1 ""
check 1 "feat(api): this subject line is far too long to read in a one line git log output listing"

rm -f "$tmp"
if [ "$fails" -ne 0 ]; then
  echo "$fails hook vector(s) failed"
  exit 1
fi
echo "commit-msg hook: 15/15 vectors OK"
