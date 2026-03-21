Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase

$projectRoot = Join-Path (Get-Location) 'artifacts/source-parity-demo'
$assetDir = Join-Path $projectRoot 'engine-assets'
$checkDir = Join-Path $projectRoot 'check'
if (Test-Path $assetDir) { Remove-Item $assetDir -Recurse -Force }
New-Item -ItemType Directory -Path $assetDir | Out-Null
if (-not (Test-Path $checkDir)) { New-Item -ItemType Directory -Path $checkDir | Out-Null }

function New-Typeface([string]$family, [switch]$Bold) {
    if ($Bold) {
        return [System.Windows.Media.Typeface]::new(
            [System.Windows.Media.FontFamily]::new($family),
            [System.Windows.FontStyles]::Normal,
            [System.Windows.FontWeights]::Bold,
            [System.Windows.FontStretches]::Normal)
    }

    return [System.Windows.Media.Typeface]::new($family)
}

function Get-TextPathInfo {
    param(
        [string]$Text,
        [string]$FontFamily,
        [double]$FontSize,
        [double]$X,
        [double]$Y,
        [switch]$Bold
    )

    $typeface = New-Typeface -family $FontFamily -Bold:$Bold
    $formatted = [System.Windows.Media.FormattedText]::new(
        $Text,
        [System.Globalization.CultureInfo]::GetCultureInfo('vi-VN'),
        [System.Windows.FlowDirection]::LeftToRight,
        $typeface,
        $FontSize,
        [System.Windows.Media.Brushes]::Black,
        1.0)
    $geometry = $formatted.BuildGeometry([System.Windows.Point]::new($X, $Y))
    $path = [System.Windows.Media.PathGeometry]::CreateFromGeometry($geometry)
    $data = $path.ToString([System.Globalization.CultureInfo]::InvariantCulture) -replace '^F[01]', ''
    [pscustomobject]@{
        Data = $data
        Bounds = $geometry.Bounds
    }
}

function Write-Utf8File([string]$path, [string]$content) {
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($path, $content, $utf8)
}

function Write-JsonFile([string]$path, $value) {
    Write-Utf8File $path (($value | ConvertTo-Json -Depth 8) + [Environment]::NewLine)
}

$titleA = Get-TextPathInfo -Text 'THÃƒâ€œI' -FontFamily 'Arial Black' -FontSize 110 -X 8 -Y 8 -Bold
$titleB = Get-TextPathInfo -Text 'QUEN' -FontFamily 'Arial Black' -FontSize 110 -X ([Math]::Ceiling($titleA.Bounds.Right) + 22) -Y 8 -Bold
$titleWidth = [Math]::Ceiling([Math]::Max($titleA.Bounds.Right, $titleB.Bounds.Right) + 12)
$titleHeight = [Math]::Ceiling([Math]::Max($titleA.Bounds.Bottom, $titleB.Bounds.Bottom) + 12)
$titleSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$titleWidth" height="$titleHeight" viewBox="0 0 $titleWidth $titleHeight">
  <path fill="#EF533A" d="$($titleA.Data)" />
  <path fill="#EF533A" d="$($titleB.Data)" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'title.svg') $titleSvg

$bodyL1 = Get-TextPathInfo -Text 'Nh?p l?p' -FontFamily 'Arial Black' -FontSize 84 -X 8 -Y 8 -Bold
$bodyL2 = Get-TextPathInfo -Text 'd? dÃƒÂ i' -FontFamily 'Arial Black' -FontSize 84 -X 42 -Y 110 -Bold
$bodyWidth = [Math]::Ceiling([Math]::Max($bodyL1.Bounds.Right, $bodyL2.Bounds.Right) + 12)
$bodyHeight = [Math]::Ceiling($bodyL2.Bounds.Bottom + 12)
$bodySvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$bodyWidth" height="$bodyHeight" viewBox="0 0 $bodyWidth $bodyHeight">
  <path fill="#111111" d="$($bodyL1.Data)" />
  <path fill="#111111" d="$($bodyL2.Data)" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'body.svg') $bodySvg

$footer = Get-TextPathInfo -Text 'CÃƒÂ´ng trÃƒÂ¬nh nghiÃƒÂªn c?u' -FontFamily 'Arial Black' -FontSize 68 -X 8 -Y 8 -Bold
$footerWidth = [Math]::Ceiling($footer.Bounds.Right + 12)
$footerHeight = [Math]::Ceiling($footer.Bounds.Bottom + 12)
$footerSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="$footerWidth" height="$footerHeight" viewBox="0 0 $footerWidth $footerHeight">
  <path fill="#EF533A" d="$($footer.Data)" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'footer.svg') $footerSvg

