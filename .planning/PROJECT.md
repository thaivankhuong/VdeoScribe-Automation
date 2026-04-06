# whiteboard-engine

## What This Is

whiteboard-engine is a .NET engine-first system that generates VideoScribe-like whiteboard videos from JSON specs. It now ships deterministic compile, render, export, batch orchestration, recovery/replay, and release-witness reliability gating suitable for repeatable production pipelines.

## Core Value

Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## Current State

Shipped milestones:
- v1.0 Engine Core (2026-03-21)
- v1.1 Source Parity (2026-04-03)
- v1.2 Controlled Automation Pipeline (2026-04-04)
- v1.3 Automation Scale and Reliability (2026-04-06)

Milestone archives:
- `.planning/milestones/v1.0-ROADMAP.md`
- `.planning/milestones/v1.0-REQUIREMENTS.md`
- `.planning/milestones/v1.1-ROADMAP.md`
- `.planning/milestones/v1.1-REQUIREMENTS.md`
- `.planning/milestones/v1.2-ROADMAP.md`
- `.planning/milestones/v1.2-REQUIREMENTS.md`
- `.planning/milestones/v1.3-ROADMAP.md`
- `.planning/milestones/v1.3-REQUIREMENTS.md`

## Next Milestone Planning

**Goal:** Define v1.4 scope from fresh requirements without weakening compile/render/export contract boundaries.

**Planned setup:**
- Recreate `.planning/REQUIREMENTS.md` for v1.4 via `$gsd-new-milestone`.
- Keep roadmap and requirements milestone-scoped with archive-first rollover.

## Requirements

### Validated

- v1.0 deterministic engine pipeline baseline shipped.
- v1.1 authored source parity witness pipeline shipped.
- v1.2 controlled automation pipeline (registry/template/compiler/batch/gates) shipped.
- v1.3 throughput scaling, deterministic preflight diagnostics, resume/replay recovery flows, and release witness reliability gating shipped.

### Active

- Define and ship v1.4 requirements.

### Out of Scope

- Interactive editor UI.
- Non-deterministic runtime generation paths.
- Plugin marketplace scope.

## Context

The system has moved from deterministic rendering foundation (v1.0), through parity hardening (v1.1), controlled automation flow control (v1.2), and scale/reliability hardening (v1.3). Next work should prioritize the highest-impact automation gaps while preserving deterministic evidence contracts.

## Constraints

- **Architecture**: Preserve module boundaries across Core, Engine, Renderer, Export, and CLI.
- **Determinism**: New automation capabilities must preserve deterministic artifact and key behavior.
- **Input Model**: Stay spec/manifest driven; avoid interactive runtime decision paths.
- **Verification**: Serial build/test remains the stable path in this workspace due intermittent parallel file locks.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Engine-first delivery before UI/editor | Protect deterministic core from premature coupling | Good |
| JSON spec as single source of truth | Enables reusable automation and stable contracts | Good |
| Batch artifacts are canonical operational evidence | Keeps automation auditable and replayable | Good |
| Regression gates are required for automated success | Blocks deterministic drift before release promotion | Good |
| Resume/replay stays file-driven through explicit batch flags | Keeps recovery deterministic and automation-friendly without interactive state | Good |
| Recovery outputs must include lineage links to prior evidence | Preserves auditability across resume/replay reruns | Good |
| Release witness bundle is generated as deterministic batch artifact | Gives milestone-level review context without reconstructing per-job history | Good |
| Reliability promotion gate compares witness bundle against deterministic baseline | Fails reproducible drift or inconsistency before milestone promotion | Good |

## Evolution

This document evolves at phase transitions and milestone boundaries.

**After each phase transition** (via `$gsd-transition`):
1. Requirements invalidated? -> Move to Out of Scope with reason
2. Requirements validated? -> Move to Validated with phase reference
3. New requirements emerged? -> Add to Active
4. Decisions to log? -> Add to Key Decisions
5. "What This Is" still accurate? -> Update if drifted

**After each milestone** (via `$gsd-complete-milestone`):
1. Full review of all sections
2. Core Value check - still the right priority?
3. Audit Out of Scope - reasons still valid?
4. Update Context with current state

---
*Last updated: 2026-04-06 after completing v1.3 milestone archive*
