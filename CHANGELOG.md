# Değişiklik Günlüğü

Bu projedeki tüm önemli değişiklikler bu dosyada belgelenir.

Biçim [Keep a Changelog](https://keepachangelog.com/tr-TR/1.1.0/) standardına dayanır; sürümleme [Semantic Versioning](https://semver.org/lang/tr/) kurallarını izleyecektir. MSI sürüm numarasının yalnızca ilk üç alanı anlamlıdır (MSI dördüncü alanı yok sayar; bkz. [ADR-0022](docs/adr/0022-msi-major-upgrade-updates.md)).

## [Yayımlanmamış]

## [0.0.0-faz0-iskelet] - 2026-09-27

### Eklendi

- **Faz 0 Walking Skeleton (5–23 Ekim 2026):**
  - `.NET 10 modüler monolit` yapısı: `OpsIntel.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`.
  - **OpsIntel.Host:** Kestrel HTTPS (127.0.0.1/::1:6500), SPA sunumu (React+Vite+Fluent UI v9), BFF kimliği (MSAL PKCE, DPAPI token kasası), Health endpoint, SSE stub, /auth/* PKCE uç noktaları.
  - **OpsIntel.Intelligence:** Temel iskeleti; Graph token taşımaz (spike B PKCE doğrulaması).
  - **OpsIntel.SetupHelper:** Sertifika yönetimi (create/trust/renew/remove), port kontrol, veritabanı yedekleme.
  - **OpsIntel.Contracts:** DTO ve çıkarım sözleşme şemaları.
  - **Modüller:**
    - `OpsIntel.Connectors.Graph`: Mail delta, takvim, sürücü; MSAL ICustomWebUi; DPAPI cache; 4-concurrency throttle; DraftWriter (gönderme yok); `SPIKE-AUTH.md` belgeleri.
    - `OpsIntel.AI.Extraction`: Şemalar, Foundry Local 2.0.1 fabrika, Türkçe alıntı doğrulayıcısı, WorkItemExtractor; `SPIKE-FOUNDRY-LOCAL.md` belgeleri.
    - `OpsIntel.Normalization`: İş parçacığı yeniden oluşturma, dil sınırı.
  - **Platform:**
    - `Abstractions`: IBlobStore, IJobQueue, IVectorIndex, IChangeFeed, ISecretStore, ICertificateProvider, ISearchIndex.
    - `Windows`: DPAPI, sertifika mağazası, EventLog, SCM.
    - `Persistence.Sqlite`: EF Core, hash zincirli denetim, SqliteJobQueue, FTS5, vektör indeksi.
    - `Observability`: OTel, Serilog, redaksiyon, tanı paketi.
  - **React web SPA:** `src/web` (React+Vite+TypeScript+Fluent UI v9, pnpm).
  - **WiX v7 Installer:** OpsIntel.Installer.wixproj, `.wxs` dosyaları (Package, Services, Folders, Config, Cert, Firewall, Shortcut), Burn bundle iskeleti, Intune ve winget manifestleri.
  - **Tests:** 112 geçen test (42 AI, 25 Graph, 25 SetupHelper, 17 unit, 3 mimari); Python eval framework (28 test).
  - **.github/workflows:** `ci.yml`, `installer.yml`, `codeql.yml` (otomatik).
  - **Dokümanlar:** `SPIKE-AUTH.md`, `SPIKE-FOUNDRY-LOCAL.md`.
  - **Hızlı başlangıç:** `dotnet build OpsIntel.slnx`, `dotnet test OpsIntel.slnx`, `cd src/web && pnpm install && pnpm dev`, `pwsh tools/build.ps1`.

### Doğrulanmadı (Faz 1 ile tamamlanacak)

- NT SERVICE hesapları ve özel anahtar ACL'leri.
- Foundry Local Windows Service modu (spike C).
- Gerçek Entra tenant'ı, Token Protection ve CAE.
- MSI kurulum matrisi testleri (Pester).

## [0.0.0-planlama] - 2026-09-27

### Planlama

- Araştırma raporu ve araştırma notları eklendi (`docs/research/`).
- Depo yapısı oluşturuldu: README, katkı ve güvenlik politikaları, `.gitignore`, `.editorconfig`.
- Mimari dokümanlar eklendi: genel bakış, C4 bağlam/konteyner diyagramları, veri modeli, tehdit modeli, M365 entegrasyonu.
- ADR 0001–0025 eklendi (MADR tarzı, Türkçe); doğrulaması Faz 0'a bağlı olanlar "Önerildi" durumunda.
- Yol haritası, MVP kapsamı, kalite hedefleri ve risk kaydı eklendi.
- KVKK ve AB AI Act uyum dokümanları ile hukuk onayı bekleyen taslak belgeler eklendi.
- Kurulum, Intune ve sorun giderme işletim dokümanları eklendi.
- Ürün dokümanları eklendi: özellik kataloğu, rakip analizi, ek geliştirme önerileri, personalar.
- GitHub PR ve issue şablonları (özellik, hata, spike, ADR) eklendi.
- Faz 0'da kod alacak dizinler için yer tutucu README'ler eklendi (`src/`, `installer/`, `tests/`, `prompts/`, `models/`, `tools/`).

[Yayımlanmamış]: #
[0.0.0-planlama]: #
