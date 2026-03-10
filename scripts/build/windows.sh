#!/bin/bash
set -e

is_windows() {
    [[ "$OSTYPE" == "msys" ]] || [[ "$OSTYPE" == "cygwin" ]] || [[ -n "$WINDIR" ]]
}

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

if ! is_windows; then
    echo "Skipped: Not running on Windows"
    exit 0
fi

POWERSHELL_BIN=""
if command -v powershell.exe >/dev/null 2>&1; then
    POWERSHELL_BIN="powershell.exe"
elif command -v pwsh >/dev/null 2>&1; then
    POWERSHELL_BIN="pwsh"
elif command -v powershell >/dev/null 2>&1; then
    POWERSHELL_BIN="powershell"
fi

if [ -z "$POWERSHELL_BIN" ]; then
    echo "Error: PowerShell not found"
    echo "Run scripts/build/windows.ps1 manually from Windows PowerShell."
    exit 1
fi

exec "$POWERSHELL_BIN" -ExecutionPolicy Bypass -File "$PROJECT_ROOT/scripts/build/windows.ps1" "$@"
