# Phase 19: Batch Automation Orchestrator - Context

**Gathered:** 2026-04-04
**Status:** Ready for planning

<domain>
## Phase Boundary

Phase 19 operationalizes deterministic script-driven production runs across the existing compile, render, and export modules.

This phase orchestrates job execution and manifest/status artifacts only. It does not redesign script compiler semantics (Phase 18) and does not implement witness/regression quality gates (Phase 20).

</domain>

<decisions>
## Implementation Decisions

### Batch job contract and orchestration shape
- **D-01:** Batch input remains file-driven (manifest JSON) with explicit ordered job entries; no ad hoc runtime prompts or UI inputs.
- **D-02:** Each job executes a fixed pipeline order: `script -> spec -> render -> export`, and every stage consumes/produces committed module contracts.
- **D-03:** CLI orchestration remains thin; stage business semantics stay in Core/Engine/Renderer/Export services.

### Deterministic manifests and status outputs
- **D-04:** Every job emits a deterministic per-job manifest with stage outputs, deterministic keys, and elapsed-stage status.
- **D-05:** Batch run emits an aggregate deterministic status report with ordered job outcomes and failure summaries.
- **D-06:** Failure handling is deterministic and auditable: retry behavior must be explicitly configured and reflected in manifests (no hidden auto-retry).

### Module boundary and scope controls
- **D-07:** Batch orchestration may call existing compile/report, render, and export contracts but must not mutate upstream semantics.
- **D-08:** Phase 19 stops at orchestration + manifest/status outputs; quality-gate enforcement and drift blocking remain Phase 20 scope.

### the agent's Discretion
- Exact CLI command naming/flags for batch script runs, as long as deterministic behavior and ordering remain explicit.
- Internal class/file decomposition for orchestrator and manifest builders.
- Optional helper fixtures for representative job sets used in tests.

</decisions>

<specifics>
## Specific Ideas

- Reuse the Phase 18 compile report artifact directly in job manifests instead of reformatting compile diagnostics in a second schema.
- Keep batch manifests append-only per job attempt, with canonical final status selection for deterministic reruns.

</specifics>

<canonical_refs>
## Canonical References

**Downstream agents MUST read these before planning or implementing.**

### Phase boundary and requirements
- `.planning/ROADMAP.md` - Phase 19 goal, dependencies, and success criteria.
- `.planning/REQUIREMENTS.md` - `AUT-01`, `AUT-02` requirement text and traceability.
- `.planning/PROJECT.md` - engine-first scope and deterministic constraints.
- `.planning/STATE.md` - latest milestone continuity and workspace blockers.

### Upstream compiler and deterministic contract baseline
- `.planning/phases/18-script-to-spec-compiler/18-02-SUMMARY.md` - compile command/service behavior and deterministic output contract.
- `.planning/phases/18-script-to-spec-compiler/18-03-SUMMARY.md` - compile report/diagnostic schema and deterministic failure handling.
- `.planning/phases/18-script-to-spec-compiler/18-VERIFICATION.md` - verified must-haves and non-blocking constraints.

</canonical_refs>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Cli/Services/ScriptCompilationOrchestrator.cs` - compile entrypoint returning deterministic spec/report artifacts.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs` - existing run orchestration that can anchor render/export stage wiring.
- `src/Whiteboard.Cli/Services/BatchPipelineOrchestrator.cs` - batch-oriented skeleton and existing batch result model surface.
- `src/Whiteboard.Cli/Models/CliBatchManifest.cs` / `CliBatchJob.cs` / `CliBatchRunResult.cs` - current batch contract entry points for expansion.
- `src/Whiteboard.Cli/Services/CliCommandParser.cs` / `src/Whiteboard.Cli/Program.cs` - CLI command routing integration points.

### Established Patterns
- Deterministic ordering and stable diagnostic contracts are already enforced in compile paths.
- CLI layer acts as orchestration shell; core semantic checks and deterministic keys are generated in lower modules.
- This workspace currently needs serial build fallback for some CLI test paths.

### Integration Points
- Batch job compile stage should call `IScriptCompilationOrchestrator` (or equivalent abstraction) and consume generated spec/report paths.
- Render/export stages should reuse existing run pipeline contracts without duplicating scene/timeline logic.
- Manifest/status outputs should include compile deterministic keys and downstream media artifact metadata in one ordered structure.

</code_context>

<deferred>
## Deferred Ideas

- Queue workers/distributed execution backend for large-scale parallel job processing.
- Dynamic policy-based quality gate execution inside batch run (Phase 20 scope).
- Interactive monitoring dashboard for job runs.

</deferred>

---

*Phase: 19-batch-automation-orchestrator*
*Context gathered: 2026-04-04*
