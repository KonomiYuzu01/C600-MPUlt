> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Independent package checker review — 2026-09-17

Final conclusion: no unresolved findings within this bounded review after correction and independent recheck of the three initial issues and one WAL issue found during recheck. The checker targets the actual frozen executable and authorized experiment routes; no frozen candidate was executed in this review. This is independent code/evidence review, not native rendering, historical migration, or user approval.

## Reviewed identity and actual evidence

- `packaging/check_package.py` initial SHA-256 `46ec7db1eefbff3f8240660771fee0e271fa70167d57185d94c9208dee0c35ee`.
- `packaging/launcher.py` SHA-256 `e69c5185a2829b23411a2a5d8a1f9c7582e0b6dab99f37f6a1eb6a9b78a6df3f`.
- `packaging/test_package_contract.py` initial SHA-256 `37b68f2409c797458c43bd5781e64935218f814b0e5cffc36f2b94edca18c119`.
- Read current `docs/final-package-notes.md`, the above files, adjacent package/engine-entry contract, and actual adapter/server/EngineProcess boundaries required to interpret responses and lifecycle.
- Inspected the author's actual 13-case log and bound receipt at `evidence/package-acceptance-20260917-041146/`. It reports 13 passing source/checker contracts with unchanged inputs. The real Workbench scenario uses a retained native profile; it is not a frozen-server handshake or GUI test.
- Independently executed `python -B -X utf8 evidence/package-review-counterexamples.py` against the initial checker, exit0. Its `package-review-counterexamples.json` records two stubbed checker-boundary counterexamples. These intentionally simulate adverse API/filesystem outcomes and prove missing checker assertions; they do not establish an engine defect or real migration loss. An initial reviewer-harness run failed cleanup due to an unclosed temporary SQLite connection; that reviewer-only error was corrected and its exact owned temp directory removed.

## Findings on the initial checker

1. **P2 — contradictory copied-profile success receipt on original drift** (`check_package.py:246`, `:249`). After successful route/reopen checks, `passed=True` is set from geometry verification; the finally block writes the receipt before `assert_original_unchanged` throws. Trigger: original fixture content changes during the check. Actual counterexample: the checker raises “The original rollback profile changed”, but the saved nested receipt says `passed:true, original_unchanged:false`. The top-level failure is correct; consumers of the linked nested result can incorrectly accept rollback preservation. Small fix: incorporate preservation into the saved result before writing and add a receipt-level adverse test.

2. **P2 — copied legacy checkpoint/key-file preservation is not asserted** (`check_package.py:207`, `:219`, `:230–245`). The fixture creates “Generated legacy unfinished” and `native_keys.json`, but later checks only labels, current head, top-level preferences and new workspace bindings. A copied checkpoint or the copied key file can disappear during opening while all tested fields stay equal. Actual stubbed counterexample: after deleting only the copied key file and returning no old checkpoints, copied-profile check returns `passed:true` with original unchanged. This does not support the note's “journal head/checkpoints and legacy data preserved” claim. Small fix: record pre-existing snapshot rows and key-file bytes after source close, verify their preservation after route exercise/reopen, and permit separately added checkpoints. Do not label this historical-version compatibility.

3. **P2 — output may be created within the candidate package** (`check_package.py:265–269`). A fresh `<bundle>/review-output` passes the only path guard, is created, and receives resources/summary reports. Subsequent package verification/launcher data guards then fail due to those files, leaving the supplied package polluted. The documented contract says verification data stays outside the package. Small fix: reject output equal to or under bundle before `mkdir` or any report write; retain a focused path test. Source-proven path issue; no package was modified by this reviewer.

## Positive checks and limits

