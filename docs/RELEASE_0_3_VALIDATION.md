# C600 Studio 0.3 validation

This record accompanies **C600 Studio 0.3**. It preserves the distinction between backend checks, WinForms fixtures, actual MPUlt/DirectX integration, executable packaging, and short performance observations. The historical [0.2.4 validation](../tests/VALIDATION.md), release tag, and downloads remain unchanged. All destructive checks used fresh isolated data; personal sessions and raw diagnostics are excluded from this repository.

## Source and package identity

The final package was built from 13 production native sources, in the order declared by [the build script](../packaging/build_windows.py). Their combined SHA-256 is `f40a18efaa55d9f24fe17e3228c286054cce70e85bd9dfe61c437e89c0e26116`.

The complete interaction/rendering integration and timed samples preceded the final version-banner update. Their combined native hash is `6188e585d4bf77e7b2bcd55f9e81e026df6e65309e709a2349eeba245bcbdcd0`. The subsequent native change centralizes the displayed version as 0.3; rendering and interaction code is unchanged. Release compilation and build-time fixtures ran again against the final versioned sources. Launcher migration, packaging version metadata, and runtime-license collection were also validated separately.

The immutable asset manifest and original MPUlt executable retain SHA-256 `b5db4469f3da9ba3cdbd93806a703f594153e61b08ce32d4fcce9de9c5cc60bc` and `228b3145460a399e2accc17cb5b891568ec8ddd33c011f1f2a32a00c59743327`, respectively. Model slots, generators, seeds, and execution trees were not changed. The [source inventory](../SOURCE_MANIFEST.json) records individual public file hashes; the portable manifest records packaged files and source identities.

## Focused correctness checks

| Check | Observed result and scope |
| --- | --- |
| Required backend regressions | `tests/test_core.py`, `tests/test_reference_maps.py`, and `tests/test_crash.py` passed after the last mechanics/persistence feature changes. Reference maps cover all 1,200 primitive generators. |
| Color, topology, filter, and inspection regression | 16 groups passed: 600 stable cell IDs, connected symmetric four-regular adjacency, center-origin layer partitions, 120 vertices and incidence, canonical/legacy grammar, moved-identity semantics, invalid-input rollback, hidden/stale rejection, and atomic update reconstruction. HTTP bridge fixtures use synthetic shuffled geometry and do not establish native picking. |
| Auxiliary backend data | Six groups passed retained geometry/count oracles, exact filter eligibility, cache reuse, focus validation, transactional failure, and complete checkpoint/reopen checks. |
| Inspection persistence | Four tracking groups passed identity following, selection independence, clearing, and complete-label reopen. Five dedicated inspection/focus groups passed atomic annotation/focus writes, both gesture meanings, stale/hidden rejection, and injected rollback. |
| Native snapshot cache | Six groups passed immutable bounded cache reuse, full/delta reconstruction, legacy v1 fallback, replacement-profile invalidation, and full-label recovery. |
| Final x86 native compilation and build-time host fixture | Passed. The fixture passed 19 groups including layout scale policy, input/editing, font and panel layout, and out-of-order filter-preview completion. No fixture is launched during normal user startup. |
| Standalone native controls | Eight Structure, six geometry-control, and eleven auxiliary-window-manager groups passed with retained geometry and fresh solved backend data. The [public runner](../tests/test_native_auxiliary_controls.py) invokes the existing focused fixtures without starting MPUlt. |
| Actual MPUlt/DirectX feature pass | All 28 feature groups passed and the process exited successfully. Coverage is described below. |
| Version migration | Eight temporary-file cases passed previous-version fallback, per-file behavior, and preservation of existing 0.3 settings. No personal settings were modified. |

The 28 native groups cover exact hit mapping and Shift inspection, Shift-left 4D dragging, ordinary 3D/Ctrl gestures and multi-click turns, filtered picking, committed label/color/style agreement, separate annotations, retained Buffer Analyzer destination, filter rollback/composition, Structure navigation without DirectX redraw, checkpoint/undo/redo/reset, macro and insertion previews, buffer/progress/session reports, and controlled renderer recovery. They also cover real global/local cell geometry, shared focus/status, Follow/Pin, independent camera motion, queued/rejected focus requests, narrow-window drawer input, minimize/close/reopen, and accepted/canceled shutdown. Canceling busy shutdown preserves the original native puzzle object; Enter in the compact Views drawer does not commit a pending puzzle preview.

Input is delivered through owned native HWND messages and the production message filter. Controlled device loss is fault injection. These checks do not claim physical mouse hardware testing, every possible driver failure, or an exhaustive matrix of all user histories.

## Actual visual review

The matching-source visual regression passed. Reviewed images combine actual WinForms/GDI controls with actual DirectX render-target output:

| Main window | Native viewport | Reviewed auxiliary presentation |
| --- | --- | --- |
| 1300 x 740 at 96 DPI | 931 x 604 | Both 420 x 348 floating views; 600 global cell markers and the actual five-cell one-hop neighborhood. |
| 1000 x 650 at 96 DPI | 631 x 514 | Compact Views drawer; pinned neighborhood remains distinct from the current main selection. |

