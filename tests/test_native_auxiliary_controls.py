"""Run the bounded structure and auxiliary WinForms fixtures in one fresh directory.

This explicitly requested developer test opens isolated test windows. It does
not load MPUlt, start an HTTP engine, or read a personal session.
"""
from pathlib import Path
import argparse
import hashlib
import json
import os
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
sys.path.insert(0, str(ROOT))


def write_json(path, value):
    path.write_text(json.dumps(value, indent=2, allow_nan=False), encoding='utf-8')


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--output', type=Path, required=True, help='Fresh private test directory; existing directories are never reused.')
    args = parser.parse_args()
    if os.name != 'nt':
        parser.error('This fixture requires Windows and the .NET Framework compiler.')
    compiler = Path(os.environ['WINDIR']) / 'Microsoft.NET/Framework/v4.0.30319/csc.exe'
    if not compiler.is_file():
        parser.error('The .NET Framework csc.exe compiler is unavailable.')
    output = args.output.resolve()
    output.mkdir(parents=True, exist_ok=False)
    from core import Model
    from session import Session

    sources = [ROOT / name for name in (
        'native/NativeCellView.cs', 'native/NativeAuxiliaryViews.cs',
        'native/NativeColorGraph.cs', 'native/NativeStructureExplorer.cs',
        'tests/native/NativeCellViewRegression.cs', 'tests/native/NativeAuxiliaryViewsRegression.cs',
        'tests/native/NativeStructureExplorerRegression.cs',
        'tests/native/NativeAuxiliaryRegressionRunner.cs',
    )]
    hashed = sources + [ROOT / 'core.py', ROOT / 'session.py', ROOT / 'assets/manifest.json', Path(__file__)]
    source_hashes = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in hashed}
    model = Model()
    write_json(output / 'structure.json', model.structure())
    session = Session(model, output / 'isolated-session')
    try:
        interactive = session.interactive_styles() != 0
        revision = hashlib.sha256(interactive.tobytes()).hexdigest()
        write_json(output / 'cell-status.json', session.cell_status(interactive, revision))
    finally:
        session.close()

    executable = output / 'NativeAuxiliaryRegressionRunner.exe'
    command = [str(compiler), '/nologo', '/target:exe', '/platform:x86', '/debug-', '/optimize+', '/utf8output', '/codepage:65001',
               '/main:NativeAuxiliaryRegressionRunner', '/out:' + str(executable)]
    command += ['/reference:' + name for name in ('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll', 'System.Web.Extensions.dll')]
    command += [str(path) for path in sources]
    build = subprocess.run(command, capture_output=True, timeout=60, creationflags=subprocess.CREATE_NO_WINDOW)
    (output / 'build.log').write_bytes(build.stdout + build.stderr)
    if build.returncode:
        raise RuntimeError('Auxiliary fixture compilation failed; inspect build.log.')
    write_json(output / 'source-hashes.json', source_hashes)
    run = subprocess.run([str(executable), str(output / 'structure.json'), str(output / 'cell-status.json'), str(output / 'report.json')],
                         capture_output=True, timeout=90, creationflags=subprocess.CREATE_NO_WINDOW)
    (output / 'stdout.log').write_bytes(run.stdout + run.stderr)
    current = {p.relative_to(ROOT).as_posix(): hashlib.sha256(p.read_bytes()).hexdigest() for p in hashed}
    if current != source_hashes:
        report = json.loads((output / 'report.json').read_text(encoding='utf-8'))
        report.update(passed=False, error='Fixture sources changed during execution; this result does not certify the final source set.')
        write_json(output / 'report.json', report)
        raise RuntimeError('Fixture sources changed during execution; rerun against a frozen source set.')
    print(run.stdout.decode('utf-8', errors='replace').strip())
    if run.returncode or not json.loads((output / 'report.json').read_text(encoding='utf-8')).get('passed'):
        raise RuntimeError('Auxiliary fixture failed; inspect report.json and stdout.log.')


if __name__ == '__main__':
    main()
