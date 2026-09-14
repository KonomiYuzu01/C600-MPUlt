# Magic 600 Cell Agents API controller

This controller applies the Staff workflow, six specialist roles, full-model and ID constraints, and separate G1/G2 native approval gates to Magic 600 Cell. Its first deployment is for analysis and planning with `environment.type=none`: no local repository, command execution, Windows UI or GPU access. Local Codex performs implementation and testing. API credentials and account access are separate from the Codex login.

## Configure the key

Create one application API key in the project's [OpenAI Platform](https://platform.openai.com/) account with `api.agents.read`, `api.agents.write` and `api.responses.write`, as specified in the [official quickstart](https://developers.openai.com/api/docs/guides/agents-api/quickstart). These are three permissions on one key. If these capabilities are unavailable, ask the project administrator to enable access. Local configuration cannot grant account permissions.

The only secret to supply is the actual issued key, assigned to `OPENAI_API_KEY`. Do not send it in chat. The default model is `gpt-6-astra` with high reasoning effort. Agent and Session IDs are saved automatically after successful creation; do not invent them. The seven roles run in batches with at most three API subagents concurrently. API and local Codex concurrency limits are separate.

From the actual source repository's `agent-control` directory outside OneDrive:

```powershell
node src/configure-key.mjs
```

On Windows, double-click `configure-key.cmd` to launch the same Node program. This entry does not run a PowerShell script or change execution policy. Only paste after the secure program displays its hidden-input prompt. It refuses a non-interactive terminal and OneDrive directory, writes a non-secret readiness status under `.state`, and saves the key to the local Git-ignored `.env` while preserving model settings. It does not verify account access. Git ignore alone does not prevent cloud-folder synchronization.

Never paste a key into a normal shell prompt. If a previous key appeared in a screenshot, command text or chat, revoke it on Platform and configure a new one. For an external environment file, copy `.env.example` to a local location outside cloud synchronization, such as `$env:LOCALAPPDATA\Magic600\agent-control.env`. Replace its empty key locally with the real value:

```dotenv
OPENAI_API_KEY=replace_this_entire_value_with_your_issued_key
OPENAI_MODEL=gpt-6-astra
```

Select that file in the terminal:

```powershell
$env:MAGIC600_AGENT_ENV_FILE = Join-Path $env:LOCALAPPDATA 'Magic600\agent-control.env'
```

An empty or placeholder key cannot authenticate live requests. Prefer the external file or process environment when the controller itself resides in OneDrive.

## Install and inspect

Requires Node.js 22 or later and pnpm. From `agent-control`:

```powershell
pnpm install --frozen-lockfile
node --test
node src/cli.mjs doctor
node src/cli.mjs preview tasks/first-review.md
```

`doctor` is offline: it reports SDK compatibility, key presence and saved IDs without printing the key or verifying account access. `preview` shows the curated context and task that `run` will send. Review `instructions.md` separately: it is sent when creating the Staff agent. The controller sends these explicit documents and the requested task; it does not automatically upload source, full project memory, personal sessions or a directory tree. Refresh the dated context snapshot before using it for later milestones.

## Create and use a durable session

After configuring the key, these commands make live API requests and use the API project's quota:

```powershell
node src/cli.mjs create-agent
node src/cli.mjs run tasks/first-review.md
node src/cli.mjs status
node src/cli.mjs items
node src/cli.mjs subagents
```

`run` submits work and saves the returned session; analysis may still be running. Query `status` and `items` again to inspect the result. Idle alone does not prove task completion or successful verification. `items` and `subagents` read all pages. This controller queries durable records rather than streaming live events.

For follow-up work, create a reviewed Markdown task or evidence file, then use:

```powershell
node src/cli.mjs continue tasks/your-next-task.md
node src/cli.mjs status
node src/cli.mjs items
```

Use `node src/cli.mjs help` for the command list. Configuration, context and `.state/state.json` resolve relative to the controller directory; a relative `MAGIC600_AGENT_ENV_FILE` also resolves there. Task file paths resolve relative to the terminal's current directory. The CLI can be launched through its full path from another directory. Use one process at a time for each controller copy; copies do not automatically share API state.

## Failures and recovery

- Repeating `create-agent` reuses the saved Agent ID. With an existing Session ID, `run` refuses to create another session; use `continue`.
- Before an API mutation, `.state/state.json` records a pending operation. Success saves the returned IDs and clears pending. SDK automatic retries are disabled. An uncertain result blocks further mutations to avoid duplicate resources.
- For HTTP 401, check the actual key and project; for 403, check scopes and API/model access; for 429, check quota and rate limits. A network interruption or 5xx may occur after acceptance, so inspect Platform records before retrying.
- Confirm an operation was not accepted before locally removing only `pending`. If it succeeded remotely, first restore its verified `agent_id` or `session_id`. Keep pending and request operator reconciliation when uncertain; do not delete the entire state file as a workaround.
- `continue` sends the operation's saved `Idempotency-Key` header. There is no automatic recovery or replay command.
- Error reporting shows only controlled validation messages, bounded HTTP status and a validated request ID. Arbitrary SDK/network messages and stacks are withheld to avoid exposing credentials.
- Changing local model settings or instructions does not update an existing remote agent. Review and explicitly update remote configuration separately. No automatic update, deletion, merge or deployment is provided.

## Verification and boundaries

The dependency is locked to `openai@7.15.0`. All **22 offline tests passed**, including 11 secure key-input/status tests and: actual SDK request serialization for agent creation, `none` sessions, continuation headers and read queries; missing-input rejection; real CLI processes covering secret-containing SDK errors, malformed state, pending/reused state, missing keys and relative environment paths. The tests intercept transport and make no network requests.

No live API request has been verified: a real user key is still required to establish account permissions, model access and inference results. API analysis and automated checks cannot approve the project's G1/G2 native deliverables.

Implementation references, checked 2026-09-15: [Agents API quickstart](https://developers.openai.com/api/docs/guides/agents-api/quickstart), [create agent](https://developers.openai.com/api/reference/typescript/resources/beta/subresources/agents/methods/create), and [session input events](https://developers.openai.com/api/reference/typescript/resources/beta/subresources/agents/subresources/sessions/subresources/events/methods/create). The installed SDK uses the literal `'Idempotency-Key'` parameter for the header; the serialization test verifies that behavior.
