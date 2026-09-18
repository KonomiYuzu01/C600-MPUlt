> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Isolated 0.4 package assembly

Ownership: `packaging/` and this note only. Existing `packaging/build_windows.py`,
the 0.3 launcher, product modules, Session and native sources remain unchanged.

The new recipe consumes a current **product** `Magic600Experiment.exe` and
`build.json` from `native_launch.build`. It refuses regression executables,
outdated source hashes and inconsistent CLR configuration. Root owns that build
and all native GUI scheduling. Assembly itself invokes neither C# nor a fixture.

The package retains `app/work/experiments/magic600-04` relative to `app/core.py`.
This is required by the existing engine's AST extension and the exact source
hashes in `OrbitInvariants._binding`. Bytecode-only packaging is not equivalent.
The fixed invariant artifact is copied byte-for-byte and checked against the
application's existing `AUDIT_SHA256`; its proof scope is unchanged. Endgame
runtime data is the immutable atlas/trees/model plus this certificate. The
large development coverage/log directories are not runtime inputs and are not
shipped.

Assembly follows explicit `native_launch` backend/native lists plus model
manifest assets and narrowly selected web/runtime files. No recursive copy of
the experiment is allowed. Third-party license material accompanies the
collected Python/NumPy/PyInstaller runtime. Managed DirectX is discovered from
the user's installation and is excluded from the portable payload.

Commands (use a fresh work/output directory each time):

```powershell
python -m pip install --target packaging/toolchain -r packaging/requirements-build.txt
python -B packaging/test_package_contract.py
python -B packaging/assemble.py --native-build <current-product-build-directory> --output <fresh-output> --work <fresh-build-work>
python -B packaging/check_package.py <output>/Magic600Cell-0.4-Windows-x64 <fresh-verification-output>
```

`assemble.py` records exact source/model/native/tool versions and every payload
hash, then optionally writes a ZIP. This is a local candidate only: no installer,
GitHub action, release metadata change or upload is performed. The final ZIP
must be regenerated if any packaged source or document changes.

The checker removes Python/compiler locations from child PATH, runs the actual
frozen executables, checks the 0.4 HTTP route and pinned proof, and exercises an
explicit legal preview/commit/undo plus a Unicode-path reopen. It separately
checks parent EOF cleanup, installed runtime discovery, damaged-proof refusal,
and byte-identical package files afterward. All test sessions/logs remain in
the specified private verification directory, outside the package.

These are package/engine checks, not native rendering, clean-machine installation,
scaling, full manual solve or release acceptance. Actual results and missing
scope will be appended only after the current host is supplied and checks run.

## Read-only readiness audit — 2026-09-17

The current allowlist resolves 68 application files and 37 native sources. It
includes `session_workflow.py`, `keymap_catalog.py`, `grips.py`, `log_io.py`,
`mpult_log.py`, and the Session/Keyboard native implementations. The local-import
closure and immutable asset/proof hash checks completed without an omission.
Personal log files, `native_keys.json`, SQLite databases and installed Microsoft
MDX assemblies are not distribution inputs. Retained MPUlt puzzle/settings files
are explicit runtime inputs; inspected settings contain puzzle/display values,
not a personal session or file path.

`native-build/39c8cb288142dba9` passed the current source/backend/configuration
binding check during this audit; its product executable SHA256 is
`8fcc6cb25d7a86ce64257145e62d7720372813eb56883731b8dafce5e78d5d60`.
This is a presently usable build input, not final native acceptance. Any later
UI/performance/backend change requires a new bound product build.

The assembly race fix and its test file are unchanged (`2741f762…56c` and
`e97c12b3…aaa`). `evidence/packaging-binding-validation.json` records eight passed
checks of staging, source/build correspondence and simulated races. Its current
dependency comparison differs for adapter, keymap catalogue and Session helper;
therefore reuse the unchanged race proof only. Run the small packaging contract
suite against the final files before assembly. The pinned toolchain is present.
No frozen package or package runtime was built/launched by this audit.

After final source freeze, run from the experiment directory with the existing
64-bit build Python (the product build and any GUI run remain root-owned):

```powershell
python -B packaging/test_package_contract.py
python -B native_launch.py --mode g2 --build-only
python -B packaging/assemble.py --native-build native-build/<new-build-identity> --output <fresh-output> --work <fresh-build-work>
python -B packaging/check_package.py <fresh-output>/Magic600Cell-0.4-Windows-x64 <fresh-check-output>
```

Keep the native `build.json`, `assembly-report.json`, package manifest and ZIP
hash, then the checker's `summary.json`, `resources.json`, `engine.json`,
`parent-eof.json`, `runtime.json` and damaged-proof result. Record the actual
candidate's normal native launch separately; the checker never opens that UI.
Preserve the before/after payload hashes and explicit private verification
directory. No source-profile copy belongs inside the distributable.

Three acceptance gaps remain concrete:

