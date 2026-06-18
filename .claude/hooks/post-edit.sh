#!/usr/bin/env bash
set -euo pipefail

FILE=$(echo "$1" | python3 -c "import sys,json; d=json.load(sys.stdin); print(d.get('file_path',''))" 2>/dev/null || echo "")
[ -z "$FILE" ] && exit 0
[ ! -f "$FILE" ] && exit 0

EXT="${FILE##*.}"

case "$EXT" in
  cs)
    command -v dotnet >/dev/null 2>&1 && dotnet format --include "$FILE" --verbosity quiet >/dev/null 2>&1 || true
    ;;
  sh)
    command -v shfmt >/dev/null 2>&1 && shfmt -w "$FILE" >/dev/null 2>&1 || true
    ;;
  ps1)
    # No-op by default; keep hook lightweight on Windows repos.
    true
    ;;
  json)
    # Leave JSON untouched unless project later adopts a formatter.
    true
    ;;
esac

exit 0
