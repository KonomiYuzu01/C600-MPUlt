> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Coherent orientation chart: bounded implementation plan

Status: read-only feasibility established; the bounded standalone helper and its
tests are now implemented. Existing application adapters and UI are not wired to
it. Static application classification, scoring and endgame integration are pending.
The coordinator accepted the deterministic retained-frame convention below under
the user's fixed/versioned coherent-reference requirement. New residual and
orientation goals must consume this chart through the unified ReviewContext,
not establish another Session or source of state truth.

## Fixed convention and its consequence

For each moving orbit, retain the atlas's ordered frames at A and B. At every
other fixed position, choose the first frame in that orbit's immutable execution
tree. Bind this to the model and `retained-first-frame-v1`. This scans static
certified data; it neither searches a setup for a target nor chooses a macro.
The selected frame is independent of the current labels, camera, Grip, selected
macro, work goal and user reference. Fixed centers retain their sole slot.

The chart defines zero orientation, not an intrinsic physical direction.
Changing the chart can change a moving edge's orientation increment and its
PurePosition/Mixed label. This is why the frame version must be visible in
details and bind every certificate. A current Local/Grip reference can describe
or transform a chosen operation but cannot silently redefine this chart.

Concrete evidence: orbit 25 node 0 is PurePosition in this convention. Node 284
has the same three moved positions but two nonidentity orientation increments,
so it is Mixed. Both can remain independently certified retained Stars. Their
different target frames must not let each tested macro choose its own zero.
The sequence node 0 followed by inverse node 284 is PureOrientation on orbit 25;
collateral three-cycles on other orbits remain separately reported.

## Exact representation and chronological convention

Let F_p[j] be the selected ordered slot at fixed position p and T the complete
source-to-destination slot permutation. Require all T(F_p[j]) to belong to one
position pi(p). Then:

```text
T(F_p[j]) = F_pi(p)[a_p[j]]
```

Store a_p as an actual small slot permutation in the verified concrete H_o
representation. Never reduce D5/A5 to an angle or element order. For A followed
by B, the implementation's existing `core.cp(a,b) = b[a]` gives:

```text
pi_AB(p) = pi_B(pi_A(p))
a_AB,p[j] = a_B,pi_A(p)[a_A,p[j]]
```

This is the user's chronological `a * b` convention. Reversing the array
composition order is wrong for noncommutative orientation groups.

For actual Session orientation, invert the authoritative label array to obtain
Home-slot-to-current-slot T. Decompose that exact mapping in the same chart.
Home identity, current position and orientation are separate. Nonidentity
orientation on a displaced piece is diagnostic only; solved status still uses
the full exact identity/position/sticker predicate in PuzzleState.
This state decomposition pi maps Home to Current. The contract's Current graph
maps Current to Home, so it must consume inverse(pi); it cannot reuse the
Operation graph's forward arrow direction. A legal non-involutive three-cycle
is the required direction regression. A correction element drawn on the
Current-to-Home edge at current position pi(p) is inverse(a_p), not the observed
forward increment a_p. The latter remains a separate actual-orientation
diagnostic; this ownership relation is not a legal suggested correction move.

## Available verified data and evidence

`core.py` exposes authoritative `slots`, `sp`, `oid`, atlas ordered frames,
execution-tree parents/moves/frames and legal source-to-destination generators.
`certify_seed` verifies the atlas's A/B/third-frame transport; `star_net` verifies
each requested transported star. The new probe additionally checks every stored
tree edge directly rather than inferring correctness from field names.

Run `python tests/probe_coherent_frames.py`. The actual headless run exited 0 in
23.794 seconds. It created no Session and changed no application state.

- All 224,164 retained frames contain exactly the slots of their stated position.
- All 224,129 nonroot parent-to-child edges equal the recorded legal generator's
  exact ordered-slot transport.
- All positions are covered by the atlas buffers and retained tree frames.
- At every nonbuffer position, relative frames give the same concrete permutation
  group for its orbit, with the census order, identity and closure verified.
- Complete 259,800-slot reconstruction passes for the identity, a real primitive,
  the stated orbit-25 witnesses and explicit D5/A5 noncommutative witnesses.
