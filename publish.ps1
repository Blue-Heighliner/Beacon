<#
.SYNOPSIS
    Builds the single self-contained Artemis.exe for offline Windows x64 deployment.

.DESCRIPTION
    Wraps the SelfContained publish profile. The resulting exe embeds the .NET 9
    runtime and every native dependency, so target machines need neither .NET nor
    network access. Requires internet on THIS machine only to restore NuGet packages.

.EXAMPLE
    .\publish.ps1
#>
[CmdletBinding()]
param(
    # Where to copy the finished exe. Defaults to the profile's publish\win-x64 folder.
    [string]$OutputDirectory
)

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

Write-Host "Publishing self-contained Artemis.exe (this takes a minute)..." -ForegroundColor Cyan
dotnet publish -p:PublishProfile=SelfContained
if ($LASTEXITCODE -ne 0) { throw "dotnet publish failed with exit code $LASTEXITCODE" }

$exe = Join-Path $PSScriptRoot 'publish\win-x64\Artemis.exe'
if (-not (Test-Path $exe)) { throw "Expected output not found: $exe" }

if ($OutputDirectory) {
    if (-not (Test-Path $OutputDirectory)) {
        New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
    }
    Copy-Item $exe $OutputDirectory -Force
    $exe = Join-Path $OutputDirectory 'Artemis.exe'
}

$sizeMb = [math]::Round((Get-Item $exe).Length / 1MB, 1)
Write-Host ""
Write-Host "Done: $exe ($sizeMb MB)" -ForegroundColor Green
Write-Host "Copy this single file to the target machine. No installer, no .NET required." -ForegroundColor Green
