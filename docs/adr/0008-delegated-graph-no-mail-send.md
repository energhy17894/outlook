# ADR-0008: Graph erişimi yalnızca delegated; PC'de app-only yok; Mail.Send hiçbir zaman yok

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, güvenlik mühendisi, KVKK danışmanı
- **İlgili ADR'ler:** [0007](0007-delegated-auth-bff-pkce.md), [0017](0017-approval-state-machine.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

- Application (app-only) izinleri her zaman yönetici onayı ister ve **tüm posta kutularına** uygulanır ([Permissions reference](https://learn.microsoft.com/en-us/graph/permissions-reference)). Bir son kullanıcı PC'sinde kiracı genelinde Mail.Read yetkili sertifika anahtarı büyük bir etki alanıdır.
- Delegated erişimle uygulama yalnızca kullanıcının zaten görebildiğini görür; bu, "çıktılara erişim, kullanıcının kaynağa erişimiyle sınırlı" ilkesini doğal olarak sağlar ([güvenlik notları §2, §5](../research/notes/guvenlik_uyum.md)).
- `Mail.ReadWrite` "Does not include permission to send mail". Katılımcılı etkinlik oluşturmak davetleri anında gönderir ve bu yapılandırılamaz ([Create event](https://learn.microsoft.com/en-us/graph/api/user-post-events?view=graph-rest-1.0)).
- Yönetilen onay politikası Mail.*, Calendars.*, Files.Read.All, Sites.Read.All, Tasks.* için kullanıcı onayını engeller; pratikte yönetici onayı gerekir.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Yalnızca delegated, Mail.Send yok (seçilen)** | En az yetki; gönderme engeli token düzeyinde | Her kullanıcı oturum açmalı; yönetici onayı gerekir |
| PC'de app-only | Kullanıcı etkileşimi yok | Kiracı genelinde erişim; anahtar PC'de |
| Delegated + Mail.Send (onaylı gönderim) | Tek tıkla gönderim | Ele geçirilirse gönderebilir; aşırı yetki (LLM06/ASI02) |

## Karar

- Microsoft Graph erişimi **yalnızca delegated** kapsamlarla yapılır.
- **MVP (salt okunur):** `User.Read offline_access Mail.Read Calendars.Read Files.Read.All Sites.Read.All`.
- **Faz 2 (artımlı onayla):** `Mail.ReadWrite` (taslaklar, kategoriler), `Tasks.ReadWrite`; `Calendars.ReadWrite` yalnızca katılımcısız "hold" kayıtları için.
- **`Mail.Send` hiçbir fazda istenmez.** Gönderimi her zaman kullanıcı Outlook'tan yapar.
- PC'lere app-only izin verilmez. Merkezi bir mod gerekirse tek bir yönetilen sunucuda çalışır ve Exchange RBAC for Applications ile kapsamlandırılır.
- Kurulum sihirbazı admin-consent bağlantısı sunar; yayıncı doğrulaması tamamlanır.

## Sonuçlar

### Olumlu

- "Gönderimi insan yapar" garantisi token düzeyinde zorunludur; uygulama ele geçirilse bile e-posta gönderemez.
- Kullanıcı başka birinin postasını OpsIntel üzerinden göremez.

### Olumsuz

- Ortak/proje posta kutuları için delegated erişim ve paylaşımlı kapsamlar (`Mail.Read.Shared`) ayrıca değerlendirilmelidir (kaynaklarda ayrıntılandırılmadı).
- Yönetici onayı alınamaması bir benimseme riskidir (risk kaydı: Yüksek/Orta).

## Doğrulama / açık noktalar

- CI'da mimari/sözleşme testi: istenen kapsam listesinde `Mail.Send` bulunmadığı doğrulanır (Faz 0'da eklenecek).
- `Sites.Selected` modunun uygulanabilirliği doğrulanmadı.

## Kaynaklar

- [Araştırma raporu §4 — M365 izinleri](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Graph entegrasyonu notları §5, §8](../research/notes/graph_entegrasyonu.md)
- [App consent policies](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/manage-app-consent-policies), [RBAC for Applications](https://learn.microsoft.com/en-us/exchange/permissions-exo/application-rbac)
- [M365 entegrasyonu](../architecture/m365-integration.md)
