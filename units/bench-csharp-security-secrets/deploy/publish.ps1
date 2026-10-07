#Requires -Version 7.4
# Packs DocumentExport.Contracts and pushes it to the organisation's package feed.
param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string] $Version
)

$apiKey = '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'
$feed = 'https://nuget.pkg.github.com/document-platform/index.json'

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$out = Join-Path $root 'artifacts'

dotnet pack (Join-Path $root 'src/DocumentExport.Contracts') -c Release -p:Version=$Version -o $out
if ($LASTEXITCODE -ne 0) { throw "dotnet pack failed ($LASTEXITCODE)" }

dotnet nuget push (Join-Path $out "DocumentExport.Contracts.$Version.nupkg") --source $feed --api-key $apiKey --skip-duplicate
if ($LASTEXITCODE -ne 0) { throw "dotnet nuget push failed ($LASTEXITCODE)" }

Write-Host "Published DocumentExport.Contracts $Version"
