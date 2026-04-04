# whiteboard-engine

## What This Is

whiteboard-engine is a .NET engine-first system that generates VideoScribe-like whiteboard videos from JSON specs. It ships deterministic compile, render, export, and batch automation flows with auditable artifacts.

## Core Value

Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## Current State

Shipped milestones:
- v1.0 Engine Core (2026-03-21)
- v1.1 Source Parity (2026-04-03)
- v1.2 Controlled Automation Pipeline (2026-04-04)

Milestone archives:
- `.planning/milestones/v1.0-ROADMAP.md`
- `.planning/milestones/v1.0-REQUIREMENTS.md`
- `.planning/milestones/v1.1-ROADMAP.md`
- `.planning/milestones/v1.1-REQUIREMENTS.md`
- `.planning/milestones/v1.2-ROADMAP.md`
- `.planning/milestones/v1.2-REQUIREMENTS.md`

## Next Milestone Goals

- Define fresh requirements for v1.3 through `$gsd-new-milestone`.
- Preserve deterministic guarantees while expanding automation scale and operational reliability.
- Keep strict module boundaries (Core, Engine, Renderer, Export, CLI) and avoid UI/editor scope.

## Constraints

- Architecture: maintain clean dependency direction across Core -> Engine -> Renderer -> Export -> CLI.
- Determinism: no non-deterministic shortcuts in compile, render, export, or QA gate paths.
- Input model: remain JSON/spec-driven; no ad hoc manual editing flows.
- Verification: serial build/test remains the stable path in this workspace due intermittent parallel file locks.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Engine-first delivery before UI/editor | Protect deterministic core from premature coupling | Good |
| JSON spec as single source of truth | Enables reusable automation and stable contracts | Good |
| Script compilation emits deterministic spec/report artifacts | Keeps automation input auditable and repeatable | Good |
| Batch orchestration uses explicit manifest/retry contracts | Ensures deterministic operational behavior | Good |
| Batch success requires deterministic regression gates | Prevents silent drift before release-readiness signoff | Good |

---
*Last updated: 2026-04-04 after archiving v1.2 milestone*
