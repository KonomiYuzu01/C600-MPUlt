# Magic 600 Cell 1.0 outlook

**Status: planned direction, not a released feature set. Updated 2026-09-16.**

Magic 600 Cell, formerly C600 Studio, is being developed to make a human-directed full 600-cell solve more practical. Version 1.0 is intended to rebuild the presentation, rendering and delivery of the application around that purpose, while preserving its mathematical foundation and accumulated solving work.

This document records the current 1.0 direction and proposed release criteria. It supersedes earlier 1.0 planning assumptions that require Intel HD 620 qualification or a dedicated D3D11 engine. It does not change the behavior or requirements of existing releases. GUI selection, implementation and release acceptance remain separate decisions; there is no announced release date.

## 1. What should change

The principal change is a graphical solving environment in which pieces, positions, orientations, cycles and protection are the main objects of interaction. A solver should be able to inspect an object, understand its role in the current plan, prepare a finite operation, review its full effect, and apply it without repeatedly reconstructing context from unrelated lists.

For example, selecting a piece in an orbit cycle should identify its current and home positions, locate it in the real Local geometry, show the relevant orientation reference, and carry that same selection into preparation and operation review. After execution, all views should agree on the new committed state. A protection conflict should be visible on the affected objects, with a concise explanation.

The intended audience is hypercubers, people investigating high-dimensional puzzles, and researchers or builders interested in mathematical interaction. The first release need not become a general-purpose 3D editor or a universal puzzle platform.

### Confirmed direction and open decisions

| Area | Direction | Still to establish |
| --- | --- | --- |
| Product purpose | Human-directed Orbit First Block Building Solving of the full puzzle | Measured improvements in preparation, verification and sustained use |
| Frontend | Substantial redesign around interactive graphics, linked views and compact tools | Final layouts and native interaction quality |
| Architecture | Replace the legacy presentation/runtime boundary while preserving useful engine and workflow contracts | Exact GUI stack, rendering implementation and migration sequence |
| Development platform | Windows first, using a Legion-class Ryzen 9 7945HX / RTX 4070 Laptop / 32 GB development machine | Published minimum requirements and measured performance |
| Later platforms | Keep a credible Linux and macOS route | Native implementation, packaging and testing on each platform |
| Rendering API | Reconsider a dedicated D3D11 engine; no API is mandatory | Selection from comparable, working prototypes |
| Release scope | A coherent, independently usable Windows application | Completion of the acceptance gates below |

Intel HD 620 is not a 1.0 qualification target. The development machine is not a claim that every user must own that configuration, nor evidence that any particular frame rate has been reached. A 240 Hz display does not establish a 240 FPS product requirement.

## 2. Rebuild boundaries, preserve mathematical meaning

Keep one authoritative mathematical state and one transaction/journal owner. The existing Python Model, PuzzleState and Session are the starting point for reuse, together with legal-operation witnesses, protection, preview/commit, undo, checkpoint and recovery semantics. Changing language everywhere is not a prerequisite for a substantial renewal.

The full model remains the contract: 600 cells, 433 labelled sticker slots per cell, 259,800 slots, 177,120 pieces, 35 moving orbits and 1,200 legal generators. Display filters, animation and simplified context views must not change these identities or the mechanical state. A genuine change to geometry, cuts, numbering or frames requires a new model identity and explicit migration.

| Keep or adapt | Replace or decouple |
| --- | --- |
| Mathematical model, canonical identities and certified finite operations | Dependence on the old WinForms/MPUlt viewport as the UI host |
| User-selected goals, buffer preparation, macro composition and protection | MPUlt-specific handshake and numbering assumptions in the frontend transport |
| Mathematical names, reference frames, effect analysis and useful scoring | Window, input, painting and GPU resource management tied to the legacy host |
| Session persistence, journals, import/export and recovery contracts | The requirement for .NET 3.5 / legacy Managed DirectX in the new launch path |
| Existing reproducible tests and solving examples | Packaging that assumes the developer's machine or legacy installation |

