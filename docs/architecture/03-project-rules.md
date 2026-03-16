# Project Rules

## Engineering Principles
- Prioritize deterministic output, clear contracts, and testability.
- Keep architecture simple and explicit over feature-rich abstractions.
- Evolve in small, verifiable, reviewable phases.
- Stay engine-first until core rendering behavior is stable.

## Architecture Constraints
- Maintain strict module separation: Core, Engine, Renderer, Export, CLI.
- Prevent cross-module leakage of responsibilities.
- Avoid dependencies that reduce portability or increase lock-in.
- Do not introduce UI/editor concerns into the engine phase.

## Deterministic Rendering Rule
- Same spec + same assets + same settings must produce the same frame/video output.
- Time, ordering, and state resolution rules must be explicit and reproducible.
- Determinism is a non-negotiable system property.

## JSON/Spec-Driven Rule
- Project/spec JSON is the source of truth for scene behavior, timing, and output intent.
- Do not rely on hardcoded scene logic.
- Plan for schema evolution with backward-compatible handling where practical.

## Simplicity and Maintainability Rules
- Favor small modules and low coupling.
- Introduce dependencies only when justified by clear value.
- Separate architecture decisions from implementation details in early phases.
- Prefer small, reviewable changes over speculative scaffolding.