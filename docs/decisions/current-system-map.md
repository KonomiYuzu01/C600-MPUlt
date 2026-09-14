# Current system map — 2026-09-15

Magic 600 Cell currently uses a Python mechanics/session engine, an existing web frontend and a native Windows/MPUlt/Managed DirectX host. Start with core.py, session.py, engine_process.py, server.py, native/ and the source repository's docs/DEVELOPMENT.md. assets/manifest.json is the immutable model boundary.

The current desktop project holds shared memory and the authoritative 0.4-R2 architecture under outputs/magic600-v0.4-design/03_ARCHITECTURE.md. The separately located source worktree is selected through ignored team-project.json. This map is a navigation aid, not a replacement for current code or memory.

The isolated 0.4 G1/G2 work lives under work/experiments/magic600-04 in the source worktree. It is ignored by Git and absent from ordinary worktrees/remote CI until deliberately packaged. Both native user approvals remain pending. Source fixtures and browser evidence do not establish native acceptance.

Deployment decision: install the same versioned team infrastructure in the source repository and expose it in the desktop project. Source repository is the canonical place for future tooling changes; update the desktop mirror deliberately and record provenance. Do not copy private PROJECT_MEMORY.md or architecture exports to a public repository. Local source PROJECT_MEMORY.md is only a private pointer to the shared memory.
