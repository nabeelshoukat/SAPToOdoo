#requires -Version 5.1
<#
Builds a self-contained, deployable copy of the SAP-Odoo Integration Gateway
and zips it up as a single file ready to copy to the client's Windows server.

Usage:
  .\publish.ps1                # win-x64 (default, matches modern SAP B1 servers)
  .\publish.ps1 -Runtime win-x86   # use if the client's SAP B1 DI API is 32-bit
#>
param(
    [ValidateSet("win-x64", "win-x86")]
    [string]$Runtime = "win-x64"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$publishDir = Join-Path $root "publish"
$zipPath = Join-Path $root "SAPToOdoo-publish-$Runtime.zip"

if (Test-Path $publishDir) { Remove-Item -Recurse -Force $publishDir }
if (Test-Path $zipPath) { Remove-Item -Force $zipPath }

dotnet publish (Join-Path $root "SAPToOdoo.csproj") `
    -c Release `
    -r $Runtime `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $publishDir

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE"
}

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host ""
Write-Host "Published: $publishDir"
Write-Host "Zipped:    $zipPath"
Write-Host ""
Write-Host "Copy $zipPath to the client server, extract it, then follow DEPLOYMENT.md."