- Frozen checks invoke `Magic600Cell.exe` / `Magic600Engine.exe`, remove external Python/compiler PATH and inherited Python module configuration, verify health reports the expected executable, and retain immutable package hashes. `engine_entry.py` explicitly binds imported core/invariant/engine modules to packaged source paths.
- Launcher engine test now performs Goal/Draft/Review/Preview/Commit/Undo through `/api/experiment/command`; preview and undo compare complete label bytes. The adapter does reject the old retained routes, so this route correction is material.
- The command/job envelopes match the real adapter: normal experiment commands return the command result; native session-log commands wrap it in the paired native reply, which `api_command` unwraps. The existing server owns the real native handshake.
- A copied source is generated only through explicit legal API action, closed before preference shaping/copying, and never opened as a session again. Copy is bounded to fresh siblings under the owned output. Original byte hashes are checked in a finally path. This is a generated legacy-shaped fixture, not an old application's migration proof.
- `EngineProcess.start` already closes on startup exception. Successful/failed body runs close the owned process in the checker's finally and require a normal shutdown. The parent-EOF check and runtime cache discovery are separate, with no native-window claim.
- The Geometry witness must match recorded bytes, expected format/count and packaged MPUlt hash, then pass the frozen server's full sticker/generator handshake. No witness leaves final acceptance incomplete. The checksum alone cannot establish capture provenance; the GUI owner's actual capture record remains required. No saved/synthetic profile is substituted by the checker.
- The reviewer did not run PyInstaller, frozen candidate, native UI, all 35 mathematical cases, or historical profiles. Existing independent GUI report remains unchanged.

## Recheck

First revision: checker `6989c2f11165cc5285fddbe4f16939ca80f2a3a255d90fa461bf03171c6735a9`, test file `4b05900d8ff940188f24780840021fa6dac9ced4a03efb4acfe3f4960fe17005`; launcher unchanged. Source inspection shows all three original findings addressed at their actual use sites. Independent execution of `evidence/package-review-preservation-recheck.py` confirms truthful failed receipt and early inside-bundle rejection.

**P1 found during recheck — inspection changes the supposedly untouched WAL original** (`retained_profile_records`, first revision `check_package.py:61`, called after original hashes/copy at `:260`). `sqlite3.connect(...?mode=ro)` is not filesystem-immutable for a WAL-mode database. A real isolated SQLite WAL fixture was closed, hashed, inspected by the actual helper, and hashed again: `session.sqlite3-shm` and `session.sqlite3-wal` were added. The source Session uses WAL. Because the helper now reads the original after `copy_generated_profile` froze its hashes, the final original-preservation check can fail due to the checker's own read. The initial helper unit fixture used SQLite's default journal and did not catch this. Historical evidence: `evidence/package-review-preservation-recheck-6989.json` bound to checker6989; that script invocation exit0 recorded the failed predicate explicitly, not a passing suite. No user's session was involved.

Final corrected checker SHA-256 `1b839fecc7acde68e2f15c7b1d60c00ecc08ce18f6e86a871c806c7600e5dab7`; tests `8018943745163f493b43f8bdaebc2cf09c6d4d275c9534f57d46716298312dcd`; launcher remains unchanged. The actual helper now refuses a nonempty WAL before opening and uses `mode=ro&immutable=1` only for the owned fully closed/checkpointed fixture. It neither deletes sidecars nor checkpoints caller files. The helper is called only after each owned engine's normal close. Source and the focused real-WAL author log were inspected.

Fresh independent command: `python -B -X utf8 evidence/package-review-preservation-recheck.py`, exit0, **5/5 predicates passed**, recorded in `evidence/package-review-preservation-recheck.json` with the final checker hash:

- Closed WAL original retains exactly the same files and bytes, with no added sidecars.
- Changed original publishes a failed nested receipt before raising.
- Changed pre-existing checkpoint is rejected.
- Removed copied legacy key file is rejected.
- Inside-package verification output is rejected before creation.

The three initial findings and the recheck WAL finding are resolved for this scope. The author's broader final receipt and actual log at `evidence/package-acceptance-20260917-042338/` were inspected: **17 source/checker tests passed, 21.085 s unittest, command exit0, bound inputs unchanged**; log hash matches the receipt. These elapsed times are not performance evidence. Historical `041723` is an invalid binding and is not reused.

A real frozen package plus actual native Geometry witness and later native launch are still required; current checks cannot establish those unrun outcomes. Root owns final candidate assembly/native acceptance; author owns checker edits; independent reviewer owns this report and its isolated adverse evidence only. GUI ownership remains with root and was not used during this review.
