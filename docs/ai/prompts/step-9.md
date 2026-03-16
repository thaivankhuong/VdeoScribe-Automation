# Prompt - Step 9

```text
Work on Step 9 for the `whiteboard-engine` solution.

Task:
Create the initial renderer contracts and render skeleton in `Whiteboard.Renderer`.

Scope:
Define how resolved engine frame state is handed to a renderer pipeline.
Do not implement SVG drawing, text rasterization, image rendering, FFmpeg export, file output, or CLI behavior.

Modify only:
- `src/Whiteboard.Renderer/`
- `tests/Whiteboard.Renderer.Tests/`

Goals:
Create renderer contract layer accepting `ResolvedFrameState` and producing structured render results.

Create groups:
1. Request/Result models (`RenderFrameRequest`, `RenderFrameResult`, `RenderSurfaceSize`)
2. Contracts (`IFrameRenderer`, `ISceneRenderer`, `IObjectRenderer`, `IRenderSurface`)
3. Skeleton services (`FrameRenderer`, `SceneRenderer`, simple object-render routing)

Requirements:
- Use .NET 8.
- Consume `ResolvedFrameState` from `Whiteboard.Engine`.
- Keep deterministic, placeholder-level structure.
- No actual drawing algorithms or file output.
```