$timeText = Get-TextPathInfo -Text 'Th?i gian' -FontFamily 'Arial Black' -FontSize 48 -X 18 -Y 178 -Bold
$avgText = Get-TextPathInfo -Text 'Trung bÃƒÂ¬nh' -FontFamily 'Arial Black' -FontSize 42 -X 0 -Y 236 -Bold
$daysText = Get-TextPathInfo -Text '66 ngÃƒÂ y' -FontFamily 'Arial Black' -FontSize 42 -X 218 -Y 236 -Bold
$clockSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="520" height="280" viewBox="0 0 520 280">
  <path fill="none" stroke="#2A2A2A" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" d="M 190 22 C 164 36 150 72 154 105" />
  <path fill="none" stroke="#2A2A2A" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" d="M 151 104 L 144 92 L 141 108" />
  <path fill="none" stroke="#2A2A2A" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" d="M 426 104 C 430 72 416 36 390 22" />
  <path fill="none" stroke="#2A2A2A" stroke-width="3.5" stroke-linecap="round" stroke-linejoin="round" d="M 439 108 L 436 92 L 429 104" />
  <path fill="#FCEBDC" d="M 105 60 C 134 60 158 84 158 113 C 158 142 134 166 105 166 C 76 166 52 142 52 113 C 52 84 76 60 105 60 Z" />
  <path fill="#FFD08B" d="M 106 68 C 130 68 150 88 150 112 C 150 136 130 156 106 156 C 82 156 62 136 62 112 C 62 88 82 68 106 68 Z" />
  <path fill="none" stroke="#C78E42" stroke-width="2.6" stroke-linecap="round" d="M 106 83 L 106 93" />
  <path fill="none" stroke="#C78E42" stroke-width="2.6" stroke-linecap="round" d="M 106 131 L 106 141" />
  <path fill="none" stroke="#C78E42" stroke-width="2.6" stroke-linecap="round" d="M 76 113 L 86 113" />
  <path fill="none" stroke="#C78E42" stroke-width="2.6" stroke-linecap="round" d="M 126 113 L 136 113" />
  <path fill="none" stroke="#A56A26" stroke-width="3.4" stroke-linecap="round" d="M 106 112 L 106 90" />
  <path fill="none" stroke="#A56A26" stroke-width="3.4" stroke-linecap="round" d="M 106 112 L 124 98" />
  <path fill="#111111" d="M 282 97 L 296 97 L 296 111 L 310 111 L 310 125 L 296 125 L 296 139 L 282 139 L 282 125 L 268 125 L 268 111 L 282 111 Z" />
  <path fill="#111111" d="$($timeText.Data)" />
  <path fill="#111111" d="$($avgText.Data)" />
  <path fill="#587FD3" d="$($daysText.Data)" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'clock-group.svg') $clockSvg

$arrowSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="430" height="210" viewBox="0 0 430 210">
  <path fill="#F0EEF2" stroke="#202020" stroke-width="4" stroke-linejoin="round" d="M 18 104 C 72 30 176 8 270 34 L 280 6 L 410 104 L 280 202 L 270 174 C 176 200 74 178 18 104 Z" />
  <path fill="none" stroke="#2A2A2A" stroke-width="5" stroke-linecap="round" stroke-linejoin="round" d="M 42 104 C 92 46 176 30 254 46" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'arrow-main.svg') $arrowSvg

