# Phase 5: Export Pipeline Integration - Context

**Gathered:** 2026-03-19
**Status:** Completed 2026-03-19
**Source:** Derived from roadmap, state, Phase 4 outputs, and repository constraints

<domain>
## Phase Boundary

Integrate deterministic renderer outputs into an Export module that packages frame results, timing metadata, and audio cue metadata without changing Engine or Renderer semantics. This phase is about export contracts and deterministic packaging flow for `PIPE-02` and `PIPE-03`; it is not editor work, not CLI batch orchestration, and not a jump into broad external encoding infrastructure.

</domain>

<decisions>
## Implementation Decisions

### Export Ownership
- Export consumes renderer outputs and project metadata only; it must not recompute timeline, draw, or camera semantics.
- Export must preserve scene/frame meaning and timing information already resolved upstream.
- Export remains a separate module with explicit contracts and no reverse dependencies into Engine or Renderer internals.

### Deterministic Packaging
- Export deterministic signatures must depend on canonical frame payloads and canonical export metadata, not incidental file-system paths alone.
- Frame ordering must stay stable and reflect explicit frame index / frame-rate semantics.
- Audio cue ordering and packaging metadata must be deterministic and derived from normalized Core contracts.

### Scope Constraints
- Do not introduce UI/editor work.
- Do not pull CLI batch orchestration work from Phase 6 into this phase.
- Do not widen into speculative encoder ecosystems or unnecessary dependencies unless clearly required by the chosen packaging contract.

### Repository Constraints
- Serial `dotnet build`/`dotnet test --no-build` is the stable verification path in this workspace.
- Existing Core contracts already include `AudioAsset`, `AudioCue`, `OutputSpec`, and normalized timeline metadata that Export should reuse.

### Claude's Discretion
- Exact export package representation for this phase, as long as it preserves semantics and determinism.
- Exact naming/shape of export manifest and audio metadata records.
- Whether phase output is represented as deterministic export-package metadata rather than real encoder output, provided `PIPE-02` and `PIPE-03` are still evidenced clearly.

</decisions>

<code_context>
## Existing Code Insights

### Reusable Assets
- `src/Whiteboard.Export/Services/ExportPipeline.cs`: current placeholder export seam.
- `src/Whiteboard.Export/Models/ExportRequest.cs`: current export request entry contract.
- `src/Whiteboard.Export/Models/ExportResult.cs`: current export result/deterministic key seam.
- `src/Whiteboard.Cli/Services/PipelineOrchestrator.cs`: current runtime integration path from renderer into export.
- `src/Whiteboard.Core/Timeline/AudioCue.cs` and `src/Whiteboard.Core/Assets/AudioAsset.cs`: normalized audio metadata already available upstream.

### Current Gaps
- Export currently only counts frames/operations and emits a placeholder deterministic key.
- Export request/result contracts do not capture frame timing, audio packaging metadata, or scene-preserving export summaries.
- CLI currently exports one rendered frame at a requested index and does not surface richer export package metadata.
- Existing deterministic tests cover CLI parity but not export-specific metadata fidelity.

</code_context>

<specifics>
## Specific Ideas

- Keep Phase 5 focused on explicit export package contracts and deterministic metadata first.
- Reuse existing CLI integration tests and add export-focused contract tests in existing test projects rather than creating a new test project unless clearly necessary.
- Use Phase 4 renderer operations plus Core audio/timing metadata as the canonical inputs to export signatures.

</specifics>

<deferred>
## Deferred Ideas

- Full FFmpeg or external encoder integration.
- Rich multi-scene batch orchestration beyond the current single-run CLI path.
- Advanced audio mixing, waveform processing, or codec-specific tuning.

</deferred>

---

*Phase: 05-export-pipeline-integration*
*Context gathered: 2026-03-19*

