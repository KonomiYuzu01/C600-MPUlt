# Build-time recipe only. Explicit allowlisted payload prepared by assemble.py.
import json
import os
from pathlib import Path

here = Path(SPECPATH)
stage = Path(os.environ['MAGIC600_PACKAGE_STAGE'])
config = json.loads((stage / 'freeze.json').read_text(encoding='utf-8'))
common = dict(pathex=config['paths'], hookspath=[], hooksconfig={}, runtime_hooks=[],
    excludes=['pytest', 'matplotlib', 'IPython', 'scipy', 'pandas', 'tkinter'],
    noarchive=False, optimize=0)
launcher = Analysis([str(here / 'launcher.py')],
    datas=[(str(stage / 'app'), 'app'), (str(stage / 'native'), 'native')],
    binaries=[], hiddenimports=['engine_process', 'directx_runtime'], **common)
engine = Analysis([str(here / 'engine_entry.py')], datas=[], binaries=[],
    hiddenimports=config['modules'], **common)
launcher_exe = EXE(PYZ(launcher.pure), launcher.scripts, [], exclude_binaries=True,
    name='Magic600Cell', debug=False, bootloader_ignore_signals=False, strip=False,
    upx=False, console=False, disable_windowed_traceback=False,
    version=str(stage / 'version.txt'), contents_directory='_internal')
engine_exe = EXE(PYZ(engine.pure), engine.scripts, [], exclude_binaries=True,
    name='Magic600Engine', debug=False, bootloader_ignore_signals=False, strip=False,
    upx=False, console=True, version=str(stage / 'version.txt'), contents_directory='_internal')
distribution = COLLECT(launcher_exe, engine_exe, launcher.binaries, launcher.datas,
    engine.binaries, engine.datas, strip=False, upx=False,
    name='Magic600Cell-0.4-Windows-x64')
