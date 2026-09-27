# ADR-0022: Güncelleme — MSI major upgrade (Intune/winget/SCCM); başlangıçta yedekle ve şema geçişi yap

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** DevOps + kurulum mühendisi, teknik lider
- **İlgili ADR'ler:** [0005](0005-single-msi-wix-v7.md), [0006](0006-code-signing.md), [0010](0010-sqlite-fts5-vector-blob-storage.md)

## Bağlam

- WiX `MajorUpgrade`'in varsayılan `afterInstallValidate` zamanlaması, yükseltme başarısız olursa makinede hiçbir sürüm bırakmayabilir; `afterInstallInitialize` kaldırmayı başarısızlıkta geri alır ([MajorUpgrade](https://docs.firegiant.com/wix/schema/wxs/majorupgrade/)).
- MSI sürümün **4. alanını yok sayar**; ilk üç alan artırılmalıdır.
- Velopack Windows servislerini kurmayı/güncellemeyi desteklemez (Temmuz 2024 ifadesi, güncelliği doğrulanmadı).
- Intune Win32 tespit kuralı, supersedence ve dönüş kodu eşlemesi sunar; winget `InstallerType: wix` destekler.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **MSI major upgrade, kanal-yerel dağıtım (seçilen)** | Kurumsal, denetlenebilir; Intune/SCCM/winget | Kendi kendini güncelleme yok |
| Uygulama içi otomatik güncelleyici (SYSTEM) | Yönetilmeyen PC'lerde kolay | İmza doğrulaması olmazsa yetki yükseltme yolu |
| Velopack | Masaüstü uygulamaları için kolay | Servis desteği yok |

## Karar

- Her sürüm **MSI major upgrade**'dir: `MajorUpgrade Schedule="afterInstallInitialize"` + `DowngradeErrorMessage`; `Package Id` sabit; sürümün ilk üç alanı artırılır.
- Dağıtım kanal-yereldir: Intune Win32 (supersedence), SCCM, winget, GPO.
- Opsiyonel: uygulama içi "güncelleme var" bildirimi (Faz 2); kurulum yine imzalı MSI ile ve yükseltilmiş yetkiyle.
- **Şema geçişleri MSI özel eylemiyle değil, servis başlarken** çalışır; geçişten önce `VACUUM INTO` ile otomatik yedek (`%ProgramData%\OpsIntel\backup\`) alınır. Böylece MSI geri alması veritabanından bağımsız kalır. Geçişler yalnızca ileri yönlü ve devam ettirilebilir olmalıdır.
- Kullanıcı verisi, yapılandırma ve modeller ProgramData'da MSI bileşeni olmayan klasörlerde tutulur; yükseltmede korunur. Registry değerleri "Remember Property" deseniyle yeniden okunur. Yükseltmede sertifika silinmez.

## Sonuçlar

### Olumlu

- N-1'den yükseltmede veri korunur; başarısız yükseltme geri alınır.
- Kurumsal BT'nin alışık olduğu süreç.

### Olumsuz

- Yönetilmeyen/küçük müşteriler için manuel güncelleme.
- Geriye dönük uyumsuz şema değişikliği, geri alma senaryosunda yedekten dönüş gerektirir.

## Doğrulama / açık noktalar

- Pester: N-1 sürümden yükseltmede veri korunur; onarım çalışır.
- Intune varsayılan dönüş kodları notlarda birebir kaydedilmedi; [intune.md](../operations/intune.md) Faz 0'da doğrulanacak.

## Kaynaklar

- [Araştırma raporu §2 — MSI'ın yaptığı işler (Yükseltme)](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [MSI notları §6, §8](../research/notes/msi_kurulum_dagitim.md), [Teknoloji yığını notları §8](../research/notes/teknoloji_yigini.md)
