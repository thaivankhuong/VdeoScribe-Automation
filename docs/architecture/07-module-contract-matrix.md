# Module Contract Matrix

## Purpose
Define the responsibilities, handoff contracts, and forbidden couplings for the whiteboard engine modules so later implementation stays engine-first, spec-driven, frame-based, and deterministic.

## Module Matrix

| Module | Primary responsibility | Inputs owned | Outputs exposed | Must not do |
| --- | --- | --- | --- | --- |
| `Whiteboard.Core` | Define stable domain primitives, contracts, validation abstractions, and shared value objects. | Schema primitives, identifiers, timing/value contracts, validation result types. | Module-neutral contracts used by higher layers. | Orchestrate timelines, render frames, package videos, or contain CLI workflow logic. |
| `Whiteboard.Engine` | Normalize validated spec input, resolve timeline/frame state, and coordinate sequencing rules. | Project/spec JSON after validation, asset references, deterministic timing policy. | Resolved frame-state contracts, sequencing metadata, evaluation diagnostics. | Rasterize visuals, encode media, or own process/argument parsing. |
| `Whiteboard.Renderer` | Transform resolved frame state into deterministic frame artifacts through renderer adapters. | Resolved frame state, asset handles, render settings provided by Engine/Core contracts. | Frame images or renderer-native frame artifacts plus render diagnostics. | Re-interpret timeline semantics, change draw ordering policy, or select export packaging rules. |
| `Whiteboard.Export` | Convert ordered frame outputs and explicit media metadata into final packaged deliverables. | Ordered frame artifacts, FPS/duration metadata, audio inputs, output profile contracts. | Export packages, export manifests, packaging diagnostics. | Resolve scene behavior, modify frame semantics, or calculate animation state. |
| `Whiteboard.Cli` | Accept commands, load spec files, compose module services, and report job outcomes. | File paths, user-supplied options, environment configuration. | Exit codes, logs, job summaries, invocation of pipeline entry points. | Embed business/rendering rules, bypass validation, or mutate module contracts ad hoc. |

## Contract Boundaries

### `Whiteboard.Core`
- Owns shared contracts that every higher module depends on.
- Exposes module-neutral types only; no references to renderer engines, encoders, or command shells.
- Acts as the single source for deterministic policy descriptions and shared contract primitives that need to be reused across modules.

### `Whiteboard.Engine`
- Consumes validated spec contracts and produces resolved frame-state contracts.
- Owns sequencing, timeline ordering, and frame-by-frame state evaluation rules.
- May expose diagnostics about normalization or evaluation, but not renderer-specific drawing commands.

### `Whiteboard.Renderer`
- Consumes Engine outputs without changing their meaning.
- Owns only the "how to draw" portion of the pipeline.
- Must treat frame-state input as authoritative rather than re-deriving timeline behavior.

### `Whiteboard.Export`
- Consumes ordered frame outputs and explicit media metadata.
- Owns packaging and encoding boundaries, not scene semantics.
- Must preserve timing and frame order established before export starts.

### `Whiteboard.Cli`
- Owns operator-facing orchestration only.
- Loads specs, invokes module entry points, and reports outcomes.
- Must never become a hidden domain layer that bypasses Engine/Core contracts.

## Forbidden Couplings
- `Whiteboard.Core` must not depend on `Engine`, `Renderer`, `Export`, or `Cli`.
- `Whiteboard.Engine` must not depend on `Renderer`, `Export`, or `Cli`.
- `Whiteboard.Renderer` must not depend on `Export` or `Cli`.
- `Whiteboard.Export` must not depend on `Renderer` internals or `Cli`.
- `Whiteboard.Cli` must not introduce hidden business rules, timeline decisions, or rendering semantics outside documented contracts.
- No module may embed hardcoded scene behavior that bypasses the project/spec JSON source of truth.

## Allowed Handoffs
1. `Whiteboard.Core` publishes stable contracts and shared validation/result types.
2. `Whiteboard.Engine` consumes those contracts and emits resolved frame-state plus sequencing metadata.
3. `Whiteboard.Renderer` consumes resolved frame-state and emits deterministic frame artifacts.
4. `Whiteboard.Export` consumes ordered frame artifacts and explicit output/audio metadata to package final deliverables.
5. `Whiteboard.Cli` coordinates end-to-end execution by invoking documented module entry points only.

## Architecture Review Checklist
- [ ] Module responsibilities stay within documented boundaries and do not leak into adjacent layers.
- [ ] Project/spec JSON remains the single source of truth for module behavior and handoff data.
- [ ] No hardcoded scene logic or hidden defaults bypass documented contracts.
- [ ] Deterministic behavior is preserved: the same spec, assets, and settings must produce the same outputs.
- [ ] New contracts describe explicit inputs, outputs, and diagnostics rather than relying on side effects.

