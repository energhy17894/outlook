# Windows MSI-based installer & deployment for a locally installed HTTPS web app (state as of Sept 2026)

Scope: one installer that puts a .NET (ASP.NET Core/Kestrel worker services) + React SPA app on a Windows machine, installs prerequisites, registers/starts services, serves https://localhost:6500 (optionally LAN), and supports upgrade/repair/uninstall, signing, enterprise deployment and CI. Node.js/Python variants are noted where the answer changes. Research note: the web-search budget ran out part-way through; later facts come from pages fetched directly by URL. Anything I could not source is under "Gaps".

## 1. Tooling: WiX v7 / Burn vs Advanced Installer / InstallShield / MSIX / Inno-NSIS; why a bundle is needed; MSI-only for Intune/GPO

### Takeaway
WiX Toolset **v7.0.0 (6 Apr 2026)** is the current release. It is an SDK-style `.wixproj` project (`WixToolset.Sdk/7.0.0`) that builds with `dotnet build`. Any organization making more than US$10k/yr must pay the Open Source Maintenance Fee (OSMF), and v7 blocks every command until the EULA is accepted (`<AcceptEula>wix7</AcceptEula>`). One MSI cannot install other installers: nested installs are deprecated. So you either (a) ship a signed WiX **Burn bundle (.exe)** that chains the prerequisites and your MSI, or (b) remove the prerequisites (self-contained .NET, embedded SQLite, in-process Foundry Local) so a **single MSI** works for Intune LOB/GPO/SCCM. For this app (b) is realistic and preferable, and (a) is only needed for optional extras such as PostgreSQL, VC++ or WebView2.

