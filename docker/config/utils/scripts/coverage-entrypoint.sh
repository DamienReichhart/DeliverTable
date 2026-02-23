#!/bin/bash
set -euo pipefail

RESULTS_DIR="/tmp/coverage-results"
REPORT_DIR="/reports"
THRESHOLD="${COVERAGE_THRESHOLD:-80}"

echo "══════════════════════════════════════════"
echo "  Running Tests with Coverage Collection"
echo "══════════════════════════════════════════"
echo ""

dotnet test DeliverTable.sln \
    --configuration Release \
    --collect:"XPlat Code Coverage" \
    --results-directory "$RESULTS_DIR" \
    --logger "console;verbosity=normal" \
    --logger "trx;LogFilePrefix=coverage"

echo ""
echo "══════════════════════════════════════════"
echo "  Generating Coverage Report"
echo "══════════════════════════════════════════"
echo ""

reportgenerator \
    "-reports:$RESULTS_DIR/**/coverage.cobertura.xml" \
    "-targetdir:$REPORT_DIR" \
    "-reporttypes:Html;Cobertura;TextSummary;MarkdownSummaryGithub" \
    "-assemblyfilters:-DeliverTableTests"

echo ""
echo "══════════════════════════════════════════"
echo "  Coverage Summary"
echo "══════════════════════════════════════════"
echo ""

cat "$REPORT_DIR/Summary.txt"

LINE_COVERAGE=$(grep -oP 'Line coverage: \K[0-9.]+' "$REPORT_DIR/Summary.txt" || echo "0")

BELOW=$(awk "BEGIN {print ($LINE_COVERAGE < $THRESHOLD) ? 1 : 0}")
if [ "$BELOW" -eq 1 ]; then
    echo ""
    echo "FAILED: Line coverage ${LINE_COVERAGE}% is below threshold ${THRESHOLD}%"
    exit 1
fi

echo ""
echo "Reports generated at: reports/coverage/"
