---
phase: 16
slug: controlled-asset-and-effect-registry
status: passed
requirements:
  - REG-01
  - REG-02
  - REG-03
  - EFX-01
  - EFX-02
verified_on: 2026-04-03
---

# Phase 16 Verification

## Verdict
Passed.

## Requirement Coverage
1. `REG-01` is satisfied by explicit asset registry snapshot contracts in core models (`AssetRegistrySnapshot`, `AssetCollection.RegistrySnapshot`, `ProjectMeta.AssetRegistrySnapshotId`).
2. `REG-02` is satisfied by deterministic snapshot normalization and semantic pinning checks (`required/id/version/mismatch`).
3. `REG-03` is satisfied by deterministic rejection of unknown/deprecated snapshot IDs and actionable CLI diagnostics.
4. `EFX-01` is satisfied by `TimelineDefinition.EffectProfiles` governance contracts plus semantic profile existence/action matching.
5. `EFX-02` is satisfied by deterministic parameter-bound validation with invariant-culture numeric parsing.

## Evidence
- `src/Whiteboard.Core/Assets/AssetRegistrySnapshot.cs`
- `src/Whiteboard.Core/Assets/AssetCollection.cs`
- `src/Whiteboard.Core/Models/ProjectMeta.cs`
- `src/Whiteboard.Core/Timeline/EffectProfile.cs`
- `src/Whiteboard.Core/Timeline/EffectParameterBound.cs`
- `src/Whiteboard.Core/Timeline/TimelineDefinition.cs`
- `src/Whiteboard.Core/Validation/SpecProcessingPipeline.cs`
- `src/Whiteboard.Cli/Services/ProjectSpecLoader.cs`
- `tests/Whiteboard.Core.Tests/VideoProjectContractTests.cs`
- `tests/Whiteboard.Core.Tests/SpecProcessingPipelineTests.cs`
- `tests/Whiteboard.Cli.Tests/ProjectSpecLoaderTests.cs`
- `tests/Whiteboard.Cli.Tests/Fixtures/phase16-controlled-registry/unknown-registry-snapshot.json`
- `tests/Whiteboard.Cli.Tests/Fixtures/phase16-controlled-registry/deprecated-registry-snapshot.json`
- `tests/Whiteboard.Cli.Tests/Fixtures/phase16-controlled-registry/effect-parameter-out-of-range.json`

## Automated Verification
```powershell
$env:DOTNET_CLI_HOME = Join-Path (Get-Location) '.dotnet'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
dotnet build "whiteboard-engine.sln" --no-restore -v minimal -m:1
dotnet test "tests/Whiteboard.Core.Tests/Whiteboard.Core.Tests.csproj" -v minimal --filter "FullyQualifiedName~VideoProjectContractTests|FullyQualifiedName~SpecProcessingPipelineTests"
dotnet test "tests/Whiteboard.Cli.Tests/Whiteboard.Cli.Tests.csproj" --no-build -v minimal --filter "FullyQualifiedName~ProjectSpecLoaderTests"
```

## Must-Have Check
- Projects can pin controlled registry snapshots in canonical spec contracts.
- Unknown or deprecated snapshot references fail deterministically at spec load.
- Effect profile usage is whitelisted and action-type consistent.
- Governed numeric parameters fail fast when out of range.
- CLI errors preserve deterministic `[Gate] Code at Path: Message` signatures for automation triage.

## Scope Note
Phase 16 establishes controlled asset/effect governance contracts and validation only. Template instantiation and script compilation remain Phase 17/18 scope.
