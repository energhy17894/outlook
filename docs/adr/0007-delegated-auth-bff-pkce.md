# ADR-0007: Kimlik — Entra public client, Host'ta PKCE'li BFF, DPAPI token kasası, CAE; WAM tray yardımcısı Faz 2

- **Durum:** Önerildi — **Faz 0 spike B kritik** (servis tarafı token edinimi CA ve Token Protection altında doğrulanmalı)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider / mimar, güvenlik mühendisi
- **İlgili ADR'ler:** [0003](0003-kestrel-loopback-https-6500.md), [0008](0008-delegated-graph-no-mail-send.md), [0011](0011-encryption-at-rest.md)

## Bağlam

Bu, mimarinin en ince noktasıdır. Graph'a arka planda erişecek süreç bir Windows servisidir, ancak:

- WAM servis bağlamında **tasarım gereği çalışmaz**: "Attempting to acquire tokens using WAM while running as a Windows service … will result in errors by design" ([MSAL.NET WAM](https://learn.microsoft.com/en-us/entra/msal/dotnet/acquiring-tokens/desktop-mobile/wam)).
- Device code akışı "high-risk" olarak nitelenir; Microsoft engellenmesini önerir ([CA: authentication flows](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-authentication-flows)).
- Refresh token varsayılan 90 gün yaşar ve her kullanımda yenilenir; CAE farkındalığı olan istemcide access token 28 saate kadar uzar ama istemci claims challenge işlemelidir.
- Token Protection'ın desteklediği uygulama listesi yalnızca Microsoft'un yerel uygulamalarıdır; "tüm uygulamalar" için zorlayan kiracılarda servis tarafı token engellenebilir.
- Entra localhost yönlendirme URI'lerinde portu eşleştirmede yok sayar.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Host'ta BFF: public client + PKCE + DPAPI önbellek (seçilen)** | Tarayıcı token görmez; secret yok; servis arka planda sessiz yenileyebilir | Servis bağlamında CA/Token Protection davranışı doğrulanmadı |
| Oturumdaki tray yardımcısı + WAM (yalnız) | Cihaza bağlı token, Token Protection uyumu olasılığı | Kullanıcı oturumu kapalıyken senkronizasyon durur; ek bileşen |
| Device code | Basit | Yüksek riskli; kurumlarda engellenir |
| App-only (sertifika) | Kullanıcı etkileşimi yok | Kiracı genelinde izin, PC'de büyük etki alanı ([ADR-0008](0008-delegated-graph-no-mail-send.md)) |

## Karar

- Uygulama Entra'da **secret taşımayan public client** olarak kaydedilir; PC'lere client secret dağıtılmaz.
- Host, tarayıcıdaki kullanıcıyı Entra'ya **PKCE'li yetkilendirme koduyla** yönlendirir ve `https://localhost:6500/signin-oidc` dönüşünde kodu token'a çevirir (MSAL.NET özel web-UI genişletme noktası bir yol; spike B'de doğrulanacak).
- Refresh token **DPAPI ile şifrelenmiş, servis SID'ine ACL'lenmiş** MSAL önbelleğinde tutulur; düz metin yedek modu reddedilir. Tarayıcı yalnızca çerez görür.
- CAE istemci yeteneği bildirilir ve claims challenge işlenir; token iptalinde arayüz yeniden oturum açma uyarısı gösterir.
- **Faz 2:** Kullanıcı oturumunda çalışan tray yardımcısı WAM ile cihaza bağlı token edinip ACL'li named pipe üzerinden Host'a verir; Windows bildirimlerini de üstlenir.
- Kiracı tercihine göre satıcıya ait çok kiracılı kayıt veya müşterinin kendi (tek kiracılı) uygulama kaydı desteklenir.

## Sonuçlar

### Olumlu

- Tarayıcıda token yok; XSS token çalamaz.
- Secret dağıtımı yok; kurulum sihirbazı admin-consent bağlantısı sunar.

### Olumsuz

- Token Protection zorlanan kiracılarda MVP engellenebilir (risk kaydında Orta/Yüksek).
- WAM ile edinilen token'ı servisle paylaşmak için Microsoft'un belgelediği bir desen bulunamadı.
- LAN modu her makine adı için ayrı yönlendirme URI kaydı ister.

## Doğrulama / açık noktalar

- **Spike B git/gitme:** servis tarafı public client PKCE + DPAPI cache + CAE claims challenge; uyumlu cihaz CA'sı ve Token Protection report-only CA altında test. **Başarısızsa tray/WAM yardımcısı MVP'ye çekilir.**
- DPAPI kapsamı (makine vs servis hesabı) ve servis SID ACL'inin Faz 0'da netleşmesi.
- S2 kabul kriteri: token iptalinde yeniden oturum açma uyarısı.

## Kaynaklar

- [Araştırma raporu §1 — Kimlik doğrulama ve token yönetimi](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Graph entegrasyonu notları §1](../research/notes/graph_entegrasyonu.md), [Teknoloji yığını notları §3](../research/notes/teknoloji_yigini.md)
- [CAE](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-continuous-access-evaluation), [Token Protection – Windows](https://learn.microsoft.com/en-us/entra/identity/conditional-access/deployment-guide-token-protection-windows), [Redirect URI kuralları](https://learn.microsoft.com/en-us/entra/identity-platform/reply-url)
- [M365 entegrasyonu](../architecture/m365-integration.md)