A renderer-independent boundary should deliver immutable geometry plus versioned state and presentation updates. Selection and commands use canonical identities; display names do not become database keys. Related views consume a consistent committed state and review context. Stale previews and delayed GPU picking results must be rejected when their state, camera or view context no longer matches.

The renderer may interpolate a legal move for display. It does not decide what move is legal, update the mathematical state independently, or grant permission to execute. The user continues to choose targets, methods, constraints and execution. Automatic setup search, autonomous macro choice and bulk solving are outside this direction.

## 3. A frontend built around graphics

### Linked views with distinct jobs

- **Abstract solving view:** actual piece cycles, position/orientation relationships, active orbit, buffers, Current, explicitly locked Next and protection. This is the main working surface.
- **Real Local view:** the original 433-sticker cell structure and a mathematically defined neighborhood, including the actual 20-cell neighborhood of a selected vertex. Abstract diagrams do not replace this geometry.
- **Global view:** the whole puzzle, progress and context. Full detail remains an explicit capability to test; a coarse overview may be a useful display choice, never an undisclosed replacement for a full-detail claim.
- **Cycle and orientation review:** Current residual structure, the complete Operation effect and predicted After state. Position-only, orientation-only, mixed and unverified cases remain distinct. A composed operation is classified from its actual result, not from the labels of its components.

Mathematical naming, original puzzle geometry and the new cycle graphics are independent capabilities and must all remain available. Selection links them through canonical identities.

### Workspace and tools

Retain a full-screen workspace. Global, Local, Solve tools and the Onscreen Keyboard should be usable as compact panels and, where useful, separate native windows. Internal docking and a real operating-system window are different capabilities and need separate acceptance tests.

Windows should cooperate: preserve the main graphics area, keep Current/Next and protection legible, recover useful placements after monitor changes, and offer an explicit layout reset. Closing a view must not discard an operation draft or a solving session. Camera position, Follow/Pin behavior and selected objects need clear ownership.

Use the existing command layer for physical keys, Onscreen Keyboard and visible controls. Solving operations retain editable single-key sets; general commands follow their declared shortcut policy. Grip and Twist labels must reflect the active local frame and fit the key surface. Pressed keys, latched Grip, rejection and focus are separate visual states. Text editing and IME composition must not trigger puzzle commands; losing focus must clear held input.

Local-center selection should be graphical and searchable, with the corresponding keyboard route. A solver must be able to change the working center without losing the selected target, the meaning of the reference frame, or a pending draft.

### Visual character

The desired character is a precise mathematical instrument with the spatial clarity of a science-fiction interface. Achieve this through geometry, typography, hierarchy and meaningful motion. Use restrained surfaces, compact controls, readable mathematical symbols and color that consistently communicates state. Avoid filling the workspace with decorative cards, repeated captions, oversized empty panels or ornamental effects that compete with the puzzle.

Labels stay short where actions occur; detailed mathematics, assumptions and recovery guidance remain inspectable. Accessibility includes alternative color cues, visible keyboard focus and readable scaling. A new framework does not by itself establish visual quality.

## 4. Rendering and GUI selection

Evaluate complete combinations of controls/text, windows/input, GPU rendering and engine transport. The current first prototype candidate is **Rust + eframe/egui + egui_dock + wgpu**. The main comparison is **Qt Quick + QQuickRhiItem/QRhi**. **Avalonia with custom GPU content** remains an alternative if its interaction and maintenance benefits justify the rendering integration work. These are recommendations, not locked architecture decisions.

