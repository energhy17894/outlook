# ADR-0010: Veri — SQLite WAL + FTS5 trigram + IVectorIndex + içerik-adresli blob; SQL Server 2025 ekip sürümü alternatifi

- **Durum:** Önerildi (Faz 0 spike D: `vec0.dll` LoadExtension testi bekleniyor)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, backend geliştiriciler
- **İlgili ADR'ler:** [0011](0011-encryption-at-rest.md), [0012](0012-db-job-outbox-quartz.md), [0018](0018-hash-chained-audit-log.md)

## Bağlam

Tek PC için sistem kaydı, tam metin arama, vektör arama, iş kuyruğu ve denetim kaydı tek MSI ile kurulabilir bir depolamada tutulmalıdır.

- **SQL Server 2025 Express:** 50 GB veritabanı sınırı, 1.410 MB buffer pool; DiskANN vektör indeksi hâlâ preview; ayrı yükleyici olduğu için tek MSI kısıtını bozar ([SQL Server 2025 sürümleri](https://learn.microsoft.com/en-us/sql/sql-server/editions-and-components-of-sql-server-2025?view=sql-server-ver17)).
- **PostgreSQL + pgvector:** Windows'ta pgvector Visual Studio araçlarıyla kaynaktan derlenmeli ([pgvector](https://github.com/pgvector/pgvector)).
- **LocalDB:** kullanıcı başına; SYSTEM altında yönetim sorunları.
- **SQLite vektör olgunluğu:** `sqlite-vec` "pre-v1, so expect breaking changes" ([sqlite-vec](https://github.com/asg017/sqlite-vec)); resmi Vec1 henüz yayımlanmadı; sqlite-vec için resmi .NET NuGet paketi yok.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **SQLite WAL + FTS5 + IVectorIndex + blob (seçilen)** | Gömülü, sıfır kurulum, tek dosya hibrit arama | Vektör eklentisi olgunlaşmamış |
| SQL Server 2025 Express | Vector, FTS, Ledger, Azure SQL ile aynı motor | Ayrı yükleyici, 50 GB, DiskANN preview |
| PostgreSQL + pgvector | Güçlü | Windows'ta derleme, ek servis |

## Karar

- Sistem kaydı **SQLite (WAL)**; EF Core OLTP varlıkları için, FTS5/vektör için ham SQL.
- Tam metin: **FTS5 trigram** tokenizer (Türkçe gövdeleyici eksikliğini dolaylı telafi eder).
- Vektör işlemleri **`IVectorIndex`** soyutlamasının arkasındadır; yerel ölçekte kaba kuvvet arama kabul edilebilir (600 bin parça, int8 ≈ 0,6 GB); sqlite-vec, Vec1 veya süreç içi .NET indeksi değiştirilebilir.
- Hibrit arama: FTS5 + vektör sonuçları **Reciprocal Rank Fusion** ile birleştirilir ([Simon Willison](https://simonwillison.net/2024/Oct/4/hybrid-full-text-search-and-vector-search-with-sqlite/)).
- Ham `.eml`/MIME, ekler ve belge anlık görüntüleri `%ProgramData%\OpsIntel\blobs\sha256\..` altında **içerik-adresli** ve tekilleştirilmiş tutulur.
- İlişkiler ilişkisel kenar tablosu + özyinelemeli CTE ile; ayrı graf veritabanı yok.
- **SQL Server 2025**, aynı soyutlama sınırları üzerinden ekip/sunucu sürümü (Faz 4) alternatifidir.

## Sonuçlar

### Olumlu

- Ek sunucu yok; yedekleme `VACUUM INTO` + değişmez blob dosyaları.
- Veritabanı küçük kalır (blob'lar diskte).

### Olumsuz

- sqlite-vec kırıcı değişiklik riski (risk kaydı: Orta/Orta) — sürüm sabitleme ve kaba kuvvet yedeği.
- Tek yazar kuyruğu gerekir; iki servis aynı veritabanına yazar (WAL eşzamanlılık davranışı Faz 0'da ölçülmeli).

## Doğrulama / açık noktalar

- **Spike D:** `vec0.dll` LoadExtension + SQLite3MC şifrelemesinin birlikte çalışması ([ADR-0011](0011-encryption-at-rest.md)).
- S4 kabul kriteri: 100 bin parçada arama p95 < 1 sn.

## Kaynaklar

- [Araştırma raporu §1 — Veri katmanı](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §6](../research/notes/teknoloji_yigini.md), [AI mimarisi notları §4](../research/notes/ai_agent_mimarisi.md)
- [SQLite forum: Vec1](https://sqlite.org/forum/info/c9d69d74c6644dd19614851e46e2bd29615b922407fdb730529a755e2630d652)
- [Veri modeli](../architecture/data-model.md)
