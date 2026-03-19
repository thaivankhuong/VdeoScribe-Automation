# AGENTS

## Mission
Build a .NET whiteboard video engine that reproduces core VideoScribe-like rendering behavior. This project is engine-first, not UI-first, and is designed to generate many videos in a consistent format from different scripts/scenarios.

## Current Phase
Phase 7 - Full-Timeline Render Sequence and Frame Artifact Generation. Phases 7-9 were added after the milestone audit to close business-output gaps between the deterministic engine milestone and real automated video generation. Do not build a VideoScribe-like editor UI, and do not re-open completed phase semantics unless a verified gap requires it.

## Architecture Rules
- Core engine is implemented in .NET.
- System is spec-driven: ingest project/spec JSON inputs, never hardcoded scenes.
- Rendering is deterministic and frame-based for repeatable outputs.
- Keep strict module boundaries: Core, Engine, Renderer, Export, CLI.
- Prefer simple, extensible architecture over premature complexity.

## Coding Rules
- Implement only the active roadmap phase and its directly required tests/docs; defer later gap-closure phases until the roadmap advances.
- Avoid unnecessary dependencies; add only when clearly justified.
- Keep contracts explicit and dependency direction clean across modules.
- Optimize for maintainability, testability, and future batch generation workflows.
- Renderer code must consume Engine handoff contracts without recomputing timeline, draw, or camera semantics.
- Export code must package and encode renderer outputs without changing engine or renderer semantics.
- CLI orchestration code must coordinate existing module contracts without embedding business logic or mutating upstream semantics.

## Delivery Rules
- Deliver phase-by-phase, with docs and verification artifacts kept current.
- Reject scope creep into UI/editor work until engine core behavior is stable.
- Preserve deterministic behavior as a non-negotiable requirement.
- Any new work must align with module separation and JSON-driven pipeline design.
