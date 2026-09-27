<#
.SYNOPSIS
    Local/CI build entry point: publish both Windows services self-contained,
    build the OpsIntel MSI, and optionally the Burn bundle.

.DESCRIPTION
    Mirrors the pipeline in msi_kurulum_dagitim.md §9 "Inferences: Pipeline":
      1. dotnet publish -c Release -r win-x64 --self-contained for OpsIntel.Host
         and OpsIntel.Intelligence (ADR-0002).
      2. dotnet build the WiX v7 SDK-style installer project
         (installer/OpsIntel.Installer.wixproj), with AcceptEula=wix7
         (installer/Directory.Build.props already sets this; -p:AcceptEula=wix7
         is passed again here defensively for CI images where MSBuild property
         inheritance across a NuGet-restored SDK has not been spike-verified).
      3. Optionally build installer/Bundle/OpsIntel.Bundle.wixproj (-Bundle).

    NOT run on Linux: WiX/MSBuild for a Windows Package/Bundle requires the
    Windows-only build tasks in WixToolset.Sdk. This script is Windows-only by
    design; CI runs it on windows-2025 (.github/workflows/installer.yml).

.PARAMETER Configuration
    MSBuild configuration, default Release.

.PARAMETER Version
    Product version (first three fields matter — MSI ignores the fourth,
    ADR-0022). Defaults to 0.1.0 for local dev builds.

.PARAMETER Bundle
    Also build the optional Burn bundle (installer/Bundle).

.PARAMETER SkipPublish
    Skip the `dotnet publish` steps and reuse whatever is already under
    artifacts\publish\ (useful for iterating on the installer only).

.EXAMPLE
    pwsh -File tools/build.ps1 -Version 2026.9.1

.EXAMPLE
    pwsh -File tools/build.ps1 -Bundle
#>

[CmdletBinding()]
param(
    [string]$Configuration = 'Release',
    [string]$Version = '0.1.0',
    [switch]$Bundle,
    [switch]$SkipPublish
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$ArtifactsDir = Join-Path $RepoRoot 'artifacts\publish'
$HostPublishDir = Join-Path $ArtifactsDir 'OpsIntel.Host'
$IntelligencePublishDir = Join-Path $ArtifactsDir 'OpsIntel.Intelligence'

# Paths to the two service projects. NOTE: these projects are being built by a
# parallel workstream and may not exist yet in every checkout — this script
# fails loudly (not silently) if they are missing, since a silently-empty
# publish dir would let the WiX build produce a broken, service-less MSI.
$HostProject = Join-Path $RepoRoot 'src\OpsIntel.Host\OpsIntel.Host.csproj'
$IntelligenceProject = Join-Path $RepoRoot 'src\OpsIntel.Intelligence\OpsIntel.Intelligence.csproj'

function Invoke-Step {
    param([string]$Description, [scriptblock]$Action)
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Step failed with exit code ${LASTEXITCODE}: $Description"
    }
}

if (-not $SkipPublish) {
    if (-not (Test-Path $HostProject)) {
        throw "OpsIntel.Host project not found at '$HostProject'. It is owned by a parallel workstream (src/OpsIntel.Host) — build it first, or pass -SkipPublish to reuse an existing artifacts\publish\ tree."
    }
    if (-not (Test-Path $IntelligenceProject)) {
        throw "OpsIntel.Intelligence project not found at '$IntelligenceProject'. Same note as above — build it first, or pass -SkipPublish."
    }

    Invoke-Step "dotnet publish OpsIntel.Host (self-contained win-x64)" {
        dotnet publish $HostProject `
            -c $Configuration `
            -r win-x64 `
            --self-contained true `
            -p:Version=$Version `
            -o $HostPublishDir
    }

    Invoke-Step "dotnet publish OpsIntel.Intelligence (self-contained win-x64)" {
        dotnet publish $IntelligenceProject `
            -c $Configuration `
            -r win-x64 `
            --self-contained true `
            -p:Version=$Version `
            -o $IntelligencePublishDir
    }
}
else {
    Write-Host "==> Skipping publish; reusing $ArtifactsDir" -ForegroundColor Yellow
    if (-not (Test-Path $HostPublishDir) -or -not (Test-Path $IntelligencePublishDir)) {
        throw "SkipPublish was set but $ArtifactsDir does not contain both OpsIntel.Host and OpsIntel.Intelligence publish output."
    }
}

# TODO(Faz 0): once src/OpsIntel.SetupHelper exists, publish it here too, into
# artifacts\publish\OpsIntel.SetupHelper (see installer/Cert.wxs header comment
# and installer/Directory.Build.props' OpsIntelSetupHelperPublishDir property).
# Left out for now because the project does not exist in this repository yet;
# the WiX build below will fail on Cert.wxs's File reference until it does.

Invoke-Step "dotnet build OpsIntel.Installer.wixproj (WiX v7 MSI)" {
    dotnet build (Join-Path $RepoRoot 'installer\OpsIntel.Installer.wixproj') `
        -c $Configuration `
        -p:Version=$Version `
        -p:AcceptEula=wix7 `
        -p:OpsIntelHostPublishDir="$HostPublishDir\" `
        -p:OpsIntelIntelligencePublishDir="$IntelligencePublishDir\"
}

if ($Bundle) {
    Invoke-Step "dotnet build OpsIntel.Bundle.wixproj (optional Burn bundle)" {
        dotnet build (Join-Path $RepoRoot 'installer\Bundle\OpsIntel.Bundle.wixproj') `
            -c $Configuration `
            -p:Version=$Version `
            -p:AcceptEula=wix7
    }
}

Write-Host "==> Build complete. MSI: installer\bin\x64\$Configuration\OpsIntel-x64.msi" -ForegroundColor Green
if ($Bundle) {
    Write-Host "==> Bundle: installer\Bundle\bin\x64\$Configuration\OpsIntelSetup.exe" -ForegroundColor Green
}
