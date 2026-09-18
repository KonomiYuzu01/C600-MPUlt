> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Static effect classes and UseScore — bounded plan

The bounded module implementation is complete; integration/native verification
remain separate. Ownership is `macro_use.py` and the new focused test file;
the coordinator owns adapter/cache,
facet and native integration. No CurrentScore, residual service or execution
change is included. Contracts 07 and 08 are authoritative.

## Exact data boundary

Add `classify_effect(chart, source, destination) -> (summary, decomposition)`.
The existing TransportedFrames instance decomposes the complete net action once.
`summary` is detached JSON: model, frame_version, orientation_complete and
orbit_effects. Each affected-orbit row has effect_class, position_count,
orientation_count, affected_count, overlap_count, unknown_count/reason,
cycle_count, single_cycle_length and the legacy pure_cycle field. Cycle counts
come from the complete position map, before any display truncation. Unsupported
orbits are proven NoEffect through the complete support/unchanged_orbits list.

The full NumPy decomposition is internal and separately cached with the bounded
full-effect entry. It is not repeated in library snapshots. Cached JSON facts
retain the summary needed for score/facets. Keys bind exact normalized recipe,
model and frame version. Bump the exact-effect proof version; old cached or
imported claims cannot satisfy it. First-time charts are built only during an
explicit cancellable check, never passive snapshot/classification.

Keep `retained_references` only for explicit Star/reference display consumers.
It must no longer define zero for Pure or scoring. The old pure_cycle_facts
function was removed after the coordinator migrated the sole application caller
to classify_effect. Historical tests/probes with the old reference-relative
expectations require explicit migration or historical labelling. No chart is
recreated per macro.

## Compatibility and truthful labels

The four exact classes are NoEffect, PurePosition, PureOrientation and Mixed;
Unknown is explicit evidence failure. Any number of disjoint position cycles may
be PurePosition. Existing Star certificates remain independent: a verified Star
can be Mixed in the fixed chart. Equal raw and Star-expanded actions have equal
dimension/score facts; only explicit Star provenance remains different.

Legacy pure_cycle.status is Verified only for PurePosition with exactly one
nontrivial cycle. Multiple pure cycles use NotSingleCycle with an explicit
reason; PureOrientation uses NotPositionCycle. Neither claims the new broad
Pure class is false. Unknown remains Unverified. Keep cycle_length/reference
fields, binding the fixed chart version rather than recipe-selected frames.
The coordinator adds pure-position/pure-orientation/mixed facets and captions;
the old single-cycle facet stays narrow. All useful manual tags, notes and pins
remain untouched.

## UseScore output

Keep existing status/memberships/other/candidates/reasons keys. Add score_version
and frame_version; each membership/candidate carries a score and contributions.
Use exactly the formula/weights/thresholds in 07. n_o is the union of position
and orientation support, N its full-model sum and L the actual expanded count.
No Star bonus, sequence reduction, inferred cost or largest-support shortcut.

Sort by unrounded score, then stable internal orbit key. Select at most three
with score >=70 and within10 of the maximum. Other is true only when a fully
checked effect has no qualifying membership. Keep every remaining affected orbit
as a clearly labelled candidate, ordered by score; this preserves Current-orbit
browsing for preparation/collateral even when not assigned a likely-use label.
At most three concise reason codes accompany a candidate; numerical contributions
remain separately inspectable. Threshold tests do not use rounded display values.

Missing/stale whole-effect facts remain Unchecked. Complete position evidence
with missing orientation gives AwaitingVerification, no memberships, other=false
and null unavailable scores. Preserve known contributions/facts; never use T=0
for Unknown. Existing native non-Checked filter safely retains this in the
unchecked group, but its caption must say Awaiting verification. Identity gives
Checked/Other/no candidates without dividing by N=0. Current protection can still
show exact body conflicts independently and never changes static score.

## Concrete counterexamples and tests

- Orbit25 node0 is four separate pure three-cycles across25/26/27/30, L=56,
  n_o=3 each, N=12. The exact prescribed score is64.815891 for each. It therefore
  becomes Other with candidates, even though its declared Star is certified.
  This is a consequence of the user formula, not a reason to silently add a
  Star bonus. Orbit33 node0 has L=56,N=3 and score83.565891 if pure; it qualifies.
- Orbit25 node284 remains Mixed on25 under the fixed chart, while its node0
  counterpart is PurePosition. Node0/inverse284 has a genuine PureOrientation
  component. A raw expansion must agree with the corresponding Star action.
- Primitive1 supplies multiple position cycles on trivial-orientation orbits;
  they must stay PurePosition. `[2,1200]` exercises counts beyond100 displayed
  cycles and160 slot pairs. Composition must not inherit component classes.
- Independent arithmetic tests cover 70, best-minus10, maximum-three and ties;
  Unknown, identity, cost changes without net changes and overlap counted once.
- Imported forged proofs/metadata and stale model/frame versions do not produce
  scores. Protection/state/camera changes leave static facts unchanged. Neither
  computing nor sorting selects a macro, changes Current/Next, or edits a draft.

Existing tests asserting recipe-relative purity or unconditional Star-use
membership are intentionally superseded. The coordinator must update those
integration expectations and native captions together with the adapter; do not
leave the old passive classifier wired beside the new one.

## Implemented evidence

PROOF_VERSION is 3; USE_SCORE_VERSION is `static-use-v1`. Formula arithmetic and
threshold/band comparisons use exact Fraction values internally; detached JSON
contains numerical contributions and scores as floats. The original expanded
primitive count is never reduced by cancelling words. All remaining affected
orbits remain candidates rather than disappearing from Current-orbit browsing.

Fresh command after the legacy function was removed:

```text
python -m unittest discover -s tests -p 'test_use_score.py' -v
```

Exit 0, 10 tests in 20.899 seconds. Real model checks cover the stated O25/O33
counterexample, fixed-chart raw/Star/inverse equivalence, multiple cycles beyond
display truncation, stationary orientation, noncommutative compositions,
overlap counted once, added cancelling-word cost, and Unknown retaining known
position evidence. Isolated arithmetic/policy tests cover 70, best-minus-10,
max-three and deterministic ties; these synthetic policy inputs are not claimed
to be legal solve states. Static results do not change with body-protection
context. Stale proof/frame versions remain Unchecked.

- `macro_use.py`: `FACE75A4CCDE0B71B5FDB5A4FE2D8D8A7D0787ADED8BD068197DD966EDA43DE6`
- `tests/test_use_score.py`: `31619A20563F81D89CF1EB203C2E69A89A1F1E95F029C55D2CD5032E661B9CB2`

Native integration must expose the new distinction: `orientation_count` includes
both moving and stationary orientation effects; `fixed_orientation_count` retains
its narrower stationary meaning. Neither Unknown nor a missing count becomes 0.
CurrentScore, ReviewContext goal evaluation and final-buffer invariants remain
outside this slice. These author-run tests are not native or human acceptance.
