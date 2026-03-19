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
- [x] **DRAW-02**: Camera pan/zoom state is evaluated per frame from explicit keyframes and interpolation policy.
- [x] **DRAW-03**: Camera behavior is integrated in frame state, not post-process side effects.

### Rendering and Export Pipeline

- [x] **PIPE-01**: Engine emits resolved frame-state contract consumable by renderer adapters.
- [x] **PIPE-02**: Renderer/export pipeline preserves scene semantics and timing metadata without nondeterministic changes.
- [x] **PIPE-03**: Export flow supports repeatable export-package generation with synchronized timeline/audio metadata.
- [ ] **PIPE-04**: Full-timeline render/export flow generates ordered frame artifacts for the entire project duration.
- [ ] **PIPE-05**: Export can encode a playable video artifact from generated frame outputs.
- [ ] **PIPE-06**: Audio cues are synchronized and muxed into final output media.

### CLI and Batch Orchestration

- [x] **CLI-01**: CLI can run spec-driven generation jobs without embedding domain logic.
- [x] **CLI-02**: CLI supports repeatable scenario runs with consistent outputs and deterministic checks.
- [ ] **CLI-03**: Batch CLI guarantees finished media artifacts per job and reports artifact paths/status deterministically.

### Production Validation

- [ ] **QA-01**: End-to-end validation proves single-run and batch spec-to-playable-media flows.

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
| DRAW-02 | Phase 3 | Complete |
| DRAW-03 | Phase 3 | Complete |
| PIPE-01 | Phase 4 | Complete |
| PIPE-02 | Phase 5 | Complete |
| PIPE-03 | Phase 5 | Complete |
| PIPE-04 | Phase 7 | Pending |
| PIPE-05 | Phase 8 | Pending |
| PIPE-06 | Phase 8 | Pending |
| CLI-01 | Phase 6 | Complete |
| CLI-02 | Phase 6 | Complete |
| CLI-03 | Phase 9 | Pending |
| QA-01 | Phase 9 | Pending |

**Coverage:**
- v1 requirements: 19 total
- Mapped to phases: 19
- Unmapped: 0

---
*Requirements defined: 2026-03-17*
*Last updated: 2026-03-19 after adding gap-closure Phases 7-9*
