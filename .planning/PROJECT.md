# whiteboard-engine

## What This Is

whiteboard-engine is a .NET engine-first system that generates VideoScribe-like whiteboard videos from JSON specs. It ships deterministic full-sequence rendering, playable media encoding, batch output, traced draw behavior, hand/text/image rendering, and source-parity witness workflows for a reference sample.

## Core Value

Given the same spec, assets, and settings, the engine must always produce the same frame/video output.

## Current State

Shipped milestones:
- v1.0 Engine Core (2026-03-21): deterministic pipeline from spec ingestion to playable media.
- v1.1 Source Parity (2026-04-03): authored object decomposition, parity motion/hand sequencing, text/illustration fidelity updates, and witness/regression evidence.

Milestone archives:
- `.planning/milestones/v1.0-ROADMAP.md`
- `.planning/milestones/v1.0-REQUIREMENTS.md`
- `.planning/milestones/v1.1-ROADMAP.md`
- `.planning/milestones/v1.1-REQUIREMENTS.md`

## Current Milestone: v1.2 Controlled Automation Pipeline

**Goal:** Deliver controlled script-to-video automation with curated assets/effects and no UI dependency.

**Target features:**
- Versioned asset registry with stable IDs and strict manifest validation.
- Whitelisted effect profiles with bounded parameters and deterministic behavior.
- Template-based script-to-spec compiler for repeatable scene generation.
- Batch automation pipeline with deterministic witness/regression quality gates.

## Requirements

### Validated

- [x] Deterministic JSON spec ingestion and normalization - v1.0
- [x] Deterministic timeline, lifecycle, draw progression, and camera state resolution - v1.0
- [x] Deterministic full-timeline frame artifact generation - v1.0
- [x] Playable media encoding and audio muxing through the CLI pipeline - v1.0
- [x] Batch media output with deterministic summary artifacts - v1.0
- [x] VideoScribe-like traced strokes, hand guidance, real hand assets, and deterministic text rendering - v1.0
- [x] Authored parity scene decomposition and deterministic parity witness pipeline - v1.1
- [x] Source-like motion/hand sequencing for authored parity sample - v1.1
- [x] Text and illustration fidelity improvements for reference sample composition - v1.1
- [x] Deterministic frame/video parity regression witnesses - v1.1

### Active

- [ ] Build versioned asset/effect governance so all generated scenes use controlled resources.
- [ ] Compile script/scenario inputs into validated spec JSON automatically through templates.
- [ ] Run end-to-end batch generation with deterministic quality gates and auditable manifests.

### Out of Scope

- Interactive editor UI - engine-first delivery remains the priority.
- Whole-frame screenshot/video-crop reconstruction as the primary rendering strategy - output quality must come from engine semantics and authored assets.
- AI-generated image creation in runtime or milestone-critical pipeline - assets must come from curated controlled libraries.
- Realtime collaborative editing - this project remains offline/batch-first.
- Plugin ecosystem and advanced non-core effects - defer until script-to-video automation reliability is stable.

## Context

The repository now contains archived v1.0 and v1.1 milestones plus parity demo assets under `artifacts/source-parity-demo/`. Current direction is to convert the proven deterministic/parity engine into a higher-throughput automation pipeline that can generate many videos from scripts/templates.

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
| v1.1 targets source parity without relying on whole-frame crops | Aligns milestone with user demand and engine-first rules | Good |
| Milestone closeout keeps ROADMAP constant-size by archiving full milestone details in `.planning/milestones/` | Preserves context efficiency for future sessions | Good |
| v1.2 automation must use curated asset/effect libraries (no generative image dependency) | Keeps output quality reviewable, legally traceable, and deterministic | Pending |
| v1.2 remains CLI/spec driven with no editor UI work | Preserves engine-first scope and execution speed | Pending |

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
*Last updated: 2026-04-03 after starting v1.2 Controlled Automation Pipeline milestone*
