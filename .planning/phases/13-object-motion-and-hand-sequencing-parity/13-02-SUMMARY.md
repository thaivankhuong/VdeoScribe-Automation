---
phase: 13-object-motion-and-hand-sequencing-parity
plan: 02
subsystem: renderer-hand-guidance
tags: [parity, hand-guidance, renderer, sequencing, deterministic]
requires:
  - phase: 13-object-motion-and-hand-sequencing-parity
    provides: transform-aware deterministic frame-state payloads for authored motion
provides:
  - ordering-aware hand guidance selection across svg, text, and image operations
  - ordering metadata on text and image render operations for deterministic hand-follow arbitration
  - repo-level authored witness coverage proving hand guidance transitions follow the authored object sequence
affects: [phase-13, parity-demo, phase-15]
tech-stack:
  added: []
  patterns: [ordering-aware overlay arbitration, authored-sequence regression checks]
key-files:
  created: [.planning/phases/13-object-motion-and-hand-sequencing-parity/13-02-SUMMARY.md]
  modified: [src/Whiteboard.Renderer/Services/TextObjectRenderer.cs, src/Whiteboard.Renderer/Services/ImageObjectRenderer.cs, src/Whiteboard.Renderer/Services/FrameRenderer.cs, src/Whiteboard.Renderer/Services/FrameRenderer.Phase11.cs, src/Whiteboard.Renderer/Services/FrameRenderer.Phase11.Parse.cs, src/Whiteboard.Renderer/Services/FrameRenderer.Image.cs, tests/Whiteboard.Renderer.Tests/FrameRendererContractTests.cs, tests/Whiteboard.Renderer.Tests/SvgObjectRendererTests.cs, tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs, .planning/STATE.md, .planning/ROADMAP.md]
key-decisions:
  - "Represent text and image draw operations with authored ordering metadata so hand guidance arbitration can stay deterministic across object types."
  - "Choose the earliest active authored ordering among overlay candidates instead of privileging SVG paths by renderer-local heuristic."
patterns-established:
  - "Any renderer-visible draw operation that can own hand guidance must carry ordering metadata when authored sequencing matters."
  - "Repo-level parity tests should validate the guidance-object transition sequence, not only the presence of a hand overlay."
requirements-completed: [PAR-03]
duration: 52 min
completed: 2026-03-22
---

# Phase 13 Plan 02: Improve hand-follow behavior and object-to-object sequencing for parity scenes Summary

**Closed the second Phase 13 gap by making hand guidance follow authored ordering across object types and by locking the authored witness transition sequence with renderer and CLI regression coverage.**

## Performance

- **Duration:** 52 min
- **Completed:** 2026-03-22
- **Tasks:** 3
- **Files modified:** 11

## Accomplishments
- Added ordering metadata to text and image render operations so hand-follow arbitration can compare SVG, text, and image candidates on the same deterministic scale.
- Replaced the old `ResolveHandGuidanceOverlay` heuristic with an ordering-aware selector that chooses the earliest active authored candidate and preserves stable tie-breaking by operation index.
- Added renderer tests that prove earlier authored text wins over a later active SVG path, and CLI integration coverage that validates the authored witness hand sequence `object-left -> object-arrow -> object-title -> object-clock-group -> object-body -> object-footer`.

## Files Created/Modified
- `src/Whiteboard.Renderer/Services/TextObjectRenderer.cs` - Emits deterministic ordering metadata for text operations.
- `src/Whiteboard.Renderer/Services/ImageObjectRenderer.cs` - Emits deterministic ordering metadata for image operations.
- `src/Whiteboard.Renderer/Services/FrameRenderer.cs` - Chooses hand guidance by authored ordering instead of hardcoded SVG precedence.
- `src/Whiteboard.Renderer/Services/FrameRenderer.Phase11.cs` - Emits text ordering metadata into SVG frame artifacts.
- `src/Whiteboard.Renderer/Services/FrameRenderer.Phase11.Parse.cs` - Parses text ordering metadata for hand guidance resolution.
- `src/Whiteboard.Renderer/Services/FrameRenderer.Image.cs` - Emits/parses image ordering metadata for hand guidance resolution.
- `tests/Whiteboard.Renderer.Tests/FrameRendererContractTests.cs` - Locks ordering-aware hand guidance behavior and text/image artifact metadata.
- `tests/Whiteboard.Renderer.Tests/SvgObjectRendererTests.cs` - Covers ordering metadata on text and image operations.
- `tests/Whiteboard.Cli.Tests/PipelineOrchestratorIntegrationTests.cs` - Verifies authored witness hand guidance transitions across the full object sequence.

## Verification
- `dotnet build 'whiteboard-engine.sln' --no-restore -v minimal /m:1`
- `dotnet test 'tests/Whiteboard.Renderer.Tests/Whiteboard.Renderer.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~FrameRendererContractTests|FullyQualifiedName~SvgObjectRendererTests"`
- `dotnet test 'tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj' --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"`

## Decisions Made
- Keep hand-follow driven by renderer-visible operations, but remove SVG-specific precedence so authored sequencing survives mixed object types.
- Leave `project-engine.json` unchanged because the authored witness already exposes the intended object transition order; the gap was regression coverage and selection semantics.

## Issues Encountered
- `apply_patch` remained unreliable in this Windows workspace, so PowerShell file writes were used again for the final edits.
- The repo still contains a large unrelated dirty worktree; this phase intentionally touched only renderer hand-guidance files, the parity tests, and planning artifacts.

## Next Phase Readiness
- Hand-follow now carries authored ordering across SVG, text, and image operations.
- The authored witness route has regression coverage for both overlap arbitration and full-scene object transitions.
- Phase 13-03 can focus on witness validation and exported motion/hand timing evidence rather than reopening selection semantics.

---
*Phase: 13-object-motion-and-hand-sequencing-parity*
*Completed: 2026-03-22*
