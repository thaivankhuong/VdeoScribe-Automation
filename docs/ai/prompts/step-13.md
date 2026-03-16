# Prompt - Step 13

```text
Work on Step 13 for the `whiteboard-engine` solution.

Task:
Expand deterministic contract validation and test coverage across Core -> Engine -> Renderer -> Export -> CLI.

Scope:
This step is about strengthening deterministic guarantees for the existing placeholder pipeline and adding minimal contract tests where test projects already exist.
Do not implement rendering algorithms, FFmpeg integration, real file encoding, file output, or any UI/editor behavior.

Modify only:
- `src/Whiteboard.Core/` (only if a small deterministic-support contract adjustment is required)
- `src/Whiteboard.Engine/` (only if a small deterministic-support contract adjustment is required)
- `src/Whiteboard.Renderer/` (only if a small deterministic-support contract adjustment is required)
- `src/Whiteboard.Export/` (only if a small deterministic-support contract adjustment is required)
- `src/Whiteboard.Cli/` (only if a small deterministic-support contract adjustment is required)
- `tests/Whiteboard.Core.Tests/`
- `tests/Whiteboard.Engine.Tests/`
- `tests/Whiteboard.Renderer.Tests/`

Do not modify:
- any UI/editor-related files
- solution structure
- prompts, skills, AGENTS.md, or unrelated docs/config
- create new test projects unless explicitly requested

Goals:
Create a stronger deterministic baseline for the integrated placeholder pipeline by validating that equivalent inputs produce equivalent contract outputs.

Focus areas:
1. Contract determinism checks
- stable frame-state results for same input
- stable renderer output summaries for same input
- stable export placeholder results for same input
- stable CLI orchestration result structure for same input if supportable without a new test project

2. Minimal support refinements
- only if needed to make deterministic assertions explicit
- keep changes small, contract-first, and module-safe

Requirements:
1. Use .NET 8.
2. Preserve engine-first architecture and strict module boundaries.
3. Prefer test expansion over new implementation.
4. Avoid runtime-heavy behavior and defer real export/render features.
5. Do not add external packages beyond what existing test projects already require.
6. Keep changes small and reviewable.

Tests:
- Add or extend tests only in existing test projects.
- If a module does not already have a test project, do not create one in this step.

Execution rules:
- First, briefly show the proposed test/validation plan.
- Then implement in small, reviewable steps.
- Report any assumptions where current module structure limits coverage.

Output:
- Show updated test files
- Show any source files changed for deterministic support
- Briefly summarize determinism checks added
- Report validation status and remaining gaps
```
