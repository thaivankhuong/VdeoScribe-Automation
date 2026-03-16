# Prompt - Step 8

```text
Work on Step 8 for the `whiteboard-engine` solution.

Task:
Create the initial frame-state resolution contracts and engine skeleton in `Whiteboard.Engine`.

Scope:
This step is about defining how the engine resolves timeline/spec data into deterministic frame state.
Do not implement rendering, SVG drawing, export logic, file output, or CLI behavior.

Modify only:
- `src/Whiteboard.Engine/`
- `tests/Whiteboard.Engine.Tests/`

Goals:
Define a clean, minimal engine contract layer that can transform project/spec input into resolved frame state.

Create model groups:
1. Frame context (`FrameContext`)
2. Resolved models (`ResolvedFrameState`, `ResolvedSceneState`, `ResolvedObjectState`, `ResolvedCameraState`, `ResolvedTimelineEvent`)
3. Resolver contracts (`IFrameStateResolver`, `ITimelineResolver`, `IObjectStateResolver`, `ICameraStateResolver`)
4. Minimal orchestration skeleton (`FrameStateResolver`)

Requirements:
- Use .NET 8.
- Consume `VideoProject` from `Whiteboard.Core`.
- Keep deterministic, frame-based behavior.
- Keep placeholder logic simple and future-safe.
- No rendering/export logic.
```