1. **Existing-profile copy and rollback:** `engine_test` deliberately requires
   a nonexistent data directory. It cannot prove copied 0.3 or older experiment
   compatibility. Close the source application, copy an isolated synthetic
   legacy profile (including its whole SQLite directory) into a fresh location,
   retain source hashes, then launch the candidate with explicit `--data`.
   Compare all labels, journal/checkpoints and retained personal data, close and
   reopen, and verify the original remains byte-identical. Rollback uses the old
   executable plus the untouched original, never the upgraded copy. The 0.4
   workspace reads `layout.magic600_experiment`; old native `native_keys.json`
   and top-level library/binding preferences are not automatically migrated
   into its new UI. Preservation and migration are different claims.
2. **New routes in frozen resources:** the existing engine check covers residual
   proof/endgame choices and retained preview/commit/undo, but not Session
   New/Resume/log operations or the new versioned keymap routes. Reuse the
   already-tested legal log/keymap fixtures against the built executable:
   C600 and verified-profile MPUlt export/check/explicit import, solved import
   without completion, and keymap export/check/explicit apply/reopen. MPUlt
   requires the real native handshake; a guessed profile is not acceptance.
3. **Normal runtime and distribution details:** actual installed-MDX/.NET native
   startup, supported scaling and compatibility are still untested in a frozen
   candidate. The final payload check must confirm actual collected dependency
   notices and no personal files/Microsoft MDX DLLs. The README/changes describe
   the older candidate feature set and do not yet mention the added Session/log
   and keymap file controls. Update those descriptions before the final ZIP;
   the retained runtime's missing-MDX error still names `C600Studio.exe`, which
   should be reconciled with `Magic600Cell.exe` before delivery.

## Checker extension — implementation, not frozen acceptance

The checker now creates a known one-primitive Session through the actual frozen
engine, closes it, and adds only generated legacy-shaped preference/key-file
fields to that owned fixture. It copies the whole closed directory into a new
owned location and records every original file hash before and after. The copy
must preserve all labels, journal head/checkpoints and legacy data; opening it
must not silently migrate the legacy native key file or top-level macro/binding
preferences into the 0.4 workspace. Reopen checks the explicitly imported new
keymap. The untouched source is the rollback artifact; no historical 0.3
executable compatibility is claimed by this generated fixture.

The same copied Session exercises New/recovery, Resume with paused timer,
non-executing seeded scramble, keymap export/check/apply, C600/MPUlt log
export/save/check/explicit import/recovery, and Home import without completion.
It uses explicit fixed legal words and keeps the previous fixture state. The
older launcher's package test now uses the authorized experiment
Draft/Review/Preview/Commit/Undo boundary; the retained `/api/preview` routes
were rejected by the 0.4 adapter.

Full MPUlt acceptance additionally needs a real native `Geometry()` export
captured by the GUI owner, its separately recorded SHA256, and its exact MPUlt
executable hash matching the package. The checker sends that descriptor through
the frozen server's normal handshake; it does not install a saved profile or
substitute synthetic geometry. The example becomes:

```powershell
python -B packaging/check_package.py <bundle> <fresh-check-output> --native-export <actual-geometry.json> --native-export-sha256 <captured-sha256>
```

Without this witness, C600/copied-profile checks may run, but the final checker
result remains incomplete and cannot pass. `copied-profile.json` adds original
before/after hashes, actual route outcomes and missing coverage. README/CHANGES
now describe the confirmed Session/log/keymap additions. Frozen execution,
native startup and historical-profile compatibility remain unclaimed until
their actual runs; no PyInstaller build or native window was started for these
checker changes. The new route-scenario unit test uses a real source Workbench
and a previously captured profile, which is deliberately narrower evidence
than frozen-server handshake or native interaction.

Checker verification: five new tests failed for the missing acceptance helpers
and disallowed launcher routes (`evidence/package-acceptance-red.log`). After
the extension, 13/13 passed in the bound run
`evidence/package-acceptance-20260917-041146/result.json`: 40.067 s for unittest,
41.237 s for the command, exit 0, source/model/profile hashes unchanged. These
times include concurrent host work and are not a performance benchmark. The
readiness audit and these tests did not assemble or launch a frozen package.

Independent review added checks for the exact pre-existing checkpoint records
and copied legacy key file after use and reopen, a truthful failed nested receipt
if the original changes, and rejection of output paths inside the package before
creating them. A real SQLite WAL fixture then reproduced that `mode=ro` itself
creates sidecar files. Closed generated profiles are now inspected with
`immutable=1`; a nonempty WAL is refused instead of ignored or modified.
The two WAL regression tests failed before this fix and passed afterward
(`evidence/package-wal-preservation-red.log` and `-green.log`). Independent
five-case recheck passed in `evidence/package-review-preservation-recheck.json`.

The intermediate `evidence/package-acceptance-20260917-041723/result.json`
records 16 passing tests but a failed source binding because
`native/ExperimentDisplay.cs` changed during that run. It is not current bound
acceptance. The final headless receipt binds all actual source Workbench,
packaging, model and captured-profile dependencies; native C# file existence
is checked, but their contents are neither executed nor claimed verified by it.

