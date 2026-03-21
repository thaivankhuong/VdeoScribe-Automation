# Phase 12: Source Sample Decomposition and Asset Authoring - Context

**Gathered:** 2026-03-21
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 12 replaces crop-based parity shortcuts with authored scene assets and object decomposition for the current reference sample set.

This phase is the foundation for a reusable VideoScribe-like asset workflow, not a one-off screenshot reconstruction path. The deliverable is a spec-driven authored asset structure that the engine can reuse for future whiteboard scenes and future asset families.

Phase 12 does not own final motion polish, hand sequencing parity, or full text/illustration fidelity tuning. Those remain in later phases.

</domain>

<decisions>
## Implementation Decisions

### Asset authoring strategy
- Final render content for the parity scene must come from authored SVG/text assets, not PNG crops from the source video.
- The source sample may be used only as visual reference and tracing input during authoring.
- Whole-frame crops and segmented source-image crops are not allowed on the main parity path for this phase.
- The canonical output of Phase 12 is authored asset files stored in the repo; helper scripts may generate them, but script output is not the only accepted representation.

### Asset library direction
- Phase 12 should be treated as the first step toward a reusable VideoScribe-like asset library.
- The planning assumption is that future scenes will need authored whiteboard assets such as women across age groups, children, adults, elderly characters, trees, houses, rainbows, and similar scene elements.
- The current reference sample is a witness scene used to prove the asset workflow, not the final product goal.
- Asset naming, manifest structure, and spec references should be clean enough to reuse in later scene libraries without redesigning the pipeline.

### Scene and object granularity
- The reference witness scene stays decomposed into 6 reviewable objects for this phase: left illustration, arrow, title, clock group, body, and footer.
- Text remains split into 3 authored blocks for the current witness scene: title, body, and footer.
- The clock composition remains a single authored group in Phase 12; deeper internal splitting can wait until later phases if motion fidelity needs it.
- Phase 12 should prefer reusable-enough structure over one-off sample hacks, but should not over-engineer a full template framework yet.

### Hand asset treatment
- Hand content remains a separate hand asset referenced through the spec manifest, not baked into scene objects.
- Phase 12 only needs the hand references and asset plumbing required for authored-scene iteration.
- Detailed hand-follow timing and sequencing fidelity are deferred to Phase 13.

### Claude's Discretion
- Exact folder and manifest naming for authored parity assets, as long as the structure is spec-driven and reusable.
- Whether authored text is stored as SVG path assets, text objects, or a mixed authored representation, as long as the final render path stays engine-first and crop-free.
- How much of the current helper scripting remains, as long as authored repo assets stay reviewable and deterministic.

</decisions>

<specifics>
## Specific Ideas

- The intended long-term outcome is a VideoScribe-like engine with a reusable whiteboard asset library plus scene/transitional animation presets.
- Example future asset families mentioned during discussion: women, children, adults, elderly characters, banana trees, shade trees, tree clusters, rainbows, houses, and similar illustrated scene elements.
- Current sample parity should be used as a witness that the engine can assemble authored objects into a VideoScribe-like scene instead of relying on source-image shortcuts.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `artifacts/source-parity-demo/project-engine.json`: current engine-first witness spec using separate authored object references.
- `artifacts/source-parity-demo/engine-assets/*.svg`: existing authored witness assets for left illustration, arrow, title, clock group, body, and footer.
- `artifacts/source-parity-demo/build-engine-assets.ps1`: helper path for generating/rebuilding authored witness assets.
- `artifacts/source-parity-demo/assets/hand.svg`: current hand asset already wired through manifest semantics.

### Established Patterns
- `src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs`: asset collections are normalized and validated through spec-driven manifests.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs`: hand and visual assets are resolved at orchestration time without changing engine semantics.
- `src/Whiteboard.Renderer/Services/SvgObjectRenderer.cs` and `src/Whiteboard.Renderer/Services/TextObjectRenderer.cs`: authored SVG/text rendering paths already exist and should remain the main parity path.
- `artifacts/source-parity-demo/project-image-hand.json` and `artifacts/source-parity-demo/segmented-assets/*.png`: these represent the shortcut branch and should not remain the main authored-scene path.

### Integration Points
- Phase 12 planning should treat `project-engine.json` as the active witness spec to evolve or replace.
- Authored parity assets should continue to load through manifest references under `assets.svgAssets`, `assets.handAssets`, and any authored text path already supported by the pipeline.
- The output must stay compatible with the existing deterministic render/export flow used by CLI witness generation.

</code_context>

<deferred>
## Deferred Ideas

- VideoScribe-like transition presets, camera polish, move/scale/fade tuning, and hand-follow sequencing parity belong to Phase 13.
- Higher-fidelity text shaping and illustration polish for the witness scene belong to Phase 14.
- Expansion from the witness scene into a larger production-ready asset catalog is the strategic direction, but the full library rollout goes beyond this single phase.

</deferred>

---

*Phase: 12-source-sample-decomposition-and-asset-authoring*
*Context gathered: 2026-03-21*
