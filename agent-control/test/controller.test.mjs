import test from 'node:test';
import assert from 'node:assert/strict';
import { makeClient, requireAgents, createAgent, runSession, continueSession } from '../src/controller.mjs';

function capture() {
  const requests = [];
  const client = makeClient('offline-test-key', {
    fetch: async (url, init) => {
      requests.push({ url: String(url), method: init.method, headers: new Headers(init.headers),
        body: init.body ? JSON.parse(init.body) : null });
      return new Response(JSON.stringify({ id: 'offline-id', status: 'idle', data: [], has_more: false }),
        { status: 200, headers: { 'content-type': 'application/json' } });
    },
  });
  return { client, requests };
}

test('missing credentials and missing SDK API fail closed', () => {
  assert.throws(() => makeClient(''), /OPENAI_API_KEY is missing/);
  assert.throws(() => requireAgents({ beta: {} }), /does not expose/);
});

test('installed SDK serializes Staff agent and beta header without network', async () => {
  const { client, requests } = capture();
  await createAgent(client, 'project-specific instructions', 'gpt-6-astra');
  assert.equal(requests.length, 1);
  const request = requests[0];
  assert.equal(request.url, 'https://api.openai.com/v1/agents');
  assert.equal(request.headers.get('OpenAI-Beta'), 'agents=v1');
  assert.equal(request.body.model, 'gpt-6-astra');
  assert.deepEqual(request.body.multi_agent, { enabled: true, max_concurrent_subagents: 3 });
  assert.deepEqual(request.body.tools, []);
});

test('initial session sends only supplied context/task in none environment', async () => {
  const { client, requests } = capture();
  await runSession(client, 'agent-fixture', 'curated context', 'review native gate');
  assert.equal(requests[0].url, 'https://api.openai.com/v1/agents/sessions');
  assert.deepEqual(requests[0].body.environment, { type: 'none' });
  assert.equal(requests[0].body.agent_id, 'agent-fixture');
  assert.match(requests[0].body.input, /curated context/);
  assert.match(requests[0].body.input, /review native gate/);
  assert.match(requests[0].body.input, /cannot inspect or edit/);
  assert.equal(requests[0].body.stream, false);
  assert.equal(requests[0].body.tools, undefined);
});

test('continuation uses SDK idempotency HEADER and same session', async () => {
  const { client, requests } = capture();
  await continueSession(client, 'sess-fixture', 'review evidence', 'fixture-operation-1');
  const request = requests[0];
  assert.equal(request.url, 'https://api.openai.com/v1/agents/sessions/sess-fixture/events');
  assert.equal(request.headers.get('Idempotency-Key'), 'fixture-operation-1');
  assert.equal(request.body.idempotencyKey, undefined);
  assert.deepEqual(request.body.events, [{ type: 'agent.session.input.message', input: [{ role: 'user', content: [{ type: 'input_text', text: 'review evidence' }] }] }]);
});

test('status, saved items and subagent queries serialize using actual SDK', async () => {
  const { client, requests } = capture();
  const sessions = requireAgents(client).sessions;
  await sessions.retrieve('sess-fixture');
  await sessions.items.list('sess-fixture');
  await sessions.subagents.list('sess-fixture');
  assert.deepEqual(requests.map(r => new URL(r.url).pathname), [
    '/v1/agents/sessions/sess-fixture', '/v1/agents/sessions/sess-fixture/items', '/v1/agents/sessions/sess-fixture/subagents',
  ]);
  assert.ok(requests.every(r => r.method === 'GET'));
});

test('blank task is rejected before any fetch', async () => {
  const { client, requests } = capture();
  await assert.rejects(runSession(client, 'agent-fixture', 'context', ''), /nonempty/);
  await assert.rejects(continueSession(client, 'sess-fixture', '  ', 'key'), /nonempty/);
  assert.equal(requests.length, 0);
});
