"""Owned local engine lifecycle, including Windows virtual-environment launchers.

A spawned executable's PID need not be the PID running server.py. Readiness is
bound to a fresh per-launch secret and an authenticated loopback health response.
Only processes created by this object are stopped. No name-wide process killing.
"""
from __future__ import annotations

import json
import os
from pathlib import Path
import re
import secrets
import signal
import subprocess
import sys
import time
from typing import Callable
from urllib.error import URLError
from urllib.parse import parse_qs, urlparse
from urllib.request import HTTPRedirectHandler, ProxyHandler, Request, build_opener

PROTOCOL = 'C600-engine-v2'
DEFAULT_STARTUP_TIMEOUT = 180.0


def redact(text: str) -> str:
    """Remove API tokens and launch secrets before printing/sharing diagnostics."""
    text = re.sub(r'(?i)(token=)[^\s&\"\']+', r'\1[REDACTED]', text)
    return re.sub(r'''(?ix)(["']?(?:token|launch_id|X-C600-Token)["']?\s*[:=]\s*["']?)[A-Za-z0-9_\-]{20,}''', r'\1[REDACTED]', text)


def tail_log(path: Path | None, limit: int = 12000) -> str:
    if path is None:
        return ''
    try:
        with Path(path).open('rb') as f:
            f.seek(0, 2)
            f.seek(max(0, f.tell() - limit))
            return redact(f.read().decode('utf-8-sig', 'replace'))
    except OSError:
        return ''


class _NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, *args, **kwargs):
        raise URLError('Local engine requests must not redirect')


def validate_launch(info: object, expected_launch_id: str) -> dict:
    if not isinstance(info, dict) or info.get('protocol') != PROTOCOL:
        raise ValueError('Missing or incompatible engine protocol')
    if not secrets.compare_digest(str(info.get('launch_id', '')), expected_launch_id):
        raise ValueError('Launch ID differs from this launch; stale metadata rejected')
    base = info.get('base')
    if not isinstance(base, str):
        raise ValueError('Missing loopback URL')
    u = urlparse(base)
    if (u.scheme != 'http' or u.hostname != '127.0.0.1' or u.username or u.password
            or u.path not in ('', '/') or u.query or u.fragment
            or u.port is None or not 1 <= u.port <= 65535):
        raise ValueError('Engine URL is not a plain numeric loopback address')
    if type(info.get('pid')) is not int or info['pid'] <= 0:
        raise ValueError('Missing positive engine PID')
    token = info.get('token')
    if not isinstance(token, str) or len(token) < 32:
        raise ValueError('Missing engine token')
    full = urlparse(str(info.get('url', '')))
    if (full.scheme != u.scheme or full.netloc != u.netloc or full.path != '/'
            or full.fragment or parse_qs(full.query).get('token') != [token]):
        raise ValueError('Launch URL does not match its authenticated endpoint')
    return info


def local_request(info: dict, path: str, body: dict | None = None, timeout: float = 2.0) -> dict:
    # Validate the destination even on cleanup/error paths. Never send a secret
    # through a system proxy or to a host specified by unvalidated launch.json.
    validate_launch(info, str(info.get('launch_id', '')))
    op = build_opener(ProxyHandler({}), _NoRedirect())
    payload = None if body is None else json.dumps(body).encode('utf-8')
    req = Request(info['base'].rstrip('/') + path, data=payload,
                  headers={'X-C600-Token': info['token'], 'Content-Type': 'application/json'})
    with op.open(req, timeout=max(.05, timeout)) as response:
        raw = response.read(65537)
    if len(raw) > 65536:
        raise ValueError('Oversized engine lifecycle response')
    value = json.loads(raw)
    if not isinstance(value, dict):
        raise ValueError('Engine lifecycle response is not an object')
    return value


