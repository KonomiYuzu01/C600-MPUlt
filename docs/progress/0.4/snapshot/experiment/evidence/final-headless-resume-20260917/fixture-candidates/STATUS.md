> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Stopped: fixture and adapter candidate handoff

User requested documentation-only closure. No remaining cases were rerun and
no further source/test edits were made for this handoff. Process inspection at
closure found no `run_product_candidate.py` or `run_candidates.py` Python worker.
The completed serial process exited 1; no native GUI was operated by this agent.

## Immutable executed evidence

product-run-20260917-065159/result.json (`product-run-20260917-065159/result.json`; local-only reference):
**42/44 passed, two failures, overall failed/exit 1; all 87 inputs unchanged
during that run.** Historical receipt and logs are retained, not rewritten.

| Group | Result |
|---|---:|
| Selected experiment contracts | 4/5 |
| Native HTTP responses | 8/9 |
| Cycle transport | 5/5 |
| Missing-frame dependency | 1/1 |
| Durable log reply boundary | 4/4 |
| Operation lifecycle | 20/20 |

The two exact failing cases are:

1. `WorkbenchContracts.test_explicit_work_orbit_restores_context_without_switching_keys`:
   `contracts.log`, line 608 of the candidate test raises `KeyError: selected_macro`.
   The fixture indexes an optional fresh-workspace field; orbit-context normalization
   uses the explicit None default. Proposed candidate correction remains **unmade
   and unrun**. This failure does not demonstrate a lost working-orbit context.
2. `NativeResponses.test_recursive_native_input_inverse_and_reference_returns`:
   `native_responses.log`, line 277 requires the entire workspace unchanged after
   saving a new macro. `personal_macros` must change for that Save. Exact recipe,
   source metadata and comparison assertions passed before this overbroad check.
   The subsequent candidate edit checks unchanged non-store workspace and the
   exact prior store plus created record. That edit is **untested**.

Both remaining single-case runs (`contract_remaining`, `native_remaining`) are
**pending and unrun**. No aggregate 44/44 or current-candidate green claim is valid.

## Real product fixes versus obsolete fixtures

Earlier original-product runs reproduced cancellation escaping after a successful
primary action and the missing-frame invariant constructor error. The tested
adapter candidate fixes these narrowly: authoritative reply retry only, no command
replay; truthful post-primary cancellation warnings; declared missing-frame
unavailability. Cycle transport verifies one commit, exact full labels/head and
retained Stop ownership. Log tests preserve durable imports across display faults.
These are tested **candidate** corrections, not a claim of product promotion.

Fixture updates use real commit/generation receipts, shared Workflow, template
confirmation, locked-Next activation and current transform return values. Setup
cleanup closes Session before temporary-directory removal. Lifecycle checks retain
both pre-write refusal and exact durable-generation post-write reconciliation.

## Tested versus current SHA256

Paths below are relative to `final-headless-resume-20260917/`.

| File | Tested SHA256 | Current status |
|---|---|---|
| `product-candidate/adapter.py` | `8c545199737361e080997cd32f9b6b1b4ca614ce7c6cfa24b3f18648d016e748` | Unchanged |
| `contract-candidate/test_experiment_contracts.py` | `efe429115bdfa2259e33d233dd523cd5592e059268eb02084a31ff1dcdd820b5` | Unchanged; optional-field correction pending |
| `fixture-candidates/test_native_responses.py` | `618cda42ff39490b33e1ea141f169cc05a165257b10c24e2b9625e83378c9919` | Untested edit: `6dccdd55918789e649b8f2a14fa7489ccbafc204c6f3ff48addf9fc95965b7d1` |
| `fixture-candidates/test_cycle_transport.py` | `47ac45a982fba60b7bd4f7206aac1c1670a96d8daf5f2b063042ba5914dd0c1a` | Unchanged |
| `fixture-candidates/test_operation_lifecycle.py` | `9740a83066530337068db7fb8b5f6f79c87a0aa47cde05596e3ed862111b03e5` | Unchanged |
| `fixture-candidates/run_product_candidate.py` | `e9e0016c1b5e438eee2d349a9b599fcd94f01580ab507e9e779d47dba7ec1380` | Two unrun single-case groups added: `fae651de9bc72811ecc8d09cd249121eff379b84fbf7a247fbd34c8c53fce7fd` |

The receipt contains full remaining dependency/model/profile bindings. These
later edits do not invalidate the historical run's unchanged-input statement;
they prevent treating that run as a pass of the current edited files.

## Promotion state

No original product Python or original tests were edited/promoted by this agent.
These preserved proposals exclude copy-only import relocation:

- `fixture-corrections.patch`: three files, SHA256 `1917579c78c468816e71d99cef52cf554c74ac4bf2e1be37205e3c5cda7dfbbe`.
- `fixture-corrections-five.patch`: five files, SHA256 `9e49bc5cb400bcd643cd7ecfa6e04fbeba04e708fa2b363d3a7a732a76d8195c`.
- `fixture-corrections-seven-v2.patch`: seven files, SHA256 `417940f4559887205d3773d75adfc623705427f61feebe735e9a58e2c47a7731`.

The seven-file patch passed read-only `git apply --check --ignore-space-change`
before the run. It still contains the now-known overbroad native workspace
assertion and does **not** include its later untested correction. It also excludes
root's adapter and contract candidate changes. None is a final verified promotion
patch. Stop here until the user resumes implementation/testing.
