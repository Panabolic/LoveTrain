param([string]$LogName = 'source-build.log', [switch]$IncludeEditor)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validationRoot = $PSScriptRoot
$templatePath = Join-Path $projectRoot 'WorkbenchValidation.csproj'
if (-not (Test-Path -LiteralPath $templatePath)) { $templatePath = Join-Path $projectRoot 'Assembly-CSharp.csproj' }
[xml]$sourceProject = Get-Content -LiteralPath $templatePath -Raw

# Reuse Unity's installed reference assemblies without changing generated projects.
$sourceProject.SelectNodes('//*[local-name()="None"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
$sourceProject.SelectNodes('//*[local-name()="Analyzer"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
$sourceProject.SelectNodes('//*[local-name()="Compile"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
$sourceProject.SelectNodes('//*[local-name()="ProjectReference"]') | ForEach-Object {
    $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($_.Include)
    $reference = $sourceProject.CreateElement('Reference', $sourceProject.DocumentElement.NamespaceURI)
    $reference.SetAttribute('Include', $assemblyName)
    $hintPath = $sourceProject.CreateElement('HintPath', $sourceProject.DocumentElement.NamespaceURI)
    $hintPath.InnerText = Join-Path $projectRoot "Library/ScriptAssemblies/$assemblyName.dll"
    $reference.AppendChild($hintPath) | Out-Null
    $_.ParentNode.ReplaceChild($reference, $_) | Out-Null
}
$sourceProject.SelectNodes('//*[local-name()="HintPath"]') | ForEach-Object {
    if (-not [System.IO.Path]::IsPathRooted($_.InnerText)) { $_.InnerText = Join-Path $projectRoot $_.InnerText }
}
$sourceProject.SelectNodes('//*[local-name()="OutputPath"]') | ForEach-Object { $_.InnerText = 'bin\' }
$sourceProject.SelectNodes('//*[local-name()="AssemblyName"]') | ForEach-Object { $_.InnerText = 'Assembly-CSharp' }

$compileGroup = $sourceProject.CreateElement('ItemGroup', $sourceProject.DocumentElement.NamespaceURI)
$sources = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Filter '*.cs'
)
foreach ($source in ($sources | Sort-Object FullName -Unique)) {
    $compile = $sourceProject.CreateElement('Compile', $sourceProject.DocumentElement.NamespaceURI)
    $compile.SetAttribute('Include', $source.FullName)
    $compileGroup.AppendChild($compile) | Out-Null
}
$sourceProject.DocumentElement.AppendChild($compileGroup) | Out-Null
$projectPath = Join-Path $validationRoot 'FleshHudSourceValidation.csproj'
$sourceProject.Save($projectPath)
$sources | Sort-Object FullName -Unique | ForEach-Object { $_.FullName } | Set-Content -LiteralPath (Join-Path $validationRoot 'compiled-sources.txt')

dotnet msbuild $projectPath /t:Rebuild /p:FrameworkPathOverride='C:/Program Files/Unity/Hub/Editor/6000.2.3f1/Editor/Data/NetStandard/compat/2.1.0/shims/netfx' /verbosity:minimal /nologo *> (Join-Path $validationRoot $LogName)
$buildExitCode = $LASTEXITCODE
$buildLog = Get-Content -LiteralPath (Join-Path $validationRoot $LogName)
if ($buildExitCode -ne 0) { $buildLog | Select-Object -Last 40 }
$warningCount = @($buildLog | Select-String 'warning CS').Count
Write-Output "Compiler warnings: $warningCount (details saved in $LogName)"
Write-Output "Source compile exit code: $buildExitCode; source count: $($sources.Count)"
if ($buildExitCode -eq 0 -and $IncludeEditor) {
    [xml]$editorProject = Get-Content -LiteralPath (Join-Path $projectRoot 'Assembly-CSharp-Editor.csproj') -Raw
    $editorProject.SelectNodes('//*[local-name()="None"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
    $editorProject.SelectNodes('//*[local-name()="Analyzer"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
    $editorProject.SelectNodes('//*[local-name()="Compile"]') | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
    $editorProject.SelectNodes('//*[local-name()="ProjectReference"]') | ForEach-Object {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($_.Include)
        $reference = $editorProject.CreateElement('Reference', $editorProject.DocumentElement.NamespaceURI)
        $reference.SetAttribute('Include', $assemblyName)
        $hintPath = $editorProject.CreateElement('HintPath', $editorProject.DocumentElement.NamespaceURI)
        $hintPath.InnerText = if ($assemblyName -eq 'Assembly-CSharp') { Join-Path $validationRoot 'bin/Assembly-CSharp.dll' } else { Join-Path $projectRoot "Library/ScriptAssemblies/$assemblyName.dll" }
        $reference.AppendChild($hintPath) | Out-Null
        $_.ParentNode.ReplaceChild($reference, $_) | Out-Null
    }
    $editorProject.SelectNodes('//*[local-name()="HintPath"]') | ForEach-Object {
        if (-not [System.IO.Path]::IsPathRooted($_.InnerText)) { $_.InnerText = Join-Path $projectRoot $_.InnerText }
    }
    $editorProject.SelectNodes('//*[local-name()="OutputPath"]') | ForEach-Object { $_.InnerText = 'bin/editor\' }
    $editorGroup = $editorProject.CreateElement('ItemGroup', $editorProject.DocumentElement.NamespaceURI)
    $editorSources = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Editor') -Filter '*.cs' -Recurse)
    foreach ($source in ($editorSources | Sort-Object FullName -Unique)) {
        $compile = $editorProject.CreateElement('Compile', $editorProject.DocumentElement.NamespaceURI)
        $compile.SetAttribute('Include', $source.FullName)
        $editorGroup.AppendChild($compile) | Out-Null
    }
    $editorProject.DocumentElement.AppendChild($editorGroup) | Out-Null
    $editorProjectPath = Join-Path $validationRoot 'FleshHudEditorValidation.csproj'
    $editorProject.Save($editorProjectPath)
    $editorLogName = [System.IO.Path]::GetFileNameWithoutExtension($LogName) + '-editor.log'
    dotnet msbuild $editorProjectPath /t:Rebuild /p:FrameworkPathOverride='C:/Program Files/Unity/Hub/Editor/6000.2.3f1/Editor/Data/NetStandard/compat/2.1.0/shims/netfx' /verbosity:minimal /nologo *> (Join-Path $validationRoot $editorLogName)
    $buildExitCode = $LASTEXITCODE
    $editorLog = Get-Content -LiteralPath (Join-Path $validationRoot $editorLogName)
    if ($buildExitCode -ne 0) { $editorLog | Select-Object -Last 40 }
    Write-Output "Editor source compile exit code: $buildExitCode; source count: $($editorSources.Count); log: $editorLogName"
}
exit $buildExitCode
