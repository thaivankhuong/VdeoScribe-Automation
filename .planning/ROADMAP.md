# Roadmap: whiteboard-engine

## Overview

Build a deterministic .NET whiteboard video engine that reproduces core VideoScribe-style behavior through a spec-driven pipeline, progressing from contracts and state resolution to rendering/export and CLI batch orchestration.

## Phases

- [x] **Phase 1: Bootstrap and Architecture Baseline** - lock planning artifacts, module contracts, and deterministic rules.
- [x] **Phase 2: Spec Schema and Deterministic Timeline Core** - define schema, normalization, validation, and frame-state evaluation semantics.
- [ ] **Phase 3: Draw Progression and Camera State Resolution** - implement and verify draw/camera behavior in frame-state outputs.
- [ ] **Phase 4: SVG Draw Rendering Adapter** - consume resolved frame state and generate deterministic frame visuals.
- [ ] **Phase 5: Export Pipeline Integration** - package frame outputs into final video with timing/audio alignment.
- [ ] **Phase 6: CLI Batch Orchestration and End-to-End Validation** - execute repeatable scenario jobs and verify deterministic outputs.

## Phase Details

### Phase 1: Bootstrap and Architecture Baseline
**Goal**: Establish validated planning, architecture boundaries, and deterministic contracts as the foundation for all implementation.
**Depends on**: Nothing (first phase)
**Requirements**: SPEC-01, SPEC-02, SPEC-03
**Success Criteria** (what must be TRUE):
1. Project planning artifacts exist and align with repository architecture docs.
2. Module boundaries and dependency direction are explicitly defined and agreed.
3. Deterministic rendering rule and JSON-driven rule are codified as non-negotiable constraints.
**Plans**: 3 plans

Plans:
- [x] 01-01: Finalize module contracts and dependency boundaries
- [x] 01-02: Define JSON spec schema/versioning and validation strategy
- [x] 01-03: Define deterministic evaluation and verification strategy

### Phase 2: Spec Schema and Deterministic Timeline Core
**Goal**: Build the schema normalization + deterministic timeline/frame-state evaluation core.
**Depends on**: Phase 1
**Requirements**: TIME-01, TIME-02, TIME-03
**Success Criteria** (what must be TRUE):
1. Timeline time maps to frame indices deterministically at fixed FPS.
2. Object lifecycle state is resolved per frame with stable ordering rules.
3. Equivalent inputs produce equivalent frame-state outputs across runs.
**Plans**: 3 plans

Plans:
- [x] 02-01: Implement schema validation and normalization pipeline
- [x] 02-02: Implement timeline-to-frame index conversion and ordering rules
- [x] 02-03: Implement frame-state resolution for object lifecycle

### Phase 3: Draw Progression and Camera State Resolution
**Goal**: Add VideoScribe-like draw reveal and camera timing behavior into resolved frame state.
**Depends on**: Phase 2
**Requirements**: DRAW-01, DRAW-02, DRAW-03
**Success Criteria** (what must be TRUE):
1. Path draw progression is timeline-driven and deterministic.
2. Camera keyframes interpolate predictably by policy.
3. Frame state fully includes draw and camera outputs for rendering handoff.
**Plans**: 3 plans

Plans:
- [ ] 03-01: Implement path-based draw progression model
- [ ] 03-02: Implement camera keyframe interpolation/state integration
- [ ] 03-03: Add deterministic tests for draw/camera frame-state behavior

### Phase 4: SVG Draw Rendering Adapter
**Goal**: Render deterministic frame visuals from resolved frame state via SVG adapter.
**Depends on**: Phase 3
**Requirements**: PIPE-01
**Success Criteria** (what must be TRUE):
1. Renderer consumes only stable frame-state contracts from engine.
2. SVG draw outputs match expected reveal progression from frame states.
3. Rendering remains deterministic for identical inputs.
**Plans**: 2 plans

Plans:
- [ ] 04-01: Implement renderer adapter interfaces and SVG rendering path
- [ ] 04-02: Validate visual output determinism and adapter boundaries

### Phase 5: Export Pipeline Integration
**Goal**: Integrate deterministic frame outputs into final video packaging with synchronized timing.
**Depends on**: Phase 4
**Requirements**: PIPE-02, PIPE-03
**Success Criteria** (what must be TRUE):
1. Export pipeline packages frame sequences without altering semantics.
2. Timing/audio metadata remains synchronized with timeline behavior.
3. Re-running export on identical inputs yields equivalent outputs.
**Plans**: 2 plans

Plans:
- [ ] 05-01: Implement export contracts and frame/audio packaging flow
- [ ] 05-02: Add repeatability checks for export outputs

### Phase 6: CLI Batch Orchestration and End-to-End Validation
**Goal**: Provide CLI workflow for repeatable spec-driven generation at scale and verify end-to-end reliability.
**Depends on**: Phase 5
**Requirements**: CLI-01, CLI-02
**Success Criteria** (what must be TRUE):
1. CLI runs single and batch scenarios without embedding business/rendering logic.
2. End-to-end runs are repeatable with deterministic validation checks.
3. Pipeline execution results are easy to inspect and automate in CI.
**Plans**: 2 plans

Plans:
- [ ] 06-01: Implement CLI orchestration commands and job pipeline wiring
- [ ] 06-02: Add integration tests for repeatable end-to-end batch workflows

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Bootstrap and Architecture Baseline | 3/3 | Complete | 01-01, 01-02, 01-03 |
| 2. Spec Schema and Deterministic Timeline Core | 3/3 | Complete | 02-01, 02-02, 02-03 |
| 3. Draw Progression and Camera State Resolution | 0/3 | Not started | - |
| 4. SVG Draw Rendering Adapter | 0/2 | Not started | - |
| 5. Export Pipeline Integration | 0/2 | Not started | - |
| 6. CLI Batch Orchestration and End-to-End Validation | 0/2 | Not started | - |


