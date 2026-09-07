"""Locate installed Managed DirectX without redistributing Microsoft DLLs."""
from __future__ import annotations
import hashlib
import os
from pathlib import Path

DOWNLOAD_URL = 'https://www.microsoft.com/en-us/download/details.aspx?id=8109'
REQUIRED = {
    'Microsoft.DirectX.dll': ('942e98f142373547493f13b14e1603b2420851aff013d3085bada7b6b2214d9c',),
    'Microsoft.DirectX.Direct3D.dll': ('f3359d5e41b1d4fec7230579a593e40fe44f6afdfacd1e2bbe52ee06d84686fb',
                                    '4fb206fa4cdcd0e93ddf2f926c0a8a325e5e50dd2d3353066c6885019499b173'),
    'Microsoft.DirectX.Direct3DX.dll': ('7986e3fbe05418fe5d8425f2f1b76b7a7b09952f3ec560b286dd744bf7178059',
                                     'a33bf14230389abb0d44f8eeb88272566def3599b0c5b2f4559f3c4e29ff97a4'),
}


def verified(path, expected):
    try:
        with path.open('rb') as stream:
            return hashlib.file_digest(stream, 'sha256').hexdigest() in expected
    except OSError:
        return False


def find_directx(preferred=(), *, windows=None, appdata=None):
    """Find only the exact tested MDX assemblies in bounded known locations.

    The package contains no Microsoft DLL payload. Files copied to its local
    runtime cache already belong to this computer's installation/application.
    """
    windows = Path(windows or os.environ.get('WINDIR', r'C:\Windows'))
    appdata = Path(appdata or os.environ.get('LOCALAPPDATA', str(Path.home())))
    result = {}
    for name, expected in REQUIRED.items():
        assembly = name[:-4]
        candidates = [Path(directory)/name for directory in preferred]
        for gac in (windows/'assembly/GAC', windows/'assembly/GAC_32', windows/'assembly/GAC_MSIL',
                    windows/'Microsoft.NET/assembly/GAC_32', windows/'Microsoft.NET/assembly/GAC_MSIL'):
            candidates.extend(sorted((gac/assembly).glob('*/'+name)))
        for folder in ('DirectX for Managed Code', 'DirectX for ManagedCode'):
            candidates.extend(sorted((windows/'Microsoft.NET'/folder).glob('*/'+name)))
        candidates.extend(sorted((appdata/'C600Studio').glob('native-host-*/runtime/*/'+name)))
        result[name] = next((path for path in candidates if verified(path, expected)), None)
    missing = [name for name, path in result.items() if path is None]
    if missing:
        raise RuntimeError('The required Managed DirectX 1.1 runtime was not found.\n\n'
            'Install the official Microsoft DirectX End-User Runtimes (June 2010):\n'+DOWNLOAD_URL+
            '\n\nExtract the Microsoft package, run DXSETUP.exe, then start C600Studio.exe again. '
            'The installer adds legacy optional components; it does not replace Windows DirectX.\n\n'
            'Missing tested assemblies: '+', '.join(missing))
    return result
