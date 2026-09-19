"""Small integrity contract shared by this isolated 0.4 package and its checks."""
from pathlib import Path, PurePosixPath
import hashlib
import json
import struct

VERSION = '0.4'
BUNDLE_NAME = 'Magic600Cell-0.4-Windows-x64'
EXPERIMENT = Path('work/experiments/magic600-04')
FORBIDDEN_DLLS = {'microsoft.directx.dll', 'microsoft.directx.direct3d.dll',
                  'microsoft.directx.direct3dx.dll'}


def sha(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def write_json(path, value):
    path = Path(path)
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, indent=2), encoding='utf-8')


def safe_target(root, name):
    relative = PurePosixPath(name)
    if (not isinstance(name, str) or not name or relative.is_absolute() or
            '..' in relative.parts or ':' in name or '\\' in name):
        raise ValueError('Invalid package path')
    target = (Path(root) / relative).resolve()
    if not target.is_relative_to(Path(root).resolve()):
        raise ValueError('Package path escapes its directory')
    return target


def verify_package(bundle):
    bundle = Path(bundle).resolve()
    manifest = json.loads((bundle / '_internal/package-manifest.json').read_text(encoding='utf-8'))
    if manifest.get('version') != VERSION or manifest.get('layout_version') != 1:
        raise ValueError('Inconsistent package version; extract the complete package again')
    rows = manifest.get('files')
    if not isinstance(rows, list) or not rows:
        raise ValueError('Empty package manifest')
    names = set()
    for row in rows:
        name = row['path']
        if name in names:
            raise ValueError('Duplicate package path')
        names.add(name)
        path = safe_target(bundle, name)
        if not path.is_file() or path.stat().st_size != row['bytes'] or sha(path) != row['sha256']:
            raise ValueError('A packaged file is missing or damaged: ' + name)
    actual = {p.relative_to(bundle).as_posix() for p in bundle.rglob('*') if p.is_file()}
    if actual != names | {'_internal/package-manifest.json'}:
        raise ValueError('Unexpected files in the application folder; extract into a fresh folder')
    return manifest


def inspect_payload(bundle):
    """No personal/runtime files may enter the clean assembled distribution."""
    bundle = Path(bundle)
    files = [p for p in bundle.rglob('*') if p.is_file()]
    for path in files:
        name = path.name.lower()
        if (name in FORBIDDEN_DLLS or name in {'csc.exe', 'launch.json', 'native_keys.json', '.env'}
                or name.endswith('regression.exe')
                or path.suffix.lower() in {'.sqlite3', '.db', '.log', '.pdb', '.pyc'}
                or any(part.lower() in {'sessions', 'diagnostics', '__pycache__'} for part in path.relative_to(bundle).parts)):
            raise ValueError('Private, developer or prohibited runtime file in payload: ' + str(path.relative_to(bundle)))
    for name, machine in (('Magic600Cell.exe', 0x8664), ('Magic600Engine.exe', 0x8664),
                          ('_internal/native/Magic600Native.exe', 0x14c)):
        raw = (bundle / name).read_bytes()
        offset = struct.unpack_from('<I', raw, 0x3c)[0]
        if raw[offset:offset+4] != b'PE\0\0' or struct.unpack_from('<H', raw, offset + 4)[0] != machine:
            raise ValueError('Wrong executable architecture: ' + name)
    return files
