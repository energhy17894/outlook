# ADR-0009: Değişiklik tespiti — delta polling; webhook yok; Event Hubs kurumsal opsiyon

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, backend geliştiriciler
- **İlgili ADR'ler:** [0008](0008-delegated-graph-no-mail-send.md), [0012](0012-db-job-outbox-quartz.md), [0025](0025-shared-content-discovery.md)

## Bağlam

Graph değişiklik bildirimleri üç kanaldan gelir: webhook, Azure Event Hubs, Azure Event Grid ([Change notifications overview](https://learn.microsoft.com/en-us/graph/change-notifications-overview)).

- Webhook'lar "publicly accessible, HTTPS-secured endpoint" ister, 3 saniye içinde yanıt bekler; yavaş uç noktaların bildirimleri düşürülür ve "Dropped notifications can't be recovered" ([Webhook teslimi](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-webhooks)). Uyku moduna geçen dizüstü için kabul edilemez.
- driveItem bildirimleri 6 saate kadar gecikebilir; her durumda doğruluğun kaynağı delta'dır.
- Delta klasör/sürücü/takvim penceresi başınadır; Outlook limitleri uygulama + posta kutusu başına 10 dakikada 10.000 istek ve **4 eşzamanlı istek**tir ([Throttling limits](https://learn.microsoft.com/en-us/graph/throttling-limits)). SharePoint'te token'lı delta 1 RU'dur.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Delta polling (seçilen)** | Genel uç nokta yok; uykudan deltaLink ile devam | Birkaç dakikalık gecikme |
| Webhook (Azure Relay / dev tunnel üzerinden) | Anlık | Genel HTTPS, 3 sn SLA, düşürülen bildirimler geri gelmez |
| Event Hubs (müşterinin aboneliği) | Genel URL yok; PC çevrimdışıyken mesajlar tutulur | Azure kaynağı ve kurulum gerekir |
| Event Grid partner topic | Retry, dead-letter | Admin yetkilendirmesi, daha çok hareketli parça |

## Karar

- Değişiklik tespiti **delta sorgusuyla yoklama** üzerinedir: klasör başına `/me/mailFolders/{id}/messages/delta`, sürücü başına `/drives/{id}/root/delta`, takvim penceresi başına `/me/calendarView/delta`.
- Tüm Outlook isteklerinde `Prefer: IdType="ImmutableId"` gönderilir. `@odata.deltaLink` opak durum olarak saklanır.
- 410 Gone / `syncStateNotFound` → tam yeniden senkronizasyon + silme uzlaştırması. `@removed` türetilmiş kayıtları siler veya referanssız bırakır.
- Posta kutusu başına 3–4'lük semafor; `Retry-After`'a uyulur; posta kutusu/sürücü başına tek eşzamanlı delta turu.
- Webhook kullanılmaz. **Event Hubs**, müşteri kendi Azure Event Hub'ını sağlarsa kurumsal opsiyondur (Faz 3); bildirim yalnızca delta turunu tetikler, bildirim içeriğine güvenilmez; SAS yerine RBAC.
- Delta döngüleri, deltaLink'leri birebir oynatmak için ince bir ham HTTP yolu kullanır.

## Sonuçlar

### Olumlu

- Genel erişime açık uç nokta ve Azure bağımlılığı yok.
- Beş klasörü dakikada bir yoklamak 10 dakikada ≈52 istek eder; bütçenin çok altında.

### Olumsuz

- Gerçek zamanlı değil; Gelen/Gönderilen için 1–2 dk gecikme (önerilen aralıklar notlardan çıkarımdır).
- Outlook delta token ömrü "sabit değil"; tam yeniden senkronizasyon yolu sağlam olmalıdır.

## Doğrulama / açık noktalar

- S2 kabul kriterleri: 10.000 e-postalık ilk senkronizasyon hatasız, ele alınmamış 429 yok; kesinti sonrası deltaLink'ten devam.
- Graph kayıt/oynatma entegrasyon testleri (`tests/integration/`).

## Kaynaklar

- [Araştırma raporu §1 — Microsoft 365 entegrasyonu](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Graph entegrasyonu notları §2–§4](../research/notes/graph_entegrasyonu.md)
- [Delta query overview](https://learn.microsoft.com/en-us/graph/delta-query-overview), [Event Hubs teslimi](https://learn.microsoft.com/en-us/graph/change-notifications-delivery-event-hubs)
