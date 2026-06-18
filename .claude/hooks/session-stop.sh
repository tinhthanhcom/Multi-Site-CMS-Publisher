#!/usr/bin/env bash
HISTORY=".context/HISTORY.md"
[ ! -f "$HISTORY" ] && exit 0

TODAY=$(date +%Y-%m-%d)
if grep -q "$TODAY" "$HISTORY" 2>/dev/null; then
  exit 0
fi

echo "Session ended. Please update .context/HISTORY.md before closing."
exit 1
