# MVP Kapsamı

*Kaynak: [araştırma raporu §6 — MVP kapsamı](../research/rapor-m365-operasyon-zekasi-platform-plani.md). MVP = Faz 1 (26 Ekim 2026 – 19 Şubat 2027), çıktı `1.0.0-pilot`.*

MVP'nin tanımı: **salt okunur, kanıtlı operasyon kaydı; yerel onaylı aksiyon taslakları.** M365'e yazma yoktur. Kapsam kaymasına karşı bu tablo katıdır; kapsam dışı bir öğe ancak ürün sahibi kararı ve yol haritası güncellemesiyle MVP'ye alınabilir (feature flag'ler, sprint başına kabul kriterleri).

## Kapsam içi / kapsam dışı

| Kapsam içi (MVP) | Kapsam dışı (sonraki fazlar) |
|---|---|
| Tek imzalı MSI; iki servis; `https://localhost:6500`; ilk çalıştırma sihirbazı; yükseltme ve kaldırma | Burn bundle, Intune paket otomasyonu, winget (Faz 2) |
| Entra oturumu, admin consent; salt okunur Graph kapsamları | M365'e yazma: Outlook taslakları, To Do/Planner, takvim hold (Faz 2) |
| Posta (Gelen, Gönderilen, seçili klasörler) ile seçili SharePoint/OneDrive kitaplıklarının delta senkronizasyonu | Teams transkriptleri, Event Hubs gerçek zaman modu (Faz 2–3) |
| Başlık yeniden kurma, TR/EN temizleme, ek ayrıştırma | Docling/Python ile gelişmiş tablo ayrıştırma (Faz 2 opsiyonu) |
| Proje otomatik tespiti (öneri + onay), aşama çıkarımı ve kapı kartları, sağlık sürücüleri | GraphRAG ile küresel Soru-Cevap, süreç haritaları, OCEL dışa aktarımı (Faz 3) |
| Kanıtlı karar, risk, açık soru, taahhüt ve talep çıkarımı; inceleme kuyruğu | Sözleşme yükümlülükleri (CUAD), LoRA ince ayar (Faz 3) |
| İşlerim / Beklediklerim; olay akışı ve zaman çizelgesi; temel portföy panosu | Organizasyon 360, brifing, toplantı hazırlığı, Windows bildirimleri (Faz 2) |
| Yerel aksiyon taslakları, onay ve denetim; dışa aktarım | Çok adımlı onaylayıcılar, otonomi seviyeleri (Faz 2) |
| Yerel LLM varsayılan; sağlayıcı soyutlaması hazır; bulut katmanı kapalı ve politikayla kilitli | Bulut katman 2 ile Purview `processContent`/`contentActivities` (Faz 2) |
| PC başına bir birincil kullanıcı, yalnızca localhost | LAN modu, çok kullanıcı, kurumsal PKI (Faz 2) |

## MVP'nin koşulsuz ilkeleri

Bu ilkeler kapsam tartışmasına konu değildir:

- `Mail.Send` istenmez ([ADR-0008](../adr/0008-delegated-graph-no-mail-send.md)).
- Doğrulanamayan öğe "gerçek" olarak gösterilmez ([ADR-0015](../adr/0015-extraction-contract-evidence.md)).
- Kişi bazlı performans/duygu puanı yoktur ([ADR-0023](../adr/0023-no-individual-performance-scoring.md)).
- İçerik içermeyen loglar ([ADR-0021](../adr/0021-observability.md)).
- İmzalı aydınlatma ve BT/AI kullanım politikası olmadan canlıya geçiş yoktur ([KVKK](../compliance/kvkk/README.md)).

## Faz 0 sonucuna bağlı kapsam değişiklikleri

| Spike sonucu | MVP'ye etkisi |
|---|---|
| B başarısız | Tray/WAM yardımcısı (normalde Faz 2) MVP'ye çekilir |
| C başarısız | Ollama standalone + WinSW; Burn bundle ihtiyacı doğabilir |
| E: CPU'lu makinelerde kalite yetersiz | Pilot GPU'lu iş istasyonlarıyla yapılır veya CPU makinelerinde kapsam daraltılır; bulut katmanı yalnızca KVKK paketi sonrası |

Ayrıntılı özellik tanımları: [features.md](../product/features.md).
