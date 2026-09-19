# Magic 600 Cell 0.4 for Windows

**Andrey Astrelin created Magic Puzzle Ultimate (MPUlt)**. This application retains
his puzzle geometry and renderer. See `CREDITS.md` and `licenses/MPUlt-MIT.txt`.

This folder is an isolated release candidate awaiting acceptance, not a public release or
a claim that all 0.4 acceptance work has finished.

Extract the complete folder, then open **Magic600Cell.exe**. Keep
`Magic600Engine.exe` and `_internal` beside it. Python, NumPy and the compiled
native host are included; Python and a C# compiler are not required on PATH.
Normal launch never builds code or opens developer fixture windows.

The retained native renderer requires 64-bit Windows, .NET Framework 4.x with
legacy activation support, and the supported Managed DirectX runtime. Microsoft
Managed DirectX DLLs are **not** included. If the launcher reports missing
assemblies, install the official [DirectX End-User Runtimes (June 2010)](https://www.microsoft.com/en-us/download/details.aspx?id=8109)
by extracting Microsoft's package and running `DXSETUP.exe`. Existing verified
installed assemblies are copied only into this computer's session runtime cache.

## Work in the existing Solve window

The existing native Puzzle view is visible by default in G2. Open Solve to choose a macro,
prepare its steps, inspect the complete result and protection, then explicitly
preview and execute. Current, locked Next and protected state are different.
Use the graphical cycle view's Current / Operation / After modes to distinguish
the actual residual, the chosen operation and its predicted residual.

Endgame families are a disclosure inside Solve, not a second solving workspace.
Choose the family and its explicit auxiliary positions and finite parameters.
Check the result and its full collateral effect before saving a new macro.
Saving does not select, insert or execute it. Reference variants similarly
require explicit source and destination ordered frames. Their net equivalence
does not inherit the original macro's intermediate-motion protection approval.

The onscreen keyboard shows the active set and its real functions. Use the set
picker to inspect or edit bindings. Functions groups utility actions separately
from direct solving keys. Custom bindings take precedence. The app does not
search for setup words, choose macro combinations or execute a solution for you.

Session provides New, Resume, explicit timer controls, staged seeded scrambles
and recoverable reset. A recorded whole-solve completion is tied to an actual
journal commit; importing Home, undoing or restoring does not manufacture one.
C600 and MPUlt logs can be exported, checked and explicitly imported. Import
retains the current workspace preferences and supplies a recovery checkpoint.
Keymap files have their own versioned export/check/apply flow; they carry
bindings, not a puzzle state or arbitrary executable commands.

## Sessions, updates and rollback

The default profile is `%LOCALAPPDATA%\Magic600Cell\0.4`. It is separate from
older `%LOCALAPPDATA%\C600Studio` and experiment profiles. No old profile is
silently imported, renamed or deleted. Logs and checkpoints belong to that data
folder, never to this application folder.

For a deliberate compatibility check, close the old application, copy its entire
session folder to a new location, then launch this package with
`Magic600Cell.exe --data "C:\Your separate copied session"`. Do not run two versions
against one profile. Preserve the original as the rollback copy; do not assume
that newer workspace preferences can be interpreted by an older application.
Old native `native_keys.json` and legacy top-level macro/binding preferences are
not automatically adopted as 0.4 workspace settings. Keeping the old data is
different from migrating its interface configuration.

To roll back, close 0.4 and reopen the prior application with its original,
untouched profile. Keep or archive the separate 0.4 profile. Replacing the
application folder does not remove personal data. Do not mix files from two
package versions; extract a new complete folder instead.

## Evidence and limits

`_internal/package-manifest.json` binds the exact application/model/native files
and the pinned invariant certificate. An integrity mismatch stops launch. The
certificate remains source- and model-bound; packaging does not relax it or
perform a new expensive generator enumeration during launch.

Headless package checks are distinct from actual native rendering, supported
Windows scaling, long solving sessions and clean-machine installation. Passing
them does not establish those separate claims. The shipped finite endgame
families and representative witnesses do not prove every arbitrary state has a
solution under a chosen protection policy. Unknown evidence remains unknown.

Changing a protection policy invalidates an earlier preview. Execute may remain
clickable, but the stale request is rejected before any transaction. Cancel the
old preview, then check and preview again under the current policy.

The accompanying acceptance report records completed checks, reused evidence
and any remaining delivery requirements. Packaging alone does not certify
native acceptance or complete the recording and English-only subtitles.

Performance optimization is scheduled for the next iteration.

See `CHANGES.md` for changes since 0.3 and the included licenses for bundled
components, including the collected Charset-Normalizer and Typing-Extensions
dependencies. This package contains no personal session, conversation, token,
private strategy material or raw machine diagnostic bundle.
