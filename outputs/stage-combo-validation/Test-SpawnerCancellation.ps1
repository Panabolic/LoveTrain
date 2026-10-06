$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$spawnerSource = Get-Content -LiteralPath (Join-Path $taskRoot 'Assets/Scripts/SangHyup/Enemy/Spawner.cs') -Raw
$checksSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'SpawnerCancellationChecks.cs') -Raw
Add-Type -TypeDefinition ($spawnerSource + [Environment]::NewLine + $checksSource) -IgnoreWarnings
$taskResult = [SpawnerCancellationChecks]::Run()
$taskResult | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'spawner-cancellation-result.txt') -Encoding utf8
Write-Output $taskResult
