# Phase 6: CLI Batch Orchestration and End-to-End Validation - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning
**Source:** Derived from roadmap, state, Phase 5 outputs, and current CLI/export implementation

<domain>
## Phase Boundary

Provide a CLI workflow that can run deterministic spec-driven generation jobs in both single-run and batch-oriented modes using the existing Core -> Engine -> Renderer -> Export pipeline contracts. This phase is about orchestration, job packaging, and end-to-end verification for `CLI-01` and `CLI-02`; it is not editor work, not new rendering/export semantics, and not broad external encoding infrastructure.

</domain>

<decisions>
## Implementation Decisions

### CLI Ownership
- CLI coordinates existing module contracts only; it must not embed business logic or recompute spec, timeline, draw, camera, renderer, or export semantics.
- CLI should continue to use explicit request/result contracts when calling loader, engine, renderer, and export seams.
- Batch execution must be an orchestration layer over repeated single-job pipeline runs, not a second implementation of the pipeline.

### Deterministic Execution
- Repeat runs over identical normalized inputs must produce equivalent job-level and batch-level deterministic summaries.
- Batch ordering must be explicit and stable; file-system enumeration order must not be trusted implicitly.
- CLI output summaries should be inspection-friendly and CI-friendly while remaining deterministic.

### Scope Constraints
- Do not introduce UI/editor workflows.
- Do not change Phase 5 export-package semantics just to satisfy CLI needs.
- Do not widen into worker queues, distributed execution, or speculative deployment/runtime infrastructure.

### Repository Constraints
- Serial `dotnet build` plus targeted `dotnet test --no-build` remains the stable verification path in this workspace.
- Current `Program` and `CliRunRequest` only support one spec path and one frame index; Phase 6 should expand orchestration without breaking deterministic existing flows.
- Existing CLI integration tests already exercise single-run deterministic behavior and can be extended into batch/end-to-end fixtures.

### Claude's Discretion
- Exact CLI command shape for batch runs, as long as it stays explicit, deterministic, and testable.
- Exact job manifest / batch result summary record structure.
- Whether batch input is represented as a directory scan, explicit file list, or manifest file, provided ordering and determinism are locked clearly.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Cli/Program.cs`: current command-line parsing and output surface.
- `src/Whiteboard.Cli/Models/CliRunRequest.cs`: current single-run request contract.
- `src/Whiteboard.Cli/Models/CliRunResult.cs`: current deterministic CLI result summary.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs`: current orchestration seam over loader, engine, renderer, and export.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs`: current end-to-end deterministic evidence.

### Current Gaps
- CLI supports only `--spec`, `--output`, and `--frame-index` for one pipeline execution.
- No batch request/result model exists for repeated spec jobs.
- No deterministic batch summary or manifest output exists for CI inspection.
- End-to-end tests stop at single-job flows and do not verify stable multi-job ordering or aggregate deterministic keys.

</code_context>

<specifics>
## Specific Ideas

- Keep Phase 6 focused on explicit CLI job orchestration contracts plus deterministic end-to-end summaries.
- Reuse `PipelineOrchestrator` as the single-job building block and compose batch behavior around it.
- Prefer fixture-driven tests that prove equivalent job sets produce identical batch summaries regardless of input listing order.

</specifics>

<deferred>
## Deferred Ideas

- Interactive authoring UX.
- Parallel/distributed job workers.
- External scheduler/service integration.
- Rich progress UIs beyond CLI-readable deterministic summaries.

</deferred>

---

*Phase: 06-cli-batch-orchestration-and-end-to-end-validation*
*Context gathered: 2026-03-19*