$leftSvg = @"
<svg xmlns="http://www.w3.org/2000/svg" width="560" height="520" viewBox="0 0 560 520">
  <path fill="none" stroke="#D3E0E2" stroke-width="18" stroke-linecap="round" d="M 50 470 L 420 470" />
  <path fill="#FFFFFF" stroke="#8B596A" stroke-width="10" stroke-linejoin="round" d="M 170 110 L 396 110 L 418 132 L 418 432 L 396 456 L 170 456 L 148 432 L 148 132 Z" />
  <path fill="none" stroke="#5E93A0" stroke-width="8" stroke-linecap="round" d="M 220 110 L 220 148 L 346 148 L 346 110" />
  <path fill="#F7F0F4" d="M 210 182 C 228 164 256 164 274 182 C 256 200 228 200 210 182 Z" />
  <path fill="#58C2B8" d="M 224 182 L 242 200 L 276 162" stroke="#58C2B8" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />
  <path fill="#F7F0F4" d="M 210 250 C 228 232 256 232 274 250 C 256 268 228 268 210 250 Z" />
  <path fill="#58C2B8" d="M 224 250 L 242 268 L 276 230" stroke="#58C2B8" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />
  <path fill="#F7F0F4" d="M 210 318 C 228 300 256 300 274 318 C 256 336 228 336 210 318 Z" />
  <path fill="#58C2B8" d="M 224 318 L 242 336 L 276 298" stroke="#58C2B8" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />
  <path fill="#F7F0F4" d="M 210 386 C 228 368 256 368 274 386 C 256 404 228 404 210 386 Z" />
  <path fill="#58C2B8" d="M 224 386 L 242 404 L 276 366" stroke="#58C2B8" stroke-width="8" stroke-linecap="round" stroke-linejoin="round" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 186 L 366 186" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 206 L 358 206" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 254 L 370 254" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 274 L 360 274" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 322 L 372 322" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 342 L 360 342" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 390 L 372 390" />
  <path fill="none" stroke="#58C2B8" stroke-width="6" stroke-linecap="round" d="M 304 410 L 356 410" />
  <path fill="#F0F3F6" stroke="#C8CED1" stroke-width="6" d="M 324 140 L 392 140 L 392 222 L 324 222 Z" />
  <path fill="#C04E79" d="M 358 154 C 377 154 390 167 390 186 L 358 186 Z" />
  <path fill="#3C7E96" d="M 358 186 L 388 196 C 384 214 370 224 352 224 Z" />
  <path fill="#E9C7A8" d="M 352 186 L 352 154 C 334 154 320 170 320 188 Z" />
  <path fill="#EFC4D8" stroke="#A6547F" stroke-width="5" stroke-linejoin="round" d="M 88 444 L 122 238 L 162 264 L 128 470 Z" />
  <path fill="#A64A7A" d="M 122 238 L 162 264 L 148 290 L 108 266 Z" />
  <path fill="#F5D07F" d="M 88 444 L 122 238 L 96 230 L 60 434 Z" />
  <path fill="#E7B15B" d="M 60 434 L 96 230 L 78 222 L 44 426 Z" />
  <path fill="#D69CB8" d="M 58 220 L 104 232 L 86 180 Z" />
  <path fill="#D99B6C" d="M 66 176 C 77 166 90 166 101 176 C 100 194 83 204 66 196 Z" />
  <path fill="#F7DDE8" d="M 52 198 C 74 198 88 214 90 248 L 98 386 L 72 470 L 36 470 L 38 382 L 46 254 C 47 224 52 208 52 198 Z" />
  <path fill="#9FA7D9" d="M 44 382 L 28 470 L 58 470 L 68 384 Z" />
  <path fill="#6E7AB7" d="M 68 384 L 80 470 L 104 470 L 90 380 Z" />
  <path fill="#7B4B5C" d="M 286 58 C 300 42 326 42 340 58 C 338 78 322 92 304 90 C 292 88 286 74 286 58 Z" />
  <path fill="#9FE3DB" d="M 286 88 L 270 196 L 310 252 L 344 248 L 330 114 Z" />
  <path fill="#1B7C86" d="M 314 98 L 324 170 L 340 248 L 366 248 L 348 104 Z" />
  <path fill="#1A6A74" d="M 316 98 L 326 152 L 300 152 L 298 102 Z" />
  <path fill="#4E144E" d="M 252 138 L 304 138 L 304 182 L 252 182 Z" />
  <path fill="#D89475" d="M 278 138 L 286 156 L 302 156 L 296 134 Z" />
  <path fill="#D99B6C" d="M 344 260 C 356 246 380 246 392 260 C 392 282 376 294 360 292 C 348 290 342 278 344 260 Z" />
  <path fill="#3B0D58" d="M 334 292 L 324 470 L 366 470 L 370 344 L 384 470 L 420 470 L 400 292 Z" />
  <path fill="#15818A" d="M 352 292 L 362 348 L 354 470 L 370 470 L 378 346 L 370 292 Z" />
  <path fill="#F5F3FA" stroke="#D3D6E4" stroke-width="5" stroke-linejoin="round" d="M 324 332 L 392 332 L 392 374 L 324 374 Z" />
  <path fill="#7C3628" d="M 448 418 L 478 418 L 486 470 L 440 470 Z" />
  <path fill="#32A49D" stroke="#236C69" stroke-width="4" d="M 462 328 C 490 348 492 390 468 418 C 442 398 438 354 462 328 Z" />
  <path fill="#1C887D" stroke="#236C69" stroke-width="4" d="M 450 388 C 432 406 432 434 452 452 C 466 436 468 410 450 388 Z" />
</svg>
"@
Write-Utf8File (Join-Path $assetDir 'left-illustration.svg') $leftSvg


$witnessObjects = @(
    [ordered]@{
        order = 1
        objectId = 'object-left'
        assetId = 'svg-left'
        assetFile = 'engine-assets/left-illustration.svg'
        role = 'Left Illustration'
        notes = 'Checklist board and characters remain one authored illustration object for Phase 12.'
        handEmbedded = $false
    },
    [ordered]@{
        order = 2
        objectId = 'object-arrow'
        assetId = 'svg-arrow'
        assetFile = 'engine-assets/arrow-main.svg'
        role = 'Arrow'
        notes = 'Connector arrow stays separate so later motion timing can target it directly.'
        handEmbedded = $false
    },
    [ordered]@{
        order = 3
        objectId = 'object-title'
        assetId = 'svg-title'
        assetFile = 'engine-assets/title.svg'
        role = 'Title'
        notes = 'Headline text is authored as its own SVG block instead of a source crop.'
        handEmbedded = $false
    },
    [ordered]@{
        order = 4
        objectId = 'object-clock-group'
        assetId = 'svg-clock-group'
        assetFile = 'engine-assets/clock-group.svg'
        role = 'Clock Group'
        notes = 'Clock illustration plus nearby text stay grouped as one witness object for this phase.'
        handEmbedded = $false
    },
    [ordered]@{
        order = 5
        objectId = 'object-body'
        assetId = 'svg-body'
        assetFile = 'engine-assets/body.svg'
        role = 'Body'
        notes = 'Body copy is its own authored object and remains separate from the title/footer blocks.'
        handEmbedded = $false
    },
    [ordered]@{
        order = 6
        objectId = 'object-footer'
        assetId = 'svg-footer'
        assetFile = 'engine-assets/footer.svg'
        role = 'Footer'
        notes = 'Footer text stays separate from the witness background and crop-based references.'
        handEmbedded = $false
    }
)

$legacyReferenceAssets = @()
foreach ($legacyDir in @('assets', 'segmented-assets')) {
    $legacyPath = Join-Path $projectRoot $legacyDir
    if (Test-Path $legacyPath) {
        $legacyReferenceAssets += Get-ChildItem $legacyPath -File |
            Where-Object { $_.Extension -eq '.png' } |
            Sort-Object Name |
            ForEach-Object { "$legacyDir/$($_.Name)" }
    }
}

$assetInventory = [ordered]@{
    phase = '12-01'
    witnessScene = 'project-engine.json'
    activePath = 'authored-witness'
    generatedBy = 'artifacts/source-parity-demo/build-engine-assets.ps1'
    generatedAt = (Get-Date).ToString('o')
    authoredObjectCount = $witnessObjects.Count
    objects = $witnessObjects
    handAsset = [ordered]@{
        assetId = 'hand-1'
        assetFile = 'assets/hand.svg'
        bakedIntoWitnessObjects = $false
        notes = 'Hand remains a separate manifest-backed asset for later sequencing work.'
    }
    legacyReferenceOnly = [ordered]@{
        specs = @(
            'project.json',
            'project-image-hand.json'
        )
        files = $legacyReferenceAssets
        notes = 'Raster crops and shortcut specs stay available only for comparison/reference, not the main parity path.'
    }
}
Write-JsonFile (Join-Path $checkDir 'authored-asset-inventory.json') $assetInventory

$decompositionLines = @(
    '# Authored Witness Scene Decomposition',
    '',
    'Active main path: `project-engine.json`',
    '',
    'This witness scene is intentionally locked to six authored scene objects:',
    ''
)
foreach ($object in $witnessObjects) {
    $decompositionLines += ("{0}. `{1}` -> `{2}` ({3})" -f $object.order, $object.objectId, $object.assetFile, $object.role)
    $decompositionLines += ("   {0}" -f $object.notes)
}
$decompositionLines += ""
$decompositionLines += 'Hand asset: `assets/hand.svg` remains separate from all six scene objects.'
$decompositionLines += 'Legacy crop specs (`project.json`, `project-image-hand.json`) remain comparison-only inputs.'
$decompositionLines += 'Generated by `build-engine-assets.ps1` to make the authored parity inventory explicit for Phase 12.'
Write-Utf8File (Join-Path $checkDir 'witness-scene-decomposition.md') ($decompositionLines -join [Environment]::NewLine)

Write-Output "Generated assets in $assetDir"
Write-Output "Authored inventory: $(Join-Path $checkDir 'authored-asset-inventory.json')"
Write-Output "Scene decomposition: $(Join-Path $checkDir 'witness-scene-decomposition.md')"
