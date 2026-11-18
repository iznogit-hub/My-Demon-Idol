#!/bin/sh
set -e
python3 /workspace/tools/validate_ki_melee.py
python3 /workspace/tools/validate_akio_movelist.py
python3 /workspace/tools/validate_blazblue.py
export PATH="/tmp/dotnet:$PATH"
if command -v dotnet >/dev/null 2>&1; then
  dotnet run --project /workspace/tools/bb_core_check/BBCoreCheck.csproj -c Release --no-restore 2>/dev/null || dotnet run --project /workspace/tools/bb_core_check/BBCoreCheck.csproj -c Release
fi
