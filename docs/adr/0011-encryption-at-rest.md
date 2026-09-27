# ADR-0011: Durağan veri — BitLocker ön kontrolü + SQLite3MC/SQLCipher + DPAPI anahtar sarma

- **Durum:** Önerildi (Faz 0 spike D: paketleme spike'ı bekleniyor)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, güvenlik mühendisi
- **İlgili ADR'ler:** [0007](0007-delegated-auth-bff-pkce.md), [0010](0010-sqlite-fts5-vector-blob-storage.md)

## Bağlam

Çıkarılmış e-posta içeriği ve Graph refresh token'ları tutan bir dizüstü yüksek değerli hedeftir. Kaybolan, şifrelenmemiş bir PC KVKK md. 12 kapsamında bildirilmesi gereken bir ihlaldir ([güvenlik notları §1, §6](../research/notes/guvenlik_uyum.md)).

- Microsoft.Data.Sqlite şifrelemeyi yalnızca şifreleme özellikli yerel kütüphane ile destekler.
- `SQLitePCLRaw.bundle_e_sqlcipher` kullanımdan kalktı; seçenekler MIT lisanslı **SQLite3 Multiple Ciphers** veya ticari **SQLCipher**'dır ([SQLitePCL.raw wiki](https://github.com/ericsink/SQLitePCL.raw/wiki/SQLite-encryption-options-for-use-with-SQLitePCLRaw)).
- SQLCipher 256-bit AES tam veritabanı şifrelemesi sunar ([Zetetic](https://www.zetetic.net/sqlcipher/)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **BitLocker ön kontrolü + DB şifrelemesi + DPAPI sarmalı anahtar (seçilen)** | İşletim sisteminden bağımsız DB şifrelemesi; crypto-shred | Paketleme değişkenliği, vektör eklentisiyle uyum belirsiz |
| Yalnızca BitLocker + NTFS ACL | En basit | BitLocker kapalıysa koruma yok; crypto-shred yok |
| SQL Server Express | — | Express'te TDE yok |

## Karar

- Kurulum/ilk çalıştırmada **BitLocker ön kontrolü** yapılır; durum yönetici sayfasında gösterilir (kapalıysa uyarı; zorlama politikası Intune uyumluluk ilkesiyle kurumda sağlanır).
- Veritabanı **SQLite3 Multiple Ciphers** (varsayılan, MIT) veya **SQLCipher** (ticari) ile şifrelenir; seçim spike D'de kesinleşir.
- Veritabanı anahtarı **DPAPI ile sarılır** ve servis SID'ine ACL'lenir; anahtarın silinmesi verinin kriptografik olarak imha edilmesini ("crypto-shred") sağlar.
- Blob deposu ve model önbelleği NTFS ACL'leriyle (SYSTEM/Administrators tam, servis SID'leri modify, Users yok) korunur. Blob dosyalarının ayrıca şifrelenmesi kaynaklarda kararlaştırılmamıştır (açık nokta).
- Kayıp cihaz için Intune protected wipe runbook'u ve Entra oturum iptali belgelenir.

## Sonuçlar

### Olumlu

- Cihaz kaybında veri DB düzeyinde de korunur; KVKK ihlal değerlendirmesi kolaylaşır.
- Offboarding'de crypto-shred.

### Olumsuz

- SQLite3MC ile sqlite-vec'in aynı bağlantıda birlikte yüklenmesi doğrulanmadı.
- Şifreleme sorgu performansını etkileyebilir (ölçülmedi).
- DPAPI kapsamı (LocalMachine vs servis hesabı) netleşmeli; makine kapsamı aynı makinedeki yöneticilere açıktır.

## Doğrulama / açık noktalar

- **Spike D git/gitme:** `vec0.dll` LoadExtension + SQLite3MC şifrelemesinin birlikte çalışması; başarısızsa vektör indeksi için alternatif (süreç içi .NET indeksi) veya şifreleme sağlayıcısı değişir.
- S8 teslimatı: DB şifrelemesi + DPAPI anahtarı; BitLocker ön kontrolü.
- Blob şifrelemesi gerekip gerekmediği DPIA'da değerlendirilecek.

## Kaynaklar

- [Araştırma raporu §1 — Veri katmanı; §4 — Uç nokta güvenliği](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §6](../research/notes/teknoloji_yigini.md), [Güvenlik notları §6](../research/notes/guvenlik_uyum.md)
- [MS Learn: Microsoft.Data.Sqlite encryption](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/encryption)
- [Intune Wipe](https://learn.microsoft.com/en-us/intune/device-management/actions/wipe)
