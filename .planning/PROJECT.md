# whiteboard-engine

## What This Is

whiteboard-engine is a .NET engine-first system that generates VideoScribe-like whiteboard videos from JSON specs. It focuses on deterministic frame-by-frame behavior for draw progression, camera motion, and export-ready sequencing. It is built for repeatable batch generation across many scripts/scenarios, not for interactive authoring.

## Core Value

Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## Requirements

### Validated

(None yet - ship to validate)

### Active

- [ ] Reproduce core VideoScribe-like timeline-driven draw behavior from JSON specs.
- [ ] Produce deterministic frame-state output suitable for renderer/export adapters.
- [ ] Support camera timing (pan/zoom) integrated into frame-state evaluation.
- [ ] Enable repeatable CLI-driven generation workflows for future batch scenarios.

### Out of Scope

- Editor UI / drag-and-drop authoring - engine behavior must stabilize first.
- Realtime interactive editing experience - this project is offline/batch generation first.
- Plugin ecosystem and advanced visual effects - defer until core parity is stable.

## Context

The repository already defines architecture documents under `docs/architecture/` with strict module boundaries: Core, Engine, Renderer, Export, CLI. The current objective is to clone core VideoScribe behavior at engine level: timeline reveal, path-based draw progression, camera timing, and stable frame-output flow. This initialization establishes GSD planning artifacts for phased execution without introducing UI/editor scope.

## Constraints

- **Architecture**: Strict module boundaries (Core, Engine, Renderer, Export, CLI) - maintain clean dependency direction.
- **Determinism**: Frame-based deterministic evaluation is non-negotiable - repeatability is a system-level contract.
- **Input Model**: Spec-driven JSON only - no hardcoded storyboard logic.
- **Current Phase**: Doc-first bootstrap and architecture alignment - no business/rendering implementation code in this step.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Engine-first delivery before any UI/editor | Protect deterministic core from premature UI coupling | - Pending |
| JSON spec as single source of truth for scenes/timeline/output intent | Enables reusable batch generation and stable contracts | - Pending |
| Deterministic frame-state as central handoff contract | Keeps renderer/export replaceable while preserving semantics | - Pending |
| Start with roadmap phases from existing architecture docs | Align GSD planning with current repository intent | - Pending |

---
*Last updated: 2026-03-17 after initialization*
