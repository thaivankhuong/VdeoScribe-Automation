# Phase 06 Research: CLI Batch Orchestration and End-to-End Validation

## Objective
Research how to plan Phase 06 so `CLI-01` and `CLI-02` are delivered through deterministic CLI orchestration that composes existing Core -> Engine -> Renderer -> Export contracts for both single-run and batch workflows.

Phase goal from roadmap:
- Provide CLI workflow for repeatable spec-driven generation at scale and verify end-to-end reliability.

## Scope Guardrails
- Engine-first only; no editor or interactive UI scope.
- CLI must orchestrate existing contracts only and must not embed timeline, draw, camera, renderer, or export business logic.
- Batch behavior must wrap repeated single-job pipeline runs rather than reimplementing pipeline semantics.
- Deterministic ordering and deterministic summaries are mandatory.
- CI-friendly outputs must be explicit and machine-readable.
- Keep Phase 06 serial and local-process oriented; no worker queue, distributed execution, or parallel scheduler scope.

## Dependency Context from Phase 05
What the repository already established:
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs` is the current single-job seam over loader, engine, renderer, and export.
- `src/Whiteboard.Cli/Models/CliRunRequest.cs` and `src/Whiteboard.Cli/Models/CliRunResult.cs` already expose explicit single-run contracts.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` already proves repeat-run determinism and equivalent-input parity for single jobs.
- `tests/Whiteboard.Cli.Tests/ExportPipelineContractTests.cs` already proves ordered export packaging and deterministic export signatures.

Planning implication:
- Phase 06 should add orchestration and validation layers above the existing single-job flow.
- `PipelineOrchestrator.Run(CliRunRequest)` should remain the only place where one spec job is executed end to end.
- Phase 06 should validate aggregate behavior by composing existing deterministic witnesses, not by inventing new pipeline semantics.

## Current-State Assessment (Phase 06 Start)

### What Exists
- `src/Whiteboard.Cli/Program.cs` parses one mode only: `--spec`, `--output`, and `--frame-index`.
- `PipelineOrchestrator` already returns a rich result including counts, ordered operations, export summaries, and deterministic keys.
- The CLI test suite already covers:
  - spec loader normalization and deterministic validation ordering,
  - single-run end-to-end execution,
  - equivalent reordered spec parity,
  - export packaging determinism with frame/audio ordering guarantees.

### Gaps vs `CLI-01` / `CLI-02`
1. CLI surface gap:
- No explicit command structure for single-run versus batch execution.
- No parser or contract coverage for CLI mode selection, batch inputs, or CI-oriented summary output.

2. Batch orchestration gap:
- No batch request/result models exist.
- No service composes repeated single-job runs with a documented deterministic ordering rule.
- No aggregate exit-code or failure-handling policy exists for partial batch failures.

3. Validation/reporting gap:
- No canonical batch summary exists for CI inspection.
- No batch-level deterministic key exists.
- No end-to-end tests cover ordered multi-job execution, repeated batch parity, or equivalent batch input parity.

## Planning-Critical Decisions

### A) Keep the single-job pipeline seam authoritative
Recommended policy:
- Preserve `IPipelineOrchestrator.Run(CliRunRequest)` as the only end-to-end execution primitive for one job.
- Add a batch orchestrator above it that accepts normalized job definitions and calls `Run(...)` repeatedly.
- Do not duplicate loader, resolver, renderer, or export logic in batch code.

### B) Use explicit batch contracts with canonical ordering
Recommended policy:
- Introduce batch request/result models, with one batch request containing a collection of single-job definitions.
- Define the manifest shape explicitly with required fields: `jobId`, `specPath`, `outputPath`, `frameIndex`.
- Resolve relative `specPath` and `outputPath` values relative to the manifest directory.
- Sort jobs deterministically by canonical `jobId`.
- Treat duplicate normalized `jobId` values as deterministic validation failures.

### C) Keep execution serial and aggregate failures deterministically
Recommended policy:
- Execute jobs serially in normalized order for Phase 06.
- Continue through the full batch and aggregate per-job results.
- Return a non-zero process exit code when any job fails.

### D) Make `--summary-output <path>` the primary CI artifact contract
Recommended policy:
- Batch mode requires `--summary-output <path>`.
- The artifact is canonical JSON written for both success and failure cases.
- Lock the top-level JSON fields to: `jobCount`, `successCount`, `failureCount`, `success`, `deterministicKey`, `jobs`.
- Lock each job entry fields to: `jobId`, `specPath`, `outputPath`, `frameIndex`, `success`, `message`, `deterministicKey`, `exportDeterministicKey`.
- Serialize using stable camelCase property names, logical relative paths rather than machine-specific absolute paths, explicit `null` values when fields are absent, and stable array ordering by canonical `jobId`.

### E) Separate parser concerns into a testable CLI parser seam
Recommended policy:
- Extract argument parsing and mode selection into a dedicated CLI parser type rather than keeping it hidden inside `Program`.
- Keep console formatting and exit-code policy at the program boundary.
- Keep pipeline and batch orchestration logic in services/models, not inside `Main`.

## Recommended Plan Decomposition

### Plan 06-01: Implement CLI orchestration commands and deterministic batch contracts
Goal:
- Add a CLI surface that supports single-run and batch execution while preserving the existing single-job pipeline seam.