Final checker/source verification:
`evidence/package-acceptance-20260917-042338/result.json`, 17/17 tests,
exit 0, 21.085 s unittest / 22.094 s command, all scoped inputs unchanged.
Checker SHA256 `1B839FECC7ACDE68E2F15C7B1D60C00ECC08CE18F6E86A871C806C7600E5DAB7`;
test SHA256 `8018943745163F493B43F8BDAEBC2CF09C6D4D275C9534F57D46716298312DCD`.
No frozen candidate, native startup or actual historical profile migration was
run in this verification. Release acceptance still requires the bound native
Geometry witness, package assembly, frozen checker and actual native checks.

## Actual assembly attempt — 2026-09-17

`packaging/artifacts/20260917-043726/` retains the first actual PyInstaller run
against product build `61a7f4c1af86173e`; assembly exited 0. Manifest integrity,
payload exclusions, known credential-pattern inspection and ZIP CRC checks
passed. Static dependency inspection nevertheless found two missing notices:
the collected Charset-Normalizer 3.5.1 and Typing-Extensions 4.16.0 licenses.
The assembly source now copies both original installed-distribution notices and
records their versions. The new notice regression failed before the fix; it and
the four existing assembly-binding checks then passed (`notices-red.log` and
`notices-green.log` in that attempt directory).

This first candidate is **incomplete**, as recorded by `static-review.json`.
Its native receipt also differs from the later test-only `run_postapproval.py`
input. It remains historical evidence and is not relabelled as a final package.
A fresh candidate will use the new bound product build and corrected notices.
No candidate executable or native window was launched during this work.

### Corrected local candidate

`packaging/artifacts/20260917-044351/` was assembled against the fresh product
build `9a8a8e39533dc179`; `exit.json` records exit 0 with no changed native receipt
inputs. `work/assembly-report.json` binds the successful assembly, and
`static-review.json` records independent final-payload/ZIP comparisons: all 68
application files match their source bytes, all 180 files including the manifest
match the archive, expected PE architectures and prohibited-file exclusions
pass, and notices for every collected third-party Python package are present
and byte-identical to their installed distribution. The text scan found no
known high-confidence credential pattern; that is not an exhaustive secret
detector. No Microsoft Managed DirectX DLL or personal Session data is included.

- Folder: `packaging/artifacts/20260917-044351/dist/Magic600Cell-0.4-Windows-x64`
- ZIP: the adjacent `Magic600Cell-0.4-Windows-x64.zip`, 55,956,537 bytes.
- ZIP SHA256: `68f39582ace0c14dab8a5bc80956c8cc51af2b80adf75045282d7081f6b27abb`.
- Manifest SHA256: `429c7cf90626019474406c68646185c3c53741f5759ab0c0b98e3d06c77d1964`.

The actual frozen launcher resource check subsequently passed with PATH limited
to System32 and Python environment paths deliberately nonexistent:
`verify-resources/resources.json` and `process.json` (exit 0, packaged true,
native host not started). This confirms frozen resource loading only. Complete
frozen engine/file-route compatibility, actual native startup and release
acceptance are separate checks and are not implied by successful assembly.

The isolated Unicode-path frozen engine check also passed (`verify-engine/`
and `frozen-smoke.json`): source-bound invariant loading, the 60 explicit A5
choices, legal Prepare/Review/Preview/Commit/Undo, all 259800 labels restored,
reopen equality, and both graceful exits without a forced shutdown. Rechecking
the complete manifest afterward found the package unchanged. This executed the
bundled engine, not an external Python interpreter; no native host window was
started. Full copied-profile and verified MPUlt checks await the actual native
Geometry witness and complete `check_package.py` run.

### Full non-GUI frozen check — current candidate only

The complete checker passed (exit 0, 185.822 s) in
`packaging/artifacts/20260917-044351/check-full-20260917-050328/summary.json`.
The adjacent `check-full-20260917-050328-receipt.json` binds the checker,
manifest, actual native export and its handshake/profile/build evidence before
and after; all remained unchanged. Root ran native diagnostics concurrently,
so the elapsed time is not an idle-host performance benchmark.

The native witness is
`native-baseline/postapproval-g2-20260917-050049/native-geometry.json`, SHA256
`277f3c46ba12252d9d91b2b94cb0a57193f5505298d8c92e3a56d04c74ad5ff4`.
The frozen server verified it through its normal native handshake. All seven
checker groups passed: payload/resources, isolated frozen transaction and
reopen, parent-EOF cleanup, installed MDX discovery without rendering,
copied generated profile plus Session/C600/MPUlt/keymap routes, and refusal of
a deliberately damaged proof followed by byte-identical payload restoration.

`copied-profile.json` records actual C600 and MPUlt file checks, verified
geometry, copied-profile reopen, exact preservation of the original generated
profile, and retention of its `Solved root` and `Generated legacy unfinished`
checkpoints and legacy key file. No automatic adoption of the legacy UI
preferences is claimed. This remains a generated legacy-shaped fixture, not
an actual historical-version migration or an old-executable rollback run.

The ZIP/manifest hashes above still identify this checked candidate. Native
window startup, GPU/scaling/solver interaction and public release remain
separate acceptance boundaries. Subsequent product/UI changes require a new
candidate and checks appropriate to those changes; these results must not be
relabelled as acceptance of a later build.
