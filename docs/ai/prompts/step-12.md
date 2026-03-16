# Prompt - Step 12

```text
Work on Step 12 for the `whiteboard-engine` solution.

Task:
Perform the end-to-end contract integration pass across Core -> Engine -> Renderer -> Export -> CLI.

Scope:
This step is about wiring the existing module contracts into a deterministic placeholder pipeline that reaches the Export layer through the CLI entry point.
Do not implement rendering algorithms, FFmpeg integration, real file encoding, file output, or any UI/editor behavior.

Modify only:
- `src/Whiteboard.Export/`
- `src/Whiteboard.Cli/`
- `tests/Whiteboard.Export.Tests/` (only if a matching test project already exists)
- `tests/Whiteboard.Cli.Tests/` (only if a matching test project already exists)

Do not modify:
- `src/Whiteboard.Core/`
- `src/Whiteboard.Engine/`
- `src/Whiteboard.Renderer/`
- any other project
- docs outside the AI dashboard workflow
- prompts, skills, AGENTS.md, config, or solution structure

Goals:
Create a clean, deterministic end-to-end placeholder path where:
- CLI loads a project spec placeholder
- Engine resolves frame state
- Renderer produces render output
- Export receives renderer output through explicit contracts
- CLI reports the integrated pipeline result

Create or complete the following groups:

1. Export request/result models
- `ExportRequest`
- `ExportResult`
- `ExportTarget`

2. Export contracts
- `IExportPipeline`
- any minimal helper contracts needed for placeholder export stages

3. Export services
- `ExportPipeline`
- lightweight placeholder export stage implementations if needed

4. CLI integration
- update `PipelineOrchestrator` to call `Whiteboard.Export`
- keep result reporting deterministic and placeholder-safe

Requirements:
1. Use .NET 8.
2. Preserve engine-first architecture and strict module boundaries.
3. Keep the pipeline spec-driven and deterministic.
4. Consume existing contracts from Engine and Renderer rather than duplicating state.
5. Placeholder outputs are acceptable, but the Export layer must be a real contract participant now.
6. Do not implement actual filesystem export, video encoding, or FFmpeg behavior.
7. Do not add external packages.
8. Keep changes small and reviewable.

Tests:
Add or update minimal tests only if matching CLI/Export test projects already exist.
If they do not exist, keep scope to the source projects only.

Execution rules:
- First, briefly show the proposed integration plan.
- Then implement in small, reviewable steps.
- Prefer contract-first and deterministic placeholder behavior over runtime-heavy logic.

Output:
- Show final folder tree under `src/Whiteboard.Export/`
- Show created/updated files under `src/Whiteboard.Cli/`
- Show created/updated test files (if any)
- Briefly summarize design choices
- Report assumptions and validation status
```
