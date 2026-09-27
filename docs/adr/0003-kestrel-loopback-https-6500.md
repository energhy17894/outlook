# ADR-0003: Kestrel HTTPS, 127.0.0.1/::1:6500, Host/Origin izin listesi; HTTP.sys yok

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, güvenlik mühendisi
- **İlgili ADR'ler:** [0004](0004-https-certificate-strategy.md), [0007](0007-delegated-auth-bff-pkce.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

Arayüz ve API `https://localhost:6500` üzerinden sunulmalıdır. Localhost arayüzü gerçek bir saldırı yüzeyidir: DNS rebinding, kötü niyetli bir sitenin JavaScript'inin 127.0.0.1'e ulaşmasını sağlar; önerilen savunmalar katı `Host` denetimi ve iç servislerde bile güçlü kimlik doğrulamadır ([GitHub Security Blog](https://github.blog/security/application-security/dns-rebinding-attacks-explained-the-lookup-is-coming-from-inside-the-house/)).

- Kestrel'in depo tabanlı sertifika yapılandırmasında `Location` varsayılan olarak `CurrentUser`'dır ([Kestrel endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)).
- ASP.NET Core geliştirme sertifikasıyla servis uç noktası korumak desteklenmez.
- HTTP.sys `netsh http add urlacl/sslcert` ile uygulama çalışmadan önce kalıcı durum ister; yalnızca Windows Integrated Authentication veya port paylaşımı için anlamlıdır ([HTTP.sys](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/httpsys?view=aspnetcore-10.0)).

## Karar etkenleri

- Ek sistem durumu (netsh) yönetmemek
- DNS rebinding, CSRF ve XSS'e karşı derinlemesine savunma
- Entra ile oturum açma (Windows Auth gerekmez)

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Kestrel, loopback, LocalMachine sertifikası (seçilen)** | Yapılandırma tabanlı, netsh durumu yok, çapraz platform | Sertifika yüklemesi doğru `Location` ile yapılmalı |
| HTTP.sys | Windows Auth, port paylaşımı | netsh durumu, MSI'da temizleme yükü |
| IIS | Olgun barındırma | Ek rol/özellik kurulumu, Hosting Bundle; tek MSI'ı bozar |

## Karar

- Kestrel varsayılan olarak **yalnızca `127.0.0.1:6500` ve `[::1]:6500`** adreslerine bağlanır; sertifikayı `LocalMachine\My` deposundan registry'deki thumbprint ile yükler.
- Host şu kontrolleri uygular:
  - Yalnızca `localhost:6500` / `127.0.0.1:6500` Host değerleri kabul edilir.
  - Durum değiştiren isteklerde `Origin` ve `Sec-Fetch-Site: same-origin` şartı + senkronizör CSRF token.
  - Oturum çerezleri `SameSite=Strict`, `HttpOnly`, `Secure`.
  - CORS başlığı gönderilmez.
  - `default-src 'self'` ile başlayan katı CSP.
  - Localhost'ta da kimlik doğrulama zorunludur ([ADR-0007](0007-delegated-auth-bff-pkce.md)).
- HSTS localhost için gönderilmez (ASP.NET Core varsayılan istisnası).
- LAN modu (Faz 2) `LAN_ENABLED=1`, kurumsal PKI sertifikası ve güvenlik duvarı kuralıyla açılır.

## Sonuçlar

### Olumlu

- MSI'da netsh URL/SSL kaydı yönetmek gerekmez.
- Tarayıcı tabanlı saldırılara karşı çok katmanlı savunma.

### Olumsuz

- Port 6500 başka bir süreç tarafından kullanılıyorsa kurulum erken başarısız olmalıdır (SetupHelper port kontrolü).
- LAN modunda her makine adı Entra'da ayrı yönlendirme URI'si ister.

## Doğrulama / açık noktalar

- S1 kabul kriteri: `https://localhost:6500/health/ready` Edge, Chrome ve Firefox'ta sertifika uyarısı olmadan açılır.
- S7 kabul kriteri: CSP testlerinde uzak içerik yüklenmez.
- S8: localhost pen testi (DNS rebinding, CSRF, Host başlığı); açık yüksek/kritik bulgu kalmaz.

## Kaynaklar

- [Araştırma raporu §1 — Web sunucusu ve HTTPS](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Güvenlik ve uyum notları §6](../research/notes/guvenlik_uyum.md), [Teknoloji yığını notları §3](../research/notes/teknoloji_yigini.md)
- [Tehdit modeli](../architecture/threat-model.md)