Hyperspeedcube is a useful reference for integrated hypercubing interaction. Its inspected source combines egui/eframe, docking and wgpu with project-specific UI and rendering code. Reuse must be assessed component by component; using the same libraries does not reproduce its product automatically. [Hyperspeedcube source](https://github.com/HactarCE/Hyperspeedcube/blob/63737aa467ed8eccacafaae3eec97cf1f980b794/Cargo.toml)

wgpu offers a native route through D3D12/Vulkan on Windows, Vulkan on Linux and Metal on macOS. Qt's rendering interface offers another multi-backend route, with a Qt-version maintenance obligation. Neither route makes later platforms tested by default. [wgpu platform support](https://github.com/gfx-rs/wgpu#supported-platforms), [Qt custom rendering](https://doc.qt.io/qt-6/qquickrhiitem.html)

First compare the same real Local geometry, detached keyboard window, mathematical text, input focus and versioned selection. Then compare transparency, full detail, multi-view load and prolonged use. Prefer Qt if both meet graphics targets and Qt requires substantially less platform/input/window repair. Prefer the Rust route if it produces the desired interaction and rendering with a maintainable implementation. A dedicated D3D11 engine needs a demonstrated project benefit to justify its future backend costs.

Own the puzzle-specific projection, clipping, shading, selection and animation. Let an established graphics layer handle the underlying devices and API adaptation where practical. Do not begin by building a general game engine or a speculative plugin system.

### Rendering acceptance questions

- Can original sticker structure be read and selected at useful scales?
- Do transparency, depth and picking agree, including filtered, hidden and annotation-only objects?
- Does a precise integer selection target resolve to the expected canonical identity? Range checks alone are insufficient.
- Can geometry be retained on the GPU while only changed state is uploaded?
- Do multiple views share appropriate resources without coupling their cameras or duplicating expensive analysis?
- Are resize, minimize/restore, device recreation and render failure handled without modifying or losing puzzle state?

Measure warmed CPU and GPU costs, presentation intervals, input response, memory and idle behavior on the actual development GPU. An initial 60 Hz working-view target at 2560x1600 is proposed for comparison, not promised performance. Establish a separate full-detail and transparency budget from measurements. A single cold offscreen submission, or a successful triangle demo, cannot answer these questions.

## 5. Carry the solving method through to the endgame

The 0.4 workflow is a foundation to finish and preserve. Version 1.0 must connect it through the new interface, including the difficult last stages of an orbit rather than only visually attractive early moves.

The intended loop is: choose an orbit and target, inspect true position/orientation residuals, prepare a user-selected finite operation, review references and complete collateral effects, check protection, explicitly apply, inspect the new state, and continue from the preserved workspace. Suggested macro usefulness and execution permission remain different concepts; an unknown orientation or reference must not be displayed as solved or scored as zero error.

Endgame presentation must distinguish non-buffer placement, non-buffer orientation, buffer completion and exact orbit completion. Allow the workflow to return to an earlier stage when collateral effects require it. Validate the applicable permutation and orientation constraints, including noncommutative orientation groups where relevant. An invariant check diagnoses a condition; it does not by itself construct a solution or prove reachability.

Cross-orbit work retains each orbit's draft, reference, useful Local setup and target context while applying protection globally. Current, locked Next, hover and mechanical protection are separate. Carry forward the already-confirmed automatic focus/protection transitions; do not add silent execution or silently select a new solving strategy.

The acceptance set must cover all 35 moving orbits with legal states and finite witnesses, including buffer and orientation residues, protection conflicts, undo and recovery. Fresh UI evidence must show that these capabilities are accessible in practice, not merely available through an internal API.

## 6. Other ideas, with scope limits

The following proposals extend the direction. Their status is deliberately distinct from required launch work.

| Idea | Proposed place | Why it matters / boundary |
| --- | --- | --- |
| An inspectable operation receipt linking target, name, reference, full effect, protection and journal event | 1.0 core review experience, reusing existing evidence | Makes a preparation understandable and replayable without inventing a second log authority |
| Guided first-use practice on a legal full-model fixture | 1.0 onboarding | Lets a new user locate a piece, inspect orientation, review one operation and reopen saved work without an entire solve |
| Contextual explanations of mathematical names and orbit structure | 1.0 Help and inspection | Supports learning without burying expert controls under permanent text |
| Saved workspace layouts, cameras and pinned references | Basic restore/reset for 1.0; richer presets later | Supports long sessions; persistent UI state must never overwrite mathematical state |
| Clear progress summaries and exact completion feedback | 1.0 | Separate exact solved state, placed pieces, orientation residues and unknowns; reset/import is not a newly completed human solve |
| Read-only session comparison, replay and exportable teaching examples | Candidate follow-up | Useful for research and demonstrations; keep source sessions intact and disclose replay provenance |
| Adaptive display budgets and optional compute acceleration | Later, only after profiling | Preserve explicit full-detail mode and exact identity; CUDA is not a required runtime dependency |
| Linux/macOS packages | After Windows acceptance | Share interfaces and algorithms where possible; require platform-native evidence |

A sub-agent engineering team may support bounded design, implementation, review and reproducible verification. Its output remains engineering assistance, not proof of mathematical correctness, native usability or human acceptance. Users of the puzzle application should not need an Agents API key or a development subscription to run the released product.

## 7. Migration sequence

| Stage | Deliverable | Exit condition |
| --- | --- | --- |
| M0: preserve the 0.4 foundation | A documented baseline, compatible data and named incomplete work | Do not relabel unfinished core solving work as a renderer issue or silently drop it |
| M1: select the GUI/rendering combination | Comparable native prototypes and recorded tradeoffs | Real geometry, windows, input and text work; select with evidence and owner review |
| M2: create the standalone presentation path | Canonical transport, owned engine lifecycle, one new frontend | No old MPUlt host or legacy Managed DirectX dependency in the new launch path; state agreement and recovery verified |
| M3: integrate graphical solving | Linked puzzle/cycle/orientation views, keyboard and complete preparation/endgame workflow | A continuous human-operated workflow reaches the defined results without hidden development tools |
| M4: external preview and stabilization | Versioned alpha/beta packages, documentation, issue intake and matching evidence | External use exposes and resolves integration, data and usability failures |
| M5: Windows 1.0 | Reproducible release package and honest release notes | All declared 1.0 gates pass; a release decision is recorded |
| M6: expand platform support | Linux/macOS builds and platform qualification | Actual native behavior and packaging pass on each advertised platform |

Keep a known working legacy version as a comparison and recovery option during migration. Do not run old and new writers against the same live session. Test migration on copies, preserve an export/recovery path, and document any format boundary before users cross it. Retaining an old release for recovery does not make its runtime a dependency of the 1.0 package.

## 8. Promotion and release assessment

**Assessment: this direction can justify promotion and a formal 1.0 for a specialist audience if the promised workflow is implemented and qualified. The present planning and prototype evidence does not establish release readiness.**

The strongest proposition is a complete, inspectable human-solving workflow for an unusually complex puzzle: mathematical structure made usable through linked graphics, precise operation review and reliable long-session state. A new renderer or a science-fiction appearance can demonstrate progress, but should not carry the whole product claim.

| Public stage | Appropriate claim | Evidence needed |
| --- | --- | --- |
| Outlook / development introduction | What is being built, for whom, and which decisions remain open | This roadmap, clear status labels and truthful existing demonstrations; concept material is identified as such |
| Downloadable alpha | A working experimental frontend with a named scope | Independently installable Windows package, real operation loop, export/recovery, declared limitations and no known silent state corruption |
| Beta / release candidate | The advertised core workflow is complete and being qualified | All-orbit/endgame coverage, prolonged use, compatibility and packaging checks; defects tracked against the exact build |
| Stable 1.0 | A reliable Windows application within a published support envelope | All gates below passed; matching binaries, source, documentation, evidence and release decision |

Preparation of positioning, a screenshot plan, tutorials and a demonstration script can start now. Publish progress as progress. A launch announcement and availability claim require an actual release artifact; this document does not schedule or authorize such an announcement.

### Stable 1.0 gates

1. **Mathematical and state integrity.** Complete-model identity, legal moves, frames, full macro effects, protections and preview/commit checks agree. No known silent corruption, wrong-target execution or protected-state violation remains.
2. **Complete promised solving workflow.** All 35 moving orbits have placement, orientation, buffer and endgame coverage; cross-orbit work and protected transitions are accessible from the real UI. Unknown or unsupported cases are not hidden behind success feedback.
3. **Sustained human use.** Real users can carry representative workflows across stages in long sessions, including correction and recovery. Automated checks do not substitute for interaction acceptance.
4. **Data continuity.** Save/reopen, undo/redo, checkpoint, import/export, crash recovery and supported version migration pass against the release build. Interrupted work is recoverable without developer intervention.
5. **Graphical and input quality.** Real geometry, transparent filters, selection, keyboard sets, independent windows, mathematical fonts, IME and display scaling are checked together. The result must be understandable and usable, not only visually polished in a still image.
6. **Measured support envelope.** Publish actual tested hardware, resolution, detail settings and limitations. Minimum hardware is determined separately from the developer's laptop; either qualify it or publish a deliberately narrow tested configuration list. Later platforms and higher refresh targets may remain future work.
7. **Independent distribution.** Install and launch on a clean supported Windows system without the legacy host/runtime chain or development tools. Verify packaged resources, uninstallation/data retention behavior, integrity hashes and a reproducible source/build relationship.
8. **Release materials and ownership.** Include accurate feature changes, migration notes, quick start, keyboard/help reference, known limitations, issue reporting and third-party notices. Record who approved the release and which evidence applies to it.

A complete human full solve would be valuable usability and publicity evidence, but it need not be the sole prerequisite for releasing a tool for such a long undertaking. Legal fixtures can cover each stage and difficult endgame, supplemented by continuous human sessions. That is not permission to omit unfinished endgame capabilities. Do not claim that a full human solve has been demonstrated until it has, and distinguish human solving from automated replay.

### Promotion materials to prepare

- A brief English description centered on human-directed solving and mathematical inspection.
- A small set of authentic screenshots: linked abstract/Local views, orientation-only review, keyboard/frame feedback, protection and an endgame result.
- A concise continuous solver demonstration: one operation, its key feedback and result, immediately followed by the next relevant feature. Use short subtitles; mark accelerated or omitted waiting and preserve the action/result relationship.
- A quick-start example, an honest comparison with the previous release, and measured performance with its settings.
- A versioned download page, known-issues list, feedback route and a clear explanation of what the software assists versus what the human decides.

Initial outreach should target hypercubing and mathematical-visualization communities with a functioning demonstration and a concrete invitation to test. Broader promotion is a later decision. No estimate of audience size, demand or sales is made here.

## 9. Current evidence and attribution

As of this outlook, 0.4 development is still in progress. Approval of earlier interaction samples does not establish complete 0.4 or 1.0 acceptance. GUI research and an isolated static offscreen mesh submission probe have been completed; no Legion performance qualification, integrated modern native GUI or Linux/macOS acceptance is claimed. The probe is not a production visual-quality demonstration.

Architecture, workflow and mathematical-method development for Magic 600 Cell build on the project author's hypercubing work. The original simulator and renderer retain primary attribution to **Andrey Astrelin's [Magic Puzzle Ultimate](https://superliminal.com/andrey/mpu/)**. A new frontend or renderer does not erase the provenance of reused code, assets, algorithms or research. Preserve [project credits](../CREDITS.md), [license](../LICENSE) and [third-party notices](../THIRD_PARTY_NOTICES.md), and record any adopted components and their terms.

This outlook publishes a direction and acceptance plan. It does not ship 1.0, select a final framework, assert an achieved performance level, or replace independent review and the release decision.
