---
gsd_state_version: 1.0
milestone: v1.1
milestone_name: Source Parity
status: Phase 13 complete; Phase 14 planning required
stopped_at: Completed 13-03-PLAN.md
last_updated: "2026-03-22T08:31:53+07:00"
last_activity: 2026-03-22 - Completed Phase 13 and opened Phase 14 planning
progress:
  total_phases: 4
  completed_phases: 2
  total_plans: 3
  completed_plans: 3
  percent: 100
---

# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-21)

**Core value:** Given the same spec, assets, and settings, the engine must always produce the same frame/video output.
**Current focus:** Phase 14 - Text and Illustration Fidelity for Parity Scenes

## Current Position

Phase: 14 of 15 (Text and Illustration Fidelity for Parity Scenes)
Plan: planning required
status: Phase 13 complete; Phase 14 planning required
Last activity: 2026-03-22 - Completed Phase 13 and opened Phase 14 planning

Progress: [##########] 100%

## Accumulated Context

### Decisions

- [v1.0]: The engine core, full-timeline rendering, playable media output, and hand/text support shipped as the first complete milestone.
- [v1.1]: Source parity must be pursued through engine semantics and authored assets, not whole-frame crop shortcuts.
- [v1.1]: Research is intentionally skipped for this milestone because the work stays inside the current domain and existing codebase.
- [Phase 12]: Generate authored asset inventory evidence from build-engine-assets.ps1 so the witness object set stays deterministic and reviewable. The same script that generates the SVG asset set also generates the inventory/decomposition evidence so the authored path cannot drift from the documented six-object breakdown.
- [Phase 12]: Keep assets/hand.svg as the active manifest-backed hand asset and limit legacy reference inventories to raster shortcut files only. This preserves hand separation for later sequencing work and keeps the authored main path unambiguous.
- [Phase 12]: Semantic validation must enforce object-type-to-asset-type matching so parity scene objects cannot silently bind to the wrong manifest collection.
- [Phase 12]: The existing CLI-to-Renderer resolved asset handoff is sufficient for the authored witness path; Phase 12-02 locks it with repo-spec integration tests instead of changing renderer semantics.
- [Phase 12]: Crop-based repo specs are now explicitly marked as legacy comparison fixtures, while project-engine.json remains the only non-legacy parity witness entry point.
- [Phase 12]: Repeated runs of the authored witness must stay package-equivalent; repo-level CLI integration tests and the committed phase12-authored-witness render witness now lock that baseline.
- [Phase 13]: Motion parity should strengthen existing Move/Scale/Rotate/Fade semantics and hand-follow sequencing on the authored witness path instead of introducing shortcut animation paths or new speculative primitives.
- [Phase 13]: Frame-state deterministic keys must include the full resolved transform payload, not just position and size, so parity motion regressions surface in engine-level evidence.
- [Phase 13]: Hand guidance must choose the earliest active authored ordering across SVG, text, and image candidates instead of hardcoding SVG-path precedence, so object-to-object transitions stay aligned with the authored route.
- [Phase 13]: Motion/hand witness validation should be anchored to committed authored export packages plus representative frame checks, not informal spot checks against shortcut fixtures.

### Roadmap Evolution

- 2026-03-19 to 2026-03-20: Expanded v1.0 from deterministic contracts into full-sequence rendering, playable media, batch output, and visual-fidelity phases.
- 2026-03-21: Archived v1.0 Engine Core and opened v1.1 Source Parity with Phases 12-15.
- 2026-03-22: Completed Phase 12-01 by locking the authored witness asset inventory and six-object decomposition evidence.
- 2026-03-22: Completed Phase 12-02 by promoting project-engine.json as the authored witness path and adding repo-level pipeline handoff coverage.
- 2026-03-22: Completed Phase 12-03 by committing a deterministic authored witness render package, demoting crop-based specs to legacy comparison fixtures, and adding repeated-run regression coverage.
- 2026-03-22: Planned Phase 13 with research and three execution plans covering transform semantics, hand sequencing, and motion witness validation.
- 2026-03-22: Completed Phase 13-01 by extending frame-state deterministic keys to include full transform payloads and adding repo-level transform handoff coverage for the authored witness scene.
- 2026-03-22: Completed Phase 13-02 by adding ordering-aware hand guidance selection, emitting ordering metadata for text/image operations, and locking authored witness hand-transition coverage.
- 2026-03-22: Completed Phase 13-03 by generating the phase13-motion-witness export package, checking representative motion/hand frames, and closing Phase 13 with auditable witness evidence.

### Pending Todos

None captured yet.

### Blockers/Concerns

- Parallel dotnet test runs in this workspace still intermittently lock build outputs under src/*/obj; serial execution remains the reliable verification path.
- Source parity work must not regress deterministic behavior or fall back to whole-frame crop-based reconstruction as the main path.
- apply_patch remains unreliable in this Windows workspace due sandbox refresh failures; PowerShell fallback may be needed for file edits.

## Session Continuity

Last session: 2026-03-22T08:01:03+07:00
Stopped at: Completed 13-03-PLAN.md
Resume file: planning required for .planning/phases/14-text-and-illustration-fidelity-for-parity-scenes