def read_launch(path: Path, process: subprocess.Popen, timeout: float = DEFAULT_STARTUP_TIMEOUT,
                *, launch_id: str, log_path: Path | None = None,
                progress: Callable[[str], None] | None = None) -> dict:
    """Wait for this launch, allowing a forwarding process with a different PID."""
    if len(launch_id) < 32:
        raise ValueError('A new per-launch ID is required')
    deadline = time.monotonic() + timeout
    last_reason = 'Launch metadata has not been published'
    last_stage = None
    reported_at = 0.0
    while time.monotonic() < deadline:
        code = process.poll()
        if code is not None:
            raise RuntimeError(f'State engine launcher exited with code {code} before readiness.\n'
                               + tail_log(log_path))
        try:
            if path.stat().st_size > 65536:
                raise ValueError('Oversized launch metadata')
            info = validate_launch(json.loads(path.read_text(encoding='utf-8-sig')), launch_id)
            health = local_request(info, '/api/health', timeout=min(1.0, max(.05, deadline-time.monotonic())))
            if (health.get('protocol') != PROTOCOL or health.get('ready') is not True
                    or not secrets.compare_digest(str(health.get('launch_id', '')), launch_id)
                    or health.get('pid') != info['pid']
                    or health.get('model_id') != info.get('model_id')):
                raise ValueError('Authenticated health reply does not match this launch')
            if process.poll() is not None:
                raise RuntimeError('Engine launcher exited during readiness verification')
            if progress:
                relation = ' (forwarding launcher)' if process.pid != info['pid'] else ''
                progress(f'Engine ready: launcher PID {process.pid}, engine PID {info["pid"]}{relation}.')
            return info
        except (OSError, ValueError) as exc:
            # Includes JSONDecodeError/UnicodeError. Preserve the final cause,
            # rather than turning every problem into an unexplained timeout.
            last_reason = redact(f'{type(exc).__name__}: {exc}')
        try:
            status = json.loads(path.with_suffix('.status.json').read_text(encoding='utf-8-sig'))
            if status.get('launch_id') == launch_id:
                stage = str(status.get('stage', 'unknown'))
                if progress and (stage != last_stage or time.monotonic()-reported_at > 10):
                    progress('State engine: '+stage)
                    last_stage, reported_at = stage, time.monotonic()
        except (OSError, ValueError):
            pass
        time.sleep(min(.1, max(0, deadline-time.monotonic())))
    raise RuntimeError(f'State engine readiness timed out after {timeout:g}s. '
                       f'Last stage: {last_stage or "not reported"}. Last check: {last_reason}\n'
                       + tail_log(log_path))


def python_child_command(script_args: list[str], *, executable: str | None = None,
                         base_executable: str | None = None,
                         windows: bool | None = None) -> tuple[list[str], dict[str, str]]:
    """Use CPython's Windows multiprocessing venv-redirection convention.

    Calling the base interpreter with __PYVENV_LAUNCHER__ preserves the venv
    while avoiding the otherwise intermediate venv python.exe process.
    Readiness remains PID-independent even if a runtime adds another wrapper.
    """
    windows = os.name == 'nt' if windows is None else windows
    executable = executable or sys.executable
    base_executable = base_executable or getattr(sys, '_base_executable', executable)
    env = os.environ.copy()
    env.update(PYTHONUTF8='1', PYTHONIOENCODING='utf-8', PYTHONUNBUFFERED='1')
    program = executable
    if (windows and os.path.normcase(executable) != os.path.normcase(base_executable)
            and Path(base_executable).is_file()):
        program = base_executable
        env['__PYVENV_LAUNCHER__'] = executable
    return [program, '-X', 'utf8', '-u', *map(str, script_args)], env


def stop_owned_process(process: subprocess.Popen, *, info: dict | None = None,
                       grace: float = 12.0, group_owned: bool = False) -> dict:
    """Graceful API/pipe shutdown first; force only this owned process as fallback."""
    result = {'graceful_request': False, 'forced': False, 'exit_code': process.poll()}
    if info is not None:
        try:
            reply = local_request(info, '/api/shutdown', {'launch_id': info['launch_id']}, timeout=2)
            result['graceful_request'] = reply.get('stopping') is True
        except (OSError, ValueError):
            pass
    # EOF also handles a failed handshake, or launcher termination, without
    # trusting a PID from a stale metadata file.
    if process.stdin is not None and not process.stdin.closed:
        try:
            process.stdin.close()
        except OSError:
            pass
    try:
        result['exit_code'] = process.wait(timeout=grace)
        return result
    except subprocess.TimeoutExpired:
        result['forced'] = True
    if os.name == 'nt':
        # /T targets this owned launcher and its descendants, not every Python.
        taskkill = Path(os.getenv('SystemRoot', r'C:\Windows')) / 'System32/taskkill.exe'
        if process.poll() is None:
            try:
                subprocess.run([str(taskkill), '/PID', str(process.pid), '/T', '/F'],
                               stdout=subprocess.PIPE, stderr=subprocess.STDOUT, timeout=10)
            except (OSError, subprocess.SubprocessError):
                process.kill()
    elif group_owned:
        try:
            os.killpg(process.pid, signal.SIGTERM)
        except ProcessLookupError:
            pass
    elif process.poll() is None:
        process.terminate()
    try:
        result['exit_code'] = process.wait(timeout=5)
    except subprocess.TimeoutExpired:
        if os.name != 'nt' and group_owned:
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
        else:
            process.kill()
        result['exit_code'] = process.wait(timeout=10)
    return result


