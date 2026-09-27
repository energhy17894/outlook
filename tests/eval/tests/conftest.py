import sys
from pathlib import Path

# tests/eval is not a package; add it to sys.path so `import normalize`, `import
# verify_quotes`, `import metrics` resolve the same way run_eval.py resolves them.
EVAL_DIR = Path(__file__).resolve().parents[1]
if str(EVAL_DIR) not in sys.path:
    sys.path.insert(0, str(EVAL_DIR))
