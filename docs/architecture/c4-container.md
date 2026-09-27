# C4 Konteyner Diyagramı (Seviye 2)

*Kaynak: [araştırma raporu §1](../research/rapor-m365-operasyon-zekasi-platform-plani.md) mimari diyagramı ve bileşen listesi. Bir üst seviye: [c4-context.md](c4-context.md).*

```mermaid
flowchart LR
  subgraph PC["Windows 11 23H2+ / Windows Server 2022+"]
    subgraph BR["Tarayıcı (Edge / Chrome / Firefox)"]
      SPA["Konteyner: SPA<br/>React + Fluent UI v9<br/>wwwroot statik dosyalar"]
    end
    subgraph HOST["Konteyner: OpsIntel.Host<br/>Windows Service · NT SERVICE\OpsIntel.Host"]
      KES["Kestrel HTTPS 127.0.0.1 / ::1 : 6500<br/>Host/Origin izin listesi · CSRF · katı CSP"]
      API["REST API + SSE<br/>BFF çerez oturumu"]
      AUTH["Kimlik: MSAL.NET public client + PKCE<br/>DPAPI token kasası · CAE"]
      GC["Graph konektörleri<br/>delta polling · posta kutusu başına en fazla 4 eşzamanlı · Retry-After"]
      WF["Onay iş akışı + yürütücü<br/>uç nokta izin listesi · Mail.Send YOK"]
      SCH["Quartz.NET zamanlayıcı<br/>iş/outbox dağıtıcı"]
      AN["Analitik okuma modelleri<br/>bildirimler · brifing"]
      AUD["Denetim kaydı<br/>hash zincirli append-only"]
    end
    subgraph AIS["Konteyner: OpsIntel.Intelligence<br/>Windows Service · NT SERVICE\OpsIntel.AI · Graph token YOK"]
      PIPE["Deterministik boru hattı<br/>normalize → triage → çıkarım → doğrulama → öneri"]
      POL["Politika kapısı<br/>etiket · özel nitelikli veri · hariç tutma · DLP"]
      FL["Foundry Local (süreç içi)<br/>LLM + embedding · WinML GPU/NPU/CPU"]
      QA["Soru-Cevap ajanı (MAF 1.x)<br/>yalnız salt-okunur araçlar"]
    end
    PAR["Konteyner: OpsIntel.Parser alt süreci<br/>düşük yetki · Job Object<br/>PDF / DOCX / XLSX / PPTX"]
    subgraph DATA["%ProgramData%\OpsIntel (ACL: servis SID'leri + Administrators)"]
      DB[("Veritabanı: SQLite WAL (şifreli)<br/>alan tabloları · FTS5 trigram · vektör<br/>iş/outbox · audit_event")]
      BLOB[("Dosya deposu: içerik-adresli blob<br/>.eml · ekler · belgeler")]
      MOD[("Model önbelleği<br/>sabitlenmiş sürüm + hash")]
    end
    TSK["Zamanlanmış görev (SYSTEM)<br/>setup-helper cert renew"]
  end
  subgraph M365["Microsoft 365 kiracısı"]
    ENTRA["Entra ID<br/>OIDC · Conditional Access · CAE"]
    GRAPH["Microsoft Graph v1.0"]
  end
  CLOUD["Opsiyonel bulut LLM (Katman 2/3)<br/>yalnız KVKK SS-2 + politika onayıyla"]

  SPA <--> KES
  KES --> API
  API --> AUTH
  AUTH <--> ENTRA
  GC <--> GRAPH
  WF -->|"yalnız onaylı öneriler"| GRAPH
  HOST <--> DB
  AIS <--> DB
  GC --> BLOB
  PIPE --> POL
  PIPE --> PAR
  PAR --> BLOB
  FL --- MOD
  AIS -.->|"politika izin verirse"| CLOUD
  TSK -.-> KES
```

## Konteyner sorumlulukları

| Konteyner | Hesap | Tuttuğu sırlar | Güven düzeyi |
|---|---|---|---|
| SPA | Kullanıcının tarayıcısı | Yalnızca oturum çerezi (`HttpOnly`) | Güvenilir kod, güvenilmeyen içerik gösterir |
| `OpsIntel.Host` | `NT SERVICE\OpsIntel.Host` | Graph refresh token (DPAPI), sertifika özel anahtarı | Yetkili süreç; model çıktısını talimat olarak işlemez |
| `OpsIntel.Intelligence` | `NT SERVICE\OpsIntel.AI` | Graph token yok; bulut katmanı açıksa sağlayıcı kimliği | Güvenilmeyen içeriği okur; araçsız çıkarım |
| `OpsIntel.Parser` | Düşük yetkili alt süreç, Job Object | Yok | Güvenilmeyen girdiyi ayrıştırır; çökebilir |
| SetupHelper / zamanlanmış görev | SYSTEM (yalnızca kurulum ve sertifika yenileme) | Yok (sertifika deposuna yazar) | İmzalı yardımcı |

## Süreçler arası iletişim

| Kanal | Kullanım | Durum |
|---|---|---|
| SQLite iş/outbox tablosu | Kalıcı iş devri (Host ↔ Intelligence) | Karar: [ADR-0012](../adr/0012-db-job-outbox-quartz.md) |
| Named pipe (servis SID ACL'li) | Düşük gecikmeli uyandırma; Faz 2'de tray yardımcısından token köprüsü | Faz 0'da doğrulanacak |
| Alt süreç stdin/stdout veya yerel kanal | Intelligence → Parser | Ayrıntı Faz 0'da belirlenecek (kaynaklarda tanımlı değil) |

İlgili: [overview.md](overview.md), [threat-model.md](threat-model.md), [ADR-0001](../adr/0001-modular-monolith-two-services.md).
