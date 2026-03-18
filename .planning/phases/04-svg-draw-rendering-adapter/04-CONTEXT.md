# Phase 4: SVG Draw Rendering Adapter - Context

**Gathered:** 2026-03-18
**Status:** Ready for planning

<domain>
## Phase Boundary

Render deterministic SVG frame visuals from resolved Engine frame-state contracts. This phase defines renderer adapter behavior for PIPE-01 only; it does not add new capabilities beyond frame-state consumption and deterministic SVG draw output.

</domain>

<decisions>
## Implementation Decisions

### SVG Draw Reveal Style
- Use path-stroke progressive reveal as the primary rendering primitive.
- Use clean, uniform default stroke styling for v1 deterministic output.
- Respect strict frame-state ordering (scene/object/layer order) during overlap compositing.
- Apply per-frame draw progress exactly as resolved by Engine (no renderer-side smoothing/interpolation).

### Camera Transform Mapping
- Apply camera pan/zoom as deterministic world-to-view transform before object draw emission.
- Use canvas center pivot as the default zoom origin policy.
- Treat Engine-resolved camera state as final semantic input (no renderer-side semantic recomputation).
- Format camera-applied numeric outputs with fixed precision for deterministic parity.

### Edge and Error Policy
- Missing referenced SVG assets are deterministic fail-fast errors.
- Unsupported object types are skipped with deterministic marker operations.
- Malformed SVG/path geometry fails that object deterministically (no auto-repair in this phase).
- Edge outcomes must be visible through renderer operations and render result status payloads.

### Claude's Discretion
- Exact naming format for deterministic marker operations (while keeping stable machine-readability).
- Internal SVG path segmentation strategy when converting draw progression into path subset output.
- Exact fixed precision digit count as long as deterministic parity guarantees are preserved.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Renderer/Services/FrameRenderer.cs`: existing frame render orchestration entry point.
- `src/Whiteboard.Renderer/Services/SceneRenderer.cs`: deterministic scene/object ordering and object-renderer dispatch.
- `src/Whiteboard.Renderer/Services/InMemoryRenderSurface.cs`: stable operation capture surface for deterministic verification.
- `src/Whiteboard.Renderer/Services/PlaceholderObjectRenderer.cs`: current fallback renderer to replace/evolve into SVG adapter behavior.
- `src/Whiteboard.Engine/Models/ResolvedObjectState.cs`: renderer-consumable draw progression payload (`DrawProgress`, `DrawPaths`, ordering keys).

### Established Patterns
- Renderer is adapter-only and must consume Engine output contracts without semantic reinterpretation.
- Determinism is validated via stable operation payloads and equivalent-input parity tests.
- Scene/object ordering is explicitly sorted (`Layer`, then object id), matching deterministic constraints.

### Integration Points
- `PipelineOrchestrator` -> `FrameRenderer.Render(RenderFrameRequest)` is the current runtime integration path.
- `FrameRenderer` -> `SceneRenderer` -> object renderer(s) is the extension seam for SVG draw adapter implementation.
- Renderer outputs (`RenderFrameResult.Operations`) feed Export pipeline deterministic signature composition.

</code_context>

<specifics>
## Specific Ideas

- Keep output deterministic first, visual stylization second for v1 adapter behavior.
- Use marker operations to make unsupported/missing inputs observable in automated parity checks.

</specifics>

<deferred>
## Deferred Ideas

- Hand-drawn style variation/jitter presets and richer artistic styling controls.
- Spec-configurable zoom pivot or renderer smoothing modes.
- Automatic malformed SVG path repair heuristics.

</deferred>

---

*Phase: 04-svg-draw-rendering-adapter*
*Context gathered: 2026-03-18*