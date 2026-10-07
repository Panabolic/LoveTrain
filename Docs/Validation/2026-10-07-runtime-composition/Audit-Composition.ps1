param([string]$Baseline = '95a7d5e', [string]$ValidationProject, [switch]$SkipMetrics)
$ErrorActionPreference = 'Stop'
$workspacePath = (Get-Location).Path
$evidencePath = Join-Path $workspacePath 'Docs/Validation/2026-10-07-runtime-composition'
if (-not $ValidationProject) { $ValidationProject = (Get-Content -LiteralPath (Join-Path $evidencePath 'validation-project.txt') -Raw).Trim() }
$scopePattern = '^Assets[\\/](?:Scripts[\\/](?:LeeJunmo|SangHyup)[\\/]|Editor[\\/]|[^\\/]+\.cs$)'
$baselineFiles = @(git ls-tree -r --name-only $Baseline -- Assets | Where-Object { $_ -match '\.cs$' -and $_ -match $scopePattern })
$currentFiles = @(rg --files Assets -g '*.cs' | Where-Object { $_ -match $scopePattern })
$metrics = @()
if (-not $SkipMetrics) {
foreach ($revision in @('baseline','working')) {
    $paths = if ($revision -eq 'baseline') { $baselineFiles } else { $currentFiles }
    $lineCount = 0; $classCount = 0; $lookupCount = 0
    foreach ($relativePath in $paths) {
        $source = if ($revision -eq 'baseline') { (git show ($Baseline + ':' + $relativePath)) -join "`n" } else { [IO.File]::ReadAllText((Join-Path $workspacePath $relativePath)) }
        $normalizedSource = $source.TrimEnd([char[]]"`r`n")
        $lineCount += ($normalizedSource -split "`n").Count
        $classCount += [regex]::Matches($source,'(?m)^\s*(?:(?:public|internal|private|protected|abstract|sealed|static|partial)\s+)*class\s+\w+').Count
        $lookupCount += [regex]::Matches($source,'GetComponent(?:InParent|InChildren)?<(?:Enemy|Mob|Boss)>').Count
    }
    $metrics += [pscustomobject]@{Revision=$revision;CsFiles=$paths.Count;Classes=$classCount;SourceLines=$lineCount;NominalTargetLookups=$lookupCount}
}
$metrics | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath 'comparison-metrics.json') -Encoding utf8
} else { $metrics = Get-Content -LiteralPath (Join-Path $evidencePath 'comparison-metrics.json') -Raw | ConvertFrom-Json }
$snapshot = @()
foreach ($relativePath in $currentFiles) {
    $original = Join-Path $workspacePath $relativePath
    $copy = Join-Path $ValidationProject $relativePath
    if (-not (Test-Path -LiteralPath $copy)) { throw ('Missing validation source: ' + $relativePath) }
    $originalHash = (Get-FileHash -LiteralPath $original -Algorithm SHA256).Hash
    $copyHash = (Get-FileHash -LiteralPath $copy -Algorithm SHA256).Hash
    if ($originalHash -ne $copyHash) { throw ('Validation source mismatch: ' + $relativePath) }
    $snapshot += [pscustomobject]@{Path=$relativePath;SHA256=$originalHash}
}
$snapshot | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $evidencePath 'source-snapshot.json') -Encoding utf8

$changed = @(git -c core.safecrlf=false diff --name-only --diff-filter=MDRCTUXB $Baseline)
$assetChanges = @($changed | Where-Object { $_ -match '\.(unity|prefab|asset|meta|asmdef|inputactions)$' -or $_ -match '^(Packages|ProjectSettings)/' })
if ($assetChanges.Count -gt 0) { throw ('Existing asset/settings change: ' + ($assetChanges -join ', ')) }
$guidPaths = @{}
foreach ($meta in @(rg --files Assets -g '*.meta')) {
    $content = [IO.File]::ReadAllText((Join-Path $workspacePath $meta))
    $match = [regex]::Match($content,'(?m)^guid:\s*([a-f0-9]{32})\s*$')
    if (-not $match.Success) { continue }
    $guid = $match.Groups[1].Value
    if ($guidPaths.ContainsKey($guid)) { throw ('Duplicate GUID: ' + $meta + ' and ' + $guidPaths[$guid]) }
    $guidPaths[$guid] = $meta
}

function Get-SerializedFields([string]$Source) {
    $fields = [Collections.Generic.HashSet[string]]::new()
    $pending = $false
    foreach ($line in ($Source -split "`n")) {
        if ($line -match '\[SerializeField(?:\]|\s)') { $pending = $true }
        if ($line -match '^\s*(?:\[[^\r\n]+\]\s*)*(public|private|protected|internal)\s+(?:(readonly|static)\s+)?([\w\.<>]+(?:\[\])?)\s+(\w+)\s*(?:[;=]|$)') {
            if ($Matches[3] -in @('class','struct','interface','enum')) { $pending = $false; continue }
            if (($Matches[1] -eq 'public' -or $pending) -and -not $Matches[2]) { [void]$fields.Add($Matches[3] + '|' + $Matches[4]) }
            $pending = $false
        } elseif ($line -match ';') { $pending = $false }
    }
    # Public fields can be packed onto one line inside a nested serialized struct.
    foreach ($fieldMatch in [regex]::Matches($Source,'\bpublic\s+(?:(?<modifier>static|readonly)\s+)?(?<type>[\w\.<>]+(?:\[\])?)\s+(?<name>\w+)\s*(?=[;=])')) {
        if (-not $fieldMatch.Groups['modifier'].Success) { [void]$fields.Add($fieldMatch.Groups['type'].Value + '|' + $fieldMatch.Groups['name'].Value) }
    }
    return ,$fields
}
$checkedFields = 0
foreach ($relativePath in @($changed | Where-Object { $_ -match '^Assets/.*\.cs$' })) {
    $before = (git show ($Baseline + ':' + $relativePath)) -join "`n"
    $after = [IO.File]::ReadAllText((Join-Path $workspacePath $relativePath))
    $beforeFields = Get-SerializedFields $before; $afterFields = Get-SerializedFields $after
    foreach ($field in $beforeFields) {
        if (-not $afterFields.Contains($field)) { throw ('Missing/changed authoring field: ' + $relativePath + ' ' + $field) }
        $checkedFields++
    }
}
$audit = [pscustomobject]@{Baseline=$Baseline;MatchingSources=$snapshot.Count;ExistingAssetChanges=$assetChanges.Count;UniqueAssetGuids=$guidPaths.Count;PreservedFieldDeclarations=$checkedFields;FieldCheck='Static declaration check plus reviewer source inspection, not Unity serialization roundtrip'}
$audit | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $evidencePath 'static-audit.json') -Encoding utf8
$metrics | ConvertTo-Json
$audit | ConvertTo-Json
