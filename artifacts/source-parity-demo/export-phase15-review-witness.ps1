param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$cliDllPath = Join-Path $repoRoot 'src\Whiteboard.Cli\bin\Debug\net8.0\Whiteboard.Cli.dll'
$specPath = Join-Path $repoRoot 'artifacts\source-parity-demo\project-engine.json'
$outputMediaPath = Join-Path $repoRoot 'artifacts\source-parity-demo\out\phase15-review-witness.mp4'
$outputPackagePath = Join-Path $repoRoot 'artifacts\source-parity-demo\out\phase15-review-witness'
$manifestPath = Join-Path $outputPackagePath 'frame-manifest.json'
$reviewTargetsPath = Join-Path $repoRoot 'artifacts\source-parity-demo\check\phase14-review-targets.json'
$bundlePath = Join-Path $repoRoot 'artifacts\source-parity-demo\check\phase15-review-bundle.json'

if (-not (Test-Path $cliDllPath)) {
    throw "CLI assembly not found: $cliDllPath"
}

if (-not $env:WHITEBOARD_ENABLE_PLAYABLE_MEDIA) {
    $env:WHITEBOARD_ENABLE_PLAYABLE_MEDIA = '0'
}

if (Test-Path $outputPackagePath) {
    Remove-Item $outputPackagePath -Recurse -Force
}

if (Test-Path $outputMediaPath) {
    Remove-Item $outputMediaPath -Force
}

& dotnet $cliDllPath --spec $specPath --output $outputMediaPath
if ($LASTEXITCODE -ne 0) {
    throw "Phase 15 witness export failed with exit code $LASTEXITCODE."
}

if (-not (Test-Path $manifestPath)) {
    throw "Witness manifest not found: $manifestPath"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$reviewTargets = Get-Content $reviewTargetsPath -Raw | ConvertFrom-Json
$playableMediaExists = Test-Path $outputMediaPath
$playableMediaStatus = if ($playableMediaExists) {
    'encoded'
}
elseif ($env:WHITEBOARD_ENABLE_PLAYABLE_MEDIA -eq '1' -and $env:WHITEBOARD_FFMPEG_PATH) {
    'requested-but-missing'
}
else {
    'not-configured'
}

$anchorFrames = @(
    foreach ($frame in $reviewTargets.frames) {
        [ordered]@{
            frameIndex = [int]$frame.frameIndex
            objectId = [string]$frame.objectId
            assetId = [string]$frame.assetId
            relativeArtifactPath = ('frames/frame-{0:000000}.svg' -f [int]$frame.frameIndex)
        }
    }
)

$bundle = [ordered]@{
    sourceSpec = 'project-engine.json'
    baselineWitness = 'out/phase14-fidelity-witness'
    reviewWitness = 'out/phase15-review-witness'
    frameManifest = 'out/phase15-review-witness/frame-manifest.json'
    frameCount = [int]$manifest.frameCount
    anchorFrames = $anchorFrames
    lineage = @(
        [ordered]@{
            phase = '14'
            witness = 'out/phase14-fidelity-witness'
            frameManifest = 'out/phase14-fidelity-witness/frame-manifest.json'
        },
        [ordered]@{
            phase = '15'
            witness = 'out/phase15-review-witness'
            frameManifest = 'out/phase15-review-witness/frame-manifest.json'
        }
    )
    playableMedia = [ordered]@{
        status = $playableMediaStatus
        relativePath = if ($playableMediaExists) { 'out/phase15-review-witness.mp4' } else { $null }
        expectedWhenEnabled = 'out/phase15-review-witness.mp4'
        enableFlag = 'WHITEBOARD_ENABLE_PLAYABLE_MEDIA'
        ffmpegPathFlag = 'WHITEBOARD_FFMPEG_PATH'
    }
}

$bundle | ConvertTo-Json -Depth 6 | Set-Content $bundlePath
