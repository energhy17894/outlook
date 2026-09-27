<#
.SYNOPSIS
    Signs the OpsIntel build outputs: own exe/dll, MSI, and (if present) the
    Burn bundle engine + final bundle.

.DESCRIPTION
    Order and mechanics follow ADR-0006 and msi_kurulum_dagitim.md §7:
      1. Sign first-party exes/dlls (skip Microsoft-signed runtime DLLs —
         self-contained publish output includes the .NET runtime itself,
         which must NOT be re-signed).
      2. Sign the MSI (embedded cabinets are covered by the MSI's own signature
         when <MediaTemplate EmbedCab="yes" /> is used, as Package.wxs does).
      3. If -Bundle is requested: detach the Burn engine, sign it, reattach,
         then sign the final bundle exe
         (docs.firegiant.com/wix/tools/signing — "bundles need to be signed in
         two pieces").

    ADR-0006: Artifact Signing (Trusted Signing) Public Trust certificates are
    NOT available to a Türkiye legal entity (US/CA/EU/UK/AU/NZ/JP/KR/SG/CH/NO/IL
    only). This script therefore defaults to a generic "signtool + PFX/HSM"
    flow (an OV certificate on a cloud HSM, or the org's own AD CS certificate
    for internal-only distribution) rather than assuming Artifact Signing.
    All signatures use SHA-256 with an RFC 3161 timestamp — mandatory here
    because Artifact Signing-style short-lived certs are worthless untimestamped,
    and because this MSI is expected to sit in Intune/GPO for years.

.PARAMETER CertificateThumbprint
    Thumbprint of a code-signing certificate already installed in the signing
    machine's certificate store (CI runner or cloud HSM-backed CNG provider).
    Mutually exclusive with -PfxPath (kept separate deliberately: a PFX on disk
    in CI is exactly the kind of secret-at-rest this project's own threat model
    warns against — prefer a store/HSM-backed cert in real pipelines).

.PARAMETER PfxPath / PfxPasswordEnvVar
    Local/dev-only alternative: a PFX file plus an environment variable name
    holding its password (never pass the password as a literal argument).

.PARAMETER Bundle
    Also perform the detach/sign-engine/reattach/sign-bundle flow for
    OpsIntelSetup.exe.

.PARAMETER TimestampUrl
    RFC 3161 timestamp authority. Defaults to DigiCert's; override for
    whichever CA issues the OV/AD CS certificate actually in use.

.NOTES
    Requires `signtool.exe` (Windows SDK) on PATH, and — only when -Bundle is
    passed — the `wix` CLI (`dotnet tool install --global wix`) for
    `wix burn detach` / `wix burn reattach`.
    NOT run on Linux. CI invokes this from windows-2025
    (.github/workflows/installer.yml), conditionally, only when the required
    signing secrets are present (see that workflow's comments) — an unsigned
    MSI is still uploaded as a build artifact so PRs can be tested without
    provisioning a signing identity.
#>

[CmdletBinding(DefaultParameterSetName = 'Thumbprint')]
param(
    [Parameter(ParameterSetName = 'Thumbprint')]
    [string]$CertificateThumbprint,

    [Parameter(ParameterSetName = 'Pfx')]
    [string]$PfxPath,

    [Parameter(ParameterSetName = 'Pfx')]
    [string]$PfxPasswordEnvVar = 'OPSINTEL_SIGNING_PFX_PASSWORD',

    [string]$TimestampUrl = 'http://timestamp.digicert.com',

    [string]$Configuration = 'Release',

    [switch]$Bundle
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot '..')
$MsiPath = Join-Path $RepoRoot "installer\bin\x64\$Configuration\OpsIntel-x64.msi"
$BundlePath = Join-Path $RepoRoot "installer\Bundle\bin\x64\$Configuration\OpsIntelSetup.exe"
$HostPublishDir = Join-Path $RepoRoot 'artifacts\publish\OpsIntel.Host'
$IntelligencePublishDir = Join-Path $RepoRoot 'artifacts\publish\OpsIntel.Intelligence'

function Invoke-Step {
    param([string]$Description, [scriptblock]$Action)
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Step failed with exit code ${LASTEXITCODE}: $Description"
    }
}

function Get-SignArgs {
    # Shared signtool arguments for the chosen credential source.
    if ($PSCmdlet.ParameterSetName -eq 'Pfx') {
        if (-not $PfxPath) { throw "-PfxPath is required in the Pfx parameter set." }
        $password = [Environment]::GetEnvironmentVariable($PfxPasswordEnvVar)
        if (-not $password) {
            throw "Environment variable '$PfxPasswordEnvVar' is not set (the PFX password is never accepted as a literal argument)."
        }
        return @('/f', $PfxPath, '/p', $password)
    }
    if (-not $CertificateThumbprint) {
        throw "Either -CertificateThumbprint (store/HSM-backed cert, preferred — ADR-0006) or -PfxPath must be supplied."
    }
    return @('/sha1', $CertificateThumbprint)
}

function Invoke-SignTool {
    param([string[]]$Files)
    $credArgs = Get-SignArgs
    foreach ($file in $Files) {
        Write-Host "Signing: $file" -ForegroundColor Cyan
        & signtool.exe sign /fd SHA256 /tr $TimestampUrl /td SHA256 @credArgs $file
        if ($LASTEXITCODE -ne 0) {
            throw "signtool failed (exit $LASTEXITCODE) for $file"
        }
    }
}

# ---------------------------------------------------------------------------
# 1. First-party exes/dlls from the self-contained publish output.
#    Only OpsIntel.* assemblies are signed — the bundled .NET runtime and any
#    third-party native libraries (SQLite, Foundry Local/ONNX Runtime) are
#    already signed by their own publishers and must not be touched.
# ---------------------------------------------------------------------------
$firstPartyPattern = 'OpsIntel\.*'
$firstPartyFiles = @()
foreach ($dir in @($HostPublishDir, $IntelligencePublishDir)) {
    if (Test-Path $dir) {
        $firstPartyFiles += Get-ChildItem -Path $dir -Recurse -Include '*.exe', '*.dll' |
            Where-Object { $_.Name -like $firstPartyPattern } |
            Select-Object -ExpandProperty FullName
    }
}
if ($firstPartyFiles.Count -gt 0) {
    Invoke-SignTool -Files $firstPartyFiles
}
else {
    Write-Warning "No first-party OpsIntel.*.exe/.dll found under artifacts\publish — did tools\build.ps1 run first?"
}

# TODO(Faz 0): once src/OpsIntel.SetupHelper exists and is published, its exe
# must be signed here too (it runs the deferred cert-provisioning custom
# action — an unsigned helper undermines the whole point of avoiding inline
# PowerShell custom actions).

# ---------------------------------------------------------------------------
# 2. The MSI itself.
# ---------------------------------------------------------------------------
if (Test-Path $MsiPath) {
    Invoke-SignTool -Files @($MsiPath)
}
else {
    Write-Warning "MSI not found at $MsiPath — run tools\build.ps1 first."
}

# ---------------------------------------------------------------------------
# 3. Optional Burn bundle: detach -> sign engine -> reattach -> sign bundle.
# ---------------------------------------------------------------------------
if ($Bundle) {
    if (-not (Test-Path $BundlePath)) {
        throw "Bundle requested but not found at $BundlePath — run tools\build.ps1 -Bundle first."
    }

    $engineDir = Join-Path $env:TEMP "opsintel-burn-$(Get-Random)"
    New-Item -ItemType Directory -Path $engineDir -Force | Out-Null
    $enginePath = Join-Path $engineDir 'engine.exe'
    $reattachedPath = Join-Path $engineDir 'bundle-reattached.exe'

    try {
        Invoke-Step 'wix burn detach' {
            & wix burn detach $BundlePath -engine $enginePath
        }

        Invoke-SignTool -Files @($enginePath)

        Invoke-Step 'wix burn reattach' {
            & wix burn reattach $BundlePath -engine $enginePath -o $reattachedPath
        }

        Copy-Item -Path $reattachedPath -Destination $BundlePath -Force
        Invoke-SignTool -Files @($BundlePath)
    }
    finally {
        Remove-Item -Path $engineDir -Recurse -Force -ErrorAction SilentlyContinue
    }
}

function Invoke-Step {
    param([string]$Description, [scriptblock]$Action)
    Write-Host "==> $Description" -ForegroundColor Cyan
    & $Action
    if ($LASTEXITCODE -and $LASTEXITCODE -ne 0) {
        throw "Step failed with exit code ${LASTEXITCODE}: $Description"
    }
}

Write-Host "==> Signing complete." -ForegroundColor Green
