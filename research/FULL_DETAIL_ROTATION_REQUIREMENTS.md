# Full Detail Rotation: hardware qualification

**Target:** continuous 3D and 4D rotation at an actual **1920 x 1080 render target**, with all **259,800 stickers submitted on every frame**, motion detail reduction disabled, and sustained performance of at least **30 FPS**. The corresponding frame budget is 33.33 ms.

**A minimum qualifying GPU model has not been established.** Intel HD Graphics 620 renders the complete model but does not meet this full-detail target. The measurements do not establish a guaranteed minimum discrete GPU or VRAM capacity.

## Current 0.3 implementation observations

The measured code contains the final 0.3 interaction and rendering implementation, before its release-version banner and packaging updates. The actual Direct3D device reported **Intel HD Graphics 620**, adapter 0, driver **31.0.101.2140**. The full-resolution run verified a **1920 x 1080 Direct3D backbuffer**. Its oversized child window was clipped by the available 1280 x 720 automation desktop; it was a real render-target workload, not a fully visible 1080p desktop session.

After two warmups, five changed-camera full-detail frames averaged **622.25 ms**, with a 570.21-690.72 ms range. The reciprocal mean is about **1.61 complete frames per second**. This is a short redraw sample, not a sustained full-detail rotation test. Its mean cost is approximately **18.67 times** the 30 FPS budget.

| Current workload or requirement | Observation or status |
| --- | --- |
| Full 259,800-sticker draw at actual 1920 x 1080 | 622.25 ms mean across five changed-camera frames |
| Production adaptive drag at that render target | 60 frames in 1.931 s: 31.07 FPS, with 1,500 sampled stickers during motion |
| Full-detail restoration on MouseUp | 600.58 ms; all 259,800 stickers restored and retained after idle |
| Complete labelled state during camera-only tests | Unchanged across all 259,800 entries |
| Continuous full-detail 1080p at 30 FPS | Not achieved by the tested system |
| Minimum qualifying GPU / VRAM | Not established |

The adaptive drag used actual owned window messages and the production timer. Windows determined the cadence of 60 requested moves at 16 ms intervals. The result exceeds 30 FPS only for that short sampled-detail interval; it cannot be called sustained full-detail 30 FPS. It also excludes physical mouse hardware and display scanout latency. The retained mechanical state remains complete throughout sampling.

The inherited renderer performs CPU-side projection and triangle/line packing as well as graphics-driver work. A GPU-only upgrade recommendation, or a claim that a GPU 18.67 times faster would solve the problem, would be unsupported. The complete CPU/GPU/driver pipeline must fit the budget. These observations and failed targets are detailed in [Development performance observations](../docs/DEVELOPMENT_PERFORMANCE.md).

## Historical compatibility and optimization evidence

The frozen 0.2.4 release demonstrated Windows 10 x64, an x86 native host, .NET Framework 4.8, installed official Managed DirectX 1.1 assemblies, and a working Direct3D9 hardware driver on the tested system. The legacy runtime remains an external dependency; see [DirectX dependency instructions](../DIRECTX.md) and [runtime provenance](../docs/RUNTIME_PROVENANCE.md). A newer DirectX feature level alone does not establish that dependency or performance compatibility.

Its eighteen optimized full-detail redraws at 931 x 604 averaged 453.45 ms, compared with 626.57 ms for the original allocation path: a 27.63% reduction for that recorded buffer-reuse workload. Those historical fixed-pose results, approximately 58 FPS sampled-motion observations, and aggregate CPU counters are not current 0.3 or sustained 1080p measurements. See the preserved [0.2.4 validation](../tests/VALIDATION.md).

## Qualification procedure

1. Record the CPU, GPU, driver, OS, legacy runtime, source/build hashes, actual render-target dimensions, and drawing settings.
2. Use the full retained model and all 259,800 sticker meshes. Disable motion sampling, filters that omit pieces, and substitute geometry. Keep clipping and frame visibility consistent with the reference workload.
3. Run reproducible continuous 3D and 4D camera paths after warm-up, for at least 60 seconds per workload. Measure frame intervals, CPU consumption, memory use, and submitted mesh counts.
4. Require a sustained mean of at least 30 FPS and a 95th-percentile frame interval no greater than 33.33 ms. Check that geometry is complete, sampled images agree with the reference path, and every labelled state entry remains unchanged during camera-only motion.
5. Report cold construction, full-detail restoration, picking latency, and stationary redraw separately. Publish failed workloads as well as passing ones.

No complete configuration has yet passed this procedure. Establishing a minimum specification requires further optimization and measurements of the actual complete configuration, rather than extrapolation from GPU specifications or pixel-area ratios.