- D5 witness: orbit 6, nodes 54 and 136 at position 29398. A5 witness: orbit 34,
  nodes 16 and 20 at position 17810. Each witness pairs node 0 with an inverse
  alternative, then composes those orientation operations. Reversing their order
  changes actual target-orbit slots. Both the position/orientation formula and
  full chronological sticker replay agree for the tested order.

The probe does not independently replay all 1,200 primitive generators or every
possible legal word. It uses existing immutable-asset validation. The complete
primitive-to-chart membership check remains an implementation acceptance test.
The group labels originate in the retained census; the probe verifies their
concrete representation and size, not a new classification of abstract groups.

Chart storage in the probe is 1,558,800 bytes (one int32 ordered-slot array and
one int16 slot-to-local-index array). Static metadata and Python object overhead
are not included. Zero-frame SHA256:
`8e8de2cf164b7dae5610f27de087043d7511a13aac628551c1f33ceca0fbf608`.

Probe source: `B9D88C19AB0F9E1FBC74CC58EA1796841AFC8D2366A91016B99736770324050B`.
Result: `5FF3AEBA950AE0489E6CF86D577083347620F7A37A9C148628A1756F270AB281`.

## Small implementation slices

1. Add one bounded chart helper, proposed `transported_frames.py`, with model /
   frame-version binding, per-orbit checked construction and cancellation. Reuse
   the model's immutable arrays and seed certificate. Store no current state.
   Missing, conflicting or outside-group evidence produces Unknown; cancellation
   publishes no partially certified orbit. Test full primitive membership,
   complete mapping reconstruction and the noncommutative witnesses above.
   Full primitive membership means all 1,200 primitives, every affected position,
   including buffer positions and the fixed-center boundary. Check that a piece
   never splits, remains in its legal orbit, and every local permutation belongs
   to the certified group. Negative tests cover missing/conflicting frames,
   outside-H orientation, cross-orbit/non-permutation input, and cancellation
   followed by successful retry. Invalid permutation input is rejected before
   publishing a classification. Missing orientation evidence cannot discard
   separately established full-slot/position facts or become an identity element.

2. Extend the existing effect analysis before display truncation. Per orbit,
   record exact position and orientation supports, their union and overlap,
   cycles and the frame version. Classify NoEffect, PurePosition,
   PureOrientation, Mixed or Unknown. Multiple disjoint cycles may be PurePosition.
   A one-n-cycle tag remains separate. Recompute every composite. Retain existing
   Star certificates and original recipes; do not reinterpret an old imported
   proof as this new certificate. Cache keys bind exact recipe, model and chart
   version. Update bounded cached summaries and facet consumers together.

3. Replace the old heuristic in `macro_use.py` with the confirmed versioned
   UseScore formula only after step 2. Use complete affected-piece unions and
   actual expanded primitive cost. Preserve raw facts and each contribution.
   NoEffect avoids division by zero and has no affected-orbit recommendation.
   Missing orientation evidence is Awaiting verification, never numeric zero.
   Threshold/multiple-membership tests include exact boundary values, collateral,
   identity, preparation-use retention and unchanged static scores after state
   or protection changes. Imported tags remain independent human metadata.

4. Feed actual-state decomposition into the unified versioned ReviewContext and
   orbit residual service. That service owns the Current/Operation/After
   boundaries and new Prepare/PlacePiece/OrientPiece/FinishBuffer/FinishOrbit
   predicates. Reuse the existing full-operation copy simulation and exact
   protection/prefix checks. Goal satisfaction and permission remain distinct.
   Do not let the new diagnostics replace PuzzleState.correct, the Session
   journal, its commit validation or exact protection locks.

5. Add CurrentScore only as a consumer of a current complete review, explicit
   roles/reference and residual/goal facts. Hard protection/currency/reference
   gates precede ranking. Versioned weights and measured terms remain inspectable.
   Missing invariant or residual evidence must remain Unknown/FrameUnknown; the
   chart alone does not certify endgame parity or abelianization constraints.

The exact new dependency is
`outputs/magic600-v0.4-design/08_INTEGRATION_AND_ENDGAME.md`: ReviewContext binds
Session epoch/revision, model/naming/frame versions, active orbit, Current/Next,
roles/intent/goals, chosen macro revision/complete recipe and protection revision.
Camera changes do not invalidate these facts. The chart helper is only an
immutable model service and cannot own any of those changing inputs.

