# ADR-0012: İşler — DB iş/outbox + Channels + Quartz.NET; Temporal/Dapr/Hangfire yok

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, backend geliştiriciler
- **İlgili ADR'ler:** [0001](0001-modular-monolith-two-services.md), [0010](0010-sqlite-fts5-vector-blob-storage.md), [0017](0017-approval-state-machine.md)

## Bağlam

Senkronizasyon, normalizasyon, çıkarım, bildirim ve yürütme işleri yeniden başlatmalardan sağ çıkmalı; Host ve Intelligence servisleri arasında devredilmelidir. Her müşteri PC'sine bir orkestratör sunucusu kurmak gereksizdir.

- Temporal'ın tek süreçli sunucusu açıkça "not intended for production use" diye işaretlenmiştir ([Temporal](https://docs.temporal.io/develop/run-a-development-server)).
- Quartz.NET'in `SQLite-Microsoft` sağlayıcısı vardır ([Quartz.NET](https://github.com/quartznet/quartznet/blob/main/src/Quartz/Impl/AdoJobStore/Common/dbproviders.properties)).
- Hangfire çekirdeği LGPL, Pro sürümü ticaridir; birinci taraf depoları SQL Server/Redis.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **DB iş/outbox + Channels + Quartz.NET (seçilen)** | Ek sunucu yok; transaction ile tutarlı niyet | İş dağıtıcıyı kendimiz yazarız |
| Temporal / Dapr | Olgun durable execution | Her PC'ye ek sunucu/sidecar; Temporal dev sunucusu üretim için değil |
| Hangfire | Pano | LGPL/Pro lisans sorusu; SQLite depolaması topluluk |
| Durable Task + MSSQL | Olgun | Yalnızca SQL Server seçilirse (ekip sürümü yükseltme yolu) |
| TickerQ | Hafif, EF Core | Genç proje |

## Karar

- Kalıcı işler veritabanındaki **`Job` tablosunda** tutulur: type, payload, `idempotency_key UNIQUE`, status, attempts, next_run_at, locked_by/until, last_error.
- **Transactional outbox:** durum değişikliği ve iş kaydı aynı transaction'da yazılır → *niyet* tam bir kez, *yürütme* en az bir kez.
- İşleyiciler **idempotenttir**; anahtarlar: Graph ID + changeKey (ingest), içerik hash'i + prompt sürümü (çıkarım), aksiyon ID'si (yürütme).
- `System.Threading.Channels` yalnızca dağıtıcı ile işçiler arasında sınırlı bellek tamponu; iş türü başına eşzamanlılık (Graph posta kutusu başına ≤4, LLM GPU kapasitesine göre).
- Üstel geri çekilme + jitter; **dead-letter** işler arayüzde görünür.
- Cron benzeri zamanlamalar **Quartz.NET** (SQLite sağlayıcısı) ile.
- Temporal, Dapr ve Hangfire kullanılmaz.

## Sonuçlar

### Olumlu

- Yeniden başlatma güvenliği; mesaj aracısı yok.
- Host ↔ Intelligence iş devri aynı mekanizmayla.

### Olumsuz

- İki servis aynı tabloyu talep ettiğinde kilitleme/talep (claim) semantiği dikkatle tasarlanmalı.
- Quartz.NET panosu yok; yönetici sayfasında iş durumu kendimiz gösteririz.

## Doğrulama / açık noktalar

- S1 teslimatı: SQLite + EF geçişleri; iş/outbox dağıtıcı.
- Dayanıklılık testi: servis öldürülüp yeniden başlatıldığında işler kaybolmaz ve çift yürütülmez (idempotency).

## Kaynaklar

- [Araştırma raporu §1 — Arka plan işleme](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §5](../research/notes/teknoloji_yigini.md)
