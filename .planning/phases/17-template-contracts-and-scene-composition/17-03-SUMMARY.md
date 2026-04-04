---
phase: 17-template-contracts-and-scene-composition
plan: 03
subsystem: cli-orchestration
tags: [cli, template-catalog, fixture-tests]
requires:
  - phase: 17-01
    provides: catalog and contract validation for repo templates
  - phase: 17-02
    provides: slot binding validation and deterministic composition in core
provides:
  - CLI template validate and instantiate commands
  - Catalog-backed template resolution in the CLI layer
  - Fixture-driven CLI repeatability and boundary coverage
affects: [phase-18-script-compiler, operator-template-flow, automation-inputs]
tech-stack:
  added: []
  patterns: [catalog-backed resolution, fixture-backed deterministic cli coverage]
key-files:
  created:
    - src/Whiteboard.Cli/Models/CliTemplateCommandRequest.cs
    - src/Whiteboard.Cli/Contracts/ITemplatePipelineOrchestrator.cs
    - src/Whiteboard.Cli/Services/TemplatePipelineOrchestrator.cs
    - tests/Whiteboard.Cli.Tests/TemplatePipelineOrchestratorTests.cs
    - tests/Whiteboard.Cli.Tests/Fixtures/phase17-templates/slot-values-valid.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase17-templates/slot-values-missing-required.json
    - tests/Whiteboard.Cli.Tests/Fixtures/phase17-templates/slot-values-unknown-slot.json
  modified:
    - src/Whiteboard.Cli/Services/CliCommandParser.cs
    - src/Whiteboard.Cli/Program.cs
    - tests/Whiteboard.Cli.Tests/CliCommandParserTests.cs
    - tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj
key-decisions:
  - Resolve template ids through `.planning/templates/index.json` first and keep direct file paths as catalog metadata, not operator inputs.
  - Keep CLI output formatting thin and delegate all template semantics to Core plus the template orchestrator service.
patterns-established:
  - Template CLI commands surface deterministic success or failure through explicit catalog, contract, slot, and composition paths.
  - Test project compile includes must be updated explicitly whenever CLI source files are added because `EnableDefaultCompileItems` is disabled.
requirements-completed: [TMP-01, TMP-02]
duration: 15min
completed: 2026-04-04
---

# Phase 17-03 Summary

**Operators can now validate and instantiate repo templates through explicit CLI commands backed by the template catalog and deterministic Core composition.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-04-04T10:18:57+07:00
- **Completed:** 2026-04-04T10:33:17+07:00
- **Tasks:** 2
- **Files modified:** 12

## Accomplishments

- Added `template validate` and `template instantiate` command parsing, dispatch, and help text in the CLI entrypoint.
- Added `TemplatePipelineOrchestrator` to resolve templates from `.planning/templates/index.json`, run Core validation/composition, and write deterministic instantiated JSON.
- Added CLI fixtures and tests covering valid flow, reordered slot payload repeatability, missing required slots, unknown slots, and unresolved template ids.

## Task Commits

Each task was committed atomically:

1. **Task 1: Add CLI template commands and a deterministic template orchestration service** - `101ec7d` (`feat`)
2. **Task 2: Add fixture-driven CLI coverage for valid, equivalent, and boundary template inputs** - `eefc8f1` (`test`)

Plan metadata is recorded in the follow-up docs commit for this summary and phase-closeout state update.

## Files Created/Modified

- `src/Whiteboard.Cli/Models/CliTemplateCommandRequest.cs` - request/result contracts for template validate and instantiate flows.
- `src/Whiteboard.Cli/Services/TemplatePipelineOrchestrator.cs` - catalog-backed resolution, slot-file loading, Core validation/composition, and deterministic output writing.
- `src/Whiteboard.Cli/Services/CliCommandParser.cs` - parses `template validate` and `template instantiate` commands with default catalog behavior.
- `src/Whiteboard.Cli/Program.cs` - dispatches template commands and prints `TemplateId`, `SlotValidationStatus`, and `DeterministicKey`.
- `tests/Whiteboard.Cli.Tests/TemplatePipelineOrchestratorTests.cs` - end-to-end CLI template validation and instantiation coverage against the committed catalog/template fixtures.

## Decisions Made

- Catalog entry resolution stays repo-driven through `.planning/templates/index.json`; operators choose `templateId`, not arbitrary template file paths.
- CLI tests use committed slot JSON fixtures plus a reordered in-test JSON variant to prove deterministic behavior across equivalent inputs.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- `Program.cs`, `CliCommandParser.cs`, and `CliCommandParserTests.cs` already had unrelated local edits in the worktree, so the template-command hunks were staged against `HEAD` separately to avoid sweeping those other changes into the Phase 17 commits.
- The initial template entry-path resolver incorrectly anchored `.planning/...` catalog entries to the test output directory; fixed by resolving those entries from the catalog’s repo root before rerunning the filtered CLI suite.

## User Setup Required

None.

## Next Phase Readiness

- Phase 17 is complete. Phase 18 can now compile higher-level script intent into repo-governed template ids plus slot payloads without inventing a new composition path.

---
*Phase: 17-template-contracts-and-scene-composition*
*Completed: 2026-04-04*
