param([string]$LogName = 'source-build.log', [switch]$IncludeEditor, [switch]$IncludeFixtures)

$ErrorActionPreference = 'Stop'
$projectRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$validationRoot = $PSScriptRoot

function New-SourceProject([string]$Template, [string]$Name, [string[]]$Sources, [bool]$EditorProject) {
    [xml]$project = Get-Content -LiteralPath (Join-Path $projectRoot $Template) -Raw
    foreach ($kind in @('None', 'Analyzer', 'Compile')) {
        $project.SelectNodes("//*[local-name()='$kind']") | ForEach-Object { $_.ParentNode.RemoveChild($_) | Out-Null }
    }
    $project.SelectNodes('//*[local-name()="ProjectReference"]') | ForEach-Object {
        $assemblyName = [System.IO.Path]::GetFileNameWithoutExtension($_.Include)
        $reference = $project.CreateElement('Reference', $project.DocumentElement.NamespaceURI)
        $reference.SetAttribute('Include', $assemblyName)
        $hintPath = $project.CreateElement('HintPath', $project.DocumentElement.NamespaceURI)
        $hintPath.InnerText = if ($EditorProject -and $assemblyName -eq 'Assembly-CSharp') { Join-Path $validationRoot 'bin/Assembly-CSharp.dll' } else { Join-Path $projectRoot "Library/ScriptAssemblies/$assemblyName.dll" }
        $reference.AppendChild($hintPath) | Out-Null
        $_.ParentNode.ReplaceChild($reference, $_) | Out-Null
    }
    $project.SelectNodes('//*[local-name()="HintPath"]') | ForEach-Object {
        if (-not [System.IO.Path]::IsPathRooted($_.InnerText)) { $_.InnerText = Join-Path $projectRoot $_.InnerText }
    }
    $project.SelectNodes('//*[local-name()="OutputPath"]') | ForEach-Object { $_.InnerText = if ($EditorProject) { 'bin/editor/' } else { 'bin/' } }
    if (-not $EditorProject) { $project.SelectNodes('//*[local-name()="AssemblyName"]') | ForEach-Object { $_.InnerText = 'Assembly-CSharp' } }
    $group = $project.CreateElement('ItemGroup', $project.DocumentElement.NamespaceURI)
    foreach ($source in $Sources) {
        $compile = $project.CreateElement('Compile', $project.DocumentElement.NamespaceURI)
        $compile.SetAttribute('Include', $source)
        $group.AppendChild($compile) | Out-Null
    }
    $project.DocumentElement.AppendChild($group) | Out-Null
    $path = Join-Path $validationRoot $Name
    $project.Save($path)
    return $path
}

$sources = @(
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Scripts') -Filter '*.cs' -Recurse
    Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets') -Filter '*.cs'
) | Sort-Object FullName -Unique | ForEach-Object { $_.FullName }
$sources | Set-Content -LiteralPath (Join-Path $validationRoot 'compiled-sources.txt')
$projectPath = New-SourceProject 'Assembly-CSharp.csproj' 'DriveBossSourceValidation.csproj' $sources $false
dotnet msbuild $projectPath /t:Rebuild /p:FrameworkPathOverride='C:/Program Files/Unity/Hub/Editor/6000.2.3f1/Editor/Data/NetStandard/compat/2.1.0/shims/netfx' /verbosity:minimal /nologo *> (Join-Path $validationRoot $LogName)
$resultCode = $LASTEXITCODE
$buildLog = Get-Content -LiteralPath (Join-Path $validationRoot $LogName)
if ($resultCode -ne 0) { $buildLog | Select-Object -Last 40 }
Write-Output "Runtime source compile exit code: $resultCode; sources: $($sources.Count); warnings: $(@($buildLog | Select-String 'warning CS').Count); log: $LogName"
if ($resultCode -eq 0 -and $IncludeEditor) {
    $editorFiles = @(Get-ChildItem -LiteralPath (Join-Path $projectRoot 'Assets/Editor') -Filter '*.cs' -Recurse)
    if ($IncludeFixtures) { $editorFiles += @(Get-ChildItem -LiteralPath $validationRoot -Filter '*.cs') }
    $editorSources = @($editorFiles | Sort-Object FullName -Unique | ForEach-Object { $_.FullName })
    $editorPath = New-SourceProject 'Assembly-CSharp-Editor.csproj' 'DriveBossEditorValidation.csproj' $editorSources $true
    $editorLogName = [System.IO.Path]::GetFileNameWithoutExtension($LogName) + '-editor.log'
    dotnet msbuild $editorPath /t:Rebuild /p:FrameworkPathOverride='C:/Program Files/Unity/Hub/Editor/6000.2.3f1/Editor/Data/NetStandard/compat/2.1.0/shims/netfx' /verbosity:minimal /nologo *> (Join-Path $validationRoot $editorLogName)
    $resultCode = $LASTEXITCODE
    if ($resultCode -ne 0) { Get-Content -LiteralPath (Join-Path $validationRoot $editorLogName) | Select-Object -Last 40 }
    Write-Output "Editor source compile exit code: $resultCode; sources: $($editorSources.Count); log: $editorLogName"
}
exit $resultCode
