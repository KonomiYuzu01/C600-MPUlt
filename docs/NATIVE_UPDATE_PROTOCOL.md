# Native update protocol

This contract describes the current development source. Existing 0.2.4 release downloads are unchanged. The backend owns all 259,800 labelled slots, finite move witnesses, journal state, and preferences. Native arrays are a display of that state; filtering never removes mechanical data.

## Profile and snapshot requests

`POST /api/native/handshake` verifies the live full-model geometry and legal move mapping. Snapshot and native-input routes require that verified profile. Its `profile_sha256` identifies the mapping; request bodies cannot supply replacement native-to-lab arrays.

| Request | Response |
| --- | --- |
| `GET /api/native/snapshot` | Legacy `C600-native-snapshot-v1`, always full |
| `GET /api/native/snapshot?protocol=2` | `C600-native-snapshot-v2`, full |
| `GET /api/native/snapshot?protocol=2&since=<revision>` | v2 delta when the base is retained and economical; otherwise full |

Both formats include `profile_sha256`, atomic `state`, base64 `colors`, and base64 `styles`. Current replies also include `interactive`. The v2 client requires it; for older v1 replies that omit it, the legacy client fallback infers interaction from nonzero styles. That fallback cannot preserve the new distinction between visible annotations and clickable pieces, so the updated native interface requests v2.

Full arrays use native slot order:

| Field | Encoding | Full length | Meaning |
| --- | --- | ---: | --- |
| `colors` | Little-endian unsigned 16-bit | 259,800 entries | Native palette IDs 0–599 |
| `styles` | Unsigned byte | 259,800 entries | Display codes 0–6; zero hides the sticker |
| `interactive` | Unsigned byte | 259,800 entries | 0 or 1; exact unpinned filter membership |

No interactive slot may have style zero. Nonzero display style does not imply interactivity: context pins and inspection annotations can show otherwise hidden geometry. The separate frame display policy does not add clickable stickers.

## Revisions and deltas

V2 adds opaque `revision`, `mode` (`full` or `delta`), and a `revisions` object:

| Revision key | Bound data |
| --- | --- |
| `state` | Complete labelled-state hash |
| `color` | Complete native color array |
| `visibility` | Complete style array, including changes between nonzero styles |
| `interaction` | Complete interaction mask |
| `annotation` | Durable inspection context and canonical cell focus |

The top-level revision also binds the profile and complete status, including preferences and pending-preview metadata. A metadata-only change can therefore produce a new revision with no mechanical change. Clients treat revisions as opaque identities, not counters.

A delta adds `base_revision` and base64 `indices`: strictly increasing, unique, in-range little-endian unsigned 32-bit native slot IDs. The three value arrays have one entry for every index in the **union** of color, style, and interaction changes, even when only one attribute changed. An empty union is valid. Full replies omit the delta base and indices.

The server retains at most four array snapshots for the current profile. A missing, unknown, evicted, or replaced-profile base yields a full reply. A delta also becomes full when its binary union payload would be at least as large as the full arrays. This is a size heuristic, not a promise of lower latency.

The native consumer validates format, profile, lengths, value ranges, and the exact predecessor revision before applying anything. It materializes complete arrays without mutating the predecessor, and derives attribute-specific changed slots for publication. An invalid delta triggers an explicit full fetch; it must never be patched onto an unrelated state.

## Atomic commands and failure handling

Adding `native_since` to a supported mutation requests `result.native_snapshot` in v2; use `null` for a full response or the cached revision for a possible delta. The server removes this transport field before validating preference payloads. Dispatch and snapshot construction share the session lock, so committed status, colors, styles, and interaction describe one state. The native-turn path consumes that returned snapshot instead of issuing a second normal fetch.

`POST /api/native/turn` checks `pre_state` and every native token against the verified translation table, then certifies and durably commits the finite full-model operation. Input publication waits for that result. After an error, the client fetches authoritative state to recover from rejected input or a lost response. A lost response does not prove rollback: a durable commit may already exist, so clients must not blindly repeat it. If no valid snapshot can be obtained, input stays disabled until reconnect succeeds.

Reconnect verifies the bridge, installs a full authoritative snapshot, releases the rendering hold, and requires an actual submitted frame through the device-recovery path. A deferred or held frame cannot be reported as ready. Recovery does not reset the journal.

