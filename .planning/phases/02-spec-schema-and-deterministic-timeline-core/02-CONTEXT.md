# Phase 2: Spec Schema and Deterministic Timeline Core - Context

**Gathered:** 2026-03-17
**Status:** Ready for planning

<domain>
## Phase Boundary

Build the schema normalization and deterministic timeline/frame-state evaluation core so equivalent inputs always produce equivalent frame-state outputs. Scope is limited to Core and Engine contracts/behavior needed for TIME-01, TIME-02, and TIME-03.

</domain>

<decisions>
## Implementation Decisions

### Schema Compatibility and Validation Contract
- Keep `major.minor` compatibility policy from Phase 1 as hard gate for all spec ingestion.
- Validation must remain gate-ordered (`contract -> schema -> normalization -> semantic -> readiness`) and return structured deterministic payloads.
- Invalid specs must fail before timeline-to-frame evaluation begins; no best-effort fallback path.

### Normalization Canonicalization Rules
- Canonicalization is mandatory before any frame evaluation.
- Normalized collections must use explicit stable ordering rules; no reliance on map iteration or source insertion side effects.
- Defaults and reference resolution must be deterministic, with ambiguity producing validation errors instead of implicit guesses.

### Timeline-to-Frame Conversion Policy
- Use fixed FPS from output spec as primary conversion input.
- Define and enforce one boundary policy for event activation windows (inclusive start, exclusive end) across all timeline events.
- Deterministic tie-break ordering is required when multiple events share same frame boundary.

### Frame-State Lifecycle Resolution
- Object lifecycle resolution (`enter/draw/hold/exit`) must be computed from normalized timeline + prior frame state using explicit precedence rules.
- Resolver output ordering must be stable for scenes, objects, and events.
- Equivalent project specs must produce equivalent `ResolvedFrameState` structures for same frame context.

### Deterministic Test Strategy for Phase 2
- Add fixture-based tests for canonical normalization outputs and deterministic frame-state snapshots.
- Add parity tests for equivalent-spec inputs to verify identical frame-state results.
- Keep tests contract-driven (Core/Engine); renderer/export behavior remains out of scope for this phase.

### Claude's Discretion
- Internal decomposition of normalization helpers and validators within `Whiteboard.Core`/`Whiteboard.Engine`.
- Exact test fixture organization and naming conventions.
- Granularity of error codes as long as ordering, structure, and determinism constraints are preserved.

</decisions>

<specifics>
## Specific Ideas

- Preserve current `FrameContext` semantics (`frameIndex/frameRate/currentTimeSeconds`) and make timeline conversion rules explicit and testable around boundary frames.
- Evolve existing resolver skeleton (`TimelineResolver`, `ObjectStateResolver`, `FrameStateResolver`) rather than introducing parallel pipelines.
- Keep CLI spec loading aligned with Phase 1 contracts by separating temporary placeholder normalization from Phase 2 canonical normalization rules.

</specifics>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Cli/Services/ProjectSpecLoader.cs`: existing spec ingestion and placeholder normalization flow to align with new schema/normalization gates.
- `src/Whiteboard.Engine/Context/FrameContext.cs`: established frame-index to time conversion contract for deterministic timeline mapping.
- `src/Whiteboard.Engine/Services/TimelineResolver.cs`: current event activation logic baseline to harden with explicit ordering/tie-break policy.
- `src/Whiteboard.Engine/Services/FrameStateResolver.cs`: orchestration point for timeline, object, and camera resolution in one deterministic pipeline.
- `tests/Whiteboard.Engine.Tests/FrameStateResolverContractTests.cs`: existing deterministic contract tests to extend for TIME-01/02/03 coverage.

### Established Patterns
- Module separation already reflects `Core -> Engine -> Renderer/Export -> CLI` boundaries.
- Resolver interfaces (`IFrameStateResolver`, `ITimelineResolver`, `IObjectStateResolver`, `ICameraStateResolver`) are in place for testable deterministic behavior.
- Contract-style unit/integration tests already exist across Core/Engine/CLI and can be expanded without adding UI/editor concerns.

### Integration Points
- Core model/schema contracts feed Engine resolvers.
- Engine resolved frame-state remains the handoff boundary to Renderer and Export.
- CLI orchestration (`PipelineOrchestrator`) consumes frame-state outputs and should stay free of business/render logic.

</code_context>

<deferred>
## Deferred Ideas

- Draw progression tuning and camera interpolation policy beyond baseline frame-state integration (Phase 3).
- SVG visual rendering behavior and adapter-specific output decisions (Phase 4).
- Export packaging/timing-audio synchronization strategy (Phase 5).
- CLI batch ergonomics and advanced orchestration UX (Phase 6).

</deferred>

---

*Phase: 02-spec-schema-and-deterministic-timeline-core*
*Context gathered: 2026-03-17*
