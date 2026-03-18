# Requirements: whiteboard-engine

**Defined:** 2026-03-17
**Core Value:** Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## v1 Requirements

### Spec and Contracts

- [x] **SPEC-01**: Engine accepts project/spec JSON as the single source of truth for timeline, scene, camera, assets, and output settings.
- [x] **SPEC-02**: Schema versioning and normalization rules are defined and validated before timeline execution.
- [x] **SPEC-03**: Invalid or inconsistent spec data is reported through explicit validation errors.

### Timeline and State Resolution

- [x] **TIME-01**: Timeline events are converted to frame indices deterministically at fixed FPS.
- [x] **TIME-02**: Object lifecycle states (enter, draw, hold, exit) are resolved per frame from timeline + prior state.
- [x] **TIME-03**: Event ordering and overlap handling are stable across repeated runs.

### Draw and Camera Behavior

- [x] **DRAW-01**: Path-based draw progression supports handwriting-like reveal as timeline-driven progress.
- [ ] **DRAW-02**: Camera pan/zoom state is evaluated per frame from explicit keyframes and interpolation policy.
- [x] **DRAW-03**: Camera behavior is integrated in frame state, not post-process side effects.

### Rendering and Export Pipeline

- [ ] **PIPE-01**: Engine emits resolved frame-state contract consumable by renderer adapters.
- [ ] **PIPE-02**: Renderer/export pipeline preserves scene semantics and timing metadata without nondeterministic changes.
- [ ] **PIPE-03**: Export flow supports repeatable frame-to-video packaging with synchronized timeline/audio metadata.

### CLI and Batch Orchestration

- [ ] **CLI-01**: CLI can run spec-driven generation jobs without embedding domain logic.
- [ ] **CLI-02**: CLI supports repeatable scenario runs with consistent outputs and deterministic checks.

## v2 Requirements

### Extensibility

- **EXT-01**: Multiple renderer backends can be added behind stable contracts.
- **EXT-02**: Additional export encoders can be integrated without changing engine semantics.
- **EXT-03**: Template/scenario batch generation ergonomics are expanded.

## Out of Scope

| Feature | Reason |
|---------|--------|
| Interactive editor UI | Explicitly excluded in current engine-first phase |
| Drag-and-drop authoring workflow | Out of scope until core behavior parity is stable |
| Realtime collaborative editing | Not required for deterministic rendering engine goals |
| Advanced visual effects/plugins | Deferred until core pipeline contracts are mature |

## Traceability

| Requirement | Phase | Status |
|-------------|-------|--------|
| SPEC-01 | Phase 1 | Complete |
| SPEC-02 | Phase 1 | Complete |
| SPEC-03 | Phase 1 | Complete |
| TIME-01 | Phase 2 | Complete |
| TIME-02 | Phase 2 | Complete |
| TIME-03 | Phase 2 | Complete |
| DRAW-01 | Phase 3 | Complete |
| DRAW-02 | Phase 3 | Pending |
| DRAW-03 | Phase 3 | Complete |
| PIPE-01 | Phase 4 | Pending |
| PIPE-02 | Phase 5 | Pending |
| PIPE-03 | Phase 5 | Pending |
| CLI-01 | Phase 6 | Pending |
| CLI-02 | Phase 6 | Pending |

**Coverage:**
- v1 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-18 after completing Phase 3 Plan 03-01*

