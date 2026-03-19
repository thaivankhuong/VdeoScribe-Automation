# Phase 7 Research: Full-Timeline Render Sequence and Frame Artifact Generation

## Objective

Research how to plan Phase 7 well so `PIPE-04` can be implemented without breaking prior deterministic semantics or leaking business logic across module boundaries.

## Requirement Focus

- `PIPE-04`: Full-timeline render/export flow generates ordered frame artifacts for the entire project duration.

## Phase Boundary

Phase 7 closes the gap between the current deterministic single-frame pipeline and a deterministic full-run frame-artifact pipeline.

This phase must do the following:

- Replace the current single-frame run path with ordered full-sequence generation for normal CLI execution.
- Generate user-visible frame artifacts for every frame in the run.
- Preserve existing engine semantics for timeline resolution, draw progression, and camera state.
- Keep responsibilities clean:
  - Core/Engine own project semantics and frame-sequence rules.
  - Renderer emits frame visuals from engine handoff contracts.
  - Export packages frame artifacts and manifests.
  - CLI orchestrates only.

This phase must not do the following:

- No playable video encoding.
- No audio mixing or muxing.
- No new editor/UI scope.
- No reinterpretation of timeline, draw, or camera semantics inside renderer/export.

## Current State

## Confirmed Implementation Baseline

- `PipelineOrchestrator` still loads one spec, resolves one `FrameContext`, renders one frame, and exports `Frames = [renderResult]`.
- `CliRunRequest` and batch jobs are centered on one `FrameIndex`.
- `FrameRenderer` emits ordered operation strings only; it does not produce a persisted visual artifact.
- `ExportPipeline` sorts and packages frame/audio metadata, but it does not write frame files.
- Batch mode is a serial wrapper over the same single-frame pipeline path.

## Existing Deterministic Primitives We Should Reuse

- `FrameContext.TimeToFrameIndex` and `FrameContext.FromFrameIndex` already define deterministic FPS and boundary-safe frame conversion.
- `FrameStateResolver` already resolves deterministic per-frame timeline/object/camera state.
- `TimelineResolver` already computes event activity windows in frame space.
- `CameraStateResolver` already supports deterministic interpolation and stable post-keyframe hold behavior.
- Export already has deterministic frame ordering, timing packaging, logical-path handling for audio, and summary-key construction.

## Current Gaps That Matter for Planning

- There is no project-level duration or frame-sequence service.
- `SceneDefinition.DurationSeconds` is validated, but it is not used to drive full-run iteration.
- There is no current contract for user-visible frame artifact content or file paths.
- There is no manifest/package shape for persisted frame artifacts.
- Current integration tests only prove one-frame export metadata, not full-run frame artifact parity.

## Planning-Critical Decisions

## 1. Add One Authoritative Full-Run Duration Rule

Phase 7 needs one place that decides how long a run is. That rule must not be duplicated across CLI, renderer, export, and tests.

Recommended rule:

- `visualDurationSeconds = max(scene.DurationSeconds, max timeline event end, max camera keyframe time)`
- `audioDurationSeconds = max audio cue end`
- `projectDurationSeconds = max(visualDurationSeconds, audioDurationSeconds)`

Why this is the best planning choice:

- `PIPE-04` says the whole project duration must be covered.
- Phase 8 is supposed to encode from generated frame outputs, not invent missing tail frames later.
- Audio-only tail coverage is therefore a Phase 7 concern for frame-count generation, even though audio rendering itself stays deferred.

## 2. Put Frame-Sequence Planning in Engine-Side Semantics, Not CLI

Recommended new concept:

- An Engine-owned service such as `IProjectRenderPlanBuilder`, `IRenderSequencePlanner`, or equivalent.

Its job should be:

- Accept normalized `VideoProject`.
- Resolve frame rate, total duration, ordered frame indices, and per-frame timing metadata.
- Emit a deterministic sequence contract that CLI can iterate and Export can package.

It should not:

- Render visuals.
- Write files.
- Know about output directories or manifest paths.

## 3. Make Full-Sequence Run the Primary Run Path

The roadmap and context say the current one-frame run path should be replaced at the CLI/export handoff boundary.

Recommended planning stance:

- Normal `run` execution becomes full-timeline by default.
- Any single-frame behavior should only survive as an explicit debug/testing path if keeping it materially reduces risk.
- Batch should continue wrapping `IPipelineOrchestrator.Run`, but it should inherit full-run behavior automatically.

This keeps Phase 7 aligned with the business gap without creating two competing primary semantics.

