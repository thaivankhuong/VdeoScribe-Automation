# Next Task Prompt (Copy/Paste Ready)

Use this exact prompt to continue development:

```text
Work on Step 10 for the `whiteboard-engine` solution.

Task:
Create the initial export contracts and export skeleton in `Whiteboard.Export`.

Scope:
This step is about defining how renderer frame results are handed to an export pipeline.
Do not implement FFmpeg integration, file writing, image encoding, muxing, or CLI behavior.

Modify only:
- `src/Whiteboard.Export/`
- `tests/Whiteboard.Export.Tests/` (create if tests for export are already planned in repo; otherwise keep scope to existing test structure)

Do not modify:
- `src/Whiteboard.Core/`
- `src/Whiteboard.Engine/`
- `src/Whiteboard.Renderer/`
- any other project
- docs outside `docs/ai-dashboard`
- prompts, skills, AGENTS.md, config, or solution structure

Goals:
Create a clean export contract layer that can accept renderer output and produce structured export results.

Create the following groups in `Whiteboard.Export`:

1. Request/Result models
- `ExportRequest`
- `ExportResult`
- `ExportTarget`

2. Exporter contracts
- `IFrameSequenceExporter`
- `IVideoExporter`
- `IExportPipeline`

3. Minimal export skeleton
- `ExportPipeline`
- lightweight placeholder implementations for frame-sequence and video export stages

Requirements:
1. Use .NET 8.
2. Keep export concerns isolated from rendering and CLI concerns.
3. Consume renderer outputs as primary input (do not pull raw scene/spec directly unless absolutely necessary).
4. Keep behavior deterministic and future-safe.
5. Placeholder-oriented results are acceptable in this step.
6. Do not implement actual file output or encoding yet.
7. Do not add external packages.
8. Keep folder organization simple (`Models/`, `Contracts/`, `Services/`).

Tests:
Add minimal tests that verify:
- an export request can be created
- export pipeline can accept request
- export result can be produced
- deterministic structure for same input

Execution rules:
- First, briefly show the proposed folder/model plan.
- Then implement in small, reviewable steps.
- Keep the export skeleton thin and contract-first.

Output:
- Show final folder tree under `src/Whiteboard.Export/`
- Show all created/updated source files
- Show all created/updated test files
- Briefly summarize design choices
- Report assumptions
```

## Handoff Context
- Step 7 (Core contracts) is done.
- Step 8 (Engine resolved-frame contracts + resolver skeleton) is done.
- Step 9 (Renderer contracts + render skeleton) is done.
- Next logical focus: Export layer contracts and pipeline skeleton.
