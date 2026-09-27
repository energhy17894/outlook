# ADR-0021: Gözlemlenebilirlik — OTel + Serilog JSON + Event Log; içerik içermeyen loglar; tanı paketi

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, DevOps mühendisi, güvenlik mühendisi
- **İlgili ADR'ler:** [0001](0001-modular-monolith-two-services.md), [0018](0018-hash-chained-audit-log.md)

## Bağlam

- .NET Windows servis şablonu `EventLogLoggerProvider` kullanır; varsayılan Event Log düzeyi Warning'dir ([MS Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)). Yalnızca yöneticiler Event Log kaynağı oluşturabilir; kaynak MSI'da `util:EventSource` ile oluşturulur.
- Loglar KVKK kapsamında kişisel veri taşıyabilir; güvenlik notları loglara yalnızca ID, hash, sayım, süre, politika kararı ve hata yazılmasını; mesaj gövdesi, prompt veya model çıktısı yazılmamasını önerir ([güvenlik notları §6](../research/notes/guvenlik_uyum.md)).
- OpenTelemetry GenAI semantik kuralları hâlâ "Development" durumundadır; Langfuse self-hosting (Postgres + ClickHouse + Redis + S3) her masaüstü kurulumu için çok ağırdır ([AI mimarisi §10](../research/notes/ai_agent_mimarisi.md)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **OTel metrik/iz + Serilog JSON dosya + Event Log (Warning+) + tanı paketi (seçilen)** | Yerel, hafif, opsiyonel OTLP | Merkezi görünüm için müşteri toplayıcısı gerekir |
| Langfuse/Phoenix yerel | Zengin LLM izleme | Ağır bağımlılıklar |
| Yalnızca Event Log | Basit | Yapılandırılmış sorgu yok |

## Karar

- **Loglar:** `%ProgramData%\OpsIntel\logs\` altında dönen yapılandırılmış JSON (Serilog); boyut ve süre sınırı. **İçerik içermez**: e-posta gövdesi, konu, prompt, model çıktısı yazılmaz; e-posta adresleri ve konular hash'lenir. SDK'lardaki ayrıntılı LLM istek loglaması kapatılır.
- **Event Log:** Warning ve üzeri; kaynak MSI tarafından oluşturulur.
- **Metrikler/izler:** OpenTelemetry (ingestion gecikmesi, delta tur süresi, 429 sayısı, kuyruk derinliği, LLM token/sn, GPU/NPU durumu); dahili yönetici sayfasında gösterilir; OTLP dışa aktarımı opsiyonel.
- **Sağlık:** `/health/live` (süreç) ve `/health/ready` (DB yazılabilir, disk alanı, sertifika geçerliliği, Graph token, AI çalışma zamanı).
- **Tanı paketi:** Yalnızca yönetici; son loglar, redakte edilmiş yapılandırma, sürümler, health çıktısı, DB bütünlük kontrolü.
- **Çökme kurtarma:** SCM recovery (restart/restart/none) + ölümcül hatada `Environment.Exit(1)`.
- Güvenlik olayları (kimlik doğrulama hataları, Host/Origin retleri, politika engelleri) SIEM'e iletilebilir biçimde loglanır.

## Sonuçlar

### Olumlu

- Log dosyaları KVKK açısından düşük riskli; destek için paylaşılabilir.
- Pilotta sorun giderme için yeterli sinyal.

### Olumsuz

- İçeriksiz loglarla bazı çıkarım hatalarının ayıklanması zorlaşır; yalnızca yönetici tarafından açılan, süreli "ayrıntılı tanı" modu gerekebilir (kaynaklarda öneri olarak geçer).

## Doğrulama / açık noktalar

- S2 kabul kriteri: log taramasında gövde metni bulunmaz.
- S8 teslimatı: tanı paketi, OTel yönetici sayfası.
- WER LocalDumps yapılandırması doğrulanmadı.

## Kaynaklar

- [Araştırma raporu §1 — Bileşen listesi (Gözlemlenebilirlik)](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §7](../research/notes/teknoloji_yigini.md)
- [Sorun giderme](../operations/troubleshooting.md)