That contract's P_nonbuffer/P_buffer count Home-position residuals; its
O_nonbuffer/O_buffer count exact sticker differences only when identity is
already at Home. Displaced orientation increments remain a separate diagnostic.
ExactSolved compares every orbit label. A missing chart can block element-level
comparison without erasing the independently known exact-label mismatch.
Position-only preservation and complete-label preservation must remain separate
requirements in the caller. The initial helper/classifier slice will not claim
to implement these goals, endgame permission, automatic Next/protection, per-orbit
parity, C2/C5 invariants or D5/A5 final-buffer correction coverage.

Native changes must display the same chart and effect facts without generating
new zero frames from a chosen macro. Native rendering, full endgame coverage,
new recommendations and human use have not been tested by this read-only probe.

## Implemented helper and fresh checks

`TransportedFrames(model)` owns no Session. `ensure(orbit)` checks and publishes
one complete independent chart, returning a detached certificate. `frame(position)`
returns the ordered canonical slots. `decompose(source,destination)` first
validates a complete slot permutation, no split/cross-orbit piece and unchanged
fixed centers. Its internal result contains:

- `position_map`: full NumPy source-position-to-destination array.
- `affected_positions`, `changed_slot_count` and `unchanged_orbits`: exact support.
- `orientation_elements`: sparse nonidentity slot permutations keyed by source
  position; `orientation_unknown` lists positions whose element is not proved.
  Missing dictionary entries mean identity **only outside that unknown list**.
- `orbits`: complete per-affected-orbit position, orientation, union and overlap
  counts with classification and explicit reason. Unknown orientation/overlap
  counts are None, with separately labelled known partial counts retained.
- `model`, `frame_version`, `frame_certificates` and `orientation_complete`.

These are internal model results, not JSON endpoints, editable references, setup
recipes or execution permits. The caller still owns exact state/version binding.
No effect requires no chart: a complete identity action has empty support and
all moving orbits listed as unchanged. Unavailable/conflicting chart or an
outside-H element leaves the verified slot/position facts intact and reports
Unknown. Invalid permutation, piece split, cross-orbit action or fixed-center
motion is rejected before classification.

Fresh command:

```text
python -m unittest discover -s tests -p 'test_transported_frames.py' -v
```

Final exit 0: 10 tests in 171.454 seconds. Every one of the 1,200 positive legal
generators was decomposed and reconstructed across all 259,800 slots. This covers
every retained buffer position and keeps all fixed centers intact. Separate
tests cover an inverse Star, actual D5/A5 noncommutative compositions, and a
non-involutive actual-state three-cycle proving the Current graph must invert
the Home-to-Current position map. It is not claimed that all 1,200 negative
generators were separately decomposed.

Negative cases include missing and conflicting frames, an actual whole-piece
slot permutation outside the certified group, wrong types (including mixed
Boolean/integer lists), non-bijection, split pieces, cross-orbit transport and
fixed-center movement. Controlled cancellation inside chart validation publishes
no partial chart; retry succeeds. Identity analysis leaves actual labels unchanged.
The initial missing-module red test preceded implementation. A later targeted
red test exposed NumPy's Boolean/integer coercion, and the final source rejects it.

- Helper SHA256: `CCBBF01B6C73EAF1A391B4FE6BE8F2DE215CC0019A5D7A40F9D08B108FF4BBE3`.
- Test SHA256: `5417DD099715AC324AD1E2C558F633191D74960D66E066B4C7E2168332E822E9`.

A separate headless timing of primitive 1 decomposition measured 8265.231 ms
on the first call, which certified all 35 affected orbit charts. Three warm
calls measured 41.760, 19.239 and 18.168 ms. This excludes net computation,
Session, native response adoption, rendering and device latency; shared host
contention was uncontrolled. First-time certificate construction must remain
an explicit cancellable analysis rather than implicit library snapshot work.

These are author-run headless model checks. Independent implementation review
and all native/UI integration remain separate; passing them does not certify
UseScore, residual goal logic, endgame invariants or complete 0.4 functionality.
