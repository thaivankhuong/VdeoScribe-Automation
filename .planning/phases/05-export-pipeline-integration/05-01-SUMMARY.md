---
phase: 05-export-pipeline-integration
plan: 01
subsystem: export
tags: [dotnet, deterministic-export, cli, audio-metadata, renderer-handoff]
requires:
  - phase: 04-svg-draw-rendering-adapter
    provides: deterministic renderer frame operations and explicit SVG asset handoff
provides:
  - explicit export request/result contracts for frame timing, audio cues, and target settings
  - deterministic export package summaries and logical-path audio packaging
  - CLI surfacing of export package metadata and export-level deterministic keys
affects: [05-02-export-repeatability, 06-cli-batch-orchestration-and-end-to-end-validation]
tech-stack:
  added: []
  patterns: [adapter-only export packaging, logical-path deterministic keys, serial dotnet verification]
key-files:
  created: [tests/Whiteboard.Cli.Tests/ExportPipelineContractTests.cs]
  modified:
    - src/Whiteboard.Export/Models/ExportRequest.cs
    - src/Whiteboard.Export/Models/ExportTarget.cs
    - src/Whiteboard.Export/Models/ExportResult.cs
    - src/Whiteboard.Export/Services/ExportPipeline.cs
    - src/Whiteboard.Cli/Models/CliRunResult.cs
    - src/Whiteboard.Cli/Services/PipelineOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - "Export deterministic keys include logical export metadata and package contents, not machine-specific resolved asset paths."
  - "CLI passes explicit frame timing and normalized audio asset inputs into Export instead of having Export infer them from project internals."
patterns-established:
  - "Export package pattern: renderer outputs plus frame timing and normalized audio metadata are packaged without recomputing engine or renderer semantics."
  - "CLI export surface pattern: combined CLI deterministic key is preserved while export-level summary and package keys are exposed separately for verification."
requirements-completed: [PIPE-02, PIPE-03]
duration: 14m
completed: 2026-03-19
---

# Phase 5 Plan 01: Export Contracts and Packaging Flow Summary

**Deterministic export package contracts with ordered frame timing, logical-path audio cue packaging, and CLI-visible export summaries**

## Performance

- **Duration:** 14 min
- **Started:** 2026-03-19T16:48:43+07:00
- **Completed:** 2026-03-19T17:03:20.8766264+07:00
- **Tasks:** 3
- **Files modified:** 9

## Accomplishments
- Expanded export contracts to carry explicit frame timing, normalized audio asset inputs, target settings, and structured package summaries.
- Replaced placeholder export behavior with deterministic frame/audio packaging and fail-fast missing-audio handling.
- Wired the CLI handoff to pass explicit export inputs and surface export package metadata, summaries, and export-level deterministic keys.

## Task Commits

Each task was committed atomically:

1. **Task 1: Expand export request/result contracts for frame and audio packaging** - `f8d9e37` (feat)
2. **Task 2: Implement deterministic export packaging and audio fail-fast rules** - `22e3387` (feat)
3. **Task 3: Integrate CLI export handoff and result surfacing** - `9bd6a36` (feat)

**Plan metadata:** pending final docs commit at summary creation time

## Files Created/Modified
- `src/Whiteboard.Export/Models/ExportRequest.cs` - Adds explicit frame timing and normalized audio asset inputs.
- `src/Whiteboard.Export/Models/ExportTarget.cs` - Captures export target frame size, frame rate, and format settings.
- `src/Whiteboard.Export/Models/ExportResult.cs` - Surfaces packaged frame/audio metadata and deterministic export summaries.
- `src/Whiteboard.Export/Services/ExportPipeline.cs` - Orders frames deterministically, packages audio cue metadata, and fails fast on missing assets.
- `src/Whiteboard.Cli/Models/CliRunResult.cs` - Exposes export package details and export-level deterministic keys to callers.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs` - Passes explicit frame timing and audio asset inputs into Export.
- `tests/Whiteboard.Cli.Tests/ExportPipelineContractTests.cs` - Verifies contract shape, frame packaging determinism, and audio asset failure behavior.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Verifies CLI export metadata surfacing, including an audio-cue integration case.
- `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` - Includes the new export contract test file in the non-default compile item list.

## Decisions Made
- Export deterministic signatures are based on packaged frame/audio contents plus logical export metadata so equivalent inputs do not vary with resolved temp paths.
- Audio cue packaging surfaces declared source paths for deterministic metadata while resolved paths are used only for existence checks.
- CLI results expose export summary/package data separately from the combined pipeline deterministic key so downstream verification can target export semantics directly.

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocking] Added explicit test compile include for the new export contract tests**
- **Found during:** Task 1 (Expand export request/result contracts for frame and audio packaging)
- **Issue:** `Whiteboard.Cli.Tests` disables default compile items, so the new `ExportPipelineContractTests.cs` file would not compile automatically.
- **Fix:** Added `ExportPipelineContractTests.cs` to `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj`.
- **Files modified:** `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj`
- **Verification:** `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1`; `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests"`
- **Committed in:** `f8d9e37`

**2. [Rule 3 - Blocking] Added the missing renderer model import in the export pipeline**
- **Found during:** Task 2 (Implement deterministic export packaging and audio fail-fast rules)
- **Issue:** `ExportPipeline.cs` referenced `RenderFrameResult` without importing `Whiteboard.Renderer.Models`, causing the build to fail.
- **Fix:** Added the missing namespace import and reran the serial build/test path.
- **Files modified:** `src/Whiteboard.Export/Services/ExportPipeline.cs`
- **Verification:** `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1`; `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~FramePackaging"`; `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~AudioPackaging"`
- **Committed in:** `22e3387`

---

**Total deviations:** 2 auto-fixed (2 blocking)
**Impact on plan:** Both deviations were required to keep the planned work compiling and verifiable. No scope creep introduced.

## Issues Encountered
- The plan's combined VSTest filter string for task 2 did not match on the local runner, so verification was split into separate `FramePackaging` and `AudioPackaging` runs while keeping the same test scope.
- `functions.apply_patch` failed in the Windows sandbox for this session, so file edits were written via safe PowerShell file writes instead.

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness
- Export contracts, packaging logic, and CLI surfacing are ready for `05-02` repeatability checks.
- Serial `dotnet build` plus targeted `dotnet test --no-build` remains the reliable verification path in this workspace.

---
*Phase: 05-export-pipeline-integration*
*Completed: 2026-03-19*

## Self-Check: PASSED
- FOUND: .planning/phases/05-export-pipeline-integration/05-01-SUMMARY.md
- FOUND: f8d9e37
- FOUND: 22e3387
- FOUND: 9bd6a36
