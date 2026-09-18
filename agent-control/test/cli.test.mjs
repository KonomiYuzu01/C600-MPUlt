import test from 'node:test';
import assert from 'node:assert/strict';
import { mkdtempSync, mkdirSync, cpSync, writeFileSync, readFileSync, rmSync } from 'node:fs';
import { fileURLToPath, pathToFileURL } from 'node:url';
import { join, resolve, relative } from 'node:path';
import { spawnSync } from 'node:child_process';
import { safeErrorMessage, UserFacingError } from '../src/controller.mjs';

const home = fileURLToPath(new URL('..', import.meta.url));
const fixtureRoot = join(home, '.state');

function fixture(t, initialState) {
  mkdirSync(fixtureRoot, { recursive: true });
  const dir = mkdtempSync(join(fixtureRoot, 'cli-test-'));
  cpSync(join(home, 'src'), join(dir, 'src'), { recursive: true });
  for (const name of ['instructions.md', 'project-context.md']) cpSync(join(home, name), join(dir, name));
  mkdirSync(join(dir, '.state'));
  if (initialState !== undefined) writeFileSync(join(dir, '.state/state.json'), JSON.stringify(initialState));
  // The real SDK runs, but its transport entry point is replaced before CLI import.
  writeFileSync(join(dir, 'offline.mjs'), `import OpenAI from 'openai';
    OpenAI.prototype.post = () => Promise.reject(new TypeError('header contains fake-secret'));
    globalThis.fetch = () => { throw new Error('Offline fixture forbids network'); };
  `);
  t.after(() => {
    const child = relative(fixtureRoot, resolve(dir));
    assert.ok(child && !child.startsWith('..') && !child.includes('..'));
    rmSync(dir, { recursive: true, force: true });
  });
  return {
    dir,
    run(args, env = {}) {
      return spawnSync(process.execPath, ['--import', pathToFileURL(join(dir, 'offline.mjs')).href, join(dir, 'src/cli.mjs'), ...args], {
        cwd: home, encoding: 'utf8', timeout: 20_000,
        env: { ...process.env, OPENAI_API_KEY: '', MAGIC600_AGENT_ENV_FILE: '', OPENAI_MODEL: 'gpt-6-astra', ...env },
      });
    },
  };
}

test('error formatter exposes only authored messages and bounded HTTP identity', () => {
  assert.equal(safeErrorMessage(new UserFacingError('Safe action')), 'Safe action');
  assert.doesNotMatch(safeErrorMessage(new TypeError('fake-secret')), /fake-secret/);
  assert.doesNotMatch(safeErrorMessage({ status: 401, message: 'fake-secret', request_id: 'fake-secret' }), /fake-secret/);
  assert.match(safeErrorMessage({ status: 403, request_id: 'req_fixture123' }), /403.*req_fixture123/);
});

test('CLI raw SDK error exits nonzero without leaking secret and preserves pending', t => {
  const f = fixture(t);
  const result = f.run(['create-agent'], { OPENAI_API_KEY: 'fake-secret' });
  assert.equal(result.status, 1, result.stderr);
  assert.match(result.stderr, /Error details were withheld/);
  assert.doesNotMatch(result.stdout + result.stderr, /fake-secret|TypeError|controller\.mjs:\d/);
  const saved = JSON.parse(readFileSync(join(f.dir, '.state/state.json'), 'utf8'));
  assert.equal(saved.pending.command, 'create-agent');
  assert.ok(saved.pending.idempotency_key);
  assert.equal(saved.agent_id, undefined);
});

test('CLI malformed state is caught without leaking JSON contents', t => {
  const f = fixture(t);
  writeFileSync(join(f.dir, '.state/state.json'), '{fake-secret');
  const result = f.run(['doctor']);
  assert.equal(result.status, 1);
  assert.doesNotMatch(result.stdout + result.stderr, /fake-secret|SyntaxError/);
  assert.match(result.stderr, /Error details were withheld/);
});

test('CLI pending mutation blocks duplicate and saved agent is reused', t => {
  const pending = fixture(t, { pending: { command: 'create-agent' } });
  const blocked = pending.run(['create-agent'], { OPENAI_API_KEY: 'offline-key' });
  assert.equal(blocked.status, 1);
  assert.match(blocked.stderr, /uncertain result/);
  const existing = fixture(t, { agent_id: 'agent_saved' });
  const reused = existing.run(['create-agent'], { OPENAI_API_KEY: 'offline-key' });
  assert.equal(reused.status, 0, reused.stderr);
  assert.deepEqual(JSON.parse(reused.stdout), { agent_id: 'agent_saved', reused: true });
});

test('CLI missing key refuses API and relative env path resolves from controller', t => {
  const f = fixture(t);
  const missing = f.run(['create-agent']);
  assert.equal(missing.status, 1);
  assert.match(missing.stderr, /OPENAI_API_KEY is missing/);
  writeFileSync(join(f.dir, 'local.env'), 'OPENAI_MODEL=local-fixture-model\n');
  const doctor = f.run(['doctor'], { MAGIC600_AGENT_ENV_FILE: 'local.env', OPENAI_MODEL: undefined });
  assert.equal(doctor.status, 0, doctor.stderr);
  assert.equal(JSON.parse(doctor.stdout).model, 'local-fixture-model');
});
