# Rendering Pipeline

## Stages: Spec to Frames
1. Input: load project/spec JSON and asset references.
2. Normalize: resolve defaults, validate ranges, resolve asset references, and order timeline events.
3. Timeline pass: compute active events per frame tick.
4. State resolution: derive object visibility, draw progress, transforms, z-order, and resolved frame state.
5. Camera integration: apply per-frame camera state using explicit interpolation policy.
6. Renderer handoff: provide resolved frame state to renderer adapters through a stable contract.
7. Frame output: emit deterministic frame artifacts and timing metadata.

## Timeline/Frame Processing Flow
- Use a fixed frame rate contract for pipeline evaluation.
- Convert timeline time to frame indices deterministically.
- Ensure event ordering and overlap rules are stable.
- Avoid realtime-dependent behavior inside frame evaluation.

## Object State Resolution
- Resolve each object state from timeline + prior state.
- Keep transitions explicit (enter, draw, hold, exit).
- Treat path reveal progress as frame-derived, not realtime-derived.
- Make resolved frame state the primary handoff contract to the renderer.

## Camera Timing Integration
- Camera changes follow timeline events and explicit interpolation policy.
- Camera state is part of frame state, not post-processing.
- Camera behavior must remain deterministic across repeated runs.

## Renderer Handoff Boundaries
- Engine owns what to render and when.
- Renderer owns how frame primitives are rasterized/emitted.
- Export does not alter scene semantics; it packages outputs.
- Asset resolution and rendering inputs must be explicit and repeatable.

## Validation Checkpoints
- Spec validation before timeline execution.
- Asset/reference validation before frame generation.
- Frame-sequence consistency checks (ordering, counts, gaps).
- Cross-run determinism checks for equivalent inputs and outputs.