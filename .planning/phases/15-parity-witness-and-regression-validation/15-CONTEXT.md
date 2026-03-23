# Phase 15: Parity Witness and Regression Validation - Context

**Gathered:** 2026-03-23
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 15 locks the authored parity workflow with reviewable witness artifacts and deterministic regression coverage.

This phase packages and validates the already-authored witness path from Phases 12-14. It does not reopen scene semantics, object decomposition, motion behavior, hand sequencing, or text/illustration fidelity. It clarifies how reviewable parity evidence should be produced and how deterministic regressions should fail.

</domain>

<decisions>
## Implementation Decisions

### Witness deliverables
- **D-01:** The canonical parity witness remains the authored export-package output rooted in `artifacts/source-parity-demo/out/...`, with `frame-manifest.json` plus committed frame artifacts as the primary review surface.
- **D-02:** Phase 15 should carry forward `phase14-fidelity-witness` as the baseline package and add review-friendly witness artifacts around it rather than replacing the package layout with a new bespoke format.
- **D-03:** Playable media output is a secondary witness derived from the same authored package, not the sole review artifact or the sole regression oracle.

### Regression evidence scope
- **D-04:** Deterministic regression must fail first on package/manifest drift: repeated-run package equivalence, artifact hash stability, and representative frame assertions take priority over subjective visual review.
- **D-05:** The representative frame anchor set stays `27, 72, 93, 130, 185, 214` unless planning discovers a concrete uncovered risk that requires adding a small number of new anchors.
- **D-06:** Equivalent-input regression should remain in scope where the repo already has an equivalent-input pattern; Phase 15 should prefer reusing established repeated-run/equivalence assertions over inventing a new comparison dimension.

### Review workflow surface
- **D-07:** Human review should center on repo-stored artifacts: witness manifest, representative-frame pointers, and optional playable media output. Review should not depend on scanning the full console deterministic key dump.
- **D-08:** Phase 15 should reuse the existing CLI invocation surface (`--spec`, `--output`, env-gated playable media) instead of introducing a new reviewer-specific CLI mode unless planning proves a thin wrapper is necessary.
- **D-09:** Witness review outputs should stay colocated with the parity demo artifacts so downstream milestone closeout can point reviewers at one stable directory tree.

### Media encoding validation
- **D-10:** Playable-media validation should use two layers: deterministic contract coverage with the fake process runner in tests, and real FFmpeg-backed inspectable output only when the existing environment variables are configured.
- **D-11:** Audio/video witness checks must validate packaging and muxing around the authored export outputs without changing engine, renderer, or export semantics.

### the agent's Discretion
- Exact naming of any Phase 15 witness subdirectories, review indexes, or summary manifests, as long as they remain under `artifacts/source-parity-demo/out/` and are easy to inspect.
- Whether the planner uses one or two plans to separate review-bundle generation from deterministic regression hardening, as long as AST-02 and VAL-01 stay explicitly covered.
- Whether a small helper script or doc is useful for assembling reviewer-facing outputs, as long as it consumes the existing authored witness package rather than introducing a new rendering path.

</decisions>

<specifics>
## Specific Ideas

