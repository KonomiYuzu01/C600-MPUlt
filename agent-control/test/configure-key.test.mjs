import test from 'node:test';
import assert from 'node:assert/strict';
import { EventEmitter } from 'node:events';
import { mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { tmpdir } from 'node:os';
import { join } from 'node:path';
import { acceptInput, assertLocalDirectory, configureKey, saveKey, updatedEnvironment, validKey, writeSetupStatus } from '../src/configure-key.mjs';

const fakeKey = 'sk-proj-FAKE_TEST_ONLY_123';

test('terminal paste shortcuts and wrappers do not reject a single key', () => {
  assert.deepEqual(acceptInput('', '\x16'), { value: '', action: 'paste-help' });
  assert.equal(acceptInput('', '\x16' + fakeKey).value, fakeKey);
  assert.equal(acceptInput('', '\x1b[200~' + fakeKey + '\x1b[201~').value, fakeKey);
  assert.equal(acceptInput('', ' ' + fakeKey + ' ').value, fakeKey);
  assert.equal(acceptInput('', fakeKey + '\r\n').action, 'save');
  assert.equal(acceptInput('', fakeKey + '\n' + fakeKey).action, 'retry');
});

test('accepts a single key only after Enter and handles CRLF', () => {
  assert.deepEqual(acceptInput('', fakeKey), { value: fakeKey, action: 'continue' });
  assert.deepEqual(acceptInput(fakeKey, '\r'), { value: fakeKey, action: 'save' });
  assert.deepEqual(acceptInput('', `${fakeKey}\r\n`), { value: fakeKey, action: 'save' });
});

test('rejects blanks, whitespace, control characters and multiple pasted keys', () => {
  for (const text of ['', 'bad', `${fakeKey} ${fakeKey}`, `${fakeKey}${fakeKey}`, `${fakeKey}\n${fakeKey}`, '\t', '\x00']) {
    assert.equal(acceptInput('', `${text}\r`).action, 'retry');
  }
  assert.equal(validKey(`${fakeKey}${fakeKey}`), false);
  assert.equal(acceptInput('', 'a'.repeat(1025)).action, 'retry');
});

test('backspace edits hidden buffer; Ctrl+C and Escape discard it', () => {
  assert.equal(acceptInput(`${fakeKey}x`, '\x7f\r').value, fakeKey);
  assert.equal(acceptInput(`${fakeKey}x`, '\b\r').value, fakeKey);
  for (const control of ['\x03', '\x1b']) assert.deepEqual(acceptInput(fakeKey, control), { value: '', action: 'cancel' });
});

test('preserves model and other env entries while replacing old key', () => {
  const previous = '# local\r\nOPENAI_MODEL=gpt-6-astra\r\nOPENAI_API_KEY=old\r\nOTHER=value\r\n';
  const next = updatedEnvironment(previous, fakeKey);
  assert.equal(next, `# local\nOPENAI_MODEL=gpt-6-astra\nOTHER=value\nOPENAI_API_KEY=${fakeKey}\n`);
  assert.throws(() => updatedEnvironment(previous, 'invalid key'), /Invalid key input/);
});

test('refuses OneDrive paths', () => {
  for (const directory of ['C:\\Users\\User\\OneDrive\\agent-control', 'C:/Users/User/OneDrive - Company/controller']) {
    assert.throws(() => assertLocalDirectory(directory), /outside OneDrive/);
  }
  assert.doesNotThrow(() => assertLocalDirectory('C:/Users/User/Documents/controller'));
});

test('saves only a local .env and preserves existing config', () => {
  const directory = mkdtempSync(join(tmpdir(), 'magic600-key-test-'));
  try {
    writeFileSync(join(directory, '.env.example'), 'OPENAI_MODEL=gpt-6-astra\nOPENAI_API_KEY=\n');
    saveKey(directory, fakeKey);
    assert.match(readFileSync(join(directory, '.env'), 'utf8'), /OPENAI_MODEL=gpt-6-astra/);
    writeFileSync(join(directory, '.env'), 'OPENAI_MODEL=retained-model\nOPENAI_API_KEY=old\n');
    saveKey(directory, fakeKey);
    assert.equal(readFileSync(join(directory, '.env'), 'utf8'), `OPENAI_MODEL=retained-model\nOPENAI_API_KEY=${fakeKey}\n`);
  } finally { rmSync(directory, { recursive: true, force: true }); }
});

function terminal(options = {}) {
  const input = new EventEmitter();
  input.isTTY = true;
  input.rawModes = [];
  input.setRawMode = mode => input.rawModes.push(mode);
  input.setEncoding = () => {};
  input.resume = () => {};
  input.pause = () => {};
  Object.assign(input, options);
  const output = { isTTY: true, text: '', write(text) { this.text += text; } };
  return { input, output, directory: 'C:/local/agent-control' };
}

test('Ctrl+V control key gives paste guidance then accepts hidden terminal paste', async () => {
  const io = terminal();
  let saved;
  const result = configureKey({ ...io, save: (directory, key) => { saved = key; } });
  io.input.emit('data', '\x16');
  assert.match(io.output.text, /RIGHT-CLICK/);
  assert.doesNotMatch(io.output.text, /Invalid input/);
  io.input.emit('data', fakeKey + '\r');
  assert.equal(await result, 0);
  assert.equal(saved, fakeKey);
  assert.equal(io.output.text.includes(fakeKey), false);
});

test('interactive save hides input and cleans up raw mode', async () => {
  const io = terminal();
  let saved;
  const statuses = [];
  const result = configureKey({ ...io, save: (directory, key) => { saved = key; }, status: state => statuses.push(state) });
  assert.match(io.output.text, /READY/);
  io.input.emit('data', `${fakeKey}\r`);
  assert.equal(await result, 0);
  assert.equal(saved, fakeKey);
  assert.equal(io.output.text.includes(fakeKey), false);
  assert.match(io.output.text, /SUCCESS/);
  assert.deepEqual(io.input.rawModes, [true, false]);
  assert.deepEqual(statuses, ['ready', 'saved']);
});

test('nonTTY and raw-mode failures fail closed before READY', async () => {
  for (const options of [{ isTTY: false }, { setRawMode() { throw new Error(fakeKey); } }]) {
    const io = terminal(options);
    assert.equal(await configureKey({ ...io, save() { assert.fail('must not save'); } }), 1);
    assert.doesNotMatch(io.output.text, /READY/);
    assert.equal(io.output.text.includes(fakeKey), false);
  }
});

test('save errors are redacted and cancellation never saves', async () => {
  const failed = terminal();
  const failure = configureKey({ ...failed, save() { throw new Error(fakeKey); } });
  failed.input.emit('data', `${fakeKey}\r`);
  assert.equal(await failure, 1);
  assert.equal(failed.output.text.includes(fakeKey), false);
  for (const control of ['\x03', '\x1b']) {
    const io = terminal();
    const result = configureKey({ ...io, save() { assert.fail('must not save'); } });
    io.input.emit('data', fakeKey);
    io.input.emit('data', control);
    assert.equal(await result, 2);
    assert.doesNotMatch(io.output.text, /SUCCESS/);
  }
});

test('invalid paste clears buffer and permits a clean retry', async () => {
  const io = terminal();
  let saved;
  const result = configureKey({ ...io, save: (directory, key) => { saved = key; } });
  io.input.emit('data', `${fakeKey}\n${fakeKey}\n`);
  assert.equal(saved, undefined);
  assert.match(io.output.text, /Invalid input cleared/);
  io.input.emit('data', `${fakeKey}\r`);
  assert.equal(await result, 0);
  assert.equal(saved, fakeKey);
});

test('setup status contains only process ID, status and time', () => {
  const directory = mkdtempSync(join(tmpdir(), 'magic600-key-status-'));
  try {
    writeSetupStatus(directory, 'ready');
    const status = JSON.parse(readFileSync(join(directory, '.state', 'key-setup.json'), 'utf8'));
    assert.deepEqual(Object.keys(status).sort(), ['pid', 'status', 'updated_at']);
    assert.equal(status.pid, process.pid);
    assert.equal(status.status, 'ready');
    writeSetupStatus(directory, 'saved');
    assert.equal(JSON.parse(readFileSync(join(directory, '.state', 'key-setup.json'), 'utf8')).status, 'saved');
  } finally { rmSync(directory, { recursive: true, force: true }); }
});
