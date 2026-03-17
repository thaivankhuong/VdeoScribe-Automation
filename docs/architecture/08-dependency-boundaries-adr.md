# ADR 0001: Dependency Boundaries

## Status
Accepted

## Context
The repository is in the bootstrap-and-architecture-baseline phase. The system must remain engine-first, deterministic, and driven by project/spec JSON inputs as the single source of truth rather than hardcoded scene behavior. Later implementation work needs an explicit dependency rule set so module ownership does not drift.

## Decision
Adopt strict dependency direction across the five modules:

- `Whiteboard.Core` is the foundation and has no dependency on higher-level modules.
- `Whiteboard.Engine` depends on `Whiteboard.Core` only.
- `Whiteboard.Renderer` depends on `Whiteboard.Core` contracts and Engine-resolved frame-state contracts.
- `Whiteboard.Export` depends on `Whiteboard.Core` contracts and explicit outputs from Engine/Renderer handoffs as packaging inputs.
- `Whiteboard.Cli` may compose all modules, but only as an orchestration surface.

## Allowed Dependency Direction

| From | May depend on | Why |
| --- | --- | --- |
| `Whiteboard.Core` | None of the other application modules | Core defines shared contracts and must remain stable and reusable. |
| `Whiteboard.Engine` | `Whiteboard.Core` | Engine consumes shared contracts to resolve deterministic frame state. |
| `Whiteboard.Renderer` | `Whiteboard.Core`, Engine-exposed contracts | Renderer needs shared primitives plus resolved frame-state contracts, not Engine internals. |
| `Whiteboard.Export` | `Whiteboard.Core`, Engine/Renderer handoff contracts | Export packages explicit outputs and metadata without owning scene logic. |
| `Whiteboard.Cli` | `Whiteboard.Core`, `Whiteboard.Engine`, `Whiteboard.Renderer`, `Whiteboard.Export` | CLI is the composition root and invocation surface only. |

## Anti-Leakage Rules
- `Whiteboard.Core` must not reference renderer adapters, exporter concerns, or command-line parsing.
- `Whiteboard.Engine` must not call renderer or exporter implementations directly when defining timeline/state semantics.
- `Whiteboard.Renderer` must not recalculate timeline ordering, camera semantics, or object lifecycle rules.
- `Whiteboard.Export` must not reinterpret frame semantics, reorder frames, or introduce timing drift.
- `Whiteboard.Cli` must not become a hidden service layer containing validation, sequencing, scene rules, or render logic.
- Cross-module contracts must stay explicit; hidden backchannels, shared mutable globals, and side-effect-driven coordination are forbidden.

## Determinism Protection
- Deterministic rules are non-negotiable across every boundary.
- A downstream module may consume upstream outputs, but it may not mutate the meaning of those outputs.
- Time, ordering, and output metadata must flow through explicit contracts so repeated runs remain reproducible and aligned with SPEC-01 through SPEC-03.

## Consequences
- Future implementation can add internals within a module without changing dependency direction.
- Renderer and export adapters remain replaceable because they depend on stable handoff contracts rather than implicit behavior.
- CLI workflows stay maintainable because orchestration is separated from domain and rendering semantics.
- Any proposal that requires a reverse dependency or hidden shared state should be treated as an architecture violation.

## Architecture Review Checklist
- [ ] Dependency direction still flows through explicit module boundaries without reverse references.
- [ ] Project/spec JSON remains the source of truth rather than CLI, renderer, or exporter shortcuts.
- [ ] No hardcoded scene logic or hidden state bypasses the documented dependency rules.
- [ ] Deterministic behavior is preserved across downstream boundaries and packaging stages.
- [ ] Any new integration uses explicit contracts instead of shared mutable state or backchannels.

