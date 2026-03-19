# Phase 05 Research: Export Pipeline Integration

## Objective
Research how to plan Phase 05 so `PIPE-02` and `PIPE-03` are delivered through a deterministic export pipeline that packages renderer outputs, timing metadata, and audio metadata without changing upstream semantics.

Phase goal from roadmap:
- Integrate deterministic frame outputs into repeatable export-package output with synchronized timing/audio metadata.

## Scope Guardrails
- Engine-first only; no editor or interactive UI scope.
- Export must consume renderer and Core contracts only; no draw/camera/timeline semantic recomputation.
- Deterministic output remains non-negotiable.
- Keep strict boundaries:
  - `Core`: normalized output/audio/timeline contracts.
  - `Engine`: resolved frame semantics.
  - `Renderer`: deterministic frame operations.
  - `Export`: packaging, timing/audio metadata integration, deterministic export signature.
  - `CLI`: orchestration only.
- Do not pull Phase 6 CLI batch concerns into this phase.

## Dependency Context from Phase 04
What Phase 04 already established:
- Renderer emits canonical camera operations and SVG path operations in stable order.
- CLI hands explicit SVG asset manifests to the renderer and includes renderer operations in the pipeline deterministic key.
- Missing SVG assets fail fast deterministically before export starts.
- Engine handoff contracts already preserve frame index, frame rate, camera state, draw paths, and object transforms.

Planning implication:
- Phase 05 should treat `RenderFrameResult` and normalized Core metadata as the only semantic inputs to export.
- The export signature should extend, not replace, the existing deterministic witnesses from Engine + Renderer.

## Current-State Assessment (Phase 05 Start)

