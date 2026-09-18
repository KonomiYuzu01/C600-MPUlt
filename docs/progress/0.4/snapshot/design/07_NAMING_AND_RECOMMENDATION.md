> Archived development document, published 2026-09-18. This is a stopped, unreleased 0.4 snapshot, not a new instruction to resume work. Historical status and failed results are retained; the current status is in the [progress index](../../README.md). Local paths are anonymized; raw evidence and runtime files are not bundled.

# Confirmed naming and recommendation contract — 2026-09-16

Latest explicit user instructions. This amendment supersedes the former single-cycle-only Pure definition and nonnumeric use inference. It does not authorize setup search, automatic macro composition or execution. Extend existing mathematical_names.py, effect analysis and full protection services. Implementation and verification are pending; this document is not evidence that the proposed signature has a particular census outcome.

## Exact names and three distinct group actions

Geometric symmetry describes the geometry; the legal turn group determines actual piece orbits; H_o is the census orientation group for an orbit. Equal H_o does not imply equal legal orbit. A geometric symmetry is not automatically a legal executable turn.

For position p: Host(p) is the complete hosting-cell set; M(p) is the complete affecting-cap set; k=|Host(p)|; m=|M(p)|. Read H_o from census, never infer it from sticker count. The project rank is m−1, not a geometric dimension.

Using the existing reference cell, verified cell frame maps F_h, and its twelve proper rotations R≅A4, compute:

Signature(p) = min over h∈Host(p), r∈R of
( sorted(r F_h^-1 Host(p)), sorted(r F_h^-1 M(p)) ).

Both sets undergo the same map. Integer IDs are exact computational coordinates, not display names. Freeze and version the reference frame and ordering; camera, selection and load order cannot change the result. Validate the Signature-to-moving-orbit relation against the actual census, without assuming it is bijective. Fixed centers retain independent identities and are not merged into a movable orbit.

Full orbit name: k stickers / m cap domains · H_o orientation · structure α.
Compact: kS·mC·H_o·α.

Use α, β, … only to distinguish full Signatures sharing counts and orientation group. Assign by fixed full-Signature order. These symbols do not mean chirality, inner/outer layers or direction. Provide genuine structure thumbnails at the same reference frame and scale; selecting the type symbol compares actual cap membership.

For each structure class, select canonical representative region u0 among the reference cell's 433 real sticker regions. Enumerate the twelve proper rotations. Fix generator order, e.g. H,T,T^-1; BFS yields shortest, lexicographically first equal-length word w such that w(u0)=u. Verify the actual region permutation. Stabilizers can yield multiple words for one region; retain one canonical word rather than counting new positions. Check complete unique coverage and inverse parsing of all 433 regions. If the prescribed classification cannot support this, report the exact counterexample before changing its meaning.

PositionAddress = CellAddress / StructureClass / AddressWord.
Confirmed correction after the exact census probe: the 433 reference regions have 42 anchored A4 components. Six full-Signature classes each have two components of twelve, so one representative per full class covers only 361 regions. The user explicitly approved a canonical u0/u1 representative for each anchored component while keeping the same StructureClass and legal orbit. Where necessary the final address component is `u0:word` or `u1:word`; this is local-address disambiguation, not chirality or a new structural alpha/beta class. The fixed ordering and shortest-word requirements apply within each component. Verify unique 433-region coverage and distinguish region/slot addresses from a multi-host physical position's canonical anchor.
CellAddress uses existing golden-ratio coordinates in a fixed geometric reference. Choose a fixed canonical anchor for multi-host pieces and expose other hosting cells on expansion. AddressWord is a geometric address, not a suggested Twist sequence. A compact structural thumbnail may replace its expanded word visually; expansion/copy/parse must recover exactly the original canonical object. Barycentric coordinates, percentages and screen direction are auxiliary only.

PieceIdentity = StructureClass + HomeAddress, invariant under moves.
Current = current PositionAddress.
Orientation = an actual H_o element in the explicit reference.

Display Object / Home / Current separately. Preserve canonical IDs, saves and old inputs. Include namingVersion and model version; display names are not database keys. C2/C5 can use powers of fixed generators. D5/A5 require verified generator words or exact slot permutations, never one angle or merely element order.

## Exact effect dimensions

For every orbit store position permutation π_o(i) and orientation increment a_o(i) in an explicit coherent transported frame. Chronological left-then-right convention:

(π,a) followed by (ρ,b): π_result(i)=ρ(π(i)); a_result(i)=a(i)·b(π(i)).

Preserve noncommutative order. Validate against full sticker replay.

