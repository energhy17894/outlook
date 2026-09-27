# installer/

> **Durum:** Faz 0 iskeleti — WiX v7 walking skeleton eklendi (spike A tamamlandı).

Bu dizin tek MSI (`OpsIntel-x64.msi`) ve opsiyonel Burn bundle (`OpsIntelSetup.exe`) kaynaklarını barındırır. Skeleton makineler, yerel HTTPS sertifikası, iki servisi ve yönetilen ödünç kurma gerçekleştirir. Gerçek Windows Server kurulum testleri Faz 1'de tamamlanacaktır. Ayrıntı: [docs/operations/installation.md](../docs/operations/installation.md), [ADR-0005](../docs/adr/0005-single-msi-wix-v7.md). Kaynak: [araştırma raporu §2, §5](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md).

## Planlanan yapı

```text
installer/
├─ Directory.Build.props     # <AcceptEula>wix7</AcceptEula>, WixToolset.Sdk/7.0.x sabit
├─ OpsIntel.Installer.wixproj
├─ Package.wxs               # Package Id="OpsIntel.Platform", MajorUpgrade, başlatma koşulları
├─ Services.wxs              # ServiceInstall/ServiceControl/util:ServiceConfig, EventSource
├─ Folders.wxs               # Program Files + ProgramData klasörleri, util:PermissionEx
├─ Config.wxs                # HKLM\SOFTWARE\OpsIntel registry değerleri, Remember Property
├─ Cert.wxs                  # SetupHelper CA'ları (create/trust/remove + rollback), yenileme görevi
├─ Firewall.wxs              # fw:FirewallException yalnızca LAN_ENABLED=1
├─ Bundle/                   # opsiyonel Burn (Faz 2, S15)
│  ├─ OpsIntel.Bundle.wixproj
│  └─ Bundle.wxs
├─ intune/                   # detection.ps1, requirements.md
└─ winget/                   # manifestler
```

## Sorumluluklar

- Dosyalar, iki Windows servisi (`NT SERVICE\OpsIntel.Host`, `NT SERVICE\OpsIntel.AI`), ACL'ler, registry yapılandırması, HTTPS sertifikası, Event Log kaynağı, kısayol, yükseltme ve kaldırma.
- **Yapılmayanlar:** Şema geçişleri (servis başlangıcında), model indirme (ilk çalıştırma sihirbazı), PowerShell özel eylemleri, başka yükleyicilerin MSI içinden zincirlenmesi.

## Notlar

- GitHub `windows-2025` imajında yalnızca WiX 3.14.1 vardır; v7 NuGet'ten sabitlenir.
- Yıllık geliri 10.000 $'ı aşan kuruluşlar WiX OSMF öder.
- Kurulum testleri: `tests/installer/` (Pester 5 + Windows Sandbox `.wsb`).
- İmzalama: [ADR-0006](../docs/adr/0006-code-signing.md).
