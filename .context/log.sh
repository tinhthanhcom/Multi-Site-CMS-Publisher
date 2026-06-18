#!/usr/bin/env sh
set -eu

if [ "$#" -lt 1 ]; then
  echo "Usage: .context/log.sh \"message\""
  exit 1
fi

date_str="$(date +%Y-%m-%d)"
line="[$date_str] $*"
printf '%s\n' "$line" >> .context/HISTORY.md
printf '%s\n' "$line"
