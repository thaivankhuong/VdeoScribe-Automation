# Roadmap: whiteboard-engine

## Overview

Continue from the shipped v1.0 engine/media baseline into a parity-focused milestone that makes authored sample scenes look materially closer to target VideoScribe/reference videos while preserving strict determinism and spec-driven architecture.

## Milestones

- [x] **v1.0 Engine Core** - Phases 1-11 (shipped 2026-03-21)
- [ ] **v1.1 Source Parity** - Phases 12-15 (planned)

## Archived Milestones

- v1.0 details: `.planning/milestones/v1.0-ROADMAP.md`
- v1.0 requirements: `.planning/milestones/v1.0-REQUIREMENTS.md`

## Phases

- [x] **Phase 12: Source Sample Decomposition and Asset Authoring** - replace crop-based parity shortcuts with authored scene assets and object decomposition for the reference sample set.
- [ ] **Phase 13: Object Motion and Hand Sequencing Parity** - make object transforms and hand-follow timing behave like the reference sample while staying in engine semantics.
- [ ] **Phase 14: Text and Illustration Fidelity for Parity Scenes** - improve authored text and illustration fidelity so parity scenes look materially closer to the reference output.
- [ ] **Phase 15: Parity Witness and Regression Validation** - lock the parity workflow with reviewable witnesses and deterministic regression coverage.

## Phase Details

### Phase 12: Source Sample Decomposition and Asset Authoring
**Goal**: Replace crop-based parity shortcuts with authored scene assets and object decomposition for the reference sample set.
**Depends on**: Phase 11
**Requirements**: PAR-01, AST-01
**Success Criteria** (what must be TRUE):
1. The target sample scene is represented as separate authored objects/assets in the spec instead of a whole-frame crop.
2. The parity asset set includes the left illustration, arrow, title, clock group, body, footer, and hand references needed for iteration.
3. The authored parity sample remains deterministic across repeated runs.
**Plans**: 3 plans

Plans:
- [x] 12-01: Define authored parity asset set and sample-scene object decomposition
- [x] 12-02: Wire parity sample specs and asset manifests through the existing pipeline
- [x] 12-03: Validate authored sample determinism and remove crop-based fallback usage from the main parity path

### Phase 13: Object Motion and Hand Sequencing Parity
**Goal**: Make object transforms and hand-follow timing behave like the reference sample while staying in engine semantics.
**Depends on**: Phase 12
**Requirements**: PAR-02, PAR-03
**Success Criteria** (what must be TRUE):
1. Object-level move/scale/rotate/fade events are sufficient to drive source-like transitions for parity scenes.
2. Hand sequencing tracks the active object/path for the parity sample through the full draw order.
3. Motion updates keep deterministic frame-state and render witnesses stable across repeated runs.
**Plans**: 3 plans

Plans:
- [x] 13-01: Finalize object transform event semantics and parity-oriented timeline usage
- [ ] 13-02: Improve hand-follow behavior and object-to-object sequencing for parity scenes
- [ ] 13-03: Validate motion and hand timing parity through frame/video witnesses

### Phase 14: Text and Illustration Fidelity for Parity Scenes
**Goal**: Improve authored text and illustration fidelity so parity scenes look materially closer to the reference output.
**Depends on**: Phase 13
**Requirements**: PAR-04, TXT-01
**Success Criteria** (what must be TRUE):
1. Title, body, and footer content render from authored text/vector assets rather than video crops.
2. The composed parity scene matches the intended object order and final layout of the reference sample closely enough for review.
3. Illustration fidelity improvements do not regress deterministic rendering or hand behavior.
**Plans**: 3 plans

Plans:
- [ ] 14-01: Improve authored text asset generation and reveal behavior for parity samples
- [ ] 14-02: Refine illustration/vector fidelity for the reference scene object set
- [ ] 14-03: Tune composition, spacing, and object ordering against parity witnesses

### Phase 15: Parity Witness and Regression Validation
**Goal**: Lock the parity workflow with reviewable witnesses and deterministic regression coverage.
**Depends on**: Phase 14
**Requirements**: AST-02, VAL-01
**Success Criteria** (what must be TRUE):
1. The parity workflow emits stable witness frames and final video outputs for review.
2. Repeated and equivalent parity runs preserve deterministic frame/video evidence.
3. The milestone closes with a review-friendly witness set for the target reference sample.
**Plans**: 2 plans

Plans:
- [ ] 15-01: Produce witness-generation and review artifacts for parity samples
- [ ] 15-02: Add deterministic regression checks for parity frame/video outputs

## Progress

| Phase | Milestone | Plans Complete | Status | Completed |
|-------|-----------|----------------|--------|-----------|
| 1-11 | v1.0 Engine Core | 29/29 | Complete | 2026-03-21 |
| 12 | v1.1 Source Parity | 3/3 | Complete | 2026-03-22 |
| 13 | v1.1 Source Parity | 1/3 | In Progress | - |
| 14 | v1.1 Source Parity | 0/3 | Not started | - |
| 15 | v1.1 Source Parity | 0/2 | Not started | - |