$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$stageSource = Get-Content -LiteralPath (Join-Path $taskRoot 'Assets/Scripts/LeeJunmo/StageManager.cs') -Raw
$eventSource = Get-Content -LiteralPath (Join-Path $taskRoot 'Assets/EventObjectSpawner.cs') -Raw
$bossSource = Get-Content -LiteralPath (Join-Path $taskRoot 'Assets/Scripts/SangHyup/Enemy/Spawner.cs') -Raw
$checksSource = Get-Content -LiteralPath (Join-Path $PSScriptRoot 'DistanceBehaviorChecks.cs') -Raw
$stageRules = $stageSource.Substring($stageSource.IndexOf('public sealed class StageDistanceProgress'))
$eventRules = $eventSource.Substring($eventSource.IndexOf('public sealed class StageEventSchedule'))
$bossRules = $bossSource.Substring($bossSource.IndexOf('public enum StageBossRequest'))
Add-Type -TypeDefinition ('using System;' + [Environment]::NewLine + $stageRules + [Environment]::NewLine + $eventRules + [Environment]::NewLine + $bossRules + [Environment]::NewLine + $checksSource.Replace('using System;', ''))
$taskResult = [DistanceBehaviorChecks]::Run()
$taskResult | Set-Content -LiteralPath (Join-Path $PSScriptRoot 'distance-behavior-result.txt') -Encoding utf8
Write-Output $taskResult
