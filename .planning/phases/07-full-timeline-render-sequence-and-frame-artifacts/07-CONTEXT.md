# Phase 7: Full-Timeline Render Sequence and Frame Artifact Generation - Context

**Gathered:** 2026-03-19
**Status:** Ready for planning
**Source:** Roadmap Phase 7 + milestone audit gap closure

<domain>
## Phase Boundary

Phase 7 closes the gap between the current single-frame deterministic pipeline and a deterministic full-timeline rendering pipeline.

This phase must extend CLI/Core/Engine/Renderer/Export orchestration so one spec-driven run can evaluate the complete project timeline, generate an ordered frame sequence, and emit user-visible frame artifacts for every frame in the run.

The phase ends at deterministic frame artifact generation and artifact-level parity evidence.

This phase must not absorb Phase 8 concerns:
- no playable video encoding
- no audio muxing into final media
- no production batch media guarantees beyond what is needed to support later phases
</domain>

<decisions>
## Implementation Decisions

### Locked Decisions
- Full-timeline execution must remain spec-driven and deterministic for identical spec, asset, and settings inputs.
- Phase 7 must replace the current one-frame run path with ordered full-sequence generation at the CLI/export handoff boundary.
- Renderer and export must produce user-visible frame artifacts rather than operation metadata only.
- Frame artifacts must be generated in stable order with deterministic naming and deterministic manifest/package evidence.
- The phase must preserve existing engine semantics for time, draw progression, and camera state rather than reinterpreting them downstream.
- Phase 7 must keep module boundaries intact: Core/Engine resolve semantics, Renderer emits frame visuals, Export packages frame artifacts, CLI orchestrates only.
- Verification for this phase must prove repeated runs and equivalent inputs yield identical ordered frame artifact sets.

### Claude's Discretion
- Exact artifact format for frame outputs, as long as it is deterministic and supports later encoding work.
- Internal batching/chunking strategy for frame generation, if deterministic and phase-scoped.
- Whether frame artifact manifests live in Export models or CLI-facing summaries, as long as responsibilities stay clean.
</decisions>

<specifics>
## Specific Ideas

- The milestone audit identified four concrete business blockers that Phase 7 starts closing:
  - run path is single-frame only
  - renderer/export output is not a user-visible frame artifact
  - batch and CLI currently prove package metadata only
  - full project timeline is not rendered end-to-end
- Current stable verification path in this repo is serial:
  - `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1`
  - followed by targeted `dotnet test --no-build` commands
- Phase requirement ID is `PIPE-04`.
</specifics>

<deferred>
## Deferred Ideas

- Playable video encoding and codec integration
- Audio render, mix, and mux into final media output
- Production guarantee that every batch job yields completed media artifacts
- Re-audit of milestone business readiness after Phases 7-9
</deferred>

---

*Phase: 07-full-timeline-render-sequence-and-frame-artifacts*
*Context gathered: 2026-03-19 via roadmap + milestone audit*
