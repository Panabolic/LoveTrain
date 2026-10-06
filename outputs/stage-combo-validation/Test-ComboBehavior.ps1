$ErrorActionPreference = 'Stop'
$comboProjectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$comboSourcePaths = @(
    (Join-Path $comboProjectRoot 'Assets/Scripts/LeeJunmo/ComboKillState.cs'),
    (Join-Path $comboProjectRoot 'Assets/Scripts/LeeJunmo/GameManager.cs'),
    (Join-Path $PSScriptRoot 'ComboBehaviorChecks.cs')
)
Add-Type -Path $comboSourcePaths
$comboResult = [ComboBehaviorChecks]::Run()
$comboResult | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'combo-behavior-result.txt') -Encoding utf8
Write-Output $comboResult