Planning deliverables:
- Refactor `Program` around a dedicated CLI parser seam.
- Add batch manifest/request/result models.
- Add a batch orchestration service that normalizes job order and composes repeated `IPipelineOrchestrator.Run(...)` calls.
- Add required `--summary-output <path>` JSON artifact writing for batch mode.
- Define process exit-code behavior for single-run and batch modes.

Done signals:
- CLI can execute one job or a batch without embedding pipeline semantics.
- Batch results are stable for equivalent logical job sets.
- The CLI emits a persisted JSON summary artifact with a locked schema suitable for CI.

### Plan 06-02: Add integration tests for repeatable end-to-end batch workflows
Goal:
- Lock `CLI-01` and `CLI-02` with focused contract tests and end-to-end multi-job validation.

Planning deliverables:
- Parser tests for run/batch argument handling and invalid combinations.
- Batch orchestrator tests using a fake/stub `IPipelineOrchestrator` to verify ordering, aggregation, JSON artifact contents, and deterministic key composition.
- End-to-end CLI integration tests using real fixtures across multiple jobs and repeated runs.
- Failure-path tests for invalid manifests, duplicate job ids, and mixed-success batches.

Done signals:
- Repeat runs produce identical batch summaries and deterministic keys.
- Equivalent batch inputs with reordered source listings normalize to the same aggregate result.
- Any batch failure yields deterministic aggregated reporting, a failing exit code, and a persisted JSON summary artifact.

## Test Strategy Mapping

### `CLI-01`: CLI runs single and batch scenarios without embedding business/rendering logic
Test focus:
- Argument parsing selects the correct mode and rejects ambiguous input.
- Batch orchestration calls the single-job orchestrator once per normalized job in deterministic order.
- Relative path resolution and manifest normalization stay in the CLI layer only.

Primary assertions:
- The batch service never recomputes domain semantics; it only wraps `CliRunRequest` instances.
- Equivalent normalized manifests produce the same ordered job execution sequence.
- Console and JSON summaries are derived from result contracts, not ad hoc logging state.

### `CLI-02`: CLI supports repeatable scenario runs with consistent outputs and deterministic checks
Test focus:
- Repeat-run parity for the same batch manifest.
- Equivalent batch inputs with reordered listings resolve to identical aggregate summaries.
- Mixed-success and deterministic-failure scenarios still emit stable per-job and batch-level witnesses.

Primary assertions:
- Batch deterministic keys remain stable across repeated runs.
- Job-level deterministic keys are preserved exactly from the underlying single-job pipeline.
- Persisted JSON summary artifacts are stable enough to diff or assert directly.

## Validation Architecture

### Test Layers
- CLI parser tests:
  - Validate command selection, required options, invalid combinations, and exit-code policy.
- Batch orchestration contract tests:
  - Use a fake `IPipelineOrchestrator` to prove canonical ordering, aggregation, duplicate-job rejection, JSON summary artifact contents, and deterministic key composition.
- End-to-end integration tests:
  - Use the real `PipelineOrchestrator` with existing fixture families to prove repeated batch parity and equivalent-input parity.

### Canonical Validation Witnesses
- Per-job witness:
  - `CliRunResult.DeterministicKey`
  - `CliRunResult.ExportDeterministicKey`
  - ordered operation/export summaries already emitted by the current single-job pipeline
- Batch witness:
  - ordered `jobId` values,
  - ordered per-job deterministic keys,
  - aggregate counts and final success flag,
  - canonical batch deterministic key,
  - persisted `--summary-output` JSON artifact contents

### Recommended Test Targets
- `tests/Whiteboard.Cli.Tests` should remain the primary Phase 06 test project.
- Add focused tests around the CLI parser and batch orchestrator rather than forcing every assertion through `Program`.
- Extend `PipelineOrchestratorIntegrationTests` with multi-job fixtures rather than replacing the existing single-run deterministic coverage.

### CI Path
- Keep the repository's stable serial verification path:
  - `dotnet build whiteboard-engine.sln --no-restore -v minimal /m:1`
  - targeted `dotnet test --no-build` commands
- Treat the persisted `--summary-output` JSON file as the main E2E validation artifact for CI checks.

## Risks and Mitigations
1. Batch scope drifting into a second pipeline implementation.
- Mitigation: require all batch execution to flow through `IPipelineOrchestrator.Run(CliRunRequest)` and test this with a fake orchestrator.

2. Nondeterminism caused by input ordering or path handling.
- Mitigation: normalize manifest-relative paths, reject duplicate `jobId` values, and sort by canonical `jobId` before execution.

3. CI automation depending on unstable console text.
- Mitigation: require a persisted `--summary-output` JSON artifact with locked fields and stable serialization rules.

4. Partial failures leaving validation ambiguous.
- Mitigation: aggregate every job result, expose stable failure counts/details, and return a failing exit code when any job fails while still writing the JSON summary artifact.

5. Phase 06 reopening renderer/export semantics indirectly.
- Mitigation: keep end-to-end assertions anchored to existing single-job deterministic keys and export summaries rather than adding new downstream behavior.

## Acceptance Signals for Phase Planning Quality
- The plan set maps directly to `CLI-01` and `CLI-02`.
- Batch execution is clearly defined as orchestration over repeated single-job runs.
- Deterministic ordering, JSON summary schema, and CI artifact strategy are specified before implementation.
- Validation is layered so parser, orchestration, and full pipeline behavior can fail independently and diagnose cleanly.
- Phase 06 stays out of UI/editor scope and does not modify upstream engine, renderer, or export semantics.

## RESEARCH COMPLETE
