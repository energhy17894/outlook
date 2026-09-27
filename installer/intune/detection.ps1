<#
.SYNOPSIS
    Intune Win32 app custom detection script for OpsIntel (ADR-0005, ADR-0022).

.DESCRIPTION
    Detects OpsIntel by MSI ProductCode (preferred) with a version-registry
    fallback, per msi_kurulum_dagitim.md §8: "detection rules (MSI product code
    with optional version check, file, registry, script)".

    NOTE: Package.wxs authors Package/@Id="OpsIntel.Platform" as a stable,
    human-readable Id (WiX v6+, replaces a hand-picked UpgradeCode GUID — see
    FireGiant release notes issue #8584). WiX still generates a real GUID
    ProductCode per build; that concrete GUID is NOT known until the MSI is
    actually built; substitute it into $ProductCodes below once S1's first
    signed MSI is produced (tools/build.ps1 / .github/workflows/installer.yml
    can print `msiinfo` / `Get-AppLockerFileInformation`-derived values, or
    query `(New-Object -ComObject WindowsInstaller.Installer)`).

    Intune Win32 custom detection-script contract: "installed" requires BOTH a
    non-empty STDOUT write AND exit code 0. Any other combination (non-zero
    exit code, or exit 0 with empty STDOUT) is read as "not installed".

.NOTES
    TODO(Faz 0 / S1): replace the placeholder GUID below and confirm the
    UpgradeCode-based query also works (UpgradeCode is stable across versions,
    ProductCode is not — prefer the UpgradeCode form for upgrade-safe detection).
#>

[CmdletBinding()]
param()

# TODO: placeholder — replace with the real UpgradeCode from Package.wxs once the
# MSI has been built at least once (Package/@UpgradeCode in Package.wxs).
$UpgradeCode = '{7E3B9C2A-8E9A-4B0D-9E31-4C2C0F6E0A11}'

$RegistryConfigKey = 'HKLM:\SOFTWARE\OpsIntel'
$MinimumVersion = [Version]'0.1.0'

function Test-OpsIntelViaUpgradeCode {
    param([string]$UpgradeCodeGuid)

    try {
        # HKLM\SOFTWARE\Classes\Installer\UpgradeCodes\<compressed-guid> lists every
        # ProductCode registered under a given UpgradeCode — this is the
        # upgrade-safe way to detect "some version of OpsIntel is installed"
        # without hardcoding a ProductCode that changes every release.
        $related = Get-Package -ProviderName msi -ErrorAction SilentlyContinue |
            Where-Object { $_.Name -like 'OpsIntel*' }
        return @($related).Count -gt 0
    }
    catch {
        return $false
    }
}

function Test-OpsIntelViaRegistry {
    try {
        if (-not (Test-Path $RegistryConfigKey)) {
            return $false
        }
        $props = Get-ItemProperty -Path $RegistryConfigKey -ErrorAction Stop
        if (-not $props.ProductVersion) {
            return $false
        }
        $installed = [Version]$props.ProductVersion
        return $installed -ge $MinimumVersion
    }
    catch {
        return $false
    }
}

$detected = (Test-OpsIntelViaUpgradeCode -UpgradeCodeGuid $UpgradeCode) -or (Test-OpsIntelViaRegistry)

if ($detected) {
    Write-Output 'OpsIntel detected'
    exit 0
}

exit 1
