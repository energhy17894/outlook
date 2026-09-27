# ADR-0025: Paylaşılan içerik keşfi — kullanıcı seçimli siteler + followedSites + /search/query; sharedWithMe kullanılmaz

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, backend geliştiriciler
- **İlgili ADR'ler:** [0008](0008-delegated-graph-no-mail-send.md), [0009](0009-delta-polling-change-detection.md)

## Bağlam

- `drive/sharedWithMe` kullanımdan kalktı: "deprecated and will operate in a degraded state until November, 2026, after which it will stop returning data" ([Graph docs](https://learn.microsoft.com/en-us/graph/api/drive-sharedwithme?view=graph-rest-1.0)).
- `GET /me/followedSites` yalnızca delegated'dır (Sites.Read.All), "has a known issue and might return incorrect results" ([List followed sites](https://learn.microsoft.com/en-us/graph/api/sites-list-followed?view=graph-rest-1.0)).
- Microsoft Search `/search/query` GA'dır, Copilot lisansı gerektirmez; posta, etkinlik ve dosyalarda arar ([search: query](https://learn.microsoft.com/en-us/graph/api/search-query?view=graph-rest-1.0)).
- SharePoint siteleri birden çok sürücüye (belge kitaplığı) sahip olabilir; `/sites/{id}/drives` ile keşfedilir; ilk tarama için delta kullanılmalıdır ([Scan guidance](https://learn.microsoft.com/en-us/onedrive/developer/rest-api/concepts/scan-guidance)).
- `sharedWithMe` için bir yedek API veya `/me/insights/shared` durumu birincil kaynakta doğrulanamadı.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Kullanıcı seçimli siteler + followedSites + /search/query + ekip/grup sürücüleri (seçilen)** | GA API'ler; kullanıcı kontrolü; amaçla sınırlılık | Tam kapsam garantisi yok |
| `sharedWithMe` | Tek çağrı | Kasım 2026'dan sonra veri yok |
| Copilot Retrieval API | Semantik | Kullanıcı başına Copilot lisansı; 200 istek/saat |
| App-only tüm site taraması | Tam kapsam | PC'de app-only yok ([ADR-0008](0008-delegated-graph-no-mail-send.md)); ölçülülük ilkesine aykırı |

## Karar

- Keşif kaynakları:
  1. İlk çalıştırma sihirbazında **kullanıcının seçtiği siteler/kitaplıklar**,
  2. `/me/followedSites` (bilinen sorunları nedeniyle yalnızca öneri kaynağı),
  3. Microsoft Search `/search/query` (driveItem/listItem; e-posta başlıklarında atıf yapılan belgeleri bulmak için),
  4. Ekip/grup sürücüleri (`/me/joinedTeams` → grup sürücüsü).
- Keşfedilen her sürücü için `/drives/{id}/root/delta` ile takip ([ADR-0009](0009-delta-polling-change-detection.md)).
- `sharedWithMe` **kullanılmaz**.
- Kısıtlama isteyen müşteriler için `Sites.Selected` modu değerlendirilir (doğrulanmadı).

## Sonuçlar

### Olumlu

- Kasım 2026 kullanımdan kaldırmasından etkilenmez.
- Kullanıcı seçimi KVKK amaçla sınırlılık ve ölçülülük ilkesini destekler.

### Olumsuz

- Kullanıcıya paylaşılan ama seçilmeyen belgeler kaçabilir; arama tabanlı keşif bunu kısmen telafi eder.
- `/search/query` limitleri Outlook kovasını paylaşır; ayrı sınırları yayımlanmamış.

## Doğrulama / açık noktalar

- S3 teslimatı: seçili SharePoint/OneDrive sürücülerinde delta + followedSites + `/search/query` ile keşif.
- Sürücü sayısı arttığında kiracı başına RU bütçesi izlenir (100 PC × 10 sürücü × 5 dk'da bir ≈ 200 RU/dk; notlardaki çıkarım).

## Kaynaklar

- [Araştırma raporu §1 — Microsoft 365 entegrasyonu](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Graph entegrasyonu notları §3, §6](../research/notes/graph_entegrasyonu.md)
- [M365 entegrasyonu](../architecture/m365-integration.md)
