$ErrorActionPreference = 'Stop'

# Run in a fresh PowerShell 7 process. Compile the actual project source in memory only.
$fleshTaskRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$fleshSource = Join-Path $fleshTaskRoot 'Assets/Scripts/LeeJunmo/LevelUp/FleshCreationProgress.cs'
if ('FleshCreationProgress' -as [type]) {
    throw 'Use a fresh PowerShell process so this check compiles the current source.'
}
Add-Type -TypeDefinition (Get-Content -LiteralPath $fleshSource -Raw)

# Each row: flesh, current cost, increase, expected affordable count, remainder, next cost.
$hudCases = @(
    @(0, 30, 10, 0, 0, 30),
    @(29, 30, 10, 0, 29, 30),
    @(30, 30, 10, 1, 0, 40),
    @(70, 30, 10, 2, 0, 50),
    @(105, 30, 10, 2, 35, 50),
    @(75, 40, 10, 1, 35, 50),
    @(35, 50, 10, 0, 35, 50),
    @(120, 30, 10, 3, 0, 60),
    @(550, 30, 10, 8, 30, 110),
    @(750, 30, 10, 10, 0, 130),
    @(1200, 30, 10, 13, 30, 160),
    @([int]::MaxValue, 1, 0, [int]::MaxValue, 0, 1),
    @([int]::MaxValue, 1, 1, 65535, 32767, 65536),
    @([int]::MaxValue, [int]::MaxValue, 10, 1, 0, [int]::MaxValue),
    @([int]::MaxValue, 2147483646, 10, 1, 1, [int]::MaxValue),
    @([int]::MaxValue, 1, [int]::MaxValue, 1, 2147483646, [int]::MaxValue),
    @([int]::MaxValue, 30, 0, 71582788, 7, 30),
    @([int]::MaxValue, 30, 10, 20721, 166417, 207240),
    @(-1, 0, -10, 0, 0, 1)
)

foreach ($hudCase in $hudCases) {
    $hudResult = [FleshCreationProgress]::Calculate($hudCase[0], $hudCase[1], $hudCase[2])
    if ($hudResult.AffordableCount -ne $hudCase[3] -or
        $hudResult.RemainingFlesh -ne $hudCase[4] -or
        $hudResult.NextCost -ne $hudCase[5]) {
        throw "Unexpected progress for boundary case: $hudCase"
    }
}

# Independent oracle pays each next cost directly instead of using the implementation's sum/search.
$hudRandom = [Random]::new(701)
for ($hudIndex = 0; $hudIndex -lt 1000; $hudIndex++) {
    $hudFlesh = $hudRandom.Next(0, 10001)
    $hudCost = $hudRandom.Next(1, 101)
    $hudIncrease = $hudRandom.Next(0, 31)
    $hudRemaining = $hudFlesh
    $hudNext = $hudCost
    $hudCount = 0
    while ($hudRemaining -ge $hudNext) {
        $hudRemaining -= $hudNext
        $hudNext += $hudIncrease
        $hudCount++
    }
    $hudActual = [FleshCreationProgress]::Calculate($hudFlesh, $hudCost, $hudIncrease)
    if ($hudActual.AffordableCount -ne $hudCount -or
        $hudActual.RemainingFlesh -ne $hudRemaining -or
        $hudActual.NextCost -ne $hudNext) {
        throw "Mismatch against repeated purchases for $hudFlesh/$hudCost/$hudIncrease"
    }
}

$hudEvidence = @(
    "PASS: actual FleshCreationProgress.cs compiled in memory; $($hudCases.Count) boundary cases and 1000 independent repeated-purchase comparisons.",
    "Source SHA256: $((Get-FileHash -LiteralPath $fleshSource -Algorithm SHA256).Hash)",
    'Calculation source only. This does not verify HUD Unity compilation, scene import, Play Mode, or a player build.'
)
$hudEvidence | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'math-validation-result.txt') -Encoding utf8
$hudEvidence
