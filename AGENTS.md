# AGENTS

## Mission
Build a .NET whiteboard video engine that reproduces core VideoScribe-like rendering behavior. This project is engine-first, not UI-first, and is designed to generate many videos in a consistent format from different scripts/scenarios.

## Current Phase
Repository bootstrap and architecture definition only. Do not build a VideoScribe-like editor UI, and do not generate application code yet.

## Architecture Rules
- Core engine is implemented in .NET.
- System is spec-driven: ingest project/spec JSON inputs, never hardcoded scenes.
- Rendering is deterministic and frame-based for repeatable outputs.
- Keep strict module boundaries: Core, Engine, Renderer, Export, CLI.
- Prefer simple, extensible architecture over premature complexity.

## Coding Rules
- No business/rendering implementation code in this phase.
- Avoid unnecessary dependencies; add only when clearly justified.
- Keep contracts explicit and dependency direction clean across modules.
- Optimize for maintainability, testability, and future batch generation workflows.

## Delivery Rules
- Deliver doc-first, phase-by-phase.
- Reject scope creep into UI/editor work until engine core behavior is stable.
- Preserve deterministic behavior as a non-negotiable requirement.
- Any new work must align with module separation and JSON-driven pipeline design.