- Because interactive discuss-phase menus are unavailable in this terminal mode, the context captures the recommended defaults grounded in the current repo state and prior phase decisions.
- The intended reviewer experience is: open one witness directory, inspect a concise manifest plus the six anchor frames, and optionally watch one playable-media output tied to the same authored spec.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase boundary and milestone rules
- `.planning/ROADMAP.md` - Phase 15 goal, dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` - AST-02 and VAL-01 acceptance targets for witness outputs and deterministic regression coverage.
- `.planning/STATE.md` - Carry-forward constraints from Phases 12-14, including non-crop rules, serial verification, and Phase 14 closeout state.
- `.planning/phases/12-source-sample-decomposition-and-asset-authoring/12-CONTEXT.md` - Canonical authored-witness and non-crop rules that Phase 15 must preserve.

### Authored witness baseline
- `.planning/phases/13-object-motion-and-hand-sequencing-parity/13-03-SUMMARY.md` - Motion/hand witness package decisions and representative frame anchors.
- `.planning/phases/14-text-and-illustration-fidelity-for-parity-scenes/14-03-SUMMARY.md` - Phase 14 composition witness closeout and the committed `phase14-fidelity-witness` baseline.
- `artifacts/source-parity-demo/project-engine.json` - Active authored parity spec that all Phase 15 witness outputs must consume.
- `artifacts/source-parity-demo/check/phase14-review-targets.json` - Representative frame contract for the current authored parity scene.
- `artifacts/source-parity-demo/out/phase12-authored-witness/frame-manifest.json` - Earliest committed authored witness package for repeated-run baseline lineage.
- `artifacts/source-parity-demo/out/phase13-motion-witness/frame-manifest.json` - Motion/hand witness manifest used to preserve Phase 13 behavior.
- `artifacts/source-parity-demo/out/phase14-fidelity-witness/frame-manifest.json` - Latest authored fidelity witness manifest that Phase 15 should validate and package for review.

### Existing regression and packaging code
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Existing repeated-run, equivalent-artifact, and authored witness integration coverage.
- `tests/Whiteboard.Cli.Tests/ParityCompositionWitnessTests.cs` - Current representative frame and authored-routing assertions for the active parity path.
- `tests/Whiteboard.Cli.Tests/PlayableMediaEncodingContractTests.cs` - Existing deterministic playable-media contract coverage and mux failure behavior.
- `src/Whiteboard.Export/Services/ExportPipeline.cs` - Export-package manifest shape, deterministic key construction, and playable-media gating.
- `src/Whiteboard.Cli/Program.cs` - Current CLI output surface and env-gated playable-media messaging.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `artifacts/source-parity-demo/out/phase12-authored-witness`, `artifacts/source-parity-demo/out/phase13-motion-witness`, and `artifacts/source-parity-demo/out/phase14-fidelity-witness` already provide committed baseline packages for witness lineage and regression reuse.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` already contains repeated-run artifact-package equivalence helpers and authored witness parity checks.
- `tests/Whiteboard.Cli.Tests/ParityCompositionWitnessTests.cs` already consumes `phase14-review-targets.json`, so Phase 15 can extend the witness workflow without inventing a new comparison style.
- `tests/Whiteboard.Cli.Tests/PlayableMediaEncodingContractTests.cs` already exercises deterministic fake-runner video/audio encoding paths.

### Established Patterns
- Export packaging uses `frame-manifest.json` plus per-frame artifact files under a deterministic directory root derived from `--output`; this is the existing reviewable package pattern.
- Deterministic keys are built from package summary, frame metadata, operations, and audio metadata in `ExportPipeline`; repeated-run tests assert manifest equality and file-byte equivalence rather than broad image-diff heuristics.
- Playable media is optional and environment-gated via `WHITEBOARD_ENABLE_PLAYABLE_MEDIA` and `WHITEBOARD_FFMPEG_PATH`; fake process runners provide deterministic contract coverage when a real encoder is unavailable.
- Representative parity review currently centers on the six authored object anchor frames `27/72/93/130/185/214` and should remain aligned with that pattern unless a specific risk justifies expansion.

### Integration Points
- Phase 15 should extend witness/regression validation inside `tests/Whiteboard.Cli.Tests` and the parity artifact tree under `artifacts/source-parity-demo/out/`.
- The active authored route remains `artifacts/source-parity-demo/project-engine.json`; any new witness generation or review bundle must consume this path rather than legacy comparison specs.
- Planning should keep outputs and verification commands compatible with the current CLI/export flow so milestone closeout can reuse the same commands and directories.

</code_context>

<deferred>
## Deferred Ideas

- Automated perceptual image-diff dashboards or parity scoring systems - useful later, but broader than the current witness/regression lock phase.
- Generalizing witness review beyond the single authored sample scene into a multi-scene parity suite - future milestone work once the current sample workflow is fully locked.
- New reviewer-specific CLI commands or UI surfaces - defer unless Phase 15 planning proves the existing CLI surface is insufficient.

</deferred>

---

*Phase: 15-parity-witness-and-regression-validation*
*Context gathered: 2026-03-23*