The review also inspected the Structure page, display controls, focus outline, and compact Follow/Pin states. This is application-rendered capture, not an independent desktop-compositor screenshot. Actual mixed-monitor/high-DPI behavior remains unverified; pure layout-scale policy tests are not a substitute. Auxiliary cameras, placements, and pin mode persist across close/reopen within the process, but not across application restarts.

## Windows executable and installer

The final portable build succeeded with CPython 3.12.14, NumPy 2.3.5, and PyInstaller 6.22.2. Its verified manifest contains 135 resource files totaling 99,775,784 bytes. The native host is x86; the launcher and engine are x64. The compiler and regression executables are excluded from the distribution. Official Microsoft Managed DirectX remains an external prerequisite.

All seven [portable verification](../tests/test_portable_package.py) groups passed using the real frozen Windows executable: notices and architecture, all packaged hashes, operation with Python/compiler paths removed, authenticated full 259,800-label engine startup, graceful shutdown and identical reopen under a Unicode data path, parent-pipe EOF cleanup and lock release, actionable rejection of damaged resources before session/UI opening, and unchanged distribution files after isolated runs.

A separate normal-entry-point smoke passed all six checks against the final 0.3 executable. The actual native window responded after live bridge synchronization, the exact packaged host and complete solved-root labels matched, and no startup fixture ran. Normal `WM_CLOSE` returned the launcher, engine, native host, and verified Windows console support process with exit code 0; no forced cleanup was used, and the session lock was reacquired after shutdown. Two earlier private smoke attempts stopped because their process guard rejected the legitimate Microsoft-signed Windows console host. The guard was corrected to recognize that exact system file; no application code was changed to obtain the passing run.

The NumPy notice collector accepts the installed distribution's metadata-root license and retains its three component notices, including OpenBLAS/LAPACK/GCC terms. No Microsoft DirectX DLLs, private logs, databases, compiler, or test fixture executable are redistributed.

The Inno Setup 6.7.3 installer compiled successfully from the verified 0.3 portable payload. Compilation and payload verification are established; an actual operating-system install/uninstall cycle was not run in this pass.

| Artifact | SHA-256 |
| --- | --- |
| `C600Studio-0.3-Setup.exe` | `59cb08ff8722ceddb46a437cd08014474230625075afbfadb621b885a7c5411b` |
| `C600Studio-0.3-Windows-x64.zip` | `8fd23453196c9ca23847310cda0e1e448c4fca837d37be252378391e0d157eba` |

## Performance and unresolved targets

The complete [method and measurements](DEVELOPMENT_PERFORMANCE.md) are part of this release record. On the actual Intel HD Graphics 620 device, the short native-turn sample reduced mean final-MouseUp-to-correct-frame latency from 478.03 to 193.27 ms with 2,400 visible stickers, and from 1,669.53 to 777.92 ms with all 259,800. Current p95 was 230.78 and 839.77 ms respectively; **the 100 ms p95 target is unmet**. The timed path excludes the normal render timer's scheduling delay.

A real 1920 x 1080 Direct3D backbuffer averaged 622.25 ms per complete changed-camera frame, approximately 1.61 render-only FPS. A short production-timer drag submitted 31.07 FPS using 1,500 sampled stickers; full detail returned in 600.58 ms. The oversized child was clipped by the available 1280 x 720 automation desktop. This is a real render-target workload, not sustained full-detail rotation or display-scanout evidence.

Structure color navigation and keyboard focus met their 50 ms p95 gates; Enter activation measured 50.04 ms and **failed** its gate. All requested zero DirectX frames. Both auxiliary views met paired 50 ms p95 gates in the sampled sizes, with some individual hover maxima around 100 ms. Idle, minimized, and closed one-second observations each produced zero auxiliary paints and zero main DirectX frames. Short samples do not certify absence of memory leaks or sustained frame-rate behavior.

The minimum GPU class for continuous full-detail 1080p/30 FPS remains unverified. HD 620 does not meet that target. No replacement GPU model is inferred from a single machine. These limitations are release findings, not passing performance claims.

## Research report and publication scope

The English LaTeX technical report was updated to 0.3 and compiled to 34 pages, with 31 numbered equations, two propositions, and 18 references. Final compilation reported no warnings, overfull/underfull boxes, or unresolved references. All pages received visual review, with changed sections and major structural pages inspected at full-page resolution. The original algorithms paper, curated replay archive, Puzzle Theory LaTeX/PDF, and retained proof-review artifacts remain byte-identical.

Only reviewed source, necessary documentation, the report source/PDF, and distributable build assets are published. Private experiments, benchmark instrumentation, failed candidates, generated machine reports, session data, conversation exports, and screenshots are excluded. See [limitations and roadmap](LIMITATIONS_AND_ROADMAP.md) for explicitly future work.