- No effect: π=id and every a(i)=id.
- Pure position: π nontrivial and every a(i)=id; multiple disjoint cycles allowed.
- Pure orientation: π=id and some a(i) nontrivial.
- Mixed: both position and orientation are nontrivial; additionally identify whether their support sets overlap.
- Unknown: insufficient reference or complete-action evidence.

Pure encompasses pure position and pure orientation. Exactly one n-cycle is an additional structural tag. Recompute a composition, never inherit purity from components. Star retains its existing exact certificate criterion.

Graphical view: directed edges for position cycles; in-place marks for fixed-position orientation-only action; orientation changes marked on moving edges. Every object links to faithful Local geometry and its mathematical address. Retain the new cycle view, original real geometry and names together. Maximize useful graphical area and minimize prose/empty windows.

## Two separate ranking layers

Evaluate only existing macros and explicitly chosen reference variants. Exact certificate per macro revision contains full legal original sequence/version, per-orbit position/orientation effects and frame version, cycles, all orientation support, Star status, complete affected scope and original primitive cost.

A_o={i : π_o(i)≠i or a_o(i)≠id}; n_o=|A_o|; N=Σn_o; L=actual expanded primitive count. Count each piece once even if it moves and reorients. No unproved sequence reduction for cost or protection.

Static use, independent of scramble:
T_o=1 for verified pure position/orientation, 0.5 for verified mixed.
J_o=n_o/N; C_o=1/(1+(n_o−1)/4); E=1/(1+L/30).
UseScore(o)=100·(0.40T_o+0.25J_o+0.20C_o+0.15E).

This is an explainable heuristic, not mathematics or human intention proof. Star, cycle length and H_o remain exact labels without double-counting the same purity evidence. Initial labels: at most three affected orbits with score≥70 and within10 of the maximum. All collateral remains inspectable. No qualifying score: Other / use undecided, retaining top candidates and reasons. Incomplete reference: Awaiting verification; unknown never becomes zero orientation. User labels remain independent. Centralize/version all weights and thresholds; calibrate only from actual use evidence, never call the score a success probability.

Dynamic ranking uses a copy simulation of the explicitly selected complete Prepare / Macro / Cleanup. A body's static use does not certify a composition. Hard gates: complete legal witness, definite reference map, current state/preview versions, all active orbit/piece protection, and selected net/strict-prefix policy. Conflicting entries stay in Macro Base but are excluded from executable recommendations. Missing evidence gets a separate pending-check group.

For passing complete operations:
F = +1 for Focus goal unsatisfied→satisfied, −1 satisfied→unsatisfied, otherwise0.
G_p = net reduction of target-orbit position errors / max(1,n_o).
G_theta = net reduction of target-orbit orientation errors / max(1,n_o).
B = net increase in met explicitly selected block requirements / max(1,total requirements), or0 without selected block.
D = previously exactly correct, now broken unprotected affected pieces / max(1,N).
M = 1 when chosen buffer roles, reference and target mapping exactly match, otherwise0.
Cost=L/(L+30).
Clamp G_p,G_theta,B to [−1,1]. Orientation diagnostics use fixed-version coherent transported frames; displaced pieces' orientation diagnostics do not establish solved status. Solved always requires full identity, position and sticker correspondence.

CurrentScore=100·(0.35F+0.25G_p+0.15G_theta+0.10B+0.10UseScore/100+0.05M−0.20D−0.05Cost).

Negative values allowed. Group first: directly beneficial (any Focus/position/orientation/selected-block improvement); potential preparation (no immediate gain but related structure); blocked/pending verification. Then score descending, damage ascending, primitive cost ascending, stable internal key. Do not hide macros just because they do not immediately increase solved count.

At most three concise reasons per recommendation, derived only from measured contributions; synchronously highlight the actual objects. No language model may invent a reason or simulation fact.

## Cache and acceptance

Static certificate keys bind macro content/model/frame versions. Dynamic keys additionally bind state/protection/goal/block versions. Relevant changes invalidate old previews. Use complete support, including hidden regions; strict protection checks execution prefixes, not just net action.

Mandatory evidence: four coarsely equal orbit groups get distinct verified structure names and visible real differences; all433 region addresses complete/unique/reversible; stable Home identity through moves/undo/resume with correct Current; pure-orientation classification/ranking/in-place marks; noncommutative composition equals full replay; high-score protection conflicts excluded from executable recommendations; Focus/stage changes affect ranking appropriately; cycle graph, real Local/puzzle and names link both ways. Finish with continuous solver-operation recording. Mathematical facts, heuristic ranking and execution permission stay visibly separate.
