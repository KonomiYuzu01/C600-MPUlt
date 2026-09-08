"""Wrap a verified portable bundle in an English per-user Windows installer."""
from pathlib import Path
import argparse, hashlib, json, os, subprocess

ROOT = Path(__file__).resolve().parents[1]
VERSION = '0.3'


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--bundle', type=Path, required=True)
    parser.add_argument('--iscc', type=Path, required=True, help='Official Inno Setup command-line compiler')
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    bundle, output = args.bundle.resolve(), args.output.resolve()
    if os.name != 'nt' or not args.iscc.is_file():
        parser.error('Build on Windows with an installed official Inno Setup compiler.')
    if not (bundle/'C600Studio.exe').is_file():
        parser.error('Choose the complete portable application directory.')
    output.mkdir(parents=True, exist_ok=True)
    installer = output/f'C600Studio-{VERSION}-Setup.exe'
    if installer.exists():
        raise RuntimeError('Installer already exists; choose a fresh output directory.')
    verify = output/'installer-input-verification.json'
    check = subprocess.run([str(bundle/'C600Studio.exe'),'--verify-package',str(verify)],
                           creationflags=subprocess.CREATE_NO_WINDOW, timeout=90)
    verification = json.loads(verify.read_text(encoding='utf-8')) if verify.exists() else {}
    if check.returncode or not verification.get('passed') or verification.get('version') != VERSION:
        raise RuntimeError('The input portable package failed verification.')
    command = [str(args.iscc.resolve()),'/Qp','/DBundleDir='+str(bundle),
               '/DInstallerOutput='+str(output), str(ROOT/'packaging/C600Studio.iss')]
    result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                            creationflags=subprocess.CREATE_NO_WINDOW, timeout=300)
    (output/'installer-build.log').write_bytes(result.stdout)
    if result.returncode or not installer.is_file():
        raise RuntimeError('Installer compilation failed; inspect installer-build.log.')
    digest = hashlib.sha256(installer.read_bytes()).hexdigest()
    print(json.dumps({'passed':True,'file':installer.name,'bytes':installer.stat().st_size,'sha256':digest}))


if __name__ == '__main__':
    main()