### What Exists
- `src/Whiteboard.Export/Models/ExportRequest.cs` accepts `ProjectId`, `Frames`, and `Target` only.
- `src/Whiteboard.Export/Models/ExportTarget.cs` currently carries `OutputPath` and `Format` only.
- `src/Whiteboard.Export/Models/ExportResult.cs` currently returns success/message, counts, and a placeholder deterministic key.
- `src/Whiteboard.Export/Services/ExportPipeline.cs` only counts frames and operations, then emits a placeholder signature.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs` currently exports a single rendered frame for a requested frame index.
- Core already exposes normalized audio and output metadata through `AudioAsset`, `AudioCue`, `OutputSpec`, and timeline contracts.

### Gaps vs Phase 05 Requirements
1. `PIPE-02` gap:
- Export does not preserve frame timing or scene semantics in an explicit package contract.
- Export request/result contracts do not carry frame-rate, duration, background/output info, or audio cue packaging data.
- There is no proof that export leaves renderer semantics intact beyond raw operation counts.

2. `PIPE-03` gap:
- Export has no concept of audio metadata, audio asset resolution, cue duration, or synchronized packaging summary.
- Deterministic signatures do not include canonical export timing/audio payloads.
- There are no export-specific repeatability tests.

3. Integration gap:
- CLI only exports one frame and surfaces only a coarse export message.
- No export manifest/package summary exists for downstream inspection or tests.

## Planning-Critical Decisions

### A) Export Representation Strategy
To stay aligned with repository constraints and avoid premature external dependencies, Phase 05 should define a deterministic export package contract first.

Recommended policy:
- Represent export output as a deterministic package/manifest summary containing:
  - ordered frame package entries,
  - frame timing metadata,
  - audio cue package entries,
  - target/output metadata,
  - export deterministic key.
- Keep real codec/file encoding integration deferred unless the contract can support it cleanly without widening scope.

### B) Frame Packaging Policy
Need a canonical rule for how renderer outputs become export payloads:
- Preserve original `RenderFrameResult.Operations` order.
- Carry explicit frame index and derived frame time / duration.
- Ensure export-level ordering is by frame index and stable for identical inputs.
- Include output surface / target metadata only as deterministic data, not as recomputed semantics.

### C) Audio Packaging Policy
Need a canonical rule for packaging normalized Core audio metadata:
- Resolve audio cues from normalized `Timeline.AudioCues` and `Assets.AudioAssets`.
- Preserve deterministic cue ordering from Core normalization.
- Carry cue start, cue duration (explicit or derived), volume, and resolved asset path/id.
- Missing referenced audio assets should be deterministic fail-fast export errors.

### D) Deterministic Signature Policy
Need a stable export signature rule:
- Signature should include canonical frame package metadata and canonical audio package metadata.
- Avoid depending on incidental runtime state or unordered collections.
- Output path may appear in result metadata, but determinism evidence should emphasize semantic package contents and stable target settings.

### E) CLI Integration Policy
Need a narrow integration step that stays within Phase 05:
- Extend `PipelineOrchestrator` only enough to pass export-relevant metadata into `ExportPipeline`.
- Keep CLI orchestration single-run oriented for now; batch expansion is Phase 6.
- Surface export package details in `CliRunResult` only if they materially support deterministic verification.

## Recommended Plan Decomposition (2 Plans)

### Plan 05-01: Implement export contracts and frame/audio packaging flow
Goal:
- Replace export placeholder behavior with deterministic export package contracts and packaging logic for frame and audio metadata.

Planning deliverables:
- Expand export request/target/result models to capture timing/audio/package data.
- Add export package models for ordered frame entries and ordered audio cue entries.
- Update `ExportPipeline` to validate audio references, package frame metadata deterministically, and emit a stable export signature.
- Update CLI orchestration to pass the required export inputs without widening into batch logic.

Done signals:
- Export result contains deterministic package metadata rather than only counts.
- Missing audio assets fail fast deterministically.
- Export pipeline can explain exactly what frames and audio cues were packaged.

### Plan 05-02: Add repeatability checks for export outputs
Goal:
- Lock `PIPE-02` and `PIPE-03` with deterministic tests at export and CLI integration levels.

Planning deliverables:
- Add export-focused contract tests for frame packaging order, audio cue synchronization, missing-audio fail-fast, and deterministic key stability.
- Extend CLI integration tests with fixtures that include audio assets/cues and equivalent reordered inputs.
- Verify the export signature changes when semantic export payload changes and remains stable when equivalent input ordering changes.

Done signals:
- Repeat runs produce identical export signatures.
- Equivalent normalized specs produce identical export package contents and deterministic keys.
- Export contract tests fail on timing/audio drift or ordering drift.

## Test Strategy Mapping to PIPE-02 / PIPE-03

### PIPE-02 (preserve scene semantics and timing metadata)
Test focus:
- Frame package ordering by frame index.
- Package-level frame timing derived from normalized frame-rate inputs.
- Export metadata reflects renderer results without changing operation order or semantics.

Primary assertions:
- Exported frame package entries preserve renderer operation lists verbatim.
- Timing metadata is deterministic and derived from explicit frame contracts only.

### PIPE-03 (repeatable frame/audio packaging)
Test focus:
- Audio cue packaging from normalized `AudioCue` and `AudioAsset` contracts.
- Fail-fast behavior for missing audio assets.
- Repeat-run/equivalent-input parity for export signatures and package contents.

Primary assertions:
- Equivalent normalized inputs produce identical export package metadata and deterministic keys.
- Audio cue ordering and durations remain stable across repeated runs.

## Validation Architecture

### Test Layers
- Export/CLI contract tests in existing test projects, primarily `tests/Whiteboard.Cli.Tests`.
- Engine contract checks in `tests/Whiteboard.Engine.Tests` only where export depends on preserved renderer-handoff fields.

### Expected Test Targets
- New export-focused contract tests for `ExportPipeline` and export package models.
- `PipelineOrchestratorIntegrationTests` for end-to-end export parity with audio metadata present.
- Existing deterministic fixtures extended with export-specific audio cases.

### Sampling Guidance
- Every task should have an automated filtered command.
- Use serial solution build + targeted `dotnet test --no-build` commands as the stable verification path.
- Keep parity tests fixture-driven so semantic equivalence remains explicit.

## Risks and Mitigations
1. Export scope drifting into full encoder/file-output integration.
- Mitigation: keep this phase contract-first and deterministic-package-first unless a narrow encoder abstraction is clearly justified.

2. Audio synchronization rules becoming underspecified.
- Mitigation: package explicit cue start/duration/volume data from normalized Core contracts and test them directly.

3. Deterministic signature coupling to incidental file paths.
- Mitigation: base export determinism primarily on canonical package payloads and stable target settings.

4. CLI orchestration broadening into Phase 6.
- Mitigation: limit CLI changes to single-run export handoff and result surfacing only.

## Acceptance Signals for Phase Planning Quality
- The plan set maps directly to `PIPE-02` and `PIPE-03`.
- Export package responsibilities are clearly separated from Engine and Renderer semantics.
- Audio cue packaging and frame timing are explicit before implementation begins.
- Deterministic verification covers both package contents and end-to-end CLI parity.
- Phase 05 stays out of UI/editor scope and out of Phase 6 batch orchestration.

## RESEARCH COMPLETE

