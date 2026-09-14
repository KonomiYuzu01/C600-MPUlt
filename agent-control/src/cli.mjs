import { readFileSync, existsSync, mkdirSync, writeFileSync, renameSync } from 'node:fs';
import { dirname, resolve, join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { loadEnvFile } from 'node:process';
import { randomUUID } from 'node:crypto';
import { makeClient, requireAgents, createAgent, runSession, continueSession, initialTask, UserFacingError, safeErrorMessage } from './controller.mjs';

const home = resolve(dirname(fileURLToPath(import.meta.url)), '..');
// Resolve configuration relative to this program, never the terminal's current directory.
const envFile = resolve(home, process.env.MAGIC600_AGENT_ENV_FILE || '.env');
const stateDir = join(home, '.state');
const stateFile = join(stateDir, 'state.json');
const read = file => readFileSync(file, 'utf8').replace(/^\uFEFF/, '');
const state = {};
const save = () => {
  mkdirSync(stateDir, { recursive: true });
  writeFileSync(`${stateFile}.tmp`, JSON.stringify(state, null, 2) + '\n');
  renameSync(`${stateFile}.tmp`, stateFile);
};
const print = value => console.log(JSON.stringify(value, null, 2));
const [command = 'help', taskFile, extra] = process.argv.slice(2);

async function main() {
  if (existsSync(envFile)) loadEnvFile(envFile);
  if (existsSync(stateFile)) Object.assign(state, JSON.parse(read(stateFile)));
  if (extra) throw new UserFacingError('Unexpected extra argument. Quote paths containing spaces.');
  if (command === 'help') {
    console.log('Commands: doctor | preview <task.md> | create-agent | run <task.md> | status | continue <task.md> | items | subagents\nRun from any directory with node <path>/src/cli.mjs. See README.md before live commands.');
    return;
  }
  const instructions = read(join(home, 'instructions.md'));
  const context = read(join(home, 'project-context.md'));
  if (command === 'preview') {
    if (!taskFile) throw new UserFacingError('Pass the task Markdown file.');
    console.log(initialTask(context, read(resolve(taskFile))));
    return;
  }
  if (command === 'doctor') {
    const client = makeClient('offline-placeholder');
    requireAgents(client);
    print({ sdk: 'compatible', apiKeyPresent: Boolean(process.env.OPENAI_API_KEY?.trim()),
      liveApiVerified: false, environment: 'none', model: process.env.OPENAI_MODEL || 'gpt-6-astra',
      stateFile, agentId: state.agent_id || null, sessionId: state.session_id || null,
      pendingOperation: state.pending || null });
    return;
  }
  if (!['create-agent', 'run', 'continue', 'status', 'items', 'subagents'].includes(command)) throw new UserFacingError('Unknown command. Use help.');
  const client = makeClient(process.env.OPENAI_API_KEY);
  const agents = requireAgents(client);
  if (['status', 'items', 'subagents'].includes(command)) {
    if (!state.session_id) throw new UserFacingError('No saved session. Run a task first.');
    if (command === 'status') print(await agents.sessions.retrieve(state.session_id));
    else {
      const resource = agents.sessions[command];
      if (typeof resource?.list !== 'function') throw new UserFacingError(`Installed SDK does not expose sessions.${command}.list.`);
      const records = [];
      for await (const record of resource.list(state.session_id)) records.push(record);
      print(records);
    }
    return;
  }
  if (state.pending) throw new UserFacingError('A previous API mutation has an uncertain result. Inspect .state/state.json and the Platform before clearing pending; do not blindly create duplicates.');
  if (command === 'create-agent' && state.agent_id) { print({ agent_id: state.agent_id, reused: true }); return; }
  if (command === 'run' && state.session_id) throw new UserFacingError('A durable session already exists. Use continue; preserve its saved state.');
  if (command !== 'create-agent' && !state.agent_id) throw new UserFacingError('Run create-agent first.');
  if (command === 'continue' && !state.session_id) throw new UserFacingError('Run a task first.');
  const task = command === 'create-agent' ? null : taskFile ? read(resolve(taskFile)) : '';
  if (task !== null && !task.trim()) throw new UserFacingError('Pass a nonempty task Markdown file.');
  state.pending = { command, started_at: new Date().toISOString(), idempotency_key: randomUUID() };
  save();
  if (command === 'create-agent') {
    const result = await createAgent(client, instructions, process.env.OPENAI_MODEL || 'gpt-6-astra');
    if (!result.id) throw new UserFacingError('API response lacked an agent id; keep pending state for reconciliation.');
    state.agent_id = result.id;
  } else if (command === 'run') {
    const result = await runSession(client, state.agent_id, context, task);
    if (!result.id) throw new UserFacingError('API response lacked a session id; keep pending state for reconciliation.');
    state.session_id = result.id;
    state.last_status = result.status;
  } else {
    await continueSession(client, state.session_id, task, state.pending.idempotency_key);
  }
  delete state.pending;
  save();
  print(state);
  console.log('Submitted. Use status and items to inspect results. Idle alone is not verified completion.');
}

main().catch(error => {
  // Do not print request headers, credentials, or arbitrary response bodies.
  console.error(safeErrorMessage(error));
  process.exitCode = 1;
});
