---
phase: 06
slug: cli-batch-orchestration-and-end-to-end-validation
status: ready
nyquist_compliant: true
wave_0_complete: true
created: 2026-03-19
---

# Phase 06 - Validation Strategy

> Per-phase validation contract for feedback sampling during execution.

---

## Test Infrastructure

| Property | Value |
|----------|-------|
| **Framework** | xUnit (.NET test SDK) |
| **Config file** | `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` |
| **Quick run command** | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"` |
| **Full suite command** | `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1` |
| **Estimated runtime** | Quick checks <= 30 seconds after build; serial build ~90-120 seconds |

---

## Sampling Rate

- **After every task commit:** Run the task-specific filtered command from the per-task map.
- **After every plan wave:** Run one serial build plus one CLI smoke suite.
- **Before `$gsd-verify-work`:** Serial solution build and targeted CLI deterministic suites must be green.
- **Max feedback latency:** 30 seconds after a successful build.

---

## Per-Task Verification Map

| Task ID | Plan | Wave | Requirement | Test Type | Automated Command | File Exists | Status |
|---------|------|------|-------------|-----------|-------------------|-------------|--------|
| 06-01-01 | 06-01 | 1 | CLI-01 | build | `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1` | ? | ? pending |
| 06-01-02 | 06-01 | 1 | CLI-01, CLI-02 | build | `dotnet build "whiteboard-engine.sln" --no-restore -v minimal /m:1` | ? | ? pending |
| 06-01-03 | 06-01 | 1 | CLI-01, CLI-02 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | ? | ? pending |
| 06-02-01 | 06-02 | 2 | CLI-01 | unit | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~CliCommandParserTests"` | ? | ? pending |
| 06-02-02 | 06-02 | 2 | CLI-02 | unit | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~BatchPipelineOrchestratorTests"` | ? | ? pending |
| 06-02-03 | 06-02 | 2 | CLI-01, CLI-02 | integration | `dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~PipelineOrchestratorIntegrationTests"` | ? | ? pending |

*Status: ? pending · ? green · ? red · ?? flaky*

---

## Wave 0 Requirements

- Existing infrastructure covers all phase requirements, but `tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj` must be kept in sync with any new CLI-side source files or test files added during execution.

---

## Manual-Only Verifications

- All phase behaviors should have automated verification; manual review is limited to confirming the persisted `--summary-output` JSON artifact remains readable enough for humans and stable enough for CI automation.

---

## Validation Sign-Off

- [x] All tasks have `<automated>` verify or Wave 0 dependencies
- [x] Sampling continuity: no 3 consecutive tasks without automated verify
- [x] Wave 0 covers all MISSING references
- [x] No watch-mode flags
- [x] Feedback latency <= 30s after build
- [x] `nyquist_compliant: true` set in frontmatter

**Approval:** pending
