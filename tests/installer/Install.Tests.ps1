<#
.SYNOPSIS
    Pester 5 install matrix for OpsIntel-x64.msi (ADR-0005 / ADR-0022 acceptance
    criteria). Run on a clean Windows 11 23H2+ / Server 2022+ VM or inside
    Windows Sandbox (see OpsIntel.wsb in this directory).

.DESCRIPTION
    Cannot run on Linux — WiX/MSI, Windows services and the Windows cert store
    are Windows-only. This file is authored and reviewed here (Faz 0, spike A);
    it has NOT been executed. Every assertion below is written against the
    design in docs/operations/installation.md and the ADRs, and must be
    confirmed on real hardware before S1 sign-off.

.NOTES
    Requires: Pester 5.x (`Install-Module Pester -MinimumVersion 5.5.0`).
    Run elevated: `Invoke-Pester -Path tests\installer\Install.Tests.ps1`.
#>

BeforeDiscovery {
    $script:MsiPath = $env:OPSINTEL_MSI_PATH
    if (-not $MsiPath) {
        $script:MsiPath = Join-Path $PSScriptRoot '..\..\installer\bin\x64\Release\OpsIntel-x64.msi'
    }
}

BeforeAll {
    $script:Port = 6500
    $script:InstallLog = Join-Path $env:TEMP 'opsintel-install.log'
    $script:UninstallLog = Join-Path $env:TEMP 'opsintel-uninstall.log'
    $script:HostServiceName = 'OpsIntel.Host'
    $script:AiServiceName = 'OpsIntel.AI'
    $script:DataDir = Join-Path $env:ProgramData 'OpsIntel\data'

    function Invoke-MsiInstall {
        param(
            [string]$MsiPath,
            [hashtable]$Properties = @{},
            [string]$LogPath
        )
        $propArgs = ($Properties.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '
        $args = "/i `"$MsiPath`" /qn /norestart $propArgs /l*v `"$LogPath`""
        $proc = Start-Process msiexec.exe -ArgumentList $args -Wait -PassThru
        return $proc.ExitCode
    }

    function Invoke-MsiUninstall {
        param([string]$MsiPath, [hashtable]$Properties = @{}, [string]$LogPath)
        $propArgs = ($Properties.GetEnumerator() | ForEach-Object { "$($_.Key)=$($_.Value)" }) -join ' '
        $args = "/x `"$MsiPath`" /qn /norestart $propArgs /l*v `"$LogPath`""
        $proc = Start-Process msiexec.exe -ArgumentList $args -Wait -PassThru
        return $proc.ExitCode
    }
}

Describe 'OpsIntel MSI - silent install' {

    BeforeAll {
        $script:ExitCode = Invoke-MsiInstall -MsiPath $MsiPath -Properties @{
            PORT      = $Port
            TENANT_ID = '00000000-0000-0000-0000-000000000000'
            CLIENT_ID = '00000000-0000-0000-0000-000000000000'
            ALLOW_LAN = 0
        } -LogPath $InstallLog
    }

    It 'exits 0 or 3010 (soft reboot pending)' {
        $ExitCode | Should -BeIn @(0, 3010)
    }

    It 'writes an install log with no "Return value 3" failures' {
        Test-Path $InstallLog | Should -BeTrue
        (Get-Content $InstallLog -Raw) | Should -Not -Match 'Return value 3'
    }

    Context 'Services' {
        It 'registers OpsIntel.Host' {
            (Get-Service -Name $HostServiceName -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        }
        It 'registers OpsIntel.AI' {
            (Get-Service -Name $AiServiceName -ErrorAction SilentlyContinue) | Should -Not -BeNullOrEmpty
        }
        It 'runs both services' {
            (Get-Service -Name $HostServiceName).Status | Should -Be 'Running'
            (Get-Service -Name $AiServiceName).Status | Should -Be 'Running'
        }
        It 'uses the expected virtual-account identities' {
            # Falls back to LocalService if ServiceInstall rejected "NT SERVICE\..."
            # without a password — see Services.wxs "OPEN RISK" comment.
            $hostAccount = (Get-CimInstance Win32_Service -Filter "Name='$HostServiceName'").StartName
            $aiAccount = (Get-CimInstance Win32_Service -Filter "Name='$AiServiceName'").StartName
            $hostAccount | Should -BeIn @('NT SERVICE\OpsIntel.Host', 'NT AUTHORITY\LocalService')
            $aiAccount | Should -BeIn @('NT SERVICE\OpsIntel.AI', 'NT AUTHORITY\LocalService')
        }
        It 'configures restart-on-failure recovery for both services' {
            # sc.exe qfailure is the only reliable way to read MSI-authored
            # util:ServiceConfig recovery actions back out.
            foreach ($svc in @($HostServiceName, $AiServiceName)) {
                $qfailure = & sc.exe qfailure $svc
                ($qfailure -join "`n") | Should -Match 'RESTART'
            }
        }
    }

    Context 'Network' {
        It "listens on 127.0.0.1:$Port but not on the LAN interface (ALLOW_LAN=0)" {
            $loopback = Get-NetTCPConnection -LocalAddress 127.0.0.1 -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
            $loopback | Should -Not -BeNullOrEmpty

            $anyLan = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue |
                Where-Object { $_.LocalAddress -notin @('127.0.0.1', '::1') }
            $anyLan | Should -BeNullOrEmpty
        }
        It 'responds on https://127.0.0.1:<port>/health/live' {
            $response = Invoke-WebRequest -Uri "https://127.0.0.1:$Port/health/live" -UseBasicParsing -TimeoutSec 15
            $response.StatusCode | Should -Be 200
        }
        It 'does not create a firewall rule when ALLOW_LAN=0' {
            Get-NetFirewallRule -DisplayName 'OpsIntel Host (HTTPS)' -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        }
    }

    Context 'ACLs and registry' {
        It 'creates ProgramData\OpsIntel with restricted ACLs (no Users ACE)' {
            Test-Path $DataDir | Should -BeTrue
            $acl = Get-Acl $DataDir
            ($acl.Access | Where-Object { $_.IdentityReference -match 'Users$' }) | Should -BeNullOrEmpty
        }
        It 'writes configuration under HKLM\SOFTWARE\OpsIntel' {
            (Get-ItemProperty 'HKLM:\SOFTWARE\OpsIntel').Port | Should -Be "$Port"
        }
    }
}

Describe 'OpsIntel MSI - upgrade from N-1' -Tag 'Upgrade' {
    # TODO(Faz 0): parameterize with a real N-1 build path once one exists;
    # this Describe block is a placeholder shape for the upgrade matrix
    # required by ADR-0022 / installation.md §10.
    It 'preserves ProgramData\OpsIntel\data across a MajorUpgrade' {
        Set-ItResult -Skipped -Because 'no N-1 build available yet in this environment'
    }
    It 'preserves the HTTPS certificate thumbprint across a MajorUpgrade' {
        Set-ItResult -Skipped -Because 'no N-1 build available yet in this environment'
    }
}

Describe 'OpsIntel MSI - repair' -Tag 'Repair' {
    It 'restores services and registry via /fvomus' {
        Set-ItResult -Skipped -Because 'run only after the install Describe block, in-process; wire up via -Tag filtering in CI'
    }
}

Describe 'OpsIntel MSI - silent uninstall (data retained)' {

    BeforeAll {
        $script:ExitCode = Invoke-MsiUninstall -MsiPath $MsiPath -LogPath $UninstallLog
    }

    It 'exits 0 or 3010' {
        $ExitCode | Should -BeIn @(0, 3010)
    }
    It 'removes both services' {
        Get-Service -Name $HostServiceName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
        Get-Service -Name $AiServiceName -ErrorAction SilentlyContinue | Should -BeNullOrEmpty
    }
    It 'removes the HTTPS leaf certificate from LocalMachine\Root' {
        (Get-ChildItem Cert:\LocalMachine\Root | Where-Object Subject -Match 'OpsIntel') | Should -BeNullOrEmpty
    }
    It 'KEEPS ProgramData\OpsIntel\data by default (REMOVE_DATA not set)' {
        Test-Path $DataDir | Should -BeTrue
    }
}

Describe 'OpsIntel MSI - silent uninstall with REMOVE_DATA=1' {

    BeforeAll {
        # Re-install first so there is something to remove-with-data.
        Invoke-MsiInstall -MsiPath $MsiPath -Properties @{ PORT = $Port } -LogPath $InstallLog | Out-Null
        $script:ExitCode = Invoke-MsiUninstall -MsiPath $MsiPath -Properties @{ REMOVE_DATA = 1 } -LogPath $UninstallLog
    }

    It 'exits 0 or 3010' {
        $ExitCode | Should -BeIn @(0, 3010)
    }
    It 'removes ProgramData\OpsIntel entirely' {
        Test-Path $DataDir | Should -BeFalse
    }
}
