import OpenAI from 'openai';

// Only explicitly authored validation messages may be shown to the operator.
export class UserFacingError extends Error {}

export function safeErrorMessage(error) {
  if (error instanceof UserFacingError) return error.message;
  if (Number.isInteger(error?.status) && error.status >= 100 && error.status <= 599) {
    const requestId = /^req_[A-Za-z0-9_-]{1,160}$/.test(error.request_id || '') ? error.request_id : 'unavailable';
    return `OpenAI request failed: HTTP ${error.status}. Check account access/scopes/quota; see README.md. Request ID: ${requestId}`;
  }
  return 'Operation failed. Check local configuration, file access and network connectivity; see README.md. If an API mutation was pending, reconcile its result before retrying. Error details were withheld to protect credentials.';
}

export function makeClient(apiKey, options = {}) {
  if (!apiKey?.trim()) throw new UserFacingError('OPENAI_API_KEY is missing. See agent-control/README.md; never send the key in chat.');
  return new OpenAI({ apiKey, maxRetries: 0, timeout: 60_000, ...options });
}

export function requireAgents(client) {
  const agents = client.beta?.agents;
  if (typeof agents?.create !== 'function' || typeof agents?.sessions?.create !== 'function' ||
      typeof agents?.sessions?.retrieve !== 'function' || typeof agents?.sessions?.events?.create !== 'function') {
    throw new UserFacingError('Installed OpenAI SDK does not expose the required beta.agents API. Do not substitute Responses or a fabricated API; update the SDK and rerun doctor/test.');
  }
  return agents;
}

export function agentDefinition(instructions, model = 'gpt-6-astra') {
  return {
    name: 'Magic 600 Cell Staff Engineer', model, instructions,
    reasoning: { effort: 'high', summary: 'concise' },
    multi_agent: { enabled: true, max_concurrent_subagents: 3 },
    tools: [],
  };
}

export function initialTask(context, task) {
  if (!task?.trim()) throw new UserFacingError('A nonempty task file is required.');
  return `Project context snapshot (reviewed locally; may become stale):\n${context}\n\nUser task:\n${task}\n\nThis API session has environment.type=none. Produce analysis, a bounded plan and review questions only. You cannot inspect or edit the local repository, run native tests, approve G1/G2, merge, or deploy. Identify missing evidence explicitly.`;
}

export async function createAgent(client, instructions, model) {
  return requireAgents(client).create(agentDefinition(instructions, model));
}

export async function runSession(client, agentId, context, task) {
  return requireAgents(client).sessions.create({
    agent_id: agentId, environment: { type: 'none' }, input: initialTask(context, task), stream: false,
  });
}

export async function continueSession(client, sessionId, text, idempotencyKey) {
  if (!text?.trim()) throw new UserFacingError('A nonempty task file is required.');
  return requireAgents(client).sessions.events.create(sessionId, {
    events: [{ type: 'agent.session.input.message', input: [{ role: 'user', content: [{ type: 'input_text', text }] }] }],
    'Idempotency-Key': idempotencyKey,
  });
}