## 4. Extend the Renderer Contract to Produce User-Visible Frame Content

Current renderer output is an ordered operation list. That is not enough for `PIPE-04`.

Recommended contract evolution:

- Keep deterministic `Operations` for parity evidence and existing tests.
- Add a renderer-owned visual artifact payload for each frame.
- Prefer SVG frame artifacts for Phase 7.

Why SVG is the best fit here:

- It is user-visible immediately.
- It aligns with the current SVG-driven rendering pipeline.
- It avoids adding a raster graphics stack in this phase.
- It gives strong deterministic diffability for repeatability checks.

Recommended implementation direction:

- Introduce a renderer-side SVG frame composer/surface so renderer, not export, is responsible for frame visual materialization.
- Export should package and persist the renderer-produced frame artifact, not reconstruct visual semantics itself.

## 5. Add Deterministic Frame Artifact Packaging in Export

Export should evolve from metadata-only packaging to file-backed frame artifact packaging.

Recommended responsibilities:

- Create the output directory/package structure.
- Persist ordered frame artifacts.
- Persist a deterministic manifest describing the artifact set.
- Preserve existing audio metadata packaging.
- Compute deterministic keys from logical manifest content and ordered frame package data, not machine-specific absolute paths.

Recommended deterministic package structure:

- `outputRoot/frames/frame-000000.svg`
- `outputRoot/frames/frame-000001.svg`
- `outputRoot/frame-manifest.json`

Recommended manifest contents:

- Project id
- Width, height, frame rate
- Total frame count
- Total duration seconds
- Ordered frame entries with:
  - frame index
  - start seconds
  - duration seconds
  - relative artifact path
  - optional deterministic per-frame witness

Use zero-padded frame file names and relative paths only.

## 6. Preserve Failure Semantics and Determinism

Recommended behavior:

- If any frame render fails, the run should fail deterministically.
- Export should avoid presenting a partial artifact set as a successful package.
- Write into a staging directory and finalize deterministically only on success, or otherwise leave a deterministic failure state that tests can assert.

This matches the existing fail-fast behavior for render and missing-audio export failures.

## Recommended Architecture Pattern

## Proposed Phase 7 Flow

1. CLI loads normalized project spec.
2. Engine render-sequence planner computes total run duration and ordered frame/timing sequence.
3. CLI iterates the planned frames in stable order.
4. Engine resolves `ResolvedFrameState` for each planned frame.
5. Renderer produces both deterministic operations and a user-visible frame artifact payload for each frame.
6. Export persists ordered frame artifacts and writes the deterministic manifest/package summary.
7. CLI returns package/artifact summary data and deterministic keys.

## Module Ownership

- Core:
  - No new business logic unless a shared value object is required.
- Engine:
  - Project-duration rule
  - Ordered render-sequence planning
  - Per-frame semantic resolution reuse
- Renderer:
  - Deterministic frame visual generation
  - No timeline recomputation
- Export:
  - Artifact persistence
  - Manifest/package generation
  - Deterministic packaging witnesses
- CLI:
  - Spec load
  - Service orchestration
  - Result shaping only

## Contract Changes Likely Needed

## CLI

- `CliRunRequest` should no longer require a single `FrameIndex` to represent the main run path.
- `CliRunResult` will need artifact-oriented summary fields, not only single-frame fields.
- Batch job compatibility must be reviewed because current batch contracts still persist `FrameIndex`.

## Renderer

- `RenderFrameResult` likely needs a new visual artifact payload in addition to `Operations`.
- Keep current operation strings unless removing them is proven safe within phase scope.

## Export

- `ExportFramePackage` likely needs a relative artifact path and possibly a per-frame artifact witness.
- `ExportResult` likely needs package root and/or manifest path fields.

## Tests

- Current tests that assert `ExportedFrameCount == 1` will need to move to expected full-run counts.
- New fixtures should include at least one audio-overhang case so project-duration coverage is proven.

## Don’t Hand-Roll

- Do not add video encoding in Phase 7.
- Do not add audio rendering or muxing in Phase 7.
- Do not move timeline or camera semantics into Export.
- Do not let CLI invent duration or frame-sequence rules locally.
- Do not adopt heavyweight raster/image dependencies unless SVG artifacts are proven insufficient.

## Common Pitfalls

- Accidentally keeping single-frame execution as the real default and only adding a loop in tests.
- Computing duration differently in Engine, Export, and tests.
- Using absolute artifact paths inside deterministic keys.
- Letting Export rebuild frame visuals from scratch instead of packaging renderer output.
- Forgetting audio-overhang duration, which would push silent/frozen-tail logic into Phase 8.
- Breaking existing deterministic ordering guarantees when expanding one frame into many.
- Returning success with partially written frame directories after mid-run failure.

