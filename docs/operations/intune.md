# Intune ile Dağıtım

*Durum: Planlama. Intune Win32 paketi ve winget manifesti Faz 2'de (S15) üretilecektir; MVP'de (S8) sessiz kurulum, Intune ve GPO belgeleri hazırlanır. Kaynak: [araştırma raporu §2](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [MSI notları §6, §8](../research/notes/msi_kurulum_dagitim.md).*

## 1. Win32 mi, LOB mu?

| Özellik | Intune Win32 (önerilen) | Intune LOB (MSI) |
|---|---|---|
| Paket | `.intunewin` (IntuneWinAppUtil ile sarılmış MSI) | Tek `.msi` |
| Komut satırı | Birden çok MSI özelliği | "Only one command-line argument can be specified" ([MS Learn](https://learn.microsoft.com/en-us/intune/app-management/deployment/add-lob-windows)) |
| Tespit kuralı | MSI ürün kodu (+ opsiyonel sürüm), dosya, registry, betik | Otomatik |
| Bağımlılık / supersedence | Var | Yok |
| Dönüş kodu eşlemesi | Var (Failed / Hard reboot / Soft reboot / Retry / Success) | Sınırlı |
| Autopilot | Win32 ile standartlaşın | Win32 ile karıştırılırsa Trusted Installer çakışması nedeniyle kurulum başarısız olabilir |
| Boyut sınırı | Uygulama başına 30 GB | — |

Kaynak: [Intune Win32](https://learn.microsoft.com/en-us/intune/intune-service/apps/apps-win32-add).

**Karar:** Kurumsal dağıtımda **Intune Win32** kullanılır.

## 2. Win32 uygulama tanımı (taslak)

| Alan | Değer |
|---|---|
| Kurulum komutu | `msiexec /i "OpsIntel-x64.msi" /qn /norestart /l*v "%ProgramData%\OpsIntel\install.log" PORT=6500 TENANTID=<guid> CLIENTID=<guid> LLM_PROVIDER=foundry LAN_ENABLED=0` |
| Kaldırma komutu | `msiexec /x {ProductCode} /qn` (veriyi silmek için `REMOVE_DATA=1`) |
| Kurulum davranışı | System |
| Tespit kuralı | MSI ürün kodu + sürüm; alternatif: servis varlığı + `HKLM\SOFTWARE\OpsIntel` sürüm değeri |
| Gereksinim kuralları | x64; Windows 11 23H2+ (ayrıntı [ADR-0024](../adr/0024-os-support-matrix.md)) |
| Dönüş kodları | Varsayılanlar korunur; 3010 soft reboot, 1641 hard reboot |
| Supersedence | Yeni sürüm bir önceki sürümün yerine geçer (MSI major upgrade, [ADR-0022](../adr/0022-msi-major-upgrade-updates.md)) |

**Doğrulanmadı:** Intune varsayılan dönüş kodu tablosu notlarda birebir kaydedilmedi (0, 1707, 3010, 1641, 1618 genel bilgidir); Faz 0'da teyit edilecek.

Kiracıya özel ayarlar (TENANTID, CLIENTID, CERT_THUMBPRINT) Intune kurulum komutu özellikleriyle geçirilir.

## 3. Önerilen eşlik eden Intune/Entra politikaları

| Politika | Amaç |
|---|---|
| Uyumluluk: BitLocker zorunlu | Durağan veri; kayıp cihazda KVKK ihlal riskini azaltma ([ADR-0011](../adr/0011-encryption-at-rest.md)) |
| Conditional Access: uyumlu cihaz + MFA | OpsIntel oturumları için |
| Token Protection | Önce **report-only** modda test; üçüncü taraf public client için davranış belgelenmemiş ([ADR-0007](../adr/0007-delegated-auth-bff-pkce.md)) |
| Kod imzalama güveni | Kurum içi AD CS sertifikası kullanılıyorsa güvenilir yayıncı olarak dağıtım |
| Defender for Cloud Apps app governance | Uygulamanın Graph kullanımını izleme |

## 4. Kayıp/çalıntı cihaz runbook'u (özet)

1. Intune'da **protected wipe** ("Wipe device, and continue to wipe even if device loses power") — bazı cihazları başlatılamaz bırakabilir; kiracı başına günde 500 silme sınırı ([Intune Wipe](https://learn.microsoft.com/en-us/intune/device-management/actions/wipe)).
2. Kullanıcının Entra oturumlarını ve refresh token'larını iptal edin.
3. Varsa anahtarları döndürün.
4. Şifreleme durumu Intune/BitLocker kanıtıyla teyit edilene kadar olayı potansiyel KVKK md. 12 ihlali olarak ele alın ([veri sahibi talepleri ve ihlal süreci](../compliance/kvkk/veri-sahibi-talepleri.md)).

## 5. Açık noktalar

- `.intunewin` üretiminin `release.yml` içinde otomasyonu (Faz 2).
- Autopilot device preparation ile karışık dağıtım senaryosu test edilmedi.
- Agent 365 / Intune envanterinde yerel ajan görünürlüğü (Faz 3 önerisi, [ek geliştirmeler](../product/additional-developments.md)).
