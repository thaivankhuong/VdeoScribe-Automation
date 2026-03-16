# Prompt - Step 7

```text
Work on Step 7 for the `whiteboard-engine` solution.

Task:
Create the initial domain model and spec contracts in `Whiteboard.Core` only.

Scope:
This step is about defining the core data structures for the whiteboard video engine.
Do not implement rendering, timeline resolution, SVG animation logic, export logic, or CLI behavior.

Create or update only files inside:
- `src/Whiteboard.Core/`
- `tests/Whiteboard.Core.Tests/`

Goals:
Define a clean, minimal, extensible contract model for project/spec-driven video generation.

Required domain model groups:

1. Project root
- `VideoProject`
- `ProjectMeta`
- `OutputSpec`

2. Assets
- `AssetCollection`
- `SvgAsset`
- `AudioAsset`
- `FontAsset`
- `HandAsset`

3. Scene
- `SceneDefinition`
- `SceneObject`

4. Timeline
- `TimelineDefinition`
- `TimelineEvent`
- `CameraTrack`
- `CameraKeyframe`
- `AudioCue`

5. Shared/value objects
- `TransformSpec`
- `Position2D`
- `Size2D`

6. Enums
- `SceneObjectType`
- `TimelineActionType`
- `EasingType`
- `AssetType`

Requirements:
1. Use .NET 8.
2. Keep everything inside `Whiteboard.Core` focused on domain models, contracts, and schema-safe structures.
3. Do not add rendering logic, export-specific logic, UI/editor concerns, or CLI orchestration logic.
4. Keep the model JSON/spec-friendly.
5. Prefer explicit, readable structures over speculative abstractions.
6. Use folders/namespaces that keep the model organized, but do not over-engineer.
7. Add minimal placeholder tests in `Whiteboard.Core.Tests` that verify:
   - a `VideoProject` can be instantiated
   - core collections/objects can be composed
   - no behavioral logic is being tested yet
8. Do not add external packages except what is already required by the test project.
9. Do not modify projects outside `Whiteboard.Core` and `Whiteboard.Core.Tests`.

Execution rules:
- If the structure choice is non-trivial, briefly show the proposed folder/model plan before writing code.
- Keep changes small and reviewable.
- Prefer records or simple classes where appropriate.
- Do not introduce base classes, generic frameworks, or advanced inheritance unless clearly necessary.
```
