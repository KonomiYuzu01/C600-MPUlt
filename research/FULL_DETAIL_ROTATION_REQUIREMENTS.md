# Full Detail Rotation: hardware qualification

**Target:** continuous 3D and 4D rotation at an actual **1920 x 1080 render target**, with all **259,800 stickers submitted on every frame**, motion detail reduction disabled, and sustained performance of at least **30 FPS**. The corresponding frame budget is 33.33 ms.

**A minimum qualifying GPU model has not been established.** Intel HD Graphics 620 is a tested rendering compatibility baseline, but it does not meet this full-detail target. No discrete GPU or VRAM capacity can currently be named as a guaranteed minimum from the available measurements.

## Measured baseline

The final 0.2.4 benchmark measured eighteen optimized full-detail redraws over three fixed poses at a 931 x 604 viewport on Intel HD Graphics 620. Mean elapsed drawing time was **453.45 ms**, with a **533.82 ms** 95th percentile. The reciprocal mean is approximately **2.2 complete frames per second**; this was not a sustained 1080p rotation test.

The comparison path averaged 626.57 ms. Reusing the upload buffers reduced elapsed drawing time by **27.63%**. The native renderer still performs CPU-side projection and triangle/line packing. Aggregate process CPU consumption during the optimized run averaged about **329.86 ms per frame**; this counter is not an isolated CPU/GPU timing breakdown. A GPU-only purchase recommendation would therefore be unsupported.

| Configuration or workload | Verified status |
| --- | --- |
| Windows/runtime compatibility | Windows 10 x64, x86 native host, .NET Framework 4.8, official Managed DirectX 1.1 assemblies, and a working Direct3D9 hardware driver on the tested system |
| Complete-model rendering on Intel HD Graphics 620 | Passed |
| Exact active-orbit / work-cell motion | 56.63 / 58.38 FPS |
| Adaptive dense-scene motion with 1,500 temporarily drawn meshes | 57.84 FPS |
| Full 259,800-mesh restoration | 429.01 ms; 630.28 ms including the quiet delay |
| Continuous full-detail 1080p at 30 FPS | No qualifying configuration measured |
| Minimum qualifying discrete GPU or VRAM | Not established |

Adaptive motion retains the complete mechanical state and restores all details after movement. Its approximately 58 FPS result must not be described as full-detail rotation at that rate. See the [release validation](../tests/VALIDATION.md), [runtime provenance](../docs/RUNTIME_PROVENANCE.md), and [DirectX dependency instructions](../DIRECTX.md).

## Qualification procedure

1. Record the CPU, GPU, driver, OS, legacy runtime, source/build hashes, actual render-target dimensions, and drawing settings.
2. Use the full retained model and all 259,800 sticker meshes. Disable motion sampling, filters that omit pieces, and substitute geometry. Keep clipping and frame visibility consistent with the reference workload.
3. Run reproducible continuous 3D and 4D camera paths after warm-up, for at least 60 seconds per workload. Measure frame intervals, CPU consumption, memory use, and submitted mesh counts.
4. Require a sustained mean of at least 30 FPS and a 95th-percentile frame interval no greater than 33.33 ms. Check that geometry is complete, sampled images agree with the reference path, and every labelled state entry remains unchanged during camera-only motion.
5. Report cold construction, full-detail restoration, picking latency, and stationary redraw separately. Publish failed workloads as well as passing ones.

At the smaller tested viewport, the current mean already exceeds the target budget by about **13.60 times**. A 1080p viewport has about **3.69 times** the pixel area, but frame time must not be extrapolated linearly because geometry preparation and CPU costs remain. Establishing a minimum specification requires optimization and measurements of the complete CPU/GPU/driver configuration.
