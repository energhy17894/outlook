# ADR-0018: Denetim — hash zincirli append-only audit_event + periyodik imzalı özet

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, güvenlik mühendisi, KVKK danışmanı
- **İlgili ADR'ler:** [0010](0010-sqlite-fts5-vector-blob-storage.md), [0017](0017-approval-state-machine.md), [0021](0021-observability.md)

## Bağlam

Kurumsal güven, KVKK md. 12 hesap verebilirliği ve karar günlüğü dışa aktarımı için "kanıt → çıkarım → öngörü → önerilen aksiyon → insan kararı → yürütülen aksiyon" zinciri değiştirilemez biçimde kaydedilmelidir ([teknoloji yığını §1](../research/notes/teknoloji_yigini.md)). SQL Server 2025 Ledger tabloları doğal tamper-evidence sunar; SQLite'ta bu hash zinciriyle uygulanmalıdır.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **SQLite'ta hash zincirli append-only tablo + imzalı özet (seçilen)** | Gömülü, SQLite ile uyumlu, doğrulanabilir | Yerel yönetici tüm zinciri yeniden yazabilir; dış çapa gerekir |
| SQL Server 2025 Ledger | Yerleşik | SQL Server gerektirir ([ADR-0010](0010-sqlite-fts5-vector-blob-storage.md)) |
| Düz log dosyası | Basit | Değiştirilebilir, sorgulanamaz |
| MAF checkpoint'leri | — | Denetim kaydı değildir |

## Karar

- `audit_event` tablosu: event_id, ts_utc, actor {user oid, service, model}, event_type, subject, payload JSON, prev_hash, **hash = SHA-256(prev_hash ‖ kanonik payload)**.
- Tablo **append-only**'dir; güncelleme/silme uygulama katmanında yasaktır.
- Kaydedilen olaylar: öneri üretimi (ExtractionRun referansıyla), onay/ret/düzenleme (kullanıcı oid, gerekçe kodu, önce/sonra farkı), yürütme (Graph request-id, yanıt durumu), politika kararları, sağlayıcı katmanı değişiklikleri, yönetici ayarları.
- Periyodik olarak **imzalı özet** (zincir başı hash'i) dışa aktarılır; imza anahtarı ve özetin nereye aktarılacağı (ör. SIEM, kurum arşivi) Faz 0/S1'de belirlenecektir.
- Payload içerik değil ID ve hash taşır (içeriksiz log ilkesiyle uyumlu, [ADR-0021](0021-observability.md)); KVKK silme talepleri zinciri kırmadan karşılanmalıdır.

## Sonuçlar

### Olumlu

- Onay kararlarının sonradan değiştirilmesi tespit edilebilir.
- KVKK ve iç denetim için kanıt.

### Olumsuz

- Veri sahibi silme talebi ile değiştirilemez kayıt arasında gerilim: kişisel veri payload'a konmaz, yalnızca ID/hash; bu tasarımın hukukça teyidi gerekir.
- Kanonik JSON serileştirme kuralları kesinleştirilmeli.

## Doğrulama / açık noktalar

- S1 teslimatı: `audit_event` hash zinciri; zincir doğrulama testi (bir satır değiştirildiğinde tespit).
- İmzalı özetin anahtar yönetimi ve hedefi açık noktadır (kaynaklarda ayrıntılandırılmamış).

## Kaynaklar

- [Araştırma raporu §3 — AuditEvent; §5 — ADR listesi](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §1](../research/notes/teknoloji_yigini.md), [Özellikler/UX notları §5](../research/notes/ozellikler_ux.md)
