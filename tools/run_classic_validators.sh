#!/usr/bin/env bash
# Portable validation harness for My Demon Idol Classic safety net and KI Melee verification.
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
WORKSPACE_ROOT="${WORKSPACE_ROOT:-"$(cd "$SCRIPT_DIR/.." && pwd)"}"
export WORKSPACE_ROOT

echo "=================================================="
echo " Running My Demon Idol Portable Classic Validators"
echo " Workspace: $WORKSPACE_ROOT"
echo "=================================================="

# 1. Run KI Melee verification with dynamic root patch
echo ""
echo "[1/2] Verifying KI Melee Controller and 11 Roster Girls..."
python3 -c "
import sys, importlib.util
from pathlib import Path

root = Path('$WORKSPACE_ROOT')
spec = importlib.util.spec_from_file_location('validate_ki_melee', root / 'tools/validate_ki_melee.py')
mod = importlib.util.module_from_spec(spec)
mod.ROOT = root
spec.loader.exec_module(mod)
mod.ROOT = root
mod.CTRL = root / 'Assets/MDI_Characters/Runtime/MDI_KI_Melee_Fighter.controller'
res = mod.main()
sys.exit(res)
"

# 2. Run BlazBlue Classic Guard check
echo ""
echo "[2/2] Running BlazBlue Classic Safety Guard..."
python3 "$WORKSPACE_ROOT/tools/validate_blazblue.py" --classic-guard

echo ""
echo "=================================================="
echo " ALL CLASSIC VALIDATORS PASSED (Zero Regression)  "
echo "=================================================="
