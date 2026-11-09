#!/usr/bin/env bash
# The coverage claim in the README, as one runnable command. CI runs this too, so
# a reader and the badge can never be looking at different numbers.
#
#   --results-directory coverage is load-bearing: without it dotnet test writes
#   each project's report under tests/<project>/TestResults/ and the pattern below
#   matches nothing.
set -euo pipefail

CONFIGURATION=${CONFIGURATION:-Debug}
EXTRA_TEST_ARGS=${EXTRA_TEST_ARGS:-}

rm -rf coverage

# shellcheck disable=SC2086
dotnet test -c "$CONFIGURATION" $EXTRA_TEST_ARGS \
  --collect:"XPlat Code Coverage" \
  --settings coverlet.runsettings \
  --results-directory coverage

dotnet tool run reportgenerator \
  -reports:'coverage/**/coverage.cobertura.xml' \
  -targetdir:coverage/report \
  -reporttypes:'TextSummary;MarkdownSummaryGithub;Cobertura;Badges'

cat coverage/report/Summary.txt

# A report with nothing in it is not a pass.
grep -q "Line coverage" coverage/report/Summary.txt