### Cited Findings
**WiX versions and licensing**
- WiX v7.0.0 was published 6-Apr-2026 (rc.1 on 6-Feb-2026, rc.2 on 5-Mar-2026). v6.0.0 was 7-Apr-2025 and v6.0.2 was 28-Aug-2025. WiX ships one release a year while "maintaining high compatibility with previous releases". — [FireGiant release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/); [GitHub releases](https://github.com/wixtoolset/wix/releases)
- The v7 FireGiant blog banner says "WiX v3, v4 and v5 are out of community support". — [FireGiant blog, WiX v7 released](https://www.firegiant.com/blog/2026/4/6/wix-v7-heatwave-and-heatwave-build-tools-are-released/)
- **v7 highlights:**
  - dual-purpose (per-user or per-machine) MSI packages in bundles;
  - OSMF EULA v1.1, under which no fee is due until you make at least US$10,000/yr;
  - an explicit gesture is required to accept the EULA;
  - BREAKING: relative paths in `Files` are now relative to the source path;
  - new `Payloads` element to harvest bundle payloads;
  - payload paths longer than MAX_PATH;
  - **Heat removed** (deprecated in v6);
  - RedirectionGuard in Burn;
  - `ProductSearch/@Result='exists'`.
  
  — [FireGiant release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/); [WiX v7 rc1 blog](https://www.firegiant.com/blog/2026/2/6/wix-v700-rc1-is-here/)
- **v6 highlights:**
  - more certificate stores for SSL certificates (issue 1520);
  - "normal," non-SNI SSL certificates in http.sys (7622);
  - `Package`/`Bundle` `Id` attribute takes a human-readable string that replaces the UpgradeCode GUID (8584);
  - creating local/domain groups (8577);
  - Heat deprecation warning (8913);
  - Burn-extracted files locked read-only (8914).
  
  — [FireGiant release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/)
- **v5 highlights:**
  - built-in file harvesting with wildcards (`Files`), so Heat is not needed;
  - "naked files";
  - default major upgrade, default install folder and default feature with no XML;
  - "Modern Windows Firewall support" in `WixToolset.Firewall.wixext`;
  - out-of-process bootstrapper applications (custom BAs need source changes).
  
  — [FireGiant release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/)
- Minimal v7 project: `<Project Sdk="WixToolset.Sdk/7.0.0"></Project>` plus `<Package Id="Acme.HelloWorld" Name=… Version=… Manufacturer=…><Files Include="*" /></Package>`, then run `dotnet build`. — [FireGiant release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/); [MSBuild docs](https://docs.firegiant.com/wix/tools/msbuild/)
- Useful MSBuild properties:
  - `InstallerPlatform` (x86/x64/arm64);
  - `OutputType` (Package/Bundle/…);
  - `DefineConstants`, `SuppressIces`, `TreatWarningsAsErrors`.
  
  A `ProjectReference` to a .csproj creates bind paths and preprocessor variables for the referenced project. — [MSBuild docs](https://docs.firegiant.com/wix/tools/msbuild/)
- **OSMF:** organizations with more than $10,000 in annual revenue "are required to sponsor the wixtoolset GitHub organization". The fee was introduced in v6, but EULA enforcement only arrived in v7 "based on feedback that NuGet does not always present the EULA". — [FireGiant OSMF page](https://docs.firegiant.com/wix/osmf/)
- **Accepting the EULA:**
  - MSBuild property `<AcceptEula>wix7</AcceptEula>`, described as "designed for use in build scripts and CI/CD";
  - `wix build -acceptEula wix7 …`;
  - once per user: `wix eula accept wix7`, or `msbuild -t:AcceptEula -p:EulaId=wix7`.
  
  The EULA ID for v6 is N/A (OSMF v1.0); for v7 it is `wix7` (OSMF v1.1). — [FireGiant OSMF page](https://docs.firegiant.com/wix/osmf/); [issue #9196](https://github.com/wixtoolset/issues/issues/9196)
- Downstream packagers broke when v7 started requiring EULA acceptance. — [Scoop issue #7841](https://github.com/ScoopInstaller/Main/issues/7841)
- **Fee tiers**, paid via GitHub Sponsors:
  - under 20 people: $10/mo;
  - 20–100 people: $40/mo;
  - over 100 people: $60/mo.
  
  — [wixtoolset/issues #8974](https://github.com/wixtoolset/issues/issues/8974) (seen in a search summary, not opened directly); the OSMF v1.1 blog quotes "$10/mo" — [robmensching.com](https://robmensching.com/blog/posts/2026/02/04/osmf-v11/)
- **HeatWave** (FireGiant's Visual Studio extension) is free and supports WiX v7 and Visual Studio 2022/2026 from one marketplace entry. "HeatWave Build Tools" is commercial and comes with the WiX Developer Direct subscription. — [FireGiant blog](https://www.firegiant.com/blog/2026/4/6/wix-v7-heatwave-and-heatwave-build-tools-are-released/)

**Why a single MSI cannot chain prerequisites**
- "Concurrent Installations, also called Nested Installations, is a deprecated feature of the Windows Installer … Do not use concurrent installations to install products that are intended to be released to the public." Once a nested install starts, the installer "locks out all other installations". Nested installs can't share components, and patching or upgrading may not work with them. — [MS Learn: Concurrent Installations](https://learn.microsoft.com/en-us/windows/win32/msi/concurrent-installations)

**Burn bundles**
- Chain package types:
  - `BundlePackage`
  - `ExePackage`
  - `MsiPackage`
  - `MspPackage`
  - `MsuPackage`
- Put packages under `<Chain>`, or in `PackageGroup`s referenced with `PackageGroupRef`. `MsiProperty` passes properties to an MSI.
- Built-in bootstrapper applications:
  - `WixStandardBootstrapperApplication` (WixStdBA), with a themable wizard UI;
  - `WixInternalUIBootstrapperApplication` (WixIUIBA), which shows the primary MSI's own UI.
  
  Both are C++ and have no extra runtime requirements. Custom native or managed .NET BAs are supported.
  
  — [FireGiant: Burn bundles](https://docs.firegiant.com/wix/tools/burn/)
- `WixPrerequisiteBootstrapperApplication` (bal, v5+) is a secondary BA that bootstraps the prerequisites a primary (e.g., managed) BA needs. — [bal:WixPrerequisiteBootstrapperApplication](https://docs.firegiant.com/wix/schema/bal/wixprerequisitebootstrapperapplication/)
- **`ExePackage`:**
  - `DetectCondition` is "necessary because Windows doesn't provide a method to detect the presence of an ExePackage". If it is false or omitted, Burn installs the package.
  - Other attributes: `InstallArguments`, `RepairArguments`, `UninstallArguments`, `Permanent`, `Vital`, `PerMachine`, `InstallCondition`, `DownloadUrl`, `Compressed`, `LogPathVariable`, `bal:PrereqPackage`, `bal:PrimaryPackageType`.
  - Child elements: `ExitCode`, `ArpEntry`, `Provides`.
  
  — [FireGiant: ExePackage](https://docs.firegiant.com/wix/schema/wxs/exepackage/)
- Bundle `Variable` elements are **not** settable from the command line unless they are `bal:Overridable="yes"`. Otherwise WixStdBA logs "Ignoring attempt to set non-overridable variable". `Hidden` and `Persisted` are available. — [FireGiant: Variable](https://docs.firegiant.com/wix/schema/wxs/variable/)
- `WixToolset.Netfx.wixext` provides bundle searches:
  - `DotNetCoreSearch`, e.g. `RuntimeType="aspnet" Platform="x64" MajorVersion="…" Variable="…"`;
  - `DotNetCoreSdkSearch`.
  
  In MSIs it provides `netfx:DotNetCompatibilityCheck`. — [FireGiant: Detecting and installing .NET](https://docs.firegiant.com/wix/tools/wixext/dotnet/)

**Alternatives**
- **MSIX + services:**
  - Services are supported from Windows 10 2004 via the MSIX Packaging Tool; installing needs admin.
  - "We currently do not support services with dependencies outside the package."
  - Adding a service needs a restricted capability.
  
  — [MS Learn: MSIX converting installer with services](https://learn.microsoft.com/en-us/windows/msix/packaging-tool/convert-an-installer-with-services)
- **MSIX `desktop6:Service`:**
  - `StartAccount` can only be `localSystem`, `localService` or `networkService` (no virtual accounts or gMSA);
  - `StartupType` can be `auto`, `manual` or `disabled`;
  - needs the `packagedServices` or `localSystemServices` restricted capability;
  - minimum OS is Windows 10 1903 (build 18362).
  
  — [MS Learn: desktop6:Service](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop6-service) (note the 1903 vs 2004 discrepancy with the MPT page)
- **Advanced Installer list prices (2026, yearly billing):**
  - Professional $399/user/yr. The page lists "Services", "Prerequisites", "IIS", "PowerShell Automation", "Trusted Signing".
  - Enterprise $1,399/user/yr, which adds "Automated VM Testing", "EXE to MSI (wrapper)", "SBOM", "MSIX Editor".
  - Architect $3,599/user/yr.
  - A free edition exists.
  
  — [Advanced Installer pricing](https://www.advancedinstaller.com/purchase.html)
- **Inno Setup:**
  - the current stable is 6.7.3 (2026-05-26), and it is not MSI;
  - winget auto-applies silent switches for Inno and Nullsoft: Inno uses `/SILENT` or `/VERYSILENT`, NSIS uses `/S`.
  
  — [Inno Setup downloads](https://jrsoftware.org/isdl.php); [MS Learn winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)
- **GPO Software Installation** deploys Windows Installer packages (.msi) from a UNC share, assigned to computers or users. — [MS Learn: Use Group Policy to remotely install software](https://learn.microsoft.com/en-us/troubleshoot/windows-server/group-policy/use-group-policy-to-install-software)
- **Intune LOB app:**
  - accepts a single `.msi`, `.appx/.appxbundle` or `.msix/.msixbundle`;
  - "Only one command-line argument can be specified. If the .MSI file needs more than one command-line argument, consider using Win32 app management."
  
  — [MS Learn: Add Windows LOB app](https://learn.microsoft.com/en-us/intune/app-management/deployment/add-lob-windows)
- **Self-contained .NET deployment** "doesn't rely on the presence of a shared framework on the host system". Set `<RuntimeIdentifier>win-x64</RuntimeIdentifier>` and publish self-contained. — [MS Learn: Host ASP.NET Core in a Windows Service](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)

### Inferences
- Recommended architecture:
  - **Core deliverable:** a per-machine x64 MSI built with WiX v7 SDK. It contains the self-contained .NET 10 publish output (backend, workers, `wwwroot` SPA), embedded SQLite, services, firewall rule, URL shortcut and config. This one MSI serves Intune (LOB or Win32), GPO, SCCM and winget (`InstallerType: wix`).
  - **Optional wrapper:** a Burn bundle `Setup.exe` (WixStdBA) only when optional components are chosen, such as PostgreSQL, Ollama, VC++ or WebView2. It chains `ExePackage`s with `DetectCondition`s and then your MSI via `MsiPackage` + `MsiProperty`, which passes PORT, TENANTID, etc. from `bal:Overridable` variables.
  - This keeps enterprise channels on the plain MSI and puts consumer/"everything automatic" installs on the EXE.
- Don't use MSIX for this app. There are no virtual service accounts, no custom actions (not directly verified; this is the general MSIX model), and cert/firewall handling is awkward. The service limitations above already rule it out for a multi-service backend with a custom service account.
- Budget: WiX tooling cost is the OSMF, $10–60/mo depending on organization size. Advanced Installer Professional (from $399/user/yr) is the fastest commercial GUI path if the team has no WiX experience.
- WiX v7 notes:
  - pin `WixToolset.Sdk/7.0.x` in the .wixproj and put `<AcceptEula>wix7</AcceptEula>` in `Directory.Build.props` so CI does not break;
  - use `Files`/`Payloads` instead of Heat;
  - use `Package Id="Company.Product"` instead of a GUID UpgradeCode (v6+).

### Gaps
- InstallShield 2025/2026 pricing and edition features were not retrieved (search budget exhausted).
- NSIS's current version was not checked.
- The exact OSMF tier amounts come from a search summary of issue #8974, not a fetched page.

## 2. Prerequisites: .NET, Hosting Bundle, VC++, WebView2, PostgreSQL vs SQLite, Ollama / Foundry Local (+ Node.js/Python)

### Takeaway
Self-contained .NET 10 publishing removes the runtime prerequisite. The ASP.NET Core Hosting Bundle is only for IIS and is not needed for Kestrel self-hosting. VC++ and WebView2 are only needed if you have native components or a desktop shell. For the database, prefer embedded SQLite; if PostgreSQL is required, bundle the EDB **zip binaries** and run `initdb` + `pg_ctl register` rather than chaining the EDB GUI installer. For the local LLM, Foundry Local went GA on 9 Apr 2026 as an **in-process SDK** with no separate install, which is much easier to package than Ollama. Ollama's Windows installer is per-user; running it as a service needs the zip plus a service wrapper. Models should be downloaded at first run, not shipped in the MSI.

### Cited Findings
**.NET and ASP.NET Core hosting**
- .NET release status (policy page last updated 8 Sep 2026):
  - .NET 10 is **LTS**, released Nov 11 2025 (10.0.12 as of Sep 8 2026), supported until **Nov 14 2028**;
  - .NET 9 is STS and ends Nov 10 2026;
  - .NET 11 is at RC1.
  
  — [.NET support policy](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)
- ASP.NET Core can run as a Windows Service "without using IIS" via `Microsoft.Extensions.Hosting.WindowsServices` and `builder.Services.AddWindowsService()`. — [MS Learn: Host ASP.NET Core in a Windows Service](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)
- `AddWindowsService` enables event-log logging, but "Only administrators can create new event sources". If the source can't be created, event logs are disabled. WiX's `util:EventSource` creates an event source at install time. — [MS Learn](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0); [FireGiant util:EventSource](https://docs.firegiant.com/wix/schema/util/eventsource/)

**`netfx:DotNetCoreSearch` / `DotNetCompatibilityCheck`** detect installed runtimes if you choose framework-dependent deployment instead. — [FireGiant: .NET detection](https://docs.firegiant.com/wix/tools/wixext/dotnet/)

**VC++ Redistributable**
- The latest v14 redistributable serves Visual Studio 2017–2026. Permalinks:
  - `https://aka.ms/vc14/vc_redist.x64.exe`
  - `https://aka.ms/vc14/vc_redist.arm64.exe`
  - `https://aka.ms/vc14/vc_redist.x86.exe`
  
  The x64 package also contains ARM64 binaries. The VS2026 redistributable supports only Windows 10/11 and Server 2016–2025. — [MS Learn: Latest supported VC++ Redistributable](https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170)
- VC++ silent install and detection:
  - switches: `/install`, `/repair`, `/uninstall`, `/passive`, `/quiet`, `/norestart`, `/log file`;
  - detection value is under `HKLM\SOFTWARE\[Wow6432Node\]Microsoft\VisualStudio\14.0\VC\Runtimes\{x86|x64|arm64}`: `Version` (REG_SZ) plus `Major`/`Minor`/`Bld`/`Rbld` DWORDs;
  - skip the install if a newer version is present, because the package otherwise fails (silently with `/quiet`);
  - **merge modules are deprecated**, and app-local DLLs are not recommended.
  
  — [MS Learn: Redistribute Visual C++ files](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files?view=msvc-170)

**WebView2** (only if there is a desktop shell)
- The Evergreen runtime "will be included as part of the Windows 11 operating system", and most Windows 10 devices already have it.
- Detection: registry value `pv` under `HKLM\SOFTWARE\WOW6432Node\Microsoft\EdgeUpdate\Clients\{F3017226-FE2A-4295-8BDF-00C3A9A7E4C5}` (and the HKCU equivalent).
- Silent install: `MicrosoftEdgeWebview2Setup.exe /silent /install` (bootstrapper) or `MicrosoftEdgeWebView2RuntimeInstaller{X64/X86/ARM64}.exe /silent /install` (offline). Running elevated installs it per-machine.
- Fixed Version is over 250 MB.

— [MS Learn: WebView2 distribution](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution)

**PostgreSQL**
- The EDB installer "can run in graphical or silent install modes". EDB also provides a **zip archive of the binaries**, "intended for users who wish to include Postgres as part of another application installer".
- PostgreSQL 18 is supported on Windows Server 2025/2022.

— [postgresql.org Windows downloads](https://www.postgresql.org/download/windows/)
- EDB installer switches and defaults:
  - `--mode unattended`
  - `--unattendedmodeui` (default `minimal`)
  - `--superpassword`
  - `--serverport` (default 5432)
  - `--prefix`
  - `--datadir`
  - `--servicename`
  - `--serviceaccount` (default `postgres`)
  - `--servicepassword`
  - `--install_runtimes`
  - `--enable-components` / `--disable-components` (defaults: server, pgAdmin, stackbuilder, commandlinetools)
  - `--create_shortcuts`
  
  — [EDB PostgreSQL command line parameters](https://www.enterprisedb.com/docs/supported-open-source/postgresql/installing/command_line_parameters/)
- `pg_ctl register [-D datadir] [-N servicename] [-U username] [-P password] [-S a[uto]|d[emand]] [-e source] [-W] [-t seconds] [-s] [-o options]` and `pg_ctl unregister [-N servicename]` register PostgreSQL as a Windows service. — [PostgreSQL 18 docs: pg_ctl](https://www.postgresql.org/docs/current/app-pg-ctl.html)

**Ollama**
- The Windows installer is **per-user and needs no admin rights**, installing into the home directory. `OllamaSetup.exe /DIR="d:\some\location"` changes the location.
- Models go to the home directory unless `OLLAMA_MODELS` is set. The API listens on `http://localhost:11434`.
- "For integration as a system service", Ollama's docs say to use the standalone `ollama-windows-amd64.zip` and run `ollama serve` under a tool like **NSSM**.
- Requirements: Windows 10 22H2+ and NVIDIA 551.61+ or AMD ROCm v7/Vulkan drivers.

— [Ollama Windows docs](https://docs.ollama.com/windows)
- NSSM's latest release is 2.24 (2014-08-31). The featured pre-release is 2.24-101 (2017-04-26), recommended for Windows 10 Creators Update and later. — [nssm.cc/download](https://nssm.cc/download)

**Foundry Local**
- **GA on April 9, 2026.** It is an in-process SDK: Foundry Local Core native library plus ONNX Runtime, for C#, Python, JS and Rust, with "no separate CLI or service required".
- Models download on first run and are cached locally. The runtime is "small enough to bundle directly inside your application installer".
- Windows uses Windows ML for execution providers. An OpenAI-compatible HTTP server is optional.

— [Microsoft Foundry Blog: Foundry Local GA](https://devblogs.microsoft.com/foundry/foundry-local-ga/)
- The C# SDK is `dotnet add package Microsoft.AI.Foundry.Local`. `model.DownloadAsync(progress)` "skips download if already cached". JS uses `foundry-local-sdk-winml` on Windows. — [MS Learn: Get started with Foundry Local](https://learn.microsoft.com/en-us/azure/foundry-local/get-started)
- The CLI path still exists (`winget install Microsoft.FoundryLocal` installs the `foundry` CLI plus a background service; needs admin and internet for the first model download). There were reported winget install problems. — [MS Learn: Foundry Local CLI reference](https://learn.microsoft.com/en-us/azure/foundry-local/reference/reference-cli); [Tech Community: Foundry Local on Windows Server 2025](https://techcommunity.microsoft.com/blog/itopstalkblog/install-and-run-azure-foundry-local-llm-server--open-webui-on-windows-server-202/4457788); [GitHub issue #79](https://github.com/microsoft/Foundry-Local/issues/79)

**Node.js / Python backends**
- Node.js **Single Executable Applications**:
  - "Stability: 1.1 – Active development", with docs at v26.10.0;
  - `node --build-sea sea-config.json` was added in v25.5.0; the Windows output needs a `.exe` name;
  - signing with `signtool` is optional;
  - there are native-addon limitations.
  
  — [Node.js docs: Single executable applications](https://nodejs.org/api/single-executable-applications.html)
- WinSW "wraps and manages any application as a Windows service". 2.x is stable; 3.x is still pre-release and needs .NET Framework 4.6.1+, or you use its native .NET 7 builds. — [WinSW README](https://github.com/winsw/winsw)

### Inferences
- **Default stack for "zero prerequisites":**
  - .NET 10 self-contained `win-x64` (optionally single-file or trimmed);
  - Kestrel;
  - SQLite (Microsoft.Data.Sqlite ships its native lib in the publish output);
  - Foundry Local SDK in-process, with the model downloaded by the first-run wizard to `%ProgramData%\<App>\models` with progress UI.
  
  Result: a single MSI with no ExePackages.
- **If PostgreSQL is mandatory**, bundle EDB zip binaries as MSI components (read-only under Program Files). Then run a deferred, `Impersonate="no"` custom action (WixQuietExec) that:
  1. runs `initdb -D %ProgramData%\<App>\pgdata -U postgres --auth=scram-sha-256 --pwfile=…`;
  2. runs `pg_ctl register -N <App>-postgres -U "NT AUTHORITY\NetworkService" -S auto -D …` (or authors the service with `ServiceInstall` pointing at `postgres.exe -D …`);
  3. has a matching rollback CA (`pg_ctl unregister`) and uninstall CA.
  
  Chaining the EDB GUI installer drags in pgAdmin/StackBuilder, a different upgrade lifecycle and a separate ARP entry.
- **Ollama:** don't chain `OllamaSetup.exe` (it is per-user, so it doesn't fit a per-machine service). Either detect an existing Ollama at `localhost:11434` at runtime, or bundle the zip plus a wrapper (WinSW 2.x or NSSM) as an optional Burn/MSI feature, with `OLLAMA_MODELS` pointed at ProgramData.
- **Node.js variant:** ship either a Node SEA `.exe` or a private `node.exe` plus the app folder. Register it through WinSW or NSSM, because Node has no native SCM integration, so WiX `ServiceInstall` points at the wrapper exe. The HTTPS certificate would be loaded from a PFX/PEM file (Node's `tls` module cannot read the Windows cert store natively). That changes the certificate section: export to a PFX under ProgramData with tight ACLs.
- **Python variant:** similar. Use the embeddable Python distribution or a PyInstaller exe plus WinSW. Python TLS (uvicorn/hypercorn) also uses PEM files.

### Gaps
- Could not verify:
  - whether the Foundry Local SDK requires a VC++ redistributable or Windows App SDK runtime on target machines;
  - the Foundry Local model cache default path;
  - the size of the bundled binaries.
- No authoritative Python-on-Windows-service source fetched: pywin32 `servicemanager` and PyInstaller docs were not retrieved.
- Did not verify EDB zip availability for PostgreSQL 18.x specifically, or its licensing notes for redistribution.

## 3. Registering Windows services in WiX (ServiceInstall/Control/Config, recovery, accounts, delayed start, dependencies)

### Takeaway
Author each service with `ServiceInstall` + `ServiceControl` (`Start="install" Stop="both" Remove="uninstall" Wait="yes"`) inside the component whose KeyPath is the service exe. Add `util:ServiceConfig` for recovery actions (all three action types are required in v4+), `ServiceDependency` for ordering, and the virtual account `NT SERVICE\<Name>` as the default least-privilege identity. Delayed auto-start is still a WiX/MSI pain point: `ServiceConfig/@DelayedAutoStart` works but produces warning WIX1149.

### Cited Findings
**`ServiceInstall` attributes**
- `Name`, `DisplayName`, `Description`
- `Type` (`ownProcess`/`shareProcess`)
- `Start` (`auto`/`demand`/`disabled`)
- `ErrorControl`
- `Account`: "Fully qualified names must be used even for local accounts, e.g.: '.\LOCAL_ACCOUNT'"
- `Password`
- `Arguments`
- `Vital`

Children: `ServiceDependency`, `ServiceConfig` (util), `ServiceConfigFailureActions`, `PermissionEx` (util), `UrlReservation` (http). "The service executable installed will point to the KeyPath for the Component." — [FireGiant: ServiceInstall](https://docs.firegiant.com/wix/schema/wxs/serviceinstall/)

**`ServiceControl`** has `Start`, `Stop` and `Remove` = `install`|`uninstall`|`both`, and `Wait` (default yes). Example semantics: `Start='install' Stop='both' Remove='uninstall'`. — [FireGiant: ServiceControl](https://docs.firegiant.com/wix/schema/wxs/servicecontrol/)

**`util:ServiceConfig`**
- `FirstFailureActionType`, `SecondFailureActionType` and `ThirdFailureActionType` are **required**; values are `none|reboot|restart|runCommand`.
- Also `ResetPeriodInDays`, `RestartServiceDelayInSeconds`, `ProgramCommandLine`, `RebootMessage`, `ServiceName`.
- Placed under a `Component` (instead of `ServiceInstall`), it configures an already existing service, and "If the service does not exist prior to the install … the install will fail".

— [FireGiant: util:ServiceConfig](https://docs.firegiant.com/wix/schema/util/serviceconfig/)

**MSI-native `ServiceConfig`** (MSI 5.0 `MsiServiceConfig` table)
- `DelayedAutoStart`, `FailureActionsWhen`, `PreShutdownDelay`
- `ServiceSid` (`none`/`restricted`/`unrestricted`)
- `RequiredPrivilege` children
- `OnInstall`/`OnReinstall`/`OnUninstall`

The docs warn: "a remark indicating the functionality does not work correctly was added later. Consider using the util:ServiceConfig element instead." — [FireGiant: ServiceConfig](https://docs.firegiant.com/wix/schema/wxs/serviceconfig/)

**Delayed start:** `util:ServiceConfig` has no delayed-start attribute. The community workaround is `<ServiceConfig DelayedAutoStart="yes" OnInstall="yes" OnReinstall="yes"/>` despite WIX1149. The alternatives are `sc.exe config … start= delayed-auto` via a custom action, or the `DelayedAutostart` registry value. There is no official best practice. — [WiX discussion #8721](https://github.com/orgs/wixtoolset/discussions/8721)

**MS Learn tutorial (WiX tab)**
- Uses `Account="LocalService"`, `Start="auto"`, `ErrorControl="normal"`, and `ServiceControl Start="install" Stop="both" Remove="uninstall" Wait="true"`.
- Lists the account options LocalService (reduced privileges, no network credentials), NetworkService (network credentials) and LocalSystem ("use with caution").
- The tutorial itself calls custom `/Install` switches with `sc.exe` "an anti-pattern".

— [MS Learn: Create a Windows Service installer](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer?tabs=wix)

**Account types**
- Virtual accounts (`NT SERVICE\<SERVICENAME>`) are automatically managed with no password, and access the network as `<domain>\<computer>$`.
- gMSA is for multi-server or load-balanced services; dMSA arrived in Windows Server 2025.
- There is a table comparing sMSA, gMSA, dMSA and virtual accounts.

— [MS Learn: Service Accounts in Windows Server (2026-09-14)](https://learn.microsoft.com/en-us/windows-server/identity/ad-ds/manage/understand-service-accounts)

**Known WiX gap with virtual accounts:** `util:User`/`util:Group` membership for `NT SERVICE\ExampleService` failed with 0x8007056b (ERROR_NO_SUCH_MEMBER). The maintainer's explanation: "Managed service accounts didn't exist when the User custom action was written." The user's workaround was a custom deferred CA scheduled **after `InstallServices`** that calls `NetLocalGroupAddMembers`. — [WiX discussion #8722](https://github.com/orgs/wixtoolset/discussions/8722)

**`PermissionEx`** (util) can grant service rights (`ServiceStart`, `ServiceStop`, `ServiceQueryStatus`, …) under `ServiceInstall`, and file rights under `CreateFolder`/`File`/`Registry`. — [FireGiant: util:PermissionEx](https://docs.firegiant.com/wix/schema/util/permissionex/)

**Windows Service runtime pitfalls**
- A service's current directory is `C:\Windows\System32`, so use `ContentRootPath`/`AppContext.BaseDirectory`.
- Common startup failures: wrong cert/resource paths, and a missing "Log on as a service" right for custom accounts.

— [MS Learn: Host ASP.NET Core in a Windows Service](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)

### Inferences
- **Suggested services** (one component each, same feature):
  - `AppName.Web` (Kestrel, HTTPS 6500), `Start=auto`;
  - `AppName.Worker` (M365 collection jobs), `Start=auto` + `ServiceDependency Id="AppName.Web"` if it needs the API, or depending on `AppName.Postgres` when PostgreSQL is used;
  - optional `AppName.Postgres` / `AppName.Ollama`.
  
  Each gets `util:ServiceConfig First/Second=restart Third=none ResetPeriodInDays=1 RestartServiceDelayInSeconds=30`.
- **Accounts:**
  - Use `Account="NT SERVICE\AppName.Web"` (virtual account) for least privilege plus a per-service SID for ACLs. Not directly verified that MSI `ServiceInstall` accepts `NT SERVICE\…` without a password; test it. The fallback is `NT AUTHORITY\LocalService`.
  - Avoid LocalSystem.
  - Use a gMSA only if the service must reach network resources with a domain identity. The app mostly calls Microsoft Graph over HTTPS with app credentials, so it doesn't need that.
- **ACLs for virtual-account principals:** because of the sequencing issue (#8722), grant file ACLs to `NT SERVICE\…` either with `PermissionEx` scheduled after `InstallServices` (verify `ExecSecureObjects` ordering), or from the app on first start. Or grant to the well-known `LocalService` SID if you use LocalService.
- **Delayed start:** prefer plain `auto` start. If needed, use `ServiceConfig DelayedAutoStart="yes"` and verify on target OSes, or set `HKLM\SYSTEM\CurrentControlSet\Services\<name>\DelayedAutostart=1` (DWORD) via `RegistryValue` in the same component.
- **Stop on upgrade:** `Stop="both"` on `ServiceControl` makes StopServices run on both install and uninstall. During a MajorUpgrade the old product's uninstall stops and removes the service before files are replaced (with the default `afterInstallValidate` scheduling).

### Gaps
- No official WiX/FireGiant guidance was found confirming `ServiceInstall Account="NT SERVICE\…"` behavior or gMSA (`DOMAIN\gmsa$`, empty password) with MSI ServiceInstall.
- Could not verify whether the `MsiServiceConfig` "does not work correctly" remark is still true on Windows 11 24H2/25H2.

## 4. HTTPS on port 6500 at install time (certificates, trust, private key ACL, Kestrel vs HTTP.sys, firewall, renewal, corporate PKI)

### Takeaway
Use Kestrel with an HTTPS endpoint on `https://*:6500` (or localhost only). Load the certificate from `LocalMachine\My` by subject or thumbprint, and give the service account read access to its private key. Create the certificate at install time (a deferred custom action run as SYSTEM) or on the service's first start. For browser trust, either:
- import only a machine-generated, **name-constrained local root CA** into `LocalMachine\Root`. Chrome/Edge honor it, and Firefox 120+ imports user-added OS roots by default on Windows. Never ship a shared CA key, and remove it on uninstall. Or
- let admins supply a corporate PKI certificate (PFX or thumbprint) via MSI properties. This is the preferred option for LAN access.

HTTP.sys + `netsh http add sslcert` is only needed if you choose HTTP.sys (Windows auth, port sharing). Add a Windows Firewall rule only when LAN access is enabled.

### Cited Findings
**Kestrel configuration**
- Endpoint config `Kestrel:Endpoints:<name>:Url = "https://*:6500"` with `Certificate` from:
  - the store: `Subject`, `Store`, `Location` (**defaults to CurrentUser**), `AllowInvalid`;
  - a PFX: `Path`, `Password`;
  - PEM: `Path`, `KeyPath`.
- `Kestrel:Certificates:Default` applies to endpoints without their own certificate. `ASPNETCORE_HTTPS_PORTS` / `ASPNETCORE_URLS` are the shorthands.
- The docs warn against plain-text passwords in `appsettings.json`.

— [MS Learn: Configure Kestrel endpoints (ASP.NET Core 10)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
- "Use of the ASP.NET Core HTTPS development certificate to secure a service endpoint isn't supported." — [MS Learn: Windows Service hosting](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)

**HTTP.sys**
- Needs `netsh http add urlacl url=https://…:port/ user=…`, the certificate in LocalMachine\My, and `netsh http add sslcert ipport=<IP>:<PORT> certhash=<THUMBPRINT> appid="{GUID}"`, all **before the app runs** and with admin rights.
- It is useful for Windows Authentication, port sharing, and some features Kestrel lacks.

— [MS Learn: HTTP.sys in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys?view=aspnetcore-10.0)
- The WiX Http extension (`http://wixtoolset.org/schemas/v4/wxs/http`) provides `UrlReservation` (`Url`, `Sddl`/`UrlAce`, `HandleExisting`) and `SniSslCertificate` (`Host`, `Port`, `Thumbprint`, `Store`, `AppId`). — [FireGiant Http schema](https://docs.firegiant.com/wix/schema/http/); [SniSslCertificate](https://docs.firegiant.com/wix/schema/http/snisslcertificate/); [UrlReservation](https://docs.firegiant.com/wix/schema/http/urlreservation/)
- v6 release notes also mention non-SNI http.sys SSL certificate support (issue 7622), but the published schema index only lists SniSslCertificate, UrlAce and UrlReservation. — [release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/) vs [Http schema](https://docs.firegiant.com/wix/schema/http/)

**`iis:Certificate`** (Iis extension) installs or uninstalls a certificate:
- from a `Binary` stream (PFX + `PFXPassword`) or a `CertificatePath` (which can be a property);
- into `StoreLocation=localMachine` and `StoreName=personal|root|ca|trustedPeople…`;
- it also has a `Request` mode that requests the certificate from a CA.

— [FireGiant: iis:Certificate](https://docs.firegiant.com/wix/schema/iis/certificate/)

**`New-SelfSignedCertificate`**
- It is documented as "for testing purposes".
- `-CertStoreLocation` can only be `Cert:\LocalMachine\My` or `Cert:\CurrentUser\My`. The default validity is **one year** (`-NotAfter`).
- `-SecurityDescriptor` sets the **private key ACL** ("Read access is required to use the private key").
- `-Signer` signs with a CA certificate from the Personal store. `-KeyUsage CertSign` is used for CA certificates.
- `-TextExtension` supports SAN (`2.5.29.17={text}DNS=localhost&IPAddress=127.0.0.1&IPAddress=::1`), EKU `1.3.6.1.5.5.7.3.1` and **Name Constraints** `2.5.29.30`.
- `-KeyExportPolicy NonExportable`.
- `-TestRoot`'s "private key … is essentially public".

— [MS Learn: New-SelfSignedCertificate](https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate)

**Risk of a local root CA:** mkcert warns that "the `rootCA-key.pem` file … gives complete power to intercept secure requests from your machine. Do not share it." — [mkcert README](https://github.com/FiloSottile/mkcert)

**Browser trust**
- On Windows, "the Chrome Certificate Verifier automatically consumes certificates added to" LocalMachine and CurrentUser "Trusted Root Certification Authorities", and "considers local trust decisions". — [Chrome Root Store FAQ](https://chromium.googlesource.com/chromium/src/+/main/net/data/ssl/chrome_root_store/faq.md)
- Firefox 120 "imports user-added TLS trust anchors … from the operating system root store. This will be enabled by default on Windows, macOS, and Android". — [Firefox 120 release notes](https://www.firefox.com/en-US/firefox/120.0/releasenotes/); [analysis by J. Rogers](https://joshua.hu/mozilla-firefox-trusting-system-root-stores-qwacs-eu)
- Older material says `security.enterprise_roots.enabled` is off by default. — [Mozilla bug 1314010](https://bugzilla.mozilla.org/show_bug.cgi?id=1314010). This conflicts with the Firefox 120 notes; the newer release notes supersede it.

**Firewall**
- `FirewallException` (Firewall extension) takes `Name`, `Port`, `Protocol` (tcp assumed), `Scope` or `RemoteAddress` children, `Profile` (default all), `Program`/`File`, `Service`, `Enabled`, `IgnoreFailure`, `OnUpdate`, `Description`, `Grouping`, `EdgeTraversal`, `Outbound`.
- If registration fails the install rolls back unless `IgnoreFailure="yes"`.

— [FireGiant: FirewallException](https://docs.firegiant.com/wix/schema/firewall/firewallexception/)

**Entra ID sign-in on localhost vs LAN**
- Redirect URIs must be `https` except for localhost. For **localhost the port is ignored** when matching (e.g. `http://localhost:5000/MyApp` equals `http://localhost:8080/MyApp`). For all other hosts the port must match.
- Wildcards aren't supported, except for work/school-only apps via the manifest, and even then aren't recommended.
- The limit is 256 redirect URIs for org-only apps.
- The docs recommend `127.0.0.1` over `localhost`.

— [MS Learn: Redirect URI restrictions](https://learn.microsoft.com/en-us/entra/identity-platform/reply-url)

### Inferences
**Recommended certificate flow**
1. A property `CERT_THUMBPRINT` (corporate PKI) or `CERT_PFX_PATH` + `CERT_PFX_PASSWORD` (Hidden) wins if supplied.
2. Otherwise a deferred, non-impersonated custom action (C# DTF or native; avoid inline PowerShell) creates:
   - a per-machine root "AppName Local CA <machine>" with Name Constraints limited to `localhost`, the machine's FQDN and optional LAN IPs, stored in LocalMachine\Root (the private key can then be deleted or kept non-exportable);
   - a leaf certificate with SAN `localhost`, `127.0.0.1`, `::1`, the hostname and FQDN, EKU serverAuth, in LocalMachine\My, with a private key ACL for `NT SERVICE\AppName.Web`.
3. Write the thumbprint to `HKLM\SOFTWARE\<Company>\<App>\CertThumbprint` so upgrades reuse it and uninstall removes it (a rollback CA deletes it if the install fails).

Name-constraint enforcement on root certificates varies by client; test Chrome, Edge and Firefox.

**Alternative (simpler, robust):** the Web service generates or renews its own certificate on first start and on each start once it is under 30 days from expiry. This matters because the `New-SelfSignedCertificate` default is 1 year, and Apple/Chrome-style limits on leaf lifetimes may apply to publicly trusted certs. The installer only runs "trust root" and "remove root" custom actions. This keeps renewal out of the MSI.

**Kestrel config:** set `"Location": "LocalMachine"`, because the default is CurrentUser, which for a service account is the service profile store, a common pitfall. Or implement `ServerCertificateSelector` by thumbprint from the registry.

**LAN mode**
- Use a `LAN_ENABLED=1` property to switch the bind from `https://localhost:6500` to `https://*:6500` and add `FirewallException Port="6500" Protocol="tcp" Scope="localSubnet" Profile="domain,private"`.
- Each LAN hostname must be registered as an Entra redirect URI (`https://host:6500/signin-oidc`). Remote clients won't trust the machine-local root, so a corporate PKI certificate is effectively required for LAN.

**Port-in-use check:** an immediate custom action or launch condition checks whether TCP 6500 is bound (e.g. `Get-NetTCPConnection -LocalPort 6500` or IPGlobalProperties in a DTF CA) and fails early with a clear message. Also make `PORT` a public property that flows into config. No source was fetched for this; it is design advice.

**Uninstall:** remove the leaf certificate, the local root from LocalMachine\Root, the firewall rule (automatic with `FirewallException`), and the urlacl/sslcert if HTTP.sys is used (automatic with the WiX Http extension). Keep the certificate on upgrade (skip removal when `UPGRADINGPRODUCTCODE` is set).

### Gaps
- No official Microsoft guidance was found specifically on shipping a per-machine local CA for localhost apps.
- Name-constraints support in each browser was not verified.
- The exact current wording of Mozilla's KB on enterprise roots could not be loaded.
- Did not fetch a Microsoft doc on granting CNG private-key ACLs to service accounts beyond `-SecurityDescriptor`.

## 5. Custom actions, config files, folders/ACLs, shortcuts, first-run setup

### Takeaway
Any custom action that changes the machine must be **deferred, `Impersonate="no"`, with a matching rollback CA**, receiving data through `CustomActionData`. Use WiX's `WixQuietExec`/`WixSilentExec` for command lines and avoid PowerShell custom actions. Use `util:XmlFile` for XML; WiX has **no JSON-editing element**, so write settings to the registry or a small `appsettings.Machine.json` the app reads, or pass them as service arguments. Binaries go in Program Files; data, logs, DB and models go in ProgramData with `util:PermissionEx` ACLs. Tenant/consent configuration belongs in a web first-run wizard at https://localhost:6500.

### Cited Findings
**Deferred custom actions:** "deferred custom actions, including rollback custom actions and commit custom actions, are the only types of actions that can run outside the users security context". Deferred CAs only get limited context (`CustomActionData`). — [MS Learn: Deferred Execution Custom Actions](https://learn.microsoft.com/en-us/windows/win32/msi/deferred-execution-custom-actions)

**`CustomAction`** has `Execute` (`immediate`/`deferred`/`rollback`/`commit`…), `Impersonate` ("Typically the value should be 'yes', except when the custom action needs elevated privileges"), and `Return` (`check`/`ignore`/`asyncWait`/`asyncNoWait`). — [FireGiant: CustomAction](https://docs.firegiant.com/wix/schema/wxs/customaction/)

**`WixQuietExec`**
- Runs a command line without a console window and logs its output.
- Deferred form: `SetProperty Id="<CA Id>" Value="…" Sequence="execute"`, then `CustomAction … BinaryRef="Wix4UtilCA_$(sys.BUILDARCHSHORT)" DllEntry="WixQuietExec" Execute="deferred" Impersonate="no" Return="check"`.
- A nonzero exit code fails the install. `WixSilentExec` also hides output from the log (for secrets).
- `WixQuietExec64`/`WixSilentExec64` "will be removed in the next major version".

— [FireGiant: Quiet execution custom actions](https://docs.firegiant.com/wix/tools/wixext/quietexec/)

**`util:XmlFile`**
- Actions: `createElement`, `deleteValue`, `setValue`, `bulkSetValue`.
- Also `ElementPath` (formatted XPath; escape `[` `]`), `Permanent`, `PreserveModifiedDate`, `Sequence`, `SelectionLanguage=XPath`.

— [FireGiant: util:XmlFile](https://docs.firegiant.com/wix/schema/util/xmlfile/). No `util:JsonFile` page exists (`docs.firegiant.com/wix/schema/util/jsonfile/` returned 404 on 2026-09-27).

**`util:PermissionEx`**
- Applies to `CreateFolder`, `File`, `Registry*` and `ServiceInstall`.
- Uses `User`/`Domain` plus granular rights (`GenericAll`, `GenericWrite`, `CreateFile`, `Traverse`…).
- `Inheritable` defaults to yes. "GenericRead … specifying this will fail to grant read access."

— [FireGiant: util:PermissionEx](https://docs.firegiant.com/wix/schema/util/permissionex/)

**`ServiceInstall/@Arguments`** passes command-line arguments or properties to the service. — [FireGiant: ServiceInstall](https://docs.firegiant.com/wix/schema/wxs/serviceinstall/)

**Other WiX v6 changes**
- v6 reduced custom-action use in WixUI (EULA printing and path checks are now built-in).
- The `PerUserProgramFilesFolder` standard directory was added.

— [release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/)

**Standard directory:** the MS Learn WiX sample uses `StandardDirectory Id="ProgramFiles6432Folder"` and `Bitness="always64"` components. — [MS Learn tutorial](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer?tabs=wix)

### Inferences
**Layout**
- `[ProgramFiles64Folder]\<Company>\<App>\` holds read-only binaries: `web\`, `worker\`, `wwwroot\`.
- `[CommonAppDataFolder]\<Company>\<App>\` holds the rest:
  - `config\` holds `appsettings.Machine.json`, generated by the app from registry values;
  - `data\` holds `app.db`;
  - `logs\`;
  - `models\`;
  - `certs\`, only for PEM/PFX in Node/Python variants.
  
  Each is a `CreateFolder` + `PermissionEx`: `SYSTEM`/`Administrators` full control, service SID modify, `Users` none (config may contain secrets).

**Configuration hand-off**
- Public properties such as `PORT=6500`, `BIND=localhost|any`, `TENANTID`, `CLIENTID`, `CERT_THUMBPRINT`, `DATA_DIR` and `LLM_PROVIDER=foundry|ollama|none` are written with `RegistryValue` under `HKLM\SOFTWARE\<Company>\<App>`.
- The .NET host adds a small registry configuration source, or reads them in `Program.cs`. This avoids JSON editing, survives repair/upgrade, and uses the WiX "Remember Property" pattern to reload values on upgrade.

**Secrets:** never put a client secret in MSI properties or the log. Mark them `Hidden="yes"` and prefer certificate credentials, or have the first-run wizard store secrets with DPAPI (LocalMachine) or in Windows Credential Manager.

**Shortcut:** use an `InternetShortcut` (WiX core element) or a `.url` file in the Start Menu, "AppName (https://localhost:6500)". Optionally launch the browser at the end of an interactive install with `WixShellExec` (util) from the exit dialog, never in silent mode.

**First-run wizard** (web UI, only reachable from localhost until setup completes):
1. check service health and certificate trust;
2. Entra ID sign-in (admin);
3. admin-consent URL for the multi-tenant or single-tenant app registration, or "bring your own app registration" (tenant ID, client ID, certificate);
4. choose data retention;
5. LLM model download with progress;
6. optional LAN enablement, which requires a corporate cert and extra redirect URIs.

**Avoid PowerShell custom actions:** execution policy, PowerShell version drift, AMSI/EDR blocking, 64-bit vs 32-bit host, and quoting all cause trouble (general practitioner knowledge, not sourced here). If unavoidable, use `WixQuietExec` with `powershell.exe -NoProfile -ExecutionPolicy Bypass -File "[#script.ps1]"`, deferred and not impersonated, with a rollback counterpart. Better: a C# DTF custom action or a small signed `setup-helper.exe` shipped in the package, run via `WixQuietExec` (verbs `cert create|trust|remove`, `port check`, `db migrate`).

### Gaps
- Not verified: the exact WiX v7 element name and syntax for internet shortcuts (`util:InternetShortcut`) and `WixShellExec` on the current docs site.
- No official FireGiant statement on PowerShell custom actions was retrieved (search budget exhausted).

## 6. Upgrades, repair, uninstall with data retention, auto-update, logging

### Takeaway
Use `MajorUpgrade` for every release. Keep `Package Id`/UpgradeCode constant, and bump one of the first three version fields: MSI ignores the fourth. Keep user data outside MSI-managed components (ProgramData created by the app, or `Permanent` components). Offer an optional "remove all data" uninstall through `util:RemoveFolderEx` with a condition. For updates, prefer channel-native mechanisms: Intune supersedence or MSI version bump, winget, SCCM. A self-updater service is possible but adds security and signing burden. Always use `/l*v` logs.

### Cited Findings
**`MajorUpgrade`**
- The default `Schedule="afterInstallValidate"` "removes the installed product entirely before installing the upgrade product … if the installation of the upgrade product fails, the machine will have neither version installed".
- `afterInstallInitialize` rolls back the removal on failure.
- `afterInstallExecute` requires strict component rules.
- `WIX_UPGRADE_DETECTED` holds the detected product codes.
- MSI ignores the 4th version field; `AllowSameVersionUpgrades="yes"` also allows downgrades within the same first three fields, which is risky.
- Also `DowngradeErrorMessage`, `IgnoreRemoveFailure`, `MigrateFeatures`.

— [FireGiant: MajorUpgrade](https://docs.firegiant.com/wix/schema/wxs/majorupgrade/)
- WiX v5+ gives a "default major upgrade" with no XML. — [release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/)

**Data retention elements**
- `Component/@Permanent="yes"`: "the installer does not remove the component during an uninstall".
- `NeverOverwrite`: doesn't reinstall if the key path exists.

— [FireGiant: Component](https://docs.firegiant.com/wix/schema/wxs/component/)
- `util:RemoveFolderEx` removes a folder tree on install, uninstall or both, with an optional `Condition`. The path must come from a property resolved before `CostInitialize`. The documented approach is the "Remember Property" pattern (store the path in the registry, read it back on uninstall). — [FireGiant: util:RemoveFolderEx](https://docs.firegiant.com/wix/schema/util/removefolderex/)
- The MS Learn sample uses `<RemoveFile Id="ALLFILES" Name="*.*" On="both" />` in the install folder. — [MS Learn tutorial](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer?tabs=wix)

**msiexec options**
- `/i`, `/x`, `/f[p|o|e|d|c|a|u|m|s|v]` (repair; default `omus`), `/q[n|b|r|f]`, `/L*v logfile` (`/l*vx` for everything).
- Only **public (uppercase) properties** can be set on the command line: `msiexec /i app.msi PORT=6500 TENANTID="…" /qn /l*v C:\Temp\app.log`.

— [MS Learn: msiexec command-line options](https://learn.microsoft.com/en-us/windows/win32/msi/command-line-options)

**Update channels**
- Intune Win32 apps have detection rules (MSI product code + version), requirement rules, return-code handling (hard/soft reboot), and install context (System/User). The size cap is 30 GB per app. — [MS Learn: Add Win32 app](https://learn.microsoft.com/en-us/intune/intune-service/apps/apps-win32-add)
- Intune LOB MSI has an "Ignore app version" option for self-updating apps, to avoid race conditions between the app's updater and Intune. — [MS Learn: LOB apps](https://learn.microsoft.com/en-us/intune/app-management/deployment/add-lob-windows)
- The winget manifest supports `InstallerType` values including `msi`, `wix` and `burn`, plus `UpgradeBehavior`, `ProductCode`, `AppsAndFeaturesEntries`, `InstallerSwitches` (`Silent`, `SilentWithProgress`, `Custom`, `Log`), `Scope` (machine/user) and `ElevationRequirement`. The manifest schema is 1.12.0. — [winget-pkgs installer schema 1.12.0](https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.12.0/installer.md); [MS Learn: Create your package manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest)

### Inferences
**Versioning:** use `Version="YYYY.MM.patch"` or semver major.minor.patch, and never rely on the 4th field. Use `MajorUpgrade Schedule="afterInstallInitialize"` (rollback-safe) together with `DowngradeErrorMessage`.

**Preserve across upgrade**
- The DB, config, certificate thumbprint and models live in ProgramData folders that the app creates, or that the MSI creates as empty `CreateFolder`s without files. MSI then only removes empty folders.
- Registry settings are re-read via AppSearch/RegistrySearch ("Remember Property") so silent upgrades keep PORT, TENANTID and the rest.
- DB schema migrations run in the service at startup (EF Core migrations) rather than in a custom action. This makes rollback of the MSI independent of the DB; take a backup copy of `app.db` before migrating.

**Uninstall choices**
- The default keeps data.
- `REMOVE_DATA=1` (a checkbox in the maintenance UI, or on the command line with `msiexec /x … REMOVE_DATA=1`) triggers `RemoveFolderEx` on the data root and removal of the local CA.

**Repair** (`/fvomus` or ARP Repair) restores binaries, services and registry. The certificate CA should be idempotent (reuse the existing thumbprint if valid).

**Auto-update**
- Enterprise: Intune Win32 app with supersedence, or LOB MSI version bump; SCCM; winget (`winget upgrade`).
- Unmanaged/SMB: a separate low-privilege "update checker" that notifies the web UI. Installation runs the signed MSI with elevation.
- A fully automatic self-updater service running as SYSTEM needs signature verification (Authenticode + pinned publisher) of downloaded MSIs to avoid becoming a privilege-escalation path.

**Logging:** document `msiexec /i … /l*v "%TEMP%\AppSetup.log"` and bundle `Setup.exe /log "%TEMP%\AppSetup.log"`. Burn writes per-package logs (`WixBundleLog_[PackageId]` variables). — [FireGiant: ExePackage LogPathVariable](https://docs.firegiant.com/wix/schema/wxs/exepackage/)

### Gaps
- Default Intune return codes (0, 1707, 3010, 1641, 1618) were not captured verbatim from the fetched page.
- Did not verify winget-pkgs community-repo policy for business apps or private REST sources.

## 7. Code signing (MSI/EXE/DLLs, Burn engine detach/reattach, Artifact Signing, EV, SmartScreen, timestamping)

### Takeaway
Sign every PE (your exes/dlls, custom-action DLLs, setup helpers), the MSI, and for bundles **both the detached Burn engine and the final bundle**, always with an RFC 3161 timestamp. Azure **Artifact Signing** (the Aug-2026 docs name for Trusted Signing) costs $9.99/month (Basic), integrates with GitHub Actions via OIDC, and uses short-lived certificates, so timestamping is mandatory. However, Public Trust certificates are only available in the US, Canada, EU, UK, Australia, NZ, Japan, South Korea, Singapore, Switzerland, Norway and Israel. **Türkiye is not listed**, so a Turkey-only entity may need a traditional OV certificate on an HSM or cloud KMS. EV no longer gives SmartScreen reputation. Since 1 Mar 2026, new code-signing certificates are limited to 460 days.

### Cited Findings
**WiX signing guidance**
- Signing MSIs:
  - Sign the MSI; embedded cabinets are covered by the MSI signature.
  - For external cabinets, sign them and run `wix msi inscribe` (MSBuild target `SignMsi` does this).
  - Inside a bundle, you only need to sign the bundle, whose manifest carries hashes of every file.
- Bundles "need to be signed in two pieces". The engine is extracted and cached for repair and uninstall, and must be signed so UAC shows a friendly prompt. The flow:
  1. build the bundle;
  2. `wix burn detach bundle.exe -engine engine.exe`;
  3. sign `engine.exe`;
  4. `wix burn reattach bundle.exe -engine engine.exe -o final.exe`;
  5. sign `final.exe`.
- MSBuild equivalents: `SignOutput=true` plus custom targets `SignCabs`, `SignMsi`, `SignBundleEngine`, `SignBundle`.

— [FireGiant: Signing packages and bundles](https://docs.firegiant.com/wix/tools/signing/)

**Artifact Signing**
- Artifact Signing was formerly Trusted Signing.
  - It requires a paid subscription.
  - There is no custom CN/O; the CN is always the validated legal name.
  - It does **not** issue EV certificates, with "no plan to issue EV certificates in the future".
  - The service is FIPS 140-3 Level 3.
  - It signs "all file types that SignTool supports".
  - The timestamp health check URL is `http://timestamp.acs.microsoft.com`.
  - The issued Authenticode certificate is "valid for three days".
  - SmartScreen reputation "builds up automatically".
  - Identity validation must be renewed, otherwise signing stops.
  
  — [MS Learn: Artifact Signing FAQ (updated 2026-08-14)](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
- Pricing: Basic $9.99/mo covers 5,000 signatures; Premium $99.99/mo covers 100,000; both charge $0.005 per extra signature. — [Azure pricing: Artifact Signing](https://azure.microsoft.com/en-us/pricing/details/artifact-signing/); [product page](https://azure.microsoft.com/en-us/products/artifact-signing)
- "Public Trust certificates are available to organizations in the United States, Canada, the European Union, the United Kingdom, Australia, New Zealand, Japan, South Korea, Singapore, Switzerland, Norway, and Israel. Individual developers must be located in the United States or Canada. These geographic restrictions do not apply to Private Trust certificates." — [MS Learn: Artifact Signing quickstart (2026-05-21)](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)
- **GitHub Action `azure/artifact-signing-action@v2`:**
  - runs on the windows-2025/2022 runners (not Arm);
  - inputs: `endpoint` (e.g. `https://eus.codesigning.azure.net/`), `signing-account-name`, `certificate-profile-name`, `files-folder`, `files-folder-filter` (e.g. `exe,dll,msi`), `file-digest: SHA256`, `timestamp-rfc3161: http://timestamp.acs.microsoft.com`, `timestamp-digest: SHA256`;
  - OIDC through `azure/login@v3` is recommended;
  - the signer needs the "Artifact Signing Certificate Profile Signer" role.
  
  — [Azure/artifact-signing-action README](https://github.com/Azure/artifact-signing-action)

**SmartScreen**
- "EV certificates no longer bypass SmartScreen … Paying a premium for EV solely to avoid SmartScreen warnings is no longer justified."
- Signed OV/EV files still show warnings until reputation accumulates ("can take several weeks and hundreds of clean installs"). Self-signed files are treated as unsigned.
- Use a consistent signing identity. Enterprises can submit files to the Microsoft Security Intelligence portal.
- On Windows 11, Smart App Control "will block execution of unsigned files unless the file has a positive reputation".

— [MS Learn: SmartScreen reputation (2026-05-04)](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)

**Certificate lifetime:** under CA/B Forum ballot CSC-31, OV/EV code-signing certificates issued on or after **1 March 2026** have a maximum validity of **460 days**. — [DigiCert blog](https://www.digicert.com/blog/understanding-the-new-code-signing-certificate-validity-change); [DigiCert KB (459-day issuance)](https://knowledge.digicert.com/alerts/code-signing-certificates-459-day-validity)

### Inferences
**Signing pipeline order**
1. `dotnet publish` output: sign your own exes/dlls (skip Microsoft-signed runtime DLLs);
2. sign setup-helper/CA DLLs;
3. build the MSI, then sign the MSI;
4. build the bundle, then detach, sign the engine, reattach, and sign the bundle.

All with SHA256 plus RFC 3161 timestamps. With 3-day Artifact Signing certificates, an untimestamped signature becomes invalid within days, so treat timestamps as mandatory.

**Türkiye-based publisher:** eligibility is a blocker for Public Trust Artifact Signing unless an eligible-country legal entity signs. Options:
- an OV certificate with a cloud HSM (Azure Key Vault Premium/Managed HSM, or a CA-hosted signing service), signing via AzureSignTool or a CA's signing tool;
- for internal-only enterprise deployment, a Private Trust profile or the organization's own AD CS code-signing certificate, deployed to `TrustedPublisher`/Root via GPO/Intune.

**Reputation:** for Intune/GPO/SCCM-deployed MSIs, SmartScreen mostly doesn't apply, because files aren't downloaded by a browser with Mark-of-the-Web. For direct downloads, expect warnings early on. Keep one signing identity across releases.

### Gaps
- Pricing and availability of alternative OV cloud-HSM signing for Turkish entities (e.g. DigiCert KeyLocker, SSL.com eSigner, Sectigo) was not researched.
- Not verified whether Artifact Signing "Private Trust" is suitable for MSI trust on unmanaged machines. It likely isn't, since it's not publicly trusted.

## 8. Enterprise deployment (Intune Win32 vs LOB MSI, SCCM, GPO, silent parameters, winget)

### Takeaway
Ship a single per-machine MSI that installs silently with public properties (`msiexec /i App.msi /qn PORT=6500 TENANTID=… CLIENTID=… LAN_ENABLED=0 ACCEPT_EULA=1 /l*v …`). Prefer **Intune Win32** (wrapped with IntuneWinAppUtil) over LOB, because it supports multiple arguments, detection, supersedence, dependencies and return codes, and avoids the Autopilot mixing issue. GPO works with the plain MSI on a UNC share. winget supports `wix`/`burn` installer types.

### Cited Findings
- **Intune LOB:**
  - single-file `.msi`;
  - "Only one command-line argument can be specified";
  - "If you mix the installation of Win32 apps and line-of-business apps during Windows Autopilot enrollment, the app installation may fail as they both use the Trusted Installer service at the same time". Autopilot device preparation supports mixing.
  
  — [MS Learn: Add a Windows LOB app (2026-04-14)](https://learn.microsoft.com/en-us/intune/app-management/deployment/add-lob-windows)
- **Intune Win32:**
  - 30 GB per app;
  - detection rules (MSI product code with optional version check, file, registry, script);
  - requirement rules;
  - install behavior System/User;
  - device restart behavior and return-code mapping (Failed / Hard reboot / Soft reboot / Retry / Success).
  
  — [MS Learn: Add and assign Win32 apps](https://learn.microsoft.com/en-us/intune/intune-service/apps/apps-win32-add)
- Practitioner comparison: Win32 supports detection, dependencies and supersedence; LOB lacks them. With Autopilot, "standardise on Win32". — [Patch My PC: LOB vs Win32](https://patchmypc.com/blog/microsoft-intune-apps-line-business/); [Tiago Carvalho: Intune packaging guide 2026](https://www.tiagoscarvalho.com/microsoft-intune/intune-app-packaging-decision-guide-2026)
- **GPO:** place the .msi on a network share and assign it to computers (installed at startup) or users. Use the UNC path, not Browse. Redeploy/remove are supported. — [MS Learn: GPO software installation](https://learn.microsoft.com/en-us/troubleshoot/windows-server/group-policy/use-group-policy-to-install-software)
- **msiexec:** property syntax, only public properties, `/qn`, `/l*v`. — [MS Learn: msiexec options](https://learn.microsoft.com/en-us/windows/win32/msi/command-line-options)
- **winget:**
  - `wingetcreate new` creates and submits manifests;
  - community repo packages must support silent installs;
  - include the MSI ProductCode for best upgrade behavior;
  - `InstallerType` includes `wix` and `burn` (known formats get standard switches).
  
  — [MS Learn: winget manifest](https://learn.microsoft.com/en-us/windows/package-manager/package/manifest); [installer schema](https://github.com/microsoft/winget-pkgs/blob/master/doc/manifest/schema/1.12.0/installer.md)
- **Burn command line:** only `bal:Overridable` variables accept `Name=Value`. — [FireGiant: Variable](https://docs.firegiant.com/wix/schema/wxs/variable/)

### Inferences
**Standard silent command lines to document**
- Install:
  `msiexec /i "AppName-x64.msi" /qn /norestart /l*v "%ProgramData%\AppName\install.log" PORT=6500 BIND=localhost TENANTID=<guid> CLIENTID=<guid> CERT_THUMBPRINT=<optional> LLM_PROVIDER=foundry REMOVE_DATA=0`
- Uninstall:
  `msiexec /x {ProductCode} /qn /l*v …`, or `msiexec /x AppName-x64.msi REMOVE_DATA=1 /qn` to wipe data.
- Bundle:
  `AppNameSetup.exe /quiet /norestart /log setup.log PORT=6500 INSTALL_POSTGRES=1` (Burn switches are `/quiet`, `/passive`, `/norestart`, `/log`, `/uninstall`, `/repair`, `/layout`; general Burn knowledge, not re-verified).

**Intune:** the Win32 app wraps the MSI (plus optional transforms or a `.ps1` pre-check). Detection is by MSI product code, or by the service existing plus a version registry value. Keep the default return codes for 3010 (soft reboot) and 1641 (hard reboot). Deploy per-tenant settings via Intune install-command properties.

**SCCM/ConfigMgr:** use an Application with an MSI deployment type (auto-generated product-code detection). The same command lines apply.

**Offline or air-gapped sites:** the Foundry model download needs internet. Provide a `MODEL_SOURCE=\\share\models` property or an offline model pack, or let the first-run wizard import a model folder.

### Gaps
- SCCM/ConfigMgr-specific docs were not fetched.
- The Intune default return-code table was not captured verbatim.
- winget private/REST source options for internal distribution were not researched.

## 9. CI/CD: building MSI/bundle in GitHub Actions and automated install tests

### Takeaway
`windows-latest` is now **Windows Server 2025 with VS 2026**. Its image preinstalls only **WiX 3.14.1** (legacy), so rely on the NuGet-restored `WixToolset.Sdk/7.0.x` (plain `dotnet build` of the .wixproj) or `dotnet tool install --global wix`. Set `AcceptEula=wix7`. Sign with `azure/artifact-signing-action@v2` (or an alternative signer), then test install, upgrade, repair and uninstall in a clean VM or Windows Sandbox with Pester 5.

### Cited Findings
**Runner images**
- `windows-latest` = `windows-2025` = `windows-2025-vs2026` (Windows Server 2025, x64). `windows-2022` and `windows-11-arm` are also available.
- `-latest` migrations happen gradually over 1–2 months, so pin the image for reproducibility.

— [actions/runner-images README](https://github.com/actions/runner-images)
- The windows-2025-vs2026 image (20260907) includes "WiX Toolset 3.14.1.8722", .NET SDKs 8.0.x/9.0.x/10.0.x (up to 10.0.400) and Pester 3.4.0/5.9.0. — [Windows2025-VS2026 image readme](https://github.com/actions/runner-images/blob/main/images/windows/Windows2025-VS2026-Readme.md)

**WiX in CI:** `dotnet tool install --global wix` installs the wix CLI. SDK-style `.wixproj` builds with `dotnet build`, and v7 needs `AcceptEula`. — [MS Learn tutorial](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer?tabs=wix); [FireGiant OSMF](https://docs.firegiant.com/wix/osmf/); [MSBuild](https://docs.firegiant.com/wix/tools/msbuild/)

**Artifact Signing action** example workflow and runner support are covered in section 7. — [Azure/artifact-signing-action](https://github.com/Azure/artifact-signing-action)

**Windows Sandbox (`.wsb`)**
- Options: `MappedFolders` (`HostFolder`, `SandboxFolder`, `ReadOnly`), `LogonCommand/Command`, `Networking`, `MemoryInMB`, `vGPU`, `ClipboardRedirection`, `ProtectedClient`.
- It is a clean disposable VM, useful for install tests.
- Mapped folders are mapped before the logon command runs. Write-mapped folder changes persist on the host (use this for logs and test results).

— [MS Learn: Windows Sandbox configuration](https://learn.microsoft.com/en-us/windows/security/application-security/application-isolation/windows-sandbox/windows-sandbox-configure-using-wsb-file)
- The FireGiant WiX tutorial (Sprint 3) tests MSIs in Windows Sandbox and embeds the cabinet with `<MediaTemplate EmbedCab="yes" />` for easy copying. — [FireGiant tutorial: Testing in the Sandbox](https://docs.firegiant.com/wix/tutorial/sprint3/testing-in-the-sandbox/)

### Inferences
**Pipeline** (GitHub Actions, `runs-on: windows-2025`):
1. `actions/setup-dotnet` (10.0.x);
2. build/test backend and SPA (`npm ci && npm run build` into `wwwroot`);
3. `dotnet publish -c Release -r win-x64 --self-contained` for each service;
4. sign binaries (Artifact Signing action, `files-folder-filter: exe,dll`);
5. `dotnet build installer/App.wixproj -c Release -p:Version=$(ver)` (with `AcceptEula` in `Directory.Build.props`);
6. sign the MSI;
7. optional bundle: build, `wix burn detach`, sign the engine, `wix burn reattach`, sign the bundle;
8. upload artifacts;
9. generate winget and Intune packages (IntuneWinAppUtil) on release tags.

**Automated install tests:** GitHub-hosted runners don't support nested virtualization for Sandbox (general knowledge, not verified here). Two options:
- run a Pester suite directly on a fresh `windows-2025` runner (it is ephemeral, so effectively a clean machine);
- use a self-hosted Hyper-V/Azure VM with checkpoints for upgrade-from-N-1 tests.

**What the Pester suite should check**
1. `msiexec /i … /qn` exit code 0 or 3010;
2. services exist, run, and use the expected account (`Get-CimInstance Win32_Service`);
3. `Invoke-WebRequest https://localhost:6500/health` succeeds with certificate validation (root trusted);
4. the firewall rule exists only when `LAN_ENABLED=1`;
5. ACLs on ProgramData;
6. an upgrade from the previous release preserves the DB and settings;
7. repair (`/fvomus`);
8. uninstall with and without `REMOVE_DATA` (no leftover services, certificates or firewall rules);
9. the log has no "Return value 3".

Developers can reproduce locally with a `.wsb` that maps `artifacts\` read-only and a `results\` folder read-write, with `LogonCommand` running `run-tests.ps1`.

### Gaps
- Nested virtualization / Windows Sandbox support on GitHub-hosted Windows runners was not verified.
- No first-party GitHub Action for WiX exists or was found. The SDK approach needs none.

## 10. Reference examples and templates

### Takeaway
The official Microsoft Learn "Create a Windows Service installer" tutorial has a WiX tab, but it is **dated**: it targets `WixToolset.Sdk/4.0.0` and a single-exe service. Use it for the `ServiceInstall`/`ServiceControl` shape, then modernize: WiX v7 SDK, `Files` harvesting, `Package Id`, `AcceptEula`, `util:ServiceConfig`, a firewall rule, and certificate handling. FireGiant's docs and tutorial are the authoritative current references.

### Cited Findings
**MS Learn tutorial (ms.date 2025-10-20)**
- Prerequisites: `dotnet tool install --global wix` and HeatWave.
- `.wixproj` uses `<Project Sdk="WixToolset.Sdk/4.0.0">` with a `ProjectReference`.
- `Package.wxs` has:
  - `MajorUpgrade DowngradeErrorMessage=…`;
  - `StandardDirectory Id="ProgramFiles6432Folder"`;
  - `Component Bitness="always64"`;
  - `File Source="$(var.App.WindowsService.TargetDir)publish\App.WindowsService.exe" KeyPath="true"`;
  - `RemoveFile Name="*.*" On="both"`;
  - `ServiceInstall … Account="LocalService"`;
  - `ServiceControl Start="install" Stop="both" Remove="uninstall" Wait="true"`.
- It warns that custom `/Install` switches with sc.exe are an anti-pattern.

— [MS Learn: Create a Windows Service installer](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service-with-installer?tabs=wix); [source on GitHub](https://github.com/dotnet/docs/blob/main/docs/core/extensions/windows-service-with-installer.md)

**FireGiant resources**
- The WiX tutorial (sprints 1–5) covers HeatWave, project references, Windows Sandbox testing, launch conditions and extensions. — [FireGiant tutorial index](https://docs.firegiant.com/wix/tutorial/)
- WiX element reference pages: `ServiceInstall`, `ServiceControl`, `util:ServiceConfig`, `FirewallException`, `http:*`, `iis:Certificate`, `util:PermissionEx`, `util:XmlFile`, `util:RemoveFolderEx`, `MajorUpgrade`, `ExePackage`, Burn and signing. All are linked in the sections above and live under [docs.firegiant.com/wix/schema](https://docs.firegiant.com/wix/schema/).

**Third-party walkthroughs** (vendor content, useful but not authoritative):
- [Advanced Installer: WiX installer add Windows services](https://www.advancedinstaller.com/versus/wix-toolset/wix-installer-add-windows-services.html)
- [Advanced Installer: WiX installer for .NET Windows service](https://www.advancedinstaller.com/versus/wix-toolset/create-wix-installer-for-windows-service-dot-net.html)
- [tabsoverspaces: lessons learned installing a service with WiX](https://www.tabsoverspaces.com/233292-lesson-learned-when-installing-windows-service-using-wix)

**Outdated practices to flag**
- Heat.exe: removed in v7.
- WiX v3 syntax (`<Product>`, `Win64="yes"`, candle/light): v3–v5 are out of community support.
- `WixToolset.Sdk/4.0.0` in the MS Learn sample.
- `WixQuietExec64`: slated for removal.
- VC++ merge modules: deprecated.
- EV certificates bought for SmartScreen: no longer effective.
- NSSM for new projects: no release since 2014/2017.
- The ASP.NET Core dev certificate for services: unsupported.

— sources: [release notes](https://docs.firegiant.com/wix/whatsnew/releasenotes/), [FireGiant blog](https://www.firegiant.com/blog/2026/4/6/wix-v7-heatwave-and-heatwave-build-tools-are-released/), [QuietExec](https://docs.firegiant.com/wix/tools/wixext/quietexec/), [VC++ redistribution](https://learn.microsoft.com/en-us/cpp/windows/redistributing-visual-cpp-files?view=msvc-170), [SmartScreen](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation), [nssm.cc](https://nssm.cc/download), [ASP.NET Core Windows Service](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)

### Inferences
**Illustrative v7 skeleton.** I assembled this from the element docs above; it has not been compiled or tested.
```xml
<!-- Installer.wixproj: <Project Sdk="WixToolset.Sdk/7.0.0"><PropertyGroup><AcceptEula>wix7</AcceptEula><InstallerPlatform>x64</InstallerPlatform></PropertyGroup>
     + PackageReference WixToolset.Util.wixext / WixToolset.Firewall.wixext (7.0.x) -->
<Wix xmlns="http://wixtoolset.org/schemas/v4/wxs"
     xmlns:util="http://wixtoolset.org/schemas/v4/wxs/util"
     xmlns:fw="http://wixtoolset.org/schemas/v4/wxs/firewall">
  <Package Id="Contoso.OpsIntel" Name="Contoso OpsIntel" Manufacturer="Contoso" Version="1.4.0" Scope="perMachine">
    <MajorUpgrade Schedule="afterInstallInitialize" DowngradeErrorMessage="A newer version is installed." />
    <Property Id="PORT" Value="6500" Secure="yes" />
    <Property Id="LAN_ENABLED" Value="0" Secure="yes" />
    <StandardDirectory Id="ProgramFiles6432Folder">
      <Directory Id="INSTALLFOLDER" Name="Contoso\OpsIntel">
        <Directory Id="WEBDIR" Name="web">
          <Component Id="WebService" Bitness="always64">
            <File Source="publish\web\OpsIntel.Web.exe" KeyPath="yes" />
            <ServiceInstall Name="OpsIntel.Web" DisplayName="OpsIntel Web" Type="ownProcess" Start="auto"
                            ErrorControl="normal" Account="NT SERVICE\OpsIntel.Web" Arguments="--urls https://localhost:[PORT]">
              <util:ServiceConfig FirstFailureActionType="restart" SecondFailureActionType="restart"
                                  ThirdFailureActionType="none" ResetPeriodInDays="1" RestartServiceDelayInSeconds="30" />
            </ServiceInstall>
            <ServiceControl Name="OpsIntel.Web" Start="install" Stop="both" Remove="uninstall" Wait="yes" />
            <RegistryValue Root="HKLM" Key="SOFTWARE\Contoso\OpsIntel" Name="Port" Value="[PORT]" Type="string" />
          </Component>
          <Files Include="publish\web\**" Exclude="publish\web\OpsIntel.Web.exe" />
        </Directory>
      </Directory>
    </StandardDirectory>
    <!-- LAN-only firewall rule: fw:FirewallException Port="[PORT]" Protocol="tcp" Scope="localSubnet" in a component conditioned on LAN_ENABLED=1 -->
    <!-- ProgramData folders with util:PermissionEx; deferred Impersonate="no" cert CA + rollback CA via WixQuietExec -->
  </Package>
</Wix>
```

**Workstream checklist for the plan** (installer and deployment):
1. Packaging decisions: self-contained .NET 10, SQLite default, Foundry SDK.
2. WiX v7 project and OSMF sponsorship.
3. Service design: accounts, recovery, dependencies.
4. Certificate/trust helper, including the LAN/corporate-PKI mode.
5. Registry-backed configuration plus the first-run web wizard (Entra consent).
6. Upgrade/uninstall data policy.
7. Code signing: vendor choice given the Türkiye eligibility issue.
8. Optional Burn bundle for PostgreSQL/Ollama.
9. CI pipeline and Pester install-matrix tests.
10. Intune/GPO/SCCM/winget packaging docs with silent command lines.
11. Support runbook: logs, repair, `/l*v`.

### Gaps
- No up-to-date official Microsoft or FireGiant end-to-end sample was found that combines WiX v6/v7, multiple ASP.NET Core services, HTTPS certificate provisioning and a firewall rule. The skeleton above is a synthesis and needs build verification.
