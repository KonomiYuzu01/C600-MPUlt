import { mkdirSync, readFileSync, realpathSync, renameSync, unlinkSync, writeFileSync } from 'node:fs';
import { randomUUID } from 'node:crypto';
import { dirname, join, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';
import process from 'node:process';

const invalid = () => ({ value: '', action: 'retry' });

export function validKey(value) {
  return /^sk-[A-Za-z0-9_-]+$/.test(value) && (value.match(/sk-/g) ?? []).length === 1;
}

// Terminal input is never echoed, included in an exception, or sent to a shell.
export function acceptInput(value, chunk) {
  // Console paste shortcuts can arrive as control keys instead of pasted text.
  // Do not read the system clipboard; let the user choose the terminal's Paste action.
  chunk = chunk.replace(/\x1b\[200~/g, '').replace(/\x1b\[201~/g, '');
  if (chunk === '\x16') return { value, action: 'paste-help' };
  chunk = chunk.replace(/^\x16(?=sk-)/, '');
  if (/[\x03\x1b]/.test(chunk)) return { value: '', action: 'cancel' };
  if (chunk.trim().startsWith('sk-') && validKey(chunk.trim())) {
    chunk = chunk.trim() + (/[\r\n]$/.test(chunk) ? '\n' : '');
  }
  const characters = [...chunk.replace(/\r\n/g, '\n')];
  for (let index = 0; index < characters.length; index += 1) {
    const character = characters[index];
    if (character === '\r' || character === '\n') {
      if (index !== characters.length - 1 || !validKey(value)) return invalid();
      return { value, action: 'save' };
    }
    if (character === '\b' || character === '\x7f') value = value.slice(0, -1);
    else if (!/[A-Za-z0-9_-]/.test(character) || value.length >= 1024) return invalid();
    else value += character;
  }
  if ((value.match(/sk-/g) ?? []).length > 1) return invalid();
  return { value, action: 'continue' };
}

export function assertLocalDirectory(directory) {
  if (/(^|[\\/])OneDrive(?:[ -][^\\/]*)?([\\/]|$)/i.test(directory)) {
    throw new Error('Key setup must run from the local source controller outside OneDrive.');
  }
}

export function updatedEnvironment(existing, key) {
  if (!validKey(key)) throw new Error('Invalid key input.');
  const lines = existing.split(/\r?\n/).filter(line => !/^\s*(?:export\s+)?OPENAI_API_KEY\s*=/.test(line));
  while (lines.at(-1) === '') lines.pop();
  return [...lines, `OPENAI_API_KEY=${key}`, ''].join('\n');
}

export function saveKey(directory, key) {
  let temporary;
  try {
    const localDirectory = realpathSync(directory);
    assertLocalDirectory(localDirectory);
    const destination = join(localDirectory, '.env');
    let previous;
    try { previous = readFileSync(destination, 'utf8'); }
    catch (error) {
      if (error.code !== 'ENOENT') throw error;
      previous = readFileSync(join(localDirectory, '.env.example'), 'utf8');
    }
    temporary = join(localDirectory, `.env.key-setup-${randomUUID()}.tmp`);
    writeFileSync(temporary, updatedEnvironment(previous, key), { encoding: 'utf8', mode: 0o600, flag: 'wx' });
    renameSync(temporary, destination);
    temporary = undefined;
  } catch {
    throw new Error('Could not save the key. Check the local controller folder and its write access.');
  } finally {
    if (temporary) { try { unlinkSync(temporary); } catch { /* Never print input or OS errors. */ } }
  }
}

export function writeSetupStatus(directory, status) {
  const stateDirectory = join(directory, '.state');
  mkdirSync(stateDirectory, { recursive: true });
  writeFileSync(join(stateDirectory, 'key-setup.json'), JSON.stringify({ pid: process.pid, status, updated_at: new Date().toISOString() }) + '\n', 'utf8');
}

export function configureKey({ input = process.stdin, output = process.stdout, directory, save = saveKey, status = () => {} }) {
  return new Promise(resolveResult => {
    let value = '';
    let raw = false;
    let finished = false;
    const finish = (code, message) => {
      if (finished) return;
      finished = true;
      value = '';
      input.removeListener('data', onData);
      input.removeListener('error', onError);
      input.removeListener('end', onEnd);
      try { if (raw) input.setRawMode(false); input.pause(); } catch { /* Best effort terminal cleanup. */ }
      try { status(code === 0 ? 'saved' : code === 2 ? 'cancelled' : 'failed'); } catch { /* No secret-bearing errors. */ }
      output.write(`\n${message}\n`);
      resolveResult(code);
    };
    const onError = () => finish(1, 'Key setup failed. No key was printed. Close this window.');
    const onEnd = () => finish(1, 'Input closed. No key was saved.');
    const onData = chunk => {
      const state = acceptInput(value, String(chunk));
      value = state.value;
      if (state.action === 'paste-help') output.write('\nThis console sent Ctrl+V as a shortcut, not text. Use RIGHT-CLICK > Paste or Shift+Insert, then Enter (hidden): ');
      else if (state.action === 'cancel') finish(2, 'Cancelled. No key was saved.');
      else if (state.action === 'retry') output.write('\nInvalid input cleared. Paste ONE new key, then press Enter (hidden): ');
      else if (state.action === 'save') {
        try {
          save(directory, value);
          finish(0, 'SUCCESS: key saved to this local controller .env. The key was not printed.');
        } catch { finish(1, 'Could not save the key. Check the local controller folder and write access. No key was printed.'); }
      }
    };
    try {
      assertLocalDirectory(directory);
      if (!input.isTTY || !output.isTTY || typeof input.setRawMode !== 'function') {
        finish(1, 'A dedicated interactive console is required. Do not paste a key into a shell prompt.');
        return;
      }
      input.setRawMode(true);
      raw = true;
      input.setEncoding('utf8');
      input.on('data', onData);
      input.on('error', onError);
      input.on('end', onEnd);
      input.resume();
      output.write('READY: secure key input is active. Characters will remain invisible.\nUse RIGHT-CLICK Paste or Shift+Insert for ONE new key, then Enter. Ctrl+C or Escape cancels: ');
      status('ready');
    } catch { finish(1, 'Secure input could not start. No key was saved. Close this window; do not paste a key.'); }
  });
}

if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  const directory = dirname(dirname(fileURLToPath(import.meta.url)));
  process.exitCode = await configureKey({ directory, status: state => writeSetupStatus(directory, state) });
}
