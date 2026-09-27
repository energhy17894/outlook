# Mimari Karar Kayıtları (ADR)

Bu dizin OpsIntel'in mimari açıdan önemli kararlarını tutar. Kararlar [araştırma raporunun §5 ADR listesinden](../research/rapor-m365-operasyon-zekasi-platform-plani.md) türetilmiştir; tümü **2026-09-27** tarihlidir.

## Süreç ve şablon

- Şablon: [template.md](template.md). Biçim [MADR](https://adr.github.io/madr/) tarzının Türkçe uyarlamasıdır: Başlık, Durum, Tarih, Bağlam, Karar etkenleri, Değerlendirilen alternatifler, Karar, Sonuçlar (olumlu/olumsuz), Doğrulama/açık noktalar, Kaynaklar.
- Dosya adı: `NNNN-ingilizce-kebab-case-baslik.md` (ASCII). İçerik Türkçedir.
- **Durumlar:**
  - **Önerildi** — karar yönü belirlendi ama Faz 0 spike'ı veya başka bir doğrulama bekleniyor.
  - **Kabul edildi** — karar geçerli. Kabul edilmiş ADR'ler değiştirilmez; karar değişirse yeni ADR yazılır.
  - **Reddedildi**, **Kullanımdan kalktı**, **Yerine geçti: ADR-NNNN**.
- Faz 0 spike'ları (A–E) sonuçlandığında ilgili "Önerildi" ADR'ler kabul edilir veya yerine yeni ADR yazılır. Katkı süreci: [CONTRIBUTING.md](../../CONTRIBUTING.md).

## ADR dizini

| No | Karar | Durum | Doğrulama / not |
|---|---|---|---|
| [0001](0001-modular-monolith-two-services.md) | Modüler monolit; iki Windows servisi (Host, Intelligence) ve sandbox parser alt süreci | Kabul edildi | Named pipe uyandırma Faz 0'da doğrulanacak |
| [0002](0002-dotnet-10-lts-self-contained.md) | .NET 10 LTS, self-contained x64; Arm64 ikincil | Kabul edildi | — |
| [0003](0003-kestrel-loopback-https-6500.md) | Kestrel HTTPS, 127.0.0.1/::1:6500, Host/Origin izin listesi; HTTP.sys yok | Kabul edildi | S8 localhost pen testi |
| [0004](0004-https-certificate-strategy.md) | Sertifika: kurumsal PKI öncelikli; yoksa makineye özel CA=false uç sertifika + SYSTEM görevle yenileme; ortak kök CA yok | Önerildi | Faz 0 tarayıcı testleri (spike A) |
| [0005](0005-single-msi-wix-v7.md) | Kurulum: WiX v7 SDK ile tek MSI (sıfır ön gereksinim) + opsiyonel Burn `Setup.exe`; MSIX yok | Önerildi | Faz 0 walking skeleton (spike A) |
| [0006](0006-code-signing.md) | Kod imzalama: bulut HSM'de OV veya kurum AD CS; Artifact Signing Türkiye'de uygun değil | Önerildi | Satın alma Faz 0'da başlar |
| [0007](0007-delegated-auth-bff-pkce.md) | Kimlik: Entra public client, Host'ta PKCE'li BFF, DPAPI token kasası, CAE; WAM tray yardımcısı Faz 2 | Önerildi | **Faz 0 spike B: kritik** |
| [0008](0008-delegated-graph-no-mail-send.md) | Graph erişimi yalnızca delegated; PC'de app-only yok; Mail.Send hiçbir zaman yok | Kabul edildi | — |
| [0009](0009-delta-polling-change-detection.md) | Değişiklik tespiti: delta polling; webhook yok; Event Hubs kurumsal opsiyon | Kabul edildi | — |
| [0010](0010-sqlite-fts5-vector-blob-storage.md) | Veri: SQLite WAL + FTS5 trigram + `IVectorIndex` + içerik-adresli blob; SQL Server 2025 ekip sürümü alternatifi | Önerildi | sqlite-vec yükleme testi (spike D) |
| [0011](0011-encryption-at-rest.md) | Durağan veri: BitLocker ön kontrolü + SQLite3MC/SQLCipher + DPAPI anahtar sarma | Önerildi | Paketleme spike'ı (spike D) |
| [0012](0012-db-job-outbox-quartz.md) | İşler: DB iş/outbox + Channels + Quartz.NET; Temporal/Dapr/Hangfire yok | Kabul edildi | — |
| [0013](0013-microsoft-extensions-ai-maf.md) | AI soyutlama: Microsoft.Extensions.AI; MAF 1.x yalnızca Soru-Cevap; yeni kodda Semantic Kernel yok | Kabul edildi | — |
| [0014](0014-foundry-local-model-hosting.md) | Model barındırma: Foundry Local süreç içi varsayılan; katman 2/3 politika ve KVKK kontrol listesiyle | Önerildi | Servis içi çalışma spike'ı (spike C) |
| [0015](0015-extraction-contract-evidence.md) | Çıkarım sözleşmesi: küçük düz şemalar, zorunlu alıntı kanıtı, deterministik doğrulama, "needs review" durumu | Kabul edildi | Spike E ölçümleriyle kalibre edilecek |
| [0016](0016-project-registry-phase-fsm.md) | Proje/aşama: kayıt defterine karşı sınıflandırma + yapılandırılabilir aşama FSM'i; kümeleme yalnızca öneri üretir | Kabul edildi | — |
| [0017](0017-approval-state-machine.md) | Onay iş akışı kendi durum makinemizde; MAF checkpoint'leri kaynak-gerçek değil | Kabul edildi | — |
| [0018](0018-hash-chained-audit-log.md) | Denetim: hash zincirli append-only `audit_event` + periyodik imzalı özet | Kabul edildi | — |
| [0019](0019-untrusted-content-quarantine.md) | Güvenilmeyen içerik karantinası, spotlighting, katı CSP, egress izin listesi | Kabul edildi | Kırmızı takım korpusu CI'da |
| [0020](0020-react-fluent-ui-spa.md) | UI: React/Vite/TS + Fluent UI v9 + ECharts/React Flow/vis-timeline/AG Grid Community + SSE | Kabul edildi | — |
| [0021](0021-observability.md) | Gözlemlenebilirlik: OTel + Serilog JSON + Event Log; içerik içermeyen loglar; tanı paketi | Kabul edildi | — |
| [0022](0022-msi-major-upgrade-updates.md) | Güncelleme: MSI major upgrade (Intune/winget/SCCM); başlangıçta yedekle ve şema geçişi yap | Kabul edildi | — |
| [0023](0023-no-individual-performance-scoring.md) | Kişi bazlı performans/duygu puanlaması yok; organizasyon metriklerinde en az 5 kişilik grup | Kabul edildi | Hukuk danışmanı onayı bekleniyor |
| [0024](0024-os-support-matrix.md) | OS desteği: Win11 23H2+, Server 2022/2025; Win10 22H2 en iyi çaba | Kabul edildi | — |
| [0025](0025-shared-content-discovery.md) | Paylaşılan içerik keşfi: kullanıcı seçimli siteler + followedSites + `/search/query`; `sharedWithMe` kullanılmaz | Kabul edildi | — |

## Faz 0 spike → ADR eşlemesi

| Spike | İlgili ADR'ler |
|---|---|
| A — WiX v7 walking skeleton | [0004](0004-https-certificate-strategy.md), [0005](0005-single-msi-wix-v7.md), [0024](0024-os-support-matrix.md) |
| B — Servis tarafı PKCE + DPAPI + CAE | [0007](0007-delegated-auth-bff-pkce.md) |
| C — Foundry Local servis içinde | [0014](0014-foundry-local-model-hosting.md), [0005](0005-single-msi-wix-v7.md) |
| D — vec0 + SQLite3MC | [0010](0010-sqlite-fts5-vector-blob-storage.md), [0011](0011-encryption-at-rest.md) |
| E — Türkçe çıkarım ön ölçümü | [0014](0014-foundry-local-model-hosting.md), [0015](0015-extraction-contract-evidence.md) |
