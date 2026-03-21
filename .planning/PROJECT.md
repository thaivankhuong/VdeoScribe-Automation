# whiteboard-engine

## What This Is

whiteboard-engine is a .NET engine-first system that generates VideoScribe-like whiteboard videos from JSON specs. It now ships deterministic full-sequence rendering, playable media encoding, batch output, traced draw behavior, and real hand/text rendering; the next milestone is focused on closing source-parity gaps against reference samples.

## Core Value

Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## Current State

Shipped v1.0 Engine Core on 2026-03-21 with Phases 1-11 completed. The codebase now covers deterministic spec loading, frame-state resolution, SVG/text/image rendering, hand assets, playable video encoding, and batch media generation.

## Current Milestone: v1.1 Source Parity

**Goal:** Close the remaining gap between deterministic engine output and target VideoScribe/reference-sample visuals without falling back to whole-frame source crops.

**Target features:**
- Per-object authored asset decomposition for parity samples
- Source-like object motion, hand sequencing, and timing polish
- Higher-fidelity text and illustration rendering for reference scenes
- Witness-based parity regression workflow for sample videos

## Requirements

### Validated

- [x] Deterministic JSON spec ingestion and normalization - v1.0
- [x] Deterministic timeline, lifecycle, draw progression, and camera state resolution - v1.0
- [x] Deterministic full-timeline frame artifact generation - v1.0
- [x] Playable media encoding and audio muxing through the CLI pipeline - v1.0
- [x] Batch media output with deterministic summary artifacts - v1.0
- [x] VideoScribe-like traced strokes, hand guidance, real hand assets, and deterministic text rendering - v1.0

### Active

- [ ] Reproduce target sample scenes through authored object assets rather than whole-frame source crops.
- [ ] Match source-like object motion, timing, and hand-follow behavior for parity samples.
- [ ] Improve text and illustration fidelity so sample outputs look materially closer to the reference video.
- [ ] Lock parity witnesses with deterministic frame/video comparison artifacts.

### Out of Scope

- Interactive editor UI - engine-first delivery remains the priority.
- Whole-frame screenshot/video-crop reconstruction as the primary rendering strategy - parity must come from engine semantics and authored assets.
- Realtime collaborative editing - this project remains offline/batch-first.
- Plugin ecosystem and advanced non-core effects - defer until parity and production reliability are stable.

## Context

The repository now contains a completed v1.0 milestone under `.planning/milestones/` plus a parity demo area under `artifacts/source-parity-demo/`. Current user feedback is pushing the project toward higher visual similarity to reference videos, especially object-by-object draw order, hand behavior, and motion quality.

## Constraints

- **Architecture**: Keep strict module boundaries (Core, Engine, Renderer, Export, CLI) - parity work cannot collapse business logic into the CLI or renderer.
- **Determinism**: Frame-based deterministic evaluation remains non-negotiable - parity improvements must preserve repeatability.
- **Input Model**: Stay spec-driven - no hardcoded storyboard logic and no whole-frame crop shortcuts as the main authoring path.
- **Verification**: Serial build/test remains the reliable path in this workspace because parallel test runs still hit intermittent obj-lock issues.

## Key Decisions

| Decision | Rationale | Outcome |
|----------|-----------|---------|
| Engine-first delivery before any UI/editor | Protect deterministic core from premature UI coupling | Good |
| JSON spec as single source of truth for scenes/timeline/output intent | Enables reusable batch generation and stable contracts | Good |
| Deterministic frame-state as central handoff contract | Keeps renderer/export replaceable while preserving semantics | Good |
| Full-timeline rendering, playable media, and hand assets were added before source-parity polish | Closed the business-output gap before pursuing final visual similarity | Good |
| v1.1 will target source parity without relying on whole-frame crops | Aligns new milestone with current user demand and engine-first rules | Pending |

---
*Last updated: 2026-03-21 after starting v1.1 Source Parity milestone*
