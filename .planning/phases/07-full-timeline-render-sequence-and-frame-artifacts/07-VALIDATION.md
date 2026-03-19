---
phase: 07
slug: full-timeline-render-sequence-and-frame-artifacts
status: draft
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-19
---

# Phase 07 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit / dotnet test |
| **Config file** | none - existing solution/test project setup |
| **Quick run command** | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~RenderSequencePlannerTests"` |
| **Full suite command** | `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1` then targeted serial Engine, Renderer, and CLI `dotnet test --no-build` runs |
| **Estimated runtime** | ~75 seconds |

---

## Sampling Rate

- **After every task commit:** Run the targeted Engine, Renderer, or CLI test group tied to the seam you changed.
- **After every plan wave:** Run the serial full verification path for all touched test projects.
- **Before `$gsd-verify-work`:** Full targeted suite must be green.
- **Max feedback latency:** 90 seconds

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 07-01-01 | 01 | 1 | PIPE-04 | engine | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~RenderSequencePlannerTests"` | partial | pending |
| 07-01-02 | 01 | 1 | PIPE-04 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests|FullyQualifiedName~BatchPipelineOrchestratorTests"` | yes | pending |
| 07-02-01 | 02 | 2 | PIPE-04 | renderer | `dotnet test "tests/Whiteboard.Renderer.Tests/Whiteboard.Renderer.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~FrameRendererContractTests"` | yes | pending |
| 07-02-02 | 02 | 2 | PIPE-04 | contract | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests|FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | yes | pending |
| 07-03-01 | 03 | 3 | PIPE-04 | engine | `dotnet test "tests/Whiteboard.Engine.Tests/Whiteboard.Engine.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~RenderSequencePlannerTests"` | partial | pending |
| 07-03-02 | 03 | 3 | PIPE-04 | parity | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ExportPipelineContractTests|FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | yes | pending |

*Status: pending / green / red / flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers build and xUnit execution.
- Add `RenderSequencePlannerTests.cs` under `tests/Whiteboard.Engine.Tests` for duration-rule and audio-overhang coverage.
- Extend existing Renderer and CLI test projects; no new framework install is expected.

---

## Manual-Only Verifications

| Behavior | Requirement | Why Manual | Test Instructions |
|----------|-------------|------------|-------------------|
| Frame artifact output is user-inspectable and ordered as expected | PIPE-04 | Human inspection is useful to confirm artifact naming/package readability | Run one full-sequence fixture, inspect generated artifact manifest and artifact directory ordering. |
| Audio-overhang tail is represented as expected in artifact output | PIPE-04 | Manual inspection helps confirm the tail is understandable in package contents | Run an audio-overhang fixture, inspect tail frame count and manifest end time. |

---

## Validation Sign-Off

- [x] All tasks have automated verify or existing infrastructure support
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all missing references
- [x] No watch-mode flags
- [x] Feedback latency < 90s on targeted path
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending

