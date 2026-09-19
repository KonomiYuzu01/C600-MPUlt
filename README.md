# Magic 600 Cell (formerly C600 Studio)

**1.0 architecture (proposed):** [Integrated architecture, PDF and editable source](docs/architecture/1.0/README.md). Revision 1.0-A3 schedules the deferred B4-12 performance workstream without lowering its thresholds or claiming hardware causality. Documentation only.

**0.4 update:** [Download Magic 600 Cell 0.4](https://github.com/KonomiYuzu01/C600-MPUlt/releases/tag/0.4) · [Release guide and provenance](docs/RELEASE_0_4.md).

A Windows workbench for the full 600-cell puzzle, built around **[Magic Puzzle Ultimate by Andrey Astrelin](https://superliminal.com/andrey/mpu/)**. Primary credit for the original puzzle simulator and renderer belongs to Andrey.

It adds recoverable reset, exact piece filters, checkpoints, macros, buffer and insertion tools, and verified C600 / MPUlt v1 log exchange while preserving the full puzzle state.

Version **0.4** adds the native solving workspace, explicit operations, macro composition, protection and recovery workflows, orientation and buffer tools, and physical keyboard input. Previous release downloads remain unchanged.

## Run

Download [Magic 600 Cell 0.4](https://github.com/KonomiYuzu01/C600-MPUlt/releases/tag/0.4). Extract the Windows x64 portable ZIP and open `Magic600Cell.exe`. No separate Python or compiler is needed. Install [Microsoft Managed DirectX](DIRECTX.md) if prompted.

Performance optimization is scheduled for the next iteration.

See [Usage](USAGE.md) and [Building](docs/DEVELOPMENT.md). Acknowledgements to ivan216 for source references and Nan Ma for historical solving material. [Credits](CREDITS.md) · [MIT license](LICENSE) · [Third-party notices](THIRD_PARTY_NOTICES.md).

The [structure explorer guide](docs/STRUCTURE_EXPLORER.md) explains canonical color IDs, cell-centered layers, vertex lookup, inspection gestures, filter previews, and the auxiliary views. [Limitations and development priorities](docs/LIMITATIONS_AND_ROADMAP.md) separates current behavior from proposed extensions; the [development log](docs/DEVELOPMENT_LOG.md) records implementation decisions and verification scope.

[Current development performance](docs/DEVELOPMENT_PERFORMANCE.md) records Intel HD 620 measurements, including adaptive 1080p motion and the remaining full-detail and instant-turn limits.

[0.3 validation](docs/RELEASE_0_3_VALIDATION.md) identifies the verified executable, focused Windows checks, and remaining qualification limits.

[Research materials](research/README.md) include an expanded LaTeX report on the puzzle's mathematics and software architecture, the full 35-orbit algorithms paper, reproducible synthetic replays and arithmetic audits, and Full Detail Rotation hardware requirements.

[Puzzle theory](research/PUZZLE_THEORY.md) develops the geometry, topology and group actions in an English LaTeX supplement, with Rethlas proof review and concrete optimization directions.

## License

C600 Studio is a derivative work of Magic Puzzle Ultimate, © 2010 Andrey Astrelin. This project is distributed under the same terms as the original project.

