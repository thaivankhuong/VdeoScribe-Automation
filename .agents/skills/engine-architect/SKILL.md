---
name: engine-architect
description: Define module boundaries, dependency direction, project structure, and architecture decisions for the .NET whiteboard engine. Use when planning or reviewing system architecture before implementation. Do not use for detailed rendering logic, SVG animation behavior, or FFmpeg export specifics.
---

## Purpose
Define and protect the engine-first architecture for a .NET whiteboard video system.

## When to Use
- Designing or refining `Core`, `Engine`, `Renderer`, `Export`, and `CLI` boundaries.
- Reviewing dependency direction and repository/project structure decisions.
- Evaluating architecture tradeoffs during bootstrap and early implementation planning.
- Checking whether a proposed change fits the current engine-first phase.

## Rules
- Keep strict module separation: `Core` defines contracts and domain models, `Engine` coordinates timeline/state logic, `Renderer` handles frame drawing, `Export` handles output packaging, and `CLI` orchestrates only.
- Keep the system spec-driven: project/video behavior must come from JSON/spec input, not hardcoded scenes.
- Prevent UI/editor scope in the current phase.
- Prefer simple, extensible contracts over framework-heavy designs.
- Avoid unnecessary dependencies.
- Do not produce implementation code in this phase unless explicitly requested.

## Expected Output
- Concise architecture decision notes.
- Proposed module map and dependency rules.
- Risks, tradeoffs, and boundary clarifications.
- Clear acceptance criteria for architecture consistency.