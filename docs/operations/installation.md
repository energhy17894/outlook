# Kurulum

*Durum: Planlama — MSI henüz mevcut değildir; bu doküman Faz 0 walking skeleton ve S1/S8 teslimatlarının hedef davranışını tanımlar. Kaynak: [araştırma raporu §2](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [MSI kurulum ve dağıtım notları](../research/notes/msi_kurulum_dagitim.md). İlgili ADR'ler: [0004](../adr/0004-https-certificate-strategy.md), [0005](../adr/0005-single-msi-wix-v7.md), [0006](../adr/0006-code-signing.md), [0022](../adr/0022-msi-major-upgrade-updates.md), [0024](../adr/0024-os-support-matrix.md).*

## 1. Teslimatlar

| Dosya | Tür | Kullanım |
|---|---|---|
| `OpsIntel-x64.msi` | WiX v7 MSI, per-machine | **Birincil.** Intune Win32/LOB, GPO, SCCM, winget (`InstallerType: wix`) için tek başına yeterli |
| `OpsIntelSetup.exe` | WiX Burn bundle | Opsiyonel (Faz 2, S15). Yalnızca ayrı yükleyici gerektiren isteğe bağlı parçalar için; ardından aynı MSI'ı çalıştırır |

## 2. Ön gereksinimler: mimariyle ortadan kaldırıldı

Bir MSI başka bir yükleyiciyi güvenli biçimde çalıştıramaz; Windows Installer'ın iç içe kurulum özelliği resmen kullanımdan kaldırılmıştır ([MS Learn: Concurrent Installations](https://learn.microsoft.com/en-us/windows/win32/msi/concurrent-installations)). Bu yüzden dış ön gereksinimler mimariyle yok edilir:

| Bileşen | Yaklaşım | Sonuç |
|---|---|---|
| .NET çalışma zamanı | Self-contained yayın; paylaşımlı framework gerekmez | MSI içinde dosya |
| ASP.NET Core Hosting Bundle | Yalnızca IIS için gerekli; Kestrel'de gerekmez | Yok |
| Veritabanı | Gömülü SQLite (yerel kütüphane yayın çıktısında) | MSI içinde dosya |
| LLM çalışma zamanı | Foundry Local SDK süreç içinde; ayrı servis yok | MSI içinde dosya |
| Web arayüzü | Statik SPA (`wwwroot`) | MSI içinde dosya |
| Modeller | GB'larca veri; MSI'a gömülmez. İlk çalıştırma sihirbazı indirir; çevrimdışı sitelerde `MODEL_SOURCE=\\paylaşım\models` | Kurulum sonrası |

**Doğrulanmadı:** Foundry Local'ın hedef makinede VC++ veya Windows App SDK runtime isteyip istemediği. Bu, Faz 0'ın ilk sorusudur (spike C). "Evet" ise VC++ v14 redistributable yalnızca Burn bundle ile zincirlenir (VC++ merge module'leri kullanımdan kalkmıştır).

**Sistem gereksinimleri:** x64; Windows 11 23H2+ veya Windows Server 2022/2025 (Windows 10 22H2 en iyi çaba); yönetici yetkisi; TCP 6500 boş. Model/donanım gereksinimleri (RAM, disk, GPU) spike C ve E sonrasında netleşecek.

## 3. MSI'ın yaptığı işler

Araç: **WiX Toolset v7.0.0** (6 Nisan 2026), SDK tarzı proje, `dotnet build`. Yıllık geliri 10.000 $'ı aşan kuruluşlar OSMF öder; v7 `<AcceptEula>wix7</AcceptEula>` olmadan derlemez ([FireGiant OSMF](https://docs.firegiant.com/wix/osmf/)).

| Sıra | İş | WiX mekanizması | Not |
|---|---|---|---|
| 1 | Başlatma koşulları: x64, Windows 11 23H2+/Server 2022+, yönetici, TCP 6500 boş | `Launch` koşulları + anlık CA `setup-helper port check` | Port doluysa net bir mesajla erken başarısız olur |
| 2 | İkili dosyalar `Program Files\OpsIntel\{host,ai,parser,web}` | `Files` toplama, `Package Id="OpsIntel.Platform"` | Salt-okunur |
| 3 | Veri klasörleri `ProgramData\OpsIntel\{config,data,blobs,logs,models,backup}` | `CreateFolder` + `util:PermissionEx` | SYSTEM/Administrators tam, servis SID'leri modify, Users yok |
| 4 | Servisler | `ServiceInstall` + `ServiceControl Start="install" Stop="both" Remove="uninstall"` + `util:ServiceConfig` (restart/restart/none) + `ServiceDependency` | Hesap: `NT SERVICE\OpsIntel.Host` / `NT SERVICE\OpsIntel.AI`; geri dönüş `LocalService`. LocalSystem kullanılmaz |
| 5 | Event Log kaynağı | `util:EventSource` | Yalnızca yöneticiler kaynak oluşturabilir |
| 6 | Yapılandırma: `PORT`, `TENANT_ID`, `CLIENT_ID`, `CERT_THUMBPRINT`, `LLM_PROVIDER`, `MODEL_SOURCE`, `LAN_ENABLED` | `RegistryValue` → `HKLM\SOFTWARE\OpsIntel` + "Remember Property" | WiX'te JSON düzenleme öğesi yoktur; uygulama registry'den okur |
| 7 | HTTPS sertifikası | Ertelenmiş, `Impersonate="no"` CA → `setup-helper cert create/trust`, rollback karşılığıyla | PowerShell CA kullanılmaz; imzalı C# yardımcı çalıştırılır |
| 8 | Sertifika yenileme görevi | CA ile SYSTEM zamanlanmış görevi (`setup-helper cert renew`, aylık) | Süresine 30 günden az kalan sertifikayı yeniler |
| 9 | Güvenlik duvarı | `fw:FirewallException` yalnızca `LAN_ENABLED=1` (domain/private, localSubnet) | Localhost modunda kural yok |
| 10 | Kısayol | Başlat menüsüne "OpsIntel (https://localhost:6500)" internet kısayolu; etkileşimli kurulum sonunda tarayıcıyı aç | Sessiz kurulumda tarayıcı açılmaz |
| 11 | Yükseltme | `MajorUpgrade Schedule="afterInstallInitialize"` + `DowngradeErrorMessage` | MSI sürümün 4. alanını yok sayar; ilk üç alan artırılır |
| 12 | Kaldırma | Veri varsayılan olarak korunur; `REMOVE_DATA=1` ile `util:RemoveFolderEx` veri ve sertifikayı siler | Yükseltmede (`UPGRADINGPRODUCTCODE`) sertifika silinmez |

**Bilinen WiX pürüzleri ve önlemler:**

- `util:ServiceConfig` gecikmeli başlatmayı desteklemez; MSI'ın yerel `ServiceConfig DelayedAutoStart` yolu WIX1149 uyarısı üretir ([WiX #8721](https://github.com/orgs/wixtoolset/discussions/8721)). Önerilen: düz `auto` başlatma.
- `NT SERVICE\…` sanal hesaplarını gruba eklemek 0x8007056b hatası verir; ACL işlemleri `InstallServices` sonrasında çalışan bir CA ile yapılır ([WiX #8722](https://github.com/orgs/wixtoolset/discussions/8722)).
- `ServiceInstall`'ın `NT SERVICE\…` hesabını parolasız kabul ettiği resmi olarak belgelenmemiştir — Faz 0'da test edilecek.

**Şema geçişleri** MSI özel eylemiyle değil servis başlarken çalışır; öncesinde `VACUUM INTO` ile otomatik yedek alınır. MSI geri alması veritabanından bağımsız kalır.

## 4. İlk çalıştırma sihirbazı

Kurulumdan sonra `https://localhost:6500` üzerinde (S2 teslimatı):

1. Servis sağlığı ve sertifika güveni kontrolü
2. Entra ile oturum açma
3. Admin consent bağlantısı (satıcıya ait veya müşterinin kendi uygulama kaydı)
4. Posta klasörü ve SharePoint/OneDrive site seçimi
5. Saklama süresi
6. Model indirme (ilerleme göstergesiyle) veya çevrimdışı model paketi

Hedef: kurulumdan ilk öneriye < 30 dk (model indirme hariç).

## 5. HTTPS sertifika stratejisi

Ortak kök CA dağıtmadan tarayıcı güveni ([ADR-0004](../adr/0004-https-certificate-strategy.md)):

1. **Kurumsal PKI önceliklidir.** `CERT_THUMBPRINT` verilirse o sertifika kullanılır; LAN modu için fiilen zorunludur.
2. **Kurumsal sertifika yoksa** kurulum makineye özel, kendinden imzalı bir **uç sertifika** üretir: `basicConstraints CA=false`; EKU serverAuth; SAN `localhost`, `127.0.0.1`, `::1`, makine adı ve FQDN; anahtar dışa aktarılamaz; özel anahtar ACL'i yalnızca `NT SERVICE\OpsIntel.Host`.
3. **Yalnızca bu uç sertifika** `LocalMachine\Root`'a eklenir. CA=false olduğu için başka sertifika imzalamakta kullanılamaz (standart X.509 yol doğrulamasına dayanan tasarım çıkarımı; test edilecek).

Neden kök CA yok: Dell'in özel anahtarıyla birlikte kök sertifika kurduğu olay MITM'e kapı açmıştı ([CERT/CC VU#925497](https://www.kb.cert.org/vuls/id/925497)).

Tarayıcılar: Chrome Windows'ta LocalMachine/CurrentUser kök depolarını otomatik tüketir; Firefox 120+ Windows'ta işletim sistemi köklerini varsayılan olarak içe aktarır. Edge Chromium tabanlıdır.

Kestrel tuzağı: depo tabanlı sertifika yapılandırmasında `Location` varsayılan olarak `CurrentUser`'dır; Host sertifikayı `LocalMachine\My`'den thumbprint ile yükler.

## 6. Sessiz kurulum ve kaldırma

```text
msiexec /i "OpsIntel-x64.msi" /qn /norestart /l*v "%ProgramData%\OpsIntel\install.log" ^
        PORT=6500 TENANT_ID=<guid> CLIENT_ID=<guid> CERT_THUMBPRINT=<opsiyonel> ^
        LLM_PROVIDER=foundry MODEL_SOURCE=\\srv\opsintel\models LAN_ENABLED=0

msiexec /x "OpsIntel-x64.msi" /qn REMOVE_DATA=1        :: veriyi de silerek kaldırma
OpsIntelSetup.exe /quiet /norestart /log setup.log      :: opsiyonel Burn bundle
```

| Özellik | Varsayılan | Açıklama |
|---|---|---|
| `PORT` | 6500 | HTTPS portu |
| `TENANT_ID` | — | Entra kiracı kimliği |
| `CLIENT_ID` | — | Uygulama (public client) kimliği |
| `CERT_THUMBPRINT` | boş | Kurumsal PKI sertifikası; boşsa makineye özel sertifika üretilir |
| `LLM_PROVIDER` | `foundry` | Varsayılan yerel sağlayıcı |
| `MODEL_SOURCE` | boş | Çevrimdışı model paylaşımı (UNC) |
| `LAN_ENABLED` | 0 | 1 ise güvenlik duvarı kuralı + LAN bağlama (Faz 2) |
| `REMOVE_DATA` | 0 | Kaldırmada 1 ise veri ve sertifika silinir |

Notlar:

- Yalnızca genel (büyük harfli) özellikler komut satırından ayarlanabilir. Burn değişkenleri yalnızca `bal:Overridable="yes"` ise komut satırından ayarlanabilir.
- Her kurulumda `/l*v` log alın; sorun giderme için bkz. [troubleshooting.md](troubleshooting.md).
- Sırlar MSI özelliklerine veya loga yazılmaz; PC'lere client secret dağıtılmaz.

## 7. Opsiyonel Burn bundle

`OpsIntelSetup.exe` (WiX Burn, WixStdBA) yalnızca gerçekten ayrı yükleyici gerektiren parçalar için kullanılır ([FireGiant Burn](https://docs.firegiant.com/wix/tools/burn/)):

- Foundry Local veya bir yerel kütüphane VC++ runtime isterse **VC++ v14 redistributable** (`ExePackage` + `DetectCondition`)
- **Opsiyonel Ollama** (standalone zip + sarmalayıcı; `OllamaSetup.exe` kullanıcı başına olduğu için zincirlenmez)
- İleride ekip sürümü için **PostgreSQL** (EDB zip ikilileri; GUI yükleyici zincirlenmez)

Her `ExePackage` için tespit koşulu zorunludur. Bundle `MsiPackage` + `MsiProperty` ile aynı MSI'ı çalıştırır. Kurumsal kanallar (GPO, Intune LOB) yalnızca MSI'ı kullanır.

## 8. Kod imzalama (Türkiye uyarısı)

- Tüm exe/dll, SetupHelper, MSI ve bundle SHA-256 + RFC 3161 zaman damgasıyla imzalanır.
- Burn bundle iki parçada imzalanır: engine ayrılır → imzalanır → geri eklenir → nihai bundle imzalanır ([FireGiant signing](https://docs.firegiant.com/wix/tools/signing/)).
- **Azure Artifact Signing Türkiye'deki kuruluşlara Public Trust sertifika vermez** (uygun ülkeler: ABD, Kanada, AB, İngiltere, Avustralya, Yeni Zelanda, Japonya, Güney Kore, Singapur, İsviçre, Norveç, İsrail — [quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)).
- Seçenekler: Türk tüzel kişiliği için **bulut HSM'de OV sertifika** (1 Mart 2026'dan itibaren en fazla 460 gün geçerlilik); yalnızca kurum içi dağıtım için **kurum AD CS** sertifikası (GPO/Intune ile güvenilir yayıncı).
- EV artık SmartScreen'i atlatmaz; Windows 11 Smart App Control itibarsız imzasız dosyaları engeller. Ayrıntı: [ADR-0006](../adr/0006-code-signing.md).

## 9. Kurumsal dağıtım kanalları

| Kanal | Not |
|---|---|
| Intune Win32 (önerilen) | Tespit kuralı, bağımlılık, supersedence, dönüş kodu eşlemesi — bkz. [intune.md](intune.md) |
| Intune LOB | Tek `.msi`, "Only one command-line argument can be specified"; Autopilot sırasında Win32 ile karıştırılmamalı |
| GPO | Yalnızca `.msi`; UNC paylaşımından, bilgisayara atanır |
| SCCM/ConfigMgr | MSI dağıtım türü (ürün kodu tespiti); aynı komut satırları (SCCM dokümanları notlarda ayrıca incelenmedi) |
| winget | `InstallerType: wix`; manifest Faz 2'de (S15) |

## 10. Kurulum testleri (CI)

GitHub `windows-2025` imajında yalnızca eski WiX 3.14.1 kurulu gelir; WiX v7 NuGet'ten `WixToolset.Sdk/7.0.x` ile sabitlenir. Pester 5 kurulum matrisi (`tests/installer/`):

1. `/qn` kurulumu 0 veya 3010 koduyla biter.
2. Servisler doğru hesapla çalışır.
3. `https://localhost:6500/health/ready`, sertifika doğrulamasıyla birlikte başarılı döner.
4. Güvenlik duvarı kuralı yalnızca LAN modunda oluşur.
5. ProgramData ACL'leri beklenen şekildedir.
6. N-1 sürümden yükseltmede veri korunur.
7. Onarım çalışır.
8. `REMOVE_DATA` ile ve onsuz kaldırmada geriye servis, sertifika veya kural kalmaz.

Geliştiriciler aynı testleri `.wsb` dosyasıyla Windows Sandbox'ta yerel çalıştırabilir.

Gerçek bir M365 kiracısına karşı elle Entra oturum açma testi için bkz.
[tenant-sign-in-test.md](tenant-sign-in-test.md).
