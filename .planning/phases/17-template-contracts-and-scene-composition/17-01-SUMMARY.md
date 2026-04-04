---
phase: 17-template-contracts-and-scene-composition
plan: 01
subsystem: core-validation
tags: [template-catalog, template-contracts, deterministic-validation]
requires:
  - phase: 16-controlled-asset-and-effect-registry
    provides: governed asset and effect-profile ID contracts used by templates
provides:
  - File-based template catalog and committed sample template package
  - Core template contract records for slots and fragment payloads
  - Deterministic single-template normalization and semantic validation pipeline
affects: [phase-17-plan-02, phase-17-plan-03, template-cli-flow]
tech-stack:
  added: []
  patterns: [repo-versioned template packages, stable template semantic codes]
key-files:
  created:
    - .planning/templates/index.json
    - .planning/templates/title-card-basic/template.json
    - src/Whiteboard.Core/Templates/TemplateCatalog.cs
    - src/Whiteboard.Core/Templates/TemplateCatalogEntry.cs
    - src/Whiteboard.Core/Templates/SceneTemplateDefinition.cs
    - src/Whiteboard.Core/Templates/TemplateSlotDefinition.cs
    - src/Whiteboard.Core/Templates/TemplateSlotConstraint.cs
    - src/Whiteboard.Core/Templates/TemplateSceneFragment.cs
    - src/Whiteboard.Core/Templates/TemplateTimelineEventFragment.cs
    - src/Whiteboard.Core/Templates/ITemplateContractPipeline.cs
    - src/Whiteboard.Core/Templates/TemplateContractPipeline.cs
    - tests/Whiteboard.Core.Tests/TemplateContractPipelineTests.cs
  modified: []
key-decisions:
  - Keep template authoring file-based under `.planning/templates/` with catalog metadata resolved separately from slot payload validation.
  - Mirror the existing Core spec pipeline shape so template contracts normalize and fail deterministically before composition.
patterns-established:
  - Template contracts use explicit slot declarations and stable placeholder semantics before any CLI or composition step.
  - Governed references may be injected by slot placeholders but may not silently default or fall back to source paths.
requirements-completed: [TMP-01]
duration: 15min
completed: 2026-04-04
---

# Phase 17-01 Summary

**Phase 17 now has a repo-versioned template catalog plus deterministic Core validation for reusable scene template packages.**

## Performance

- **Duration:** 15 min
- **Started:** 2026-04-04T09:52:00+07:00
- **Completed:** 2026-04-04T10:07:25+07:00
- **Tasks:** 2
- **Files modified:** 13

## Accomplishments

- Added `.planning/templates/index.json` and a committed `title-card-basic` sample package as the Phase 17 authoring baseline.
- Added strongly typed Core records for catalog entries, template slots, scene fragments, and timeline event fragments.
- Added a deterministic single-template pipeline with normalization ordering and stable semantic failure codes.

## Task Commits

Each task was committed atomically:

1. **Task 1: Create the repo template catalog and the core contract records** - `a4c62d4` (`feat`)
2. **Task 2: Add deterministic template normalization and validation in Core** - `2eaddbc` (`feat`)

Plan metadata is recorded in the follow-up docs commit for this summary and planning state update.

## Files Created/Modified

- `.planning/templates/index.json` - central catalog with stable template entry metadata.
- `.planning/templates/title-card-basic/template.json` - canonical Phase 17 sample template package.
- `src/Whiteboard.Core/Templates/SceneTemplateDefinition.cs` - root template contract for slots and fragment payloads.
- `src/Whiteboard.Core/Templates/TemplateSlotDefinition.cs` - slot contract surface including value type, required flag, defaults, and constraints.
- `src/Whiteboard.Core/Templates/TemplateContractPipeline.cs` - contract/schema/normalization/semantic validation pipeline for a single template document.
- `tests/Whiteboard.Core.Tests/TemplateContractPipelineTests.cs` - success and failure-path coverage for deterministic template validation.

## Decisions Made

- Reused the existing Core validation gate structure so template authoring errors surface with ordered gates and stable issue codes.
- Canonicalization sorts slots and fragment collections by ordinal keys to keep equivalent template JSON byte-stable after normalization.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

- Parallel MSBuild remains unreliable in this workspace; focused verification continues to use the known stable serial test path when needed.

## User Setup Required

None.

## Next Phase Readiness

- Template catalog shape and contract validation are stable for slot-binding validation and deterministic composition in 17-02.

---
*Phase: 17-template-contracts-and-scene-composition*
*Completed: 2026-04-04*
