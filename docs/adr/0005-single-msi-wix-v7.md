# ADR-0005: WiX v7 SDK ile tek MSI (sıfır ön gereksinim) + opsiyonel Burn Setup.exe; MSIX yok

- **Durum:** Önerildi (Faz 0 spike A walking skeleton ve spike C'deki VC++ bağımlılığı sonucu bekleniyor)
- **Tarih:** 2026-09-27
- **Karar vericiler:** DevOps + kurulum/QA mühendisi, teknik lider
- **İlgili ADR'ler:** [0002](0002-dotnet-10-lts-self-contained.md), [0004](0004-https-certificate-strategy.md), [0006](0006-code-signing.md), [0014](0014-foundry-local-model-hosting.md), [0022](0022-msi-major-upgrade-updates.md)

## Bağlam

Ürün tek bir MSI ile kurulmalıdır. Ancak **bir MSI başka bir yükleyiciyi zincirleyemez**: Windows Installer'ın iç içe kurulum özelliği resmen kullanımdan kaldırılmıştır — "Do not use concurrent installations to install products that are intended to be released to the public" ([MS Learn](https://learn.microsoft.com/en-us/windows/win32/msi/concurrent-installations)). Desteklenen zincirleme yolu WiX Burn bundle `.exe`'dir.

Kurumsal kanallar MSI'ı tercih eder: GPO yalnızca `.msi` kurar; Intune LOB tek bir `.msi` kabul eder ve yalnızca tek komut satırı argümanına izin verir.

MSIX: paketlenmiş servisler yalnızca `localSystem`, `localService` veya `networkService` ile çalışır ve kısıtlı bir capability ister ([desktop6:Service](https://learn.microsoft.com/en-us/uwp/schemas/appxpackage/uapmanifestschema/element-desktop6-service)); sanal servis hesabı ve sertifika özel eylemleri gerektiren bu ürüne uymaz.

WiX Toolset **v7.0.0** (6 Nisan 2026) SDK tarzı projedir, `dotnet build` ile derlenir; Heat kaldırıldı, yerine `Files` toplayıcısı var. Yıllık geliri 10.000 $'ı aşan kuruluşlar OSMF öder; v7 `<AcceptEula>wix7</AcceptEula>` olmadan derleme yapmaz ([FireGiant OSMF](https://docs.firegiant.com/wix/osmf/)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Ön gereksinimleri mimariyle yok etmek + tek MSI (seçilen)** | Intune/GPO/SCCM/winget ile doğrudan uyum | Tüm bağımlılıklar self-contained olmalı |
| Burn bundle zorunlu | Her ön gereksinimi zincirler | GPO/Intune LOB `.exe` kabul etmez |
| MSIX | Modern paketleme | Sanal servis hesabı yok, kısıtlı capability |
| Advanced Installer / Inno Setup | GUI, hızlı | Ticari lisans (Advanced Installer) / MSI değil (Inno) |

## Karar

- Birincil teslimat **`OpsIntel-x64.msi`** (WiX v7 SDK, `WixToolset.Sdk/7.0.x` NuGet'ten sabitlenir; `Package Id="OpsIntel.Platform"`). Self-contained .NET, gömülü SQLite, süreç içi Foundry Local SDK ve statik SPA MSI içinde dosya olarak gelir; modeller MSI'a gömülmez.
- İkincil teslimat **`OpsIntelSetup.exe`** (Burn bundle) yalnızca gerçekten ayrı yükleyici gerektiren isteğe bağlı parçalar içindir (gerekirse VC++ v14 redistributable, opsiyonel Ollama, ileride PostgreSQL) ve ardından aynı MSI'ı çalıştırır. Faz 2 (S15) kapsamındadır.
- MSI'ın yaptığı işler (servisler, ACL'ler, registry, sertifika CA'ları, güvenlik duvarı, kısayol, yükseltme, kaldırma) [installation.md](../operations/installation.md) dokümanındadır.
- PowerShell özel eylemi kullanılmaz; imzalı C# `SetupHelper` çalıştırılır. Şema geçişleri MSI CA'sında değil servis başlangıcında yapılır.
- Bilinen WiX pürüzleri: gecikmeli başlatma yerine düz `auto`; `NT SERVICE\…` ACL'leri `InstallServices` sonrasında CA ile; `ServiceInstall`'ın `NT SERVICE\…` hesabını parolasız kabul etmesi test edilir.
- MSIX kullanılmaz. OSMF bütçelenir.

## Sonuçlar

### Olumlu

- Tek MSI; Intune Win32/LOB, GPO, SCCM ve winget (`InstallerType: wix`) için yeterli.
- MSI'ın zincirleme yapamaması engel değil, uyumluluk sağlayan bir ürün özelliğine dönüşür.

### Olumsuz

- Foundry Local'ın VC++ veya Windows App SDK runtime isteyip istemediği **doğrulanmadı**; "evet" ise tek MSI hedefi yalnızca Burn ile karşılanabilir.
- GitHub `windows-2025` imajında yalnızca WiX 3.14.1 kurulu gelir; v7 NuGet ile sabitlenmelidir.

## Doğrulama / açık noktalar

- Spike A: WiX v7 walking skeleton (2 servis + sertifika + `/health`).
- Spike C: Foundry Local'ın VC++ bağımlılığı (keşif fazının ilk sorusu).
- Pester 5 kurulum matrisi: `/qn` 0/3010; servis hesapları; health; güvenlik duvarı yalnız LAN modunda; ACL'ler; N-1 yükseltme; onarım; `REMOVE_DATA` ± kaldırma.

## Kaynaklar

- [Araştırma raporu §2](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [MSI notları §1–§6, §9](../research/notes/msi_kurulum_dagitim.md)
- [FireGiant Burn](https://docs.firegiant.com/wix/tools/burn/), [FireGiant sürüm notları](https://docs.firegiant.com/wix/whatsnew/releasenotes/)
- [Intune LOB](https://learn.microsoft.com/en-us/intune/app-management/deployment/add-lob-windows)
