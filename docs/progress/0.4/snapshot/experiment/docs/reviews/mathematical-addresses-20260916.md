> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Mathematical address implementation — 2026-09-16

This implements the confirmed `07_NAMING_AND_RECOMMENDATION.md` address rule,
including the user-approved u0/u1 distinction within six full StructureClasses.
Canonical model IDs, assets, operations and saved identity mappings are unchanged.

## Interface and scope

`MathematicalNames` keeps `cell`, `orbit`, `slot`, `address`, `piece_record` and
`incidence_signature`. New `parse_slot`, `parse_address` and `parse_identity`
resolve distinct typed copied addresses to canonical region slots, fixed positions
and Home-based physical identities respectively. Copies bind
`incidence-address-v1` and the immutable model ID. Emitters use the existing fixed
hosting anchor. The parser can resolve another exact hosting-region address for
the same position; it never guesses a type or current occupant.

All 433 reference regions have unique shortlex H/T/T^-1 words within their
anchored A4 component. These are geometric addresses, not executable setup words.
The 42 components preserve 36 full classes, including the fixed-center class.
The six double-component moving classes are canonical orbits 9, 14, 16, 20, 23
and 30; each has 12 regions in u0 and 12 in u1. Alpha/beta is reserved for the
four pairs with equal counts and orientation group, ordered by full Signature.
The actual census groups remain trivial, C2, C5, D5 and A5.

Primary names no longer use barycentric coordinates. Approximate coordinates
remain available as secondary data and are not orientation certificates.
`identity_lines` / `current_lines` remain two lines: exact pole, then compact
class and local word. Maximum measured line lengths are 20 and 23 characters;
the maximum joined short name is 46 characters and the longest word has three
symbols. Typed/versioned/model-bound copies are separate from compact labels.

Native address copy/input integration belongs to the integrating agent. This
module check does not establish that a native control displays or accepts the
new format. No native UI, GPU or human-solving trial is claimed here.

## Fresh verification

Command from the experiment directory:

```text
python -m unittest discover -s tests -p 'test_mathematical_*.py' -v
```

Exit 0: 18 tests in 32.717 seconds. This covers all 433 unique region addresses,
all 177,120 unique position address roundtrips, distinct hosting slots for
1/2/5/20-cell structures, independent shortlex enumeration, all 600 fixed-center
identities, invalid type/model/version input, and a legal real-Session move with
commit, undo, redo and resume. Home identity remains stable; Current follows
the actual piece. All prior naming checks remain, except the obsolete assertion
that an orbit caption contains lambda was replaced with actual structure-symbol
and no-lambda checks.

The preceding feasibility probe checks exact integer Host/M covariance across
259,800 slots; see `orbit_signature_feasibility_20260916.md` for its precise
scope. Golden-coordinate reconstruction remains a checked floating-source
display mapping, not a formal exact-arithmetic proof of the source geometry.

## Measured initialization cost

Five sequential headless constructions reused one real Model. Other native
activity could contend for resources. Constructor milliseconds:
800.649, 615.554, 543.754, 556.021, 569.034 (median 569.034).
The measured `_build_addresses` portion was 83.330, 68.834, 74.060, 85.270,
73.850 ms (median 74.060). This includes only the new bounded reference-region
address dictionaries, not a giant full-position-name cache.

No byte-identical old source was available. These are current-constructor and
incremental-stage measurements, not an old/new benchmark or a speedup claim.

## Frozen inputs

- `mathematical_names.py`: `2607D1C1A37CE0E893231E79DF18002C06FE700CF51EF82F5E1BD1F856224918`
- `tests/test_mathematical_addresses.py`: `76CE9F8CE3569CBBCE4886F139C400DC8B60B67BEFFAD8714A7C6C89E87922FC`
- `tests/test_mathematical_names.py`: `07C4299B4A0375BE0AEFEC86AD3CB0564998D9D5A64974D8B8DEDD4DEEC4EBB0`

Independent review and native integration are separate from these author-run
checks. Further effect-classification and ranking changes require the coherent
frame contract; this address change does not certify new Pure or UseScore claims.