class EngineProcess:
    """An engine's entire owned lifetime, including failed startup."""
    def __init__(self, root: Path, data: Path, launch_file: Path, log_file: Path,
                 timeout: float = DEFAULT_STARTUP_TIMEOUT,
                 progress: Callable[[str], None] | None = None,
                 *, forwarder: Path | None = None, extra_args: list[str] | None = None,
                 engine_command: list[str] | None = None, hidden_console: bool = False):
        self.root, self.data = Path(root).resolve(), Path(data).resolve()
        self.launch_file, self.log_file = Path(launch_file).resolve(), Path(log_file).resolve()
        self.timeout, self.progress = timeout, progress
        self.launch_id = secrets.token_hex(32)
        self.process = None
        self.info = None
        self.log = None
        self.forwarder = forwarder
        self.extra_args = extra_args or []
        # A frozen distribution supplies its bundled console engine executable.
        # Source launches retain the venv-aware Python command above unchanged.
        if engine_command is not None and (not engine_command or
                any(not isinstance(arg, str) or not arg for arg in engine_command)):
            raise ValueError('engine_command must contain nonempty command arguments')
        self.engine_command = list(engine_command) if engine_command is not None else None
        self.hidden_console = bool(hidden_console)
        self.cleanup_result = None
        self.failure = None

    def start(self):
        self.launch_file.parent.mkdir(parents=True, exist_ok=True)
        self.log_file.parent.mkdir(parents=True, exist_ok=True)
        # Launch ID rejects stale files even if deletion/publication is delayed.
        args = ['--no-browser', '--port', '0', '--data', str(self.data),
                '--launch-info', str(self.launch_file), *self.extra_args]
        if self.engine_command is None:
            cmd, env = python_child_command([str(self.root/'server.py'), *args])
        else:
            cmd, env = [*self.engine_command, *args], os.environ.copy()
            # Frozen engines own their interpreter and modules. Inherited Python
            # configuration must not redirect them to an installed interpreter.
            for name in ('__PYVENV_LAUNCHER__', 'PYTHONHOME', 'PYTHONPATH'):
                env.pop(name, None)
            env.update(PYTHONUTF8='1', PYTHONIOENCODING='utf-8', PYTHONUNBUFFERED='1')
        env['C600_LAUNCH_ID'] = self.launch_id
        env['C600_WATCH_PARENT_STDIN'] = '1'
        if self.forwarder is not None:
            outer, _ = python_child_command([str(self.forwarder), *cmd])
            cmd = outer
        self.log = self.log_file.open('wb')
        try:
            self.process = subprocess.Popen(cmd, cwd=self.root, env=env, stdin=subprocess.PIPE,
                    stdout=self.log, stderr=subprocess.STDOUT,
                    start_new_session=os.name != 'nt',
                    creationflags=(subprocess.CREATE_NEW_PROCESS_GROUP |
                        (subprocess.CREATE_NO_WINDOW if self.hidden_console else 0)) if os.name == 'nt' else 0)
            self.info = read_launch(self.launch_file, self.process, timeout=self.timeout,
                                   launch_id=self.launch_id, log_path=self.log_file, progress=self.progress)
            self._write_result('ready')
            return self
        except BaseException as exc:
            self.failure = redact(str(exc))
            self._write_result('failed')
            self.close()
            raise

    def _write_result(self, stage: str):
        # Deliberately excludes tokens, launch IDs and the token-bearing URL.
        payload = dict(stage=stage, python=sys.version, executable=sys.executable,
                       launcher_pid=self.process.pid if self.process else None,
                       engine_pid=self.info['pid'] if self.info else None,
                       pid_match=(self.info['pid']==self.process.pid) if self.info else None,
                       failure=self.failure, cleanup=self.cleanup_result,
                       launch_file=str(self.launch_file), log_file=str(self.log_file))
        try:
            self.launch_file.with_suffix('.lifecycle.json').write_text(json.dumps(payload, indent=2), encoding='utf-8')
        except OSError:
            pass

    def close(self):
        if self.process is not None and self.cleanup_result is None:
            try:
                self.cleanup_result = stop_owned_process(self.process, info=self.info, group_owned=os.name!='nt')
            except Exception as exc:
                self.cleanup_result = {'cleanup_error': redact(str(exc))}
        if self.log is not None and not self.log.closed:
            self.log.close()
        self._write_result('failed' if self.failure else 'stopped')

    def __enter__(self):
        return self.start()

    def __exit__(self, exc_type, exc, tb):
        if exc is not None:
            self.failure = redact(str(exc))
        self.close()
        # Never replace the original readiness/test failure with a cleanup one.
        return False