## Recommended Plan Decomposition

## 07-01: Expand Timeline Execution From Single-Frame to Ordered Full-Sequence Generation

Primary outcomes:

- Introduce render-sequence planning/duration service.
- Change `PipelineOrchestrator` to resolve and render every planned frame in stable order.
- Update run and batch contracts only as far as needed for full-run orchestration.

Key acceptance points:

- Same spec yields identical ordered frame indices and timings across repeated runs.
- Equivalent input orderings yield identical sequence plans.
- Batch still works through the same orchestrator path.

## 07-02: Emit Deterministic Frame Artifacts for Full-Sequence Export Handoff

Primary outcomes:

- Extend renderer result to include user-visible frame artifact payload.
- Persist ordered SVG frame artifacts.
- Add deterministic manifest/package output.
- Update result contracts with artifact summary fields.

Key acceptance points:

- Output contains one artifact per planned frame.
- Frame file naming and manifest ordering are stable.
- Deterministic keys ignore machine-specific absolute output roots.

## 07-03: Add Repeatability Tests for Full-Timeline Frame Generation

Primary outcomes:

- Add unit, contract, and integration coverage for full-run determinism.
- Replace one-frame integration assertions with full-sequence assertions.
- Add artifact-level parity checks across repeated and equivalent runs.

Key acceptance points:

- Same input produces identical manifest content and artifact set.
- Equivalent reordered inputs produce identical artifact-level evidence.
- Audio-overhang fixture proves total duration coverage.

## Validation Architecture

## Unit Validation

Add focused tests for the new render-sequence planner:

- Duration rule uses the max of scene, event, camera, and audio bounds.
- Frame count uses existing `FrameContext` conversion semantics without off-by-one drift.
- Equivalent input ordering yields identical planned sequence output.
- Audio-only tail extends planned frames deterministically.

## Renderer Contract Validation

Add renderer tests that prove:

- Same resolved frame state yields identical visual artifact payload and identical operations.
- Equivalent resolved states yield equivalent frame artifact payload.
- Missing/malformed SVG inputs still fail deterministically.
- Artifact payload ordering matches layer/path ordering already proven by prior phases.

## Export Contract Validation

Add export tests that prove:

- Ordered frame artifacts are written with deterministic zero-padded names.
- Manifest entries are sorted by frame index.
- Relative artifact paths are used in package data and deterministic keys.
- Repeated runs with equivalent logical inputs produce identical manifest/package witnesses.
- Failures do not present partial success packages.

## CLI Integration Validation

Add integration tests that prove:

- One CLI run against a fixture spec exports the full expected frame sequence.
- `ExportedFrameCount` equals the expected total frame count.
- Manifest and frame files exist on disk.
- Repeated runs against the same spec yield identical artifact-level evidence.
- Equivalent reordered specs yield identical manifest content and deterministic keys.

## Batch Compatibility Validation

Keep Phase 7 batch validation narrow:

- Batch still routes through the full-run pipeline path.
- Per-job deterministic summaries remain stable after the single-frame-to-full-run change.

Do not expand Phase 7 into “finished media artifact per job” guarantees. That belongs to Phase 9.

## Verification Strategy

Use the repo’s stable serial verification pattern:

- `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1`
- targeted `dotnet test --no-build` runs per affected test project

Prefer serial targeted test execution because the workspace already has intermittent parallel build-lock issues.

## Open Questions to Lock During Planning

- Whether single-frame CLI mode survives as an explicit debug path or is removed entirely from public usage.
- Exact result-model shape for artifact roots/manifest paths.
- Whether renderer emits full SVG markup directly or via a dedicated renderer-side composition service.
- Staging/finalization strategy for avoiding partial frame-package success states.

## Final Recommendation

Plan Phase 7 as a three-plan phase, matching the roadmap:

1. Introduce one engine-owned frame-sequence planner and switch normal run execution to full-sequence generation.
2. Extend renderer/export contracts to produce and persist deterministic SVG frame artifacts plus manifest/package evidence.
3. Add artifact-level repeatability validation, including repeated-run, equivalent-input, and audio-overhang scenarios.

This is the smallest architecture change that satisfies `PIPE-04`, preserves prior deterministic semantics, and leaves Phase 8 focused on encoding/muxing rather than backfilling missing frame-generation behavior.
