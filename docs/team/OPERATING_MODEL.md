# Magic 600 Cell engineering team

This workflow applies to this project. Start with AGENTS.md and the current PROJECT_MEMORY.md, then the current architecture and actual source. Use team-project.json for a local source location; copy team-project.example.json when working at the source repository root. Never commit machine-specific paths or credentials.

The user sets product direction and approves native G1 and G2 independently. Ordinary isolated implementation, dependency diagnosis, tests and fixes are authorized within that direction. Escalate unresolved architecture choices with the evidence and the smallest affected decision. Do not pause unrelated authorized work.

## Roles and handoffs

| Role | Responsibility | Required output |
|---|---|---|
| staff-orchestrator | Scope, dependencies, ownership, reconciliation | Plan, decisions, integrated evidence |
| architect | Inspect existing system and define boundaries | Spec with invariants, alternatives and acceptance criteria |
| planner | Break approved scope into bounded work | Tasks, dependencies, file ownership, verification commands |
| implementer | Implement assigned scope | Patch, tests, known limitations |
| spec-reviewer | Independently check requirements | Violations with source evidence or explicit no-findings |
| code-reviewer | Independently check implementation | Reproducible correctness, regression and maintainability findings |
| verifier | Run checks against the final integrated state | Commands, exit codes, revision, dirty state, limitations |

The role skills live in .agents/skills; project agent definitions live in .codex/agents. These are seven responsibilities, not seven permanently running agents. In this environment use at most three children plus the primary. Inherit the user's model and reasoning settings. Batch review after implementation; do not invent extra concurrency or silently downgrade models.

## Execution

1. Read current memory, inspect actual code and dirty changes, state scope and acceptance criteria. Write substantial specifications in docs/specs and plans in docs/plans. Small changes can use the task's written plan.
2. Have a separate reviewer check the spec. Only unresolved product/architecture direction requires a new user decision; routine details do not.
3. Have a separate reviewer check the task plan for missing dependencies and acceptance coverage. Assign independent tasks with explicit file ownership, input revision, non-goals and check commands. Use git worktrees for concurrent edits to tracked source when a committed baseline represents the intended inputs.
4. Combine changes surgically within the authorized experimental or production scope. Combining an isolated G1/G2 sample does not authorize formal production integration. Never stage unrelated work. The implementer does not approve its own patch; a separate reviewer examines the actual integrated diff and source, not just the implementer's summary.
5. Resolve spec and code findings. Run fresh relevant verification after the final change. A stale green report does not apply to a changed tree.
6. Report what is proved and what remains untested. Update shared memory after rereading it. No automatic merge, release or public upload follows from a passing check.

Each delegated task states: objective; authoritative inputs; allowed files; invariant/non-goals; expected artifact; exact verification; stop condition. A reply states: conclusion; inspected revision/files; findings and evidence; remaining uncertainty; next owner. Disagreement is resolved through code, a counterexample or a test, not majority vote or repeated unsupported agreement.

## Worktree and shared-resource rules

Codex subagents share the parent's directory unless explicitly assigned another directory. Spawning does not create a git worktree. scripts/new-team-worktree.ps1 creates a detached worktree only from a clean, explicit Git reference. It refuses dirty source, including untracked files; ignored experimental files are not included in Git snapshots even when Git is clean. Do not use it for the current ignored 0.4 experiment without first preparing an explicitly agreed snapshot.

For ongoing ignored experiments, use bounded file ownership in the existing directory, with the primary integrating serially. Never reset, clean, stash, delete, or overwrite another task's work to make a baseline look clean. Only the primary operates shared native windows, GPU fixtures, ports and personal-session interfaces; tests use isolated data.

## Project acceptance boundaries

Preserve the full model (177120 pieces, 259800 labels, 35 moving orbits, 1200 generators), stable piece/position/sticker mappings, legal finite witnesses, collateral effects and one authoritative Session/journal. Rendering and filtering cannot change mechanical identity. Preserve upstream credits and distribution restrictions.

Orbit First Block Building Solving, keyboard-first operation, orbit-specific keybind groups and the abstract hub guide product decisions. Current, explicitly locked Next, hover and mechanical protection are separate states. Do not introduce automatic setup search, macro-combination solving or bulk solving.

G1 Grip/Twist and G2 abstract hub are separate native Windows/WinForms/MPUlt/DirectX samples. Both user approvals are currently pending. Browser output, source checks, agent approval and benchmarks cannot satisfy native or human acceptance. Formal integration waits for both approvals. Record each user's actual decision and artifact revision; never infer approval from silence.

## Verification and API boundary

Use scripts/verify.ps1 or scripts/verify.sh; see VERIFICATION.md for explicit stages. Infrastructure checks prove team tooling only. Mathematical, lifecycle, native auxiliary UI, native renderer and human acceptance remain distinct evidence.

agent-control is a separately authenticated Agents API controller. Its environment:none session can plan from the curated context and explicit prompt; it cannot inspect this checkout, run local tests, drive DirectX, or inherit Codex desktop login. Desktop agents perform local development. API output is advisory until locally inspected and verified. Hosted/self-hosted execution is a later environment choice, not silently enabled by this deployment.

Official configuration references, checked 2026-09-15: [skills](https://learn.chatgpt.com/docs/customization/overview), [project subagents](https://learn.chatgpt.com/docs/agent-configuration/subagents), [Agents API quickstart](https://developers.openai.com/api/docs/guides/agents-api/quickstart).
