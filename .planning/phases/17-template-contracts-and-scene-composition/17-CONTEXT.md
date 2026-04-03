# Phase 17: Template Contracts and Scene Composition - Context

**Gathered:** 2026-04-03
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 17 defines reusable template contracts (with named slots and constraints) and deterministic composition/instantiation rules that produce scene/timeline fragments.

This phase stays engine-first and file-driven. It does not add editor UI, database-backed template management, or runtime asset-upload flows.

</domain>

<decisions>
## Implementation Decisions

### Template package and identity
- **D-01:** Template storage is file-based in `.planning/templates/{templateId}/...` with a central catalog at `.planning/templates/index.json`.
- **D-02:** Template identity uses immutable `templateId` + explicit `version` + lifecycle `status` (`active|deprecated`).
- **D-03:** Templates may reference governed resources only by IDs (`assetId`, `effectProfileId`) from Phase 16 contracts; direct source-path fallback is disallowed.

### Slot contract strictness
- **D-04:** Every template must declare slots explicitly with `required`/`optional` semantics and type constraints.
- **D-05:** Unknown slot keys and missing required slots fail fast during validation (deterministic error codes/messages).
- **D-06:** Defaults are allowed only for optional non-governance values; governed references (`assetId`, `effectProfileId`) cannot silently default.

### Composition semantics
- **D-07:** Instantiation must generate deterministic namespaced IDs for produced scene objects/timeline events (instance-scoped prefixing).
- **D-08:** Composition applies explicit time/layer offsets and preserves stable ordering rules; no downstream recomputation of engine semantics.
- **D-09:** Duplicate/colliding IDs after composition are validation failures, not auto-rewritten heuristics.

### Validation and authoring workflow (no UI)
- **D-10:** Template contract validation is required before compose/instantiate and must be deterministic/repeatable.
- **D-11:** Authoring workflow remains repo-based and reviewable (JSON templates, deterministic normalization, test fixtures), not UI-driven.
- **D-12:** CLI integration should support deterministic validation/instantiation flow using existing module boundaries (Core validates contracts, CLI orchestrates).

### the agent's Discretion
- Exact schema field names and file split between template metadata, slot definitions, and fragment payload.
- Exact CLI verb names for validation/instantiation entry points, as long as behavior is deterministic and testable.
- Internal helper services/classes used to keep Core/CLI boundaries clean.

</decisions>

<specifics>
## Specific Ideas

- User confirmed "focus vào mục tiêu làm đúng" and approved recommended choices for remaining gray areas (2, 3, 4).
- User explicitly proposed future Database/UI upload for templates and assets; captured as deferred because it is out of Phase 17 scope.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase boundary and requirements
- `.planning/ROADMAP.md` - Phase 17 goal, dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` - `TMP-01`, `TMP-02` requirements and out-of-scope constraints.
- `.planning/PROJECT.md` - v1.2 direction and non-negotiables (engine-first, deterministic, no UI-first scope).
- `.planning/STATE.md` - carry-forward constraints and milestone continuity.

### Upstream governance contracts (Phase 16)
- `.planning/phases/16-controlled-asset-and-effect-registry/16-VERIFICATION.md` - accepted registry/effect governance behavior.
- `.planning/phases/16-controlled-asset-and-effect-registry/16-01-SUMMARY.md` - snapshot pinning contract decisions.
- `.planning/phases/16-controlled-asset-and-effect-registry/16-02-SUMMARY.md` - effect profile governance decisions.
- `.planning/phases/16-controlled-asset-and-effect-registry/16-03-SUMMARY.md` - deterministic CLI diagnostics expectations.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Core/Models/VideoProject.cs` - root spec contract where composed fragments must integrate.
- `src/Whiteboard.Core/Scene/SceneDefinition.cs` and `src/Whiteboard.Core/Scene/SceneObject.cs` - target shape for template-produced scene fragments.
- `src/Whiteboard.Core/Timeline/TimelineDefinition.cs` and `src/Whiteboard.Core/Timeline/TimelineEvent.cs` - target shape for template-produced timeline fragments.
- `src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs` - deterministic normalization and semantic validation pattern to reuse.
- `src/Whiteboard.Cli/Services/ProjectSpecLoader.cs` - deterministic error formatting contract for automation triage.

### Established Patterns
- Deterministic ordering + stable semantic error codes are already enforced in Core validation and must remain consistent for template contracts.
- CLI orchestrates module calls (`ProjectSpecLoader` -> planner/resolver/renderer/export) and should not absorb business semantics.
- Governance references are ID-based (`assetRegistrySnapshotId`, `effectProfileId`) and validated before execution.

### Integration Points
- New template contract/instantiation services should attach to Core (contract + normalization + semantic checks) and be invoked by CLI orchestration.
- Template-produced fragments must map directly into existing `VideoProject` scene/timeline contracts without changing renderer/export semantics.
- Tests should follow existing fixture-driven approach in `tests/Whiteboard.Core.Tests` and `tests/Whiteboard.Cli.Tests`.

</code_context>

<deferred>
## Deferred Ideas

- Database-backed template registry and upload workflows for team collaboration.
- UI for template/asset/SVG management and operator authoring.
- Live runtime ingestion of user-uploaded assets outside controlled repository flow.

</deferred>

---

*Phase: 17-template-contracts-and-scene-composition*
*Context gathered: 2026-04-03*
