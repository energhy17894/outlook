# Intune gereksinimleri (Faz 0 taslağı)

> Kaynak: [ADR-0005](../../docs/adr/0005-single-msi-wix-v7.md), [ADR-0024](../../docs/adr/0024-os-support-matrix.md), [installation.md](../../docs/operations/installation.md).

## Uygulama türü

- **Win32 app** tercih edilir (LOB değil): tespit kuralları, gereksinim kuralları, supersedence ve dönüş kodu eşlemesi sunar; LOB bunlardan yoksundur ve tek komut satırı argümanına izin verir.
- Paket: `OpsIntel-x64.msi`, `IntuneWinAppUtil` ile `.intunewin` olarak sarmalanır.

## Gereksinim kuralları

| Kural | Değer |
|---|---|
| İşletim sistemi | Windows 11 23H2+ veya Windows Server 2022/2025 (Windows 10 22H2 en iyi çaba, ADR-0024) |
| Mimari | x64 |
| Disk alanı | En az 2 GB (kesin değer Faz 0 spike E sonrası netleşir) |
| Bellek | En az 8 GB RAM önerilir (yalnızca CPU çıkarımı için) |
| Yönetici yetkisi | Gerekli (per-machine kurulum) |
| TCP 6500 | Boş olmalı (kurulum başında kontrol edilir) |

## Kurulum komutu

```text
msiexec /i "OpsIntel-x64.msi" /qn /norestart PORT=6500 TENANT_ID=<guid> CLIENT_ID=<guid> CERT_THUMBPRINT=<opsiyonel> ALLOW_LAN=0 /l*v "%ProgramData%\OpsIntel\logs\install.log"
```

## Kaldırma komutu

```text
msiexec /x {ProductCode} /qn /norestart /l*v "%ProgramData%\OpsIntel\logs\uninstall.log"
```

Veriyi de silmek için `REMOVE_DATA=1` eklenir.

## Tespit kuralı

`detection.ps1` betiği (bu dizinde) kullanılır: ProductCode/UpgradeCode veya `HKLM\SOFTWARE\OpsIntel\ProductVersion` üzerinden.

## Dönüş kodları

| Kod | Anlam |
|---|---|
| 0 | Başarılı |
| 3010 | Başarılı, yeniden başlatma gerekli (yumuşak) |
| 1641 | Başarılı, yeniden başlatma başlatıldı |
| 1618 | Başka bir kurulum devam ediyor, yeniden denenmeli |

> **Doğrulanmadı:** Intune'un varsayılan dönüş kodu eşlemesi bu depoda birebir doğrulanmadı (msi_kurulum_dagitim.md §6 Gaps); Faz 0'da gerçek bir Intune kiracısında test edilecek.

## Bilinen sınırlamalar

- Windows Autopilot cihaz hazırlama sırasında Win32 ve LOB uygulamalarının karıştırılması "Trusted Installer" servisi çakışmasına yol açabilir; OpsIntel yalnızca Win32 olarak dağıtılarak bu sorundan kaçınılır.
- Çevrimdışı/hava boşluklu (air-gapped) sahalarda model indirme için `MODEL_SOURCE=\\paylaşım\models` özelliği veya çevrimdışı model paketi gerekir (Faz 2).