## Inspection and filter invariants

`POST /api/native/inspect` requires `native_sticker`, `gesture`, and exact `pre_state`; an optional `profile_sha256` must match. Gestures are `home-centers` and `required-piece`. The exact hit is mapped through the verified profile and checked against the interaction mask before saving its context. Stale, malformed, or hidden inspection is rejected without changing labels or preferences.

Home inspection stores the occupying identity and returns all solved home colors plus its current position/piece. Required-piece inspection stores the clicked destination, returns the current location of the identity that belongs there, and binds buffer analysis to that destination's orbit. Both retain original sticker/cell metadata and atomically set `prefs.focus_color` to the clicked physical cell (`clicked_slot // 433 + 1`) in the same preference transaction. No second focus request is needed. `POST /api/inspection/clear` clears only the piece annotation; cell focus, existing `selected`, rules, and active orbit remain independent.

`GET /api/structure` supplies immutable full topology for local navigation. `POST /api/filter-preview` evaluates an expression or ordered rules using the current active orbit and returns exact unpinned counts, added/removed counts, normalized rules, and `context_hash`. `POST /api/filter-apply` accepts those rules and context; it revalidates current state, preferences, and pending-preview context atomically before saving the rules with context pins disabled. Stale or invalid apply preserves the prior preferences and labelled state.

## Auxiliary geometry, focus and counts

The structure payload contains `geometry`, format `C600-cell-geometry-v1`: 120 four-dimensional `vertices4`, 600 four-vertex `cells` rows, and 600 `centers4`. Coordinates use **W, X, Y, Z** order; vertex indices are zero-based. Existing outer adjacency and displayed colors remain canonical C1–C600. These are the actual cell vertices and centers derived from the immutable full model, not a replacement high-resolution sticker model. The native auxiliary controls share one validated geometry object and navigate it locally.

`POST /api/focus` accepts exactly `{ "color": 1 }` through `{ "color": 600 }`, or `{ "color": null }` to clear. With `native_since`, it returns the usual atomic native snapshot. This durable preference changes no labels, inspection, filter, protection, original selected identity or interaction mask. `state.focus` is null or `{color, lab_cell, center_position, center_slot}`; native code maps the lab cell through the verified face mapping for a separate non-interactive outline. Selecting focus does not recenter the main camera.

Only native snapshot status adds `state.cell_status`, format `C600-cell-status-v1`, with `state_hash`, opaque `revision`, numeric active `orbit` (0–34), `focus_color`, `buffer_cells`, `selected_cells`, and six arrays of length 600:

| Array | Count for each physical cell |
| --- | --- |
| `active_total`, `active_solved` | Positions of the active orbit touching the cell; subset whose complete labelled piece is solved |
| `visible_total`, `visible_unsolved` | Positions admitted by the exact unpinned interaction filter; subset whose complete labelled piece is not solved |
| `eligible_total`, `eligible_solved` | Intersection of visible and active positions; subset whose complete labelled piece is solved |

A position is counted once per cell it touches. Totals across different cells overlap and must not be added as a global progress count. No color-only or estimated completion percentage is substituted. The arrays reuse the snapshot's already computed interaction mask and are cached by complete state, active orbit and interaction revision. Focus-only updates reuse those counts. Both auxiliary views receive the same atomic status object; hovering requires no backend request.

## Limits and verification

The server still constructs and hashes full display arrays before selecting delta entries. The immutable profile cache avoids repeated mapping conversion, but a delta does not eliminate full-state validation, durable journaling, or full-detail rendering costs. This protocol provides consistency, not a 100ms UI or frame-rate guarantee.

Backend and synthetic HTTP coverage is in [test_color_inspection.py](../tests/test_color_inspection.py); immutable mapping/palette coverage is in [test_native_snapshot_cache.py](../tests/test_native_snapshot_cache.py). Auxiliary geometry/count/focus checks are in [test_cell_views.py](../tests/test_cell_views.py), and the inspection transaction is covered by [test_inspection_focus.py](../tests/test_inspection_focus.py). Native materializer checks are in [NativeDataRegression.cs](../tests/native/NativeDataRegression.cs). Actual input, reconnect, rendering, and performance require separate current-build Windows validation. See [the development log](DEVELOPMENT_LOG.md) for measured scope and decisions.
