# Next Task Prompt (Copy/Paste Ready)

Use this exact prompt to continue development:

```text
Work on Step 11 for the `whiteboard-engine` solution.

Task:
Create the initial CLI orchestration contracts and CLI skeleton in `Whiteboard.Cli`.

Scope:
This step is about wiring a deterministic, spec-driven pipeline entry point that coordinates Core -> Engine -> Renderer -> Export.
Do not implement rendering algorithms, export encoding, FFmpeg integration, or full runtime workflow behavior.

Modify only:
- `src/Whiteboard.Cli/`
- `tests/Whiteboard.Cli.Tests/` (only if a CLI test project already exists; if not, keep scope to current CLI project)

Do not modify:
- `src/Whiteboard.Core/`
- `src/Whiteboard.Engine/`
- `src/Whiteboard.Renderer/`
- `src/Whiteboard.Export/`
- any other project
- docs outside `docs/ai-dashboard`
- prompts, skills, AGENTS.md, config, or solution structure

Goals:
Create a clean CLI contract layer and orchestration skeleton that can invoke the pipeline in a placeholder-safe way.

Create the following groups in `Whiteboard.Cli`:

1. CLI request/result models
- `CliRunRequest`
- `CliRunResult`

2. Orchestration contracts
- `IPipelineOrchestrator`
- `IProjectSpecLoader` (placeholder)

3. Minimal CLI skeleton
- `PipelineOrchestrator`
- `Program` wiring that parses minimal args and invokes orchestrator
- keep behavior placeholder-level and deterministic

Requirements:
1. Use .NET 8.
2. Keep CLI concerns isolated from Core/Engine/Renderer/Export implementation details.
3. Consume existing contracts from other modules; do not duplicate domain models.
4. Keep deterministic and spec-driven direction explicit.
5. Placeholder-oriented outputs are acceptable in this step.
6. Do not implement real file IO parsing/serialization beyond minimal placeholder handling.
7. Do not add external packages.
8. Keep folder organization simple (`Models/`, `Contracts/`, `Services/`).

Tests:
Add minimal tests (if test project exists) that verify:
- CLI request can be created
- orchestrator can accept request
- result can be produced
- deterministic structure for same input

Execution rules:
- First, briefly show the proposed folder/model plan.
- Then implement in small, reviewable steps.
- Keep CLI skeleton thin and contract-first.

Output:
- Show final folder tree under `src/Whiteboard.Cli/`
- Show all created/updated source files
- Show all created/updated test files (if any)
- Briefly summarize design choices
- Report assumptions
```

## Handoff Context
- Step 7 (Core contracts) is done.
- Step 8 (Engine resolved-frame contracts + resolver skeleton) is done.
- Step 9 (Renderer contracts + render skeleton) is done.
- Step 10 (Export contracts + export skeleton) is done.
- Next logical focus: CLI orchestration contracts and skeleton.
