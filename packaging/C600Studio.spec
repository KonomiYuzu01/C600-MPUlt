# Reproducible layout recipe; invoked by build_windows.py with explicit staging.
import os
from pathlib import Path

root = Path(SPECPATH).parent
stage = Path(os.environ['C600_PACKAGING_STAGE'])
common = dict(pathex=[str(root),str(root/'native')], hookspath=[], hooksconfig={}, runtime_hooks=[],
              excludes=['pytest','matplotlib','IPython','scipy','pandas','tkinter'],
              noarchive=False, optimize=0)
launcher = Analysis([str(root/'packaging/launcher.py')],
    datas=[(str(root/'assets'),'assets'), (str(root/'web'),'web'), (str(stage/'native'),'native')],
    binaries=[], hiddenimports=[], **common)
engine = Analysis([str(root/'packaging/engine_entry.py')],
    datas=[], binaries=[], hiddenimports=[], **common)
launcher_exe = EXE(PYZ(launcher.pure), launcher.scripts, [], exclude_binaries=True,
    name='C600Studio', debug=False, bootloader_ignore_signals=False, strip=False,
    upx=False, console=False, disable_windowed_traceback=False,
    version=str(stage/'version.txt'), contents_directory='_internal')
engine_exe = EXE(PYZ(engine.pure), engine.scripts, [], exclude_binaries=True,
    name='C600Engine', debug=False, bootloader_ignore_signals=False, strip=False,
    upx=False, console=True, version=str(stage/'version.txt'), contents_directory='_internal')
distribution = COLLECT(launcher_exe, engine_exe,
    launcher.binaries, launcher.datas, engine.binaries, engine.datas,
    strip=False, upx=False, name='C600Studio-0.3-Windows-x64')
