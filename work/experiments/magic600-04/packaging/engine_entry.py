"""Frozen interpreter, unchanged source-layout engine and ownership protocol."""
import os
from pathlib import Path
import sys


if __name__ == '__main__':
    if os.environ.get('C600_WATCH_PARENT_STDIN') != '1' or len(os.environ.get('C600_LAUNCH_ID', '')) < 32:
        raise SystemExit('Open Magic600Cell.exe. This private engine is not a second user interface.')
    root = Path(sys._MEIPASS) / 'app'
    experiment = root / 'work/experiments/magic600-04'
    sys.dont_write_bytecode = True
    sys.path[:0] = [str(experiment), str(root), str(root / 'native')]
    import core
    import orbit_invariants
    import engine
    # The reviewed certificate reads original source bytes. Never silently
    # substitute PYZ bytecode modules with different __file__ identities.
    for module, expected in ((core, root / 'core.py'),
                             (orbit_invariants, experiment / 'orbit_invariants.py'),
                             (engine, experiment / 'engine.py')):
        if Path(module.__file__).resolve() != expected.resolve():
            raise RuntimeError('Packaged source-layout binding failed: ' + module.__name__)
    engine.main()
