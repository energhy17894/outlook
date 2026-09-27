# src/

> **Durum:** Faz 0 iskeleti — .NET 10 walking skeleton eklendi (5–23 Ekim 2026).

Bu dizin OpsIntel'in .NET 10 modüler monolitini ve React SPA'sını barındırır. Skeleton validasyondadır; servis modu, gerçek Entra tenant'ı ve Outlook veri erişimi Faz 1'de tamamlanacaktır. Mimari: [docs/architecture/overview.md](../docs/architecture/overview.md), [ADR-0001](../docs/adr/0001-modular-monolith-two-services.md). Kaynak: [araştırma raporu §5](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md).

## Planlanan yapı

```text
src/
├─ OpsIntel.Host/            # Program.cs, AddWindowsService, Kestrel, BFF auth, API, SSE
├─ OpsIntel.Intelligence/    # AI servisi host'u
├─ OpsIntel.Parser/          # sandbox alt süreç
├─ OpsIntel.SetupHelper/     # cert create|trust|renew|remove, port check, db backup
├─ Modules/
│  ├─ OpsIntel.Connectors.Graph/     # mail/drive/calendar delta, throttling, immutable IDs
│  ├─ OpsIntel.Evidence/             # blob store, hash, dedup, evidence anchors
│  ├─ OpsIntel.Normalization/        # thread rebuild, TR/EN stripper, chunking, lang-id
│  ├─ OpsIntel.Policy/               # etiket, özel nitelikli veri, hariç tutma, sağlayıcı katmanı
│  ├─ OpsIntel.AI.Extraction/        # şemalar, prompt sürümleri, doğrulama merdiveni
│  ├─ OpsIntel.AI.Agents/            # MAF Soru-Cevap ajanı, salt-okunur araçlar
│  ├─ OpsIntel.Knowledge/            # proje/aşama/WorkItem/kişi/org, edge'ler, arama
│  ├─ OpsIntel.Workflow.Approval/    # öneri durum makinesi, yürütücü, izin listesi
│  ├─ OpsIntel.Analytics/            # okuma modelleri, KPI, OCEL dışa aktarım
│  └─ OpsIntel.Notifications/        # SSE, brifing, (Faz 2) tray köprüsü
├─ Platform/
│  ├─ OpsIntel.Platform.Abstractions/ # IBlobStore, IJobQueue, IVectorIndex, IChangeFeed,
│  │                                  # ISecretStore, ICertificateProvider, ISearchIndex
│  ├─ OpsIntel.Platform.Windows/      # DPAPI, cert store, EventLog, SCM (net10.0-windows)
│  ├─ OpsIntel.Persistence.Sqlite/    # EF Core, migrations, FTS5, vektör, audit hash zinciri
│  └─ OpsIntel.Observability/         # OTel, Serilog, redaction, tanı paketi
├─ OpsIntel.Contracts/                # DTO'lar, JSON şemaları (çıkarım sözleşmeleri)
└─ web/                               # React + Vite + TS
   ├─ src/features/ {projects, my-work, review-queue, timeline, dashboards, ask, admin, setup}
   ├─ src/shared/ {api-client (OpenAPI'den üretilir), evidence, charts, i18n (tr/en)}
   └─ tests/ (Vitest)
```

## Sorumluluklar

| Proje | Sorumluluk | İlgili ADR |
|---|---|---|
| `OpsIntel.Host` | Windows Service (`NT SERVICE\OpsIntel.Host`): HTTPS 6500, SPA sunumu, REST+SSE, BFF oturumu, kimlik, Graph konektörleri, zamanlayıcı, onay/yürütücü, analitik, bildirim, denetim | [0003](../docs/adr/0003-kestrel-loopback-https-6500.md), [0007](../docs/adr/0007-delegated-auth-bff-pkce.md) |
| `OpsIntel.Intelligence` | Windows Service (`NT SERVICE\OpsIntel.AI`): normalizasyon, triage, çıkarım, doğrulama, embedding, Soru-Cevap; **Graph token tutmaz** | [0013](../docs/adr/0013-microsoft-extensions-ai-maf.md), [0014](../docs/adr/0014-foundry-local-model-hosting.md) |
| `OpsIntel.Parser` | Düşük yetkili, Job Object ile sınırlı belge ayrıştırma alt süreci | [0001](../docs/adr/0001-modular-monolith-two-services.md) |
| `OpsIntel.SetupHelper` | İmzalı konsol exe; MSI özel eylemleri ve SYSTEM zamanlanmış görevi için sertifika/port/yedek işlemleri | [0004](../docs/adr/0004-https-certificate-strategy.md) |
| `Modules/*` | İş modülleri; her modül kendi tablolarının sahibidir, modüller arası doğrudan tablo erişimi yoktur | [0001](../docs/adr/0001-modular-monolith-two-services.md) |
| `Platform/*` | Soyutlamalar ve Windows'a özgü uygulamalar; kalıcılık; gözlemlenebilirlik | [0010](../docs/adr/0010-sqlite-fts5-vector-blob-storage.md), [0021](../docs/adr/0021-observability.md) |
| `OpsIntel.Contracts` | DTO'lar ve çıkarım JSON şemaları | [0015](../docs/adr/0015-extraction-contract-evidence.md) |
| `web/` | React + Vite + TS SPA, Fluent UI v9 | [0020](../docs/adr/0020-react-fluent-ui-spa.md) |

## Kurallar

- Faz 0'da kökte `global.json`, `Directory.Build.props`, `Directory.Packages.props` (merkezi paket sürümü) ve `OpsIntel.slnx` eklenecektir.
- Modül sınırları `tests/architecture/` altındaki mimari testleriyle korunur.
- Loglara içerik yazılmaz; gerçek e-posta verisi depoya konmaz ([CONTRIBUTING.md](../CONTRIBUTING.md)).
