<#
.SYNOPSIS
Build both Dragonfall plugins using the game's legacy Mono references.
.EXAMPLE
.\build.ps1
.EXAMPLE
.\build.ps1 -GamePath 'D:\Games\Dragonfall' -OutputPath '.\artifacts\custom'
#>
[CmdletBinding()]
param(
    [string]$GamePath = (Join-Path $PSScriptRoot '..'),
    [string]$BepInExCorePath,
    [string]$OutputPath = (Join-Path $PSScriptRoot 'artifacts'),
    [string]$CompilerPath = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\csc.exe"
)
$ErrorActionPreference = 'Stop'

# Resolve paths once. Explicit relative paths are relative to the current directory.
$game = (Resolve-Path -LiteralPath $GamePath).Path
$managed = Join-Path $game 'Dragonfall_Data\Managed'
if (!$BepInExCorePath) {
    $BepInExCorePath = Join-Path $game 'BepInEx\core'
}
$output = $ExecutionContext.SessionState.Path.GetUnresolvedProviderPathFromPSPath($OutputPath)

# Keep the old Unity core libraries: do not compile against desktop .NET defaults.
$references = @(
    (Join-Path $managed 'mscorlib.dll')
    (Join-Path $managed 'System.dll')
    (Join-Path $managed 'System.Core.dll')
    (Join-Path $managed 'UnityEngine.dll')
    (Join-Path $managed 'Assembly-CSharp.dll')
    (Join-Path $BepInExCorePath 'BepInEx.dll')
    (Join-Path $BepInExCorePath '0Harmony.dll')
)
$plugins = @(
    @{ Name = 'SRDFUIScale'; Source = (Join-Path $PSScriptRoot 'src\SRDFUIScale.cs') }
    @{ Name = 'SRDFCameraComfort'; Source = (Join-Path $PSScriptRoot 'src\SRDFCameraComfort.cs') }
)

# Report missing inputs before producing any output.
$inputs = @($CompilerPath) + $references + @($plugins | ForEach-Object { $_.Source })
$missing = @($inputs | Where-Object { !(Test-Path -LiteralPath $_ -PathType Leaf) })
if ($missing.Count -gt 0) {
    throw ("Missing build inputs:`n - " + ($missing -join "`n - ") + "`nCheck GamePath, BepInExCorePath and CompilerPath.")
}
$compiler = (Resolve-Path -LiteralPath $CompilerPath).Path
$referenceArguments = @($references | ForEach-Object {
    '/reference:' + (Resolve-Path -LiteralPath $_).Path
})
$compilerArguments = @(
    '/nologo'
    '/noconfig'
    '/nostdlib+'
    '/target:library'
    '/optimize+'
    '/langversion:3'
) + $referenceArguments

Write-Host "Game:     $game"
Write-Host "Compiler: $compiler"
Write-Host "Output:   $output"
foreach ($plugin in $plugins) {
    $directory = Join-Path $output "BepInEx\plugins\$($plugin.Name)"
    $assembly = Join-Path $directory "$($plugin.Name).dll"
    New-Item -ItemType Directory -Force $directory | Out-Null

    Write-Host "Building $($plugin.Name)..."
    & $compiler @compilerArguments "/out:$assembly" $plugin.Source
    if ($LASTEXITCODE -ne 0) {
        throw "Compilation failed for $($plugin.Name) (exit code $LASTEXITCODE)."
    }
    Get-FileHash -LiteralPath $assembly | Select-Object Path, Hash
}
