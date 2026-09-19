"""Isolated route installation into the existing owned state-engine process."""
from pathlib import Path
import ast
import sys

ROOT = Path(__file__).resolve().parents[3]
EXPERIMENT = Path(__file__).resolve().parent
sys.path.insert(0, str(ROOT))
sys.path.insert(0, str(EXPERIMENT))


def main():
    # Insert one explicit experimental extension point without editing server.py.
    # All original startup, worker, lock, Session and lifecycle code is executed.
    source = ROOT / 'server.py'
    tree = ast.parse(source.read_text(encoding='utf-8-sig'), filename=str(source))
    function = next(n for n in tree.body if isinstance(n, ast.FunctionDef) and n.name == 'main')
    body = next(n for n in function.body if isinstance(n, ast.Try)).body
    index = next(i for i, n in enumerate(body) if isinstance(n, ast.ClassDef) and n.name == 'EngineHTTPServer')
    hook = ast.parse('from adapter import install\ninstall(Handler, locals())').body
    body[index:index] = hook
    ast.fix_missing_locations(tree)
    namespace = {'__name__': 'magic600_experiment_engine', '__file__': str(source)}
    exec(compile(tree, str(source), 'exec'), namespace)
    namespace['main']()


if __name__ == '__main__':
    main()
