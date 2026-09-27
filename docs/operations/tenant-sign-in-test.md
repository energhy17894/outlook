# Kiracı ile oturum açma testi (Faz 0 manuel doğrulama)

*Bu doküman, OpsIntel Faz 0 iskeletini kendi Windows PC'nizde, kendi gerçek Microsoft 365
kiracınıza karşı elle test etmek içindir — SPIKE-AUTH.md'nin "gerçek bir kiracı gerektirir ve bu
kum havuzunda çalıştırılamadı" dediği adımı kapatır (bkz.
[`src/Modules/OpsIntel.Connectors.Graph/SPIKE-AUTH.md`](../../src/Modules/OpsIntel.Connectors.Graph/SPIKE-AUTH.md)
§"What is not validated here"). Kaynak: kodun kendisi (`OpsIntel.Host`, `OpsIntel.Connectors.Graph`),
`installer/*.wxs`, `.github/workflows/installer.yml`, [ADR-0007](../adr/0007-delegated-auth-bff-pkce.md),
[ADR-0008](../adr/0008-delegated-graph-no-mail-send.md), [M365 entegrasyonu](../architecture/m365-integration.md)
ve [Graph entegrasyonu notları](../research/notes/graph_entegrasyonu.md). Doğrulanmamış/tahmini
noktalar açıkça **[DOĞRULANMADI]** olarak işaretlenmiştir.*

**Bu test neyi doğrulamaz:** ürün özelliklerini. Faz 0 iskeleti şu anda yalnızca sağlık uç
noktaları (`/health/live`, `/health/ready`), Entra oturum açma BFF akışı (`/auth/*`) ve statik SPA
kabuğunu (`OpsIntel.Host`'un `wwwroot`'undan) sunar. Posta/takvim/dosya alımı (ingestion), inceleme
kuyruğu, panolar gibi ürün özellikleri için henüz kullanıcı arayüzü bağlanmamıştır — bu testin
amacı yalnızca **kimlik doğrulama zincirinin gerçek bir kiracıda uçtan uca çalıştığını** (veya
nerede tıkandığını) tespit etmektir.

## 1. Önkoşullar

- Windows 11 23H2+ (x64) veya Windows Server 2022/2025; Windows 10 22H2 "en iyi çaba" olarak kabul
  edilir ([`docs/operations/installation.md`](installation.md) §2, ADR-0024). MSI, `VersionNT64`
  ve minimum `VersionNT >= 603` şartını zorunlu kılar (`installer/Package.wxs`); Windows 11/10
  sürümünü ayırt eden kesin bir build-numarası koşulu henüz yazılmamıştır — **[DOĞRULANMADI]**.
- Yerel yönetici (Administrator) hakları — MSI `Launch Condition="Privileged"` ile bunu zorunlu
  kılar (`installer/Package.wxs`).
- TCP 6500 boş olmalı (varsayılan port; `appsettings.json` → `OpsIntel:Kestrel:Port`).
- Microsoft Entra ID'de **en az biri**:
  - Uygulama kaydı oluşturabilecek bir rol (varsayılan olarak her kullanıcı "Application
    Developer" hakkına sahip olabilir, ama birçok kiracıda bu kısıtlanmıştır), **veya**
  - Sizin için kayıt yapacak ve admin consent verecek bir Genel Yönetici / Uygulama Yöneticisi.
- **Copilot lisansı gerekmez.** Bu akış yalnızca Microsoft Graph delegated izinleri kullanır;
  Copilot Retrieval/Chat/Search API'leri, Work IQ MCP ve Enterprise MCP kasıtlı olarak
  kullanılmaz ([`m365-integration.md`](../architecture/m365-integration.md) §9).

## 2. Entra uygulama kaydı

Kod, MSAL.NET'in `IPublicClientApplication` (`PublicClientApplicationBuilder.Create`) yolunu
kullanır — **secret'sız public client** (`GraphAuthServiceCollectionExtensions.cs`). Bu, platform
seçimini belirler:

1. [Entra admin merkezi](https://entra.microsoft.com) → **Applications → App registrations → New
   registration**.
2. **Kimin oturum açabileceği:** kendi kiracınızda test ediyorsanız **"Accounts in this
   organizational directory only (Single tenant)"** seçin. (Kodun varsayılanı `TenantId =
   "organizations"`, yani "herhangi bir kurumsal kiracı" — `GraphAuthOptions.cs`; testte kendi
   `TENANT_ID`'nizi vereceğiniz için single-tenant kayıt yeterlidir.)
3. **Redirect URI:** platform olarak **"Mobile and desktop applications"** seçin (public client —
   `http://localhost` özel muafiyetini yalnızca bu platform tipi sunar,
   `SPIKE-AUTH.md` §"Redirect URI"). Tam URI olarak şunu girin (port 6500'ün varsayılan
   `OpsIntel:Kestrel:Port` değerinden türetildiği URI —
   `GraphAuthRedirectUriResolver.Resolve` / `AuthEndpoints.cs`'in `/auth/callback` rotası):

   ```
   https://localhost:6500/auth/callback
   ```

   Not: bu **tam yollu, sabit** bir URI'dir — MSAL'in "localhost için portu görmezden gel" özel
   durumu yalnızca çıplak `http://localhost` biçimine uygulanır; kod bilerek yolu (`/auth/callback`)
   sabitleyip kayıt gerektirir (`SPIKE-AUTH.md`). Portunuzu 6500 dışında bir değere değiştirdiyseniz
   (`PORT` MSI özelliği veya `OpsIntel:Kestrel:Port`), redirect URI'yi buna göre güncelleyin.
4. Kayıt oluşturulduktan sonra **Authentication** sayfasına gidin:
   - Redirect URI'nin **Web** değil **"Mobile and desktop applications"** altında listelendiğini
     doğrulayın.
   - **"Allow public client flows"** anahtarını **Yes** yapın — bu, secret olmadan
     `AcquireTokenInteractive`/PKCE akışına izin veren ayardır.
5. **API permissions** → **Add a permission** → **Microsoft Graph** → **Delegated permissions** —
   kodun istediği tam liste, `GraphScopes.Delegated`'dan (fazladan izin eklemeyin; test bunu
   birebir yansıtmalı):

   | İzin | Neden |
   |---|---|
   | `User.Read` | Oturum/profil |
   | `Mail.ReadWrite` | Yanıt taslağı oluşturmak için (`Mail.Read` değil — "Does not include permission to send mail", ADR-0008) |
   | `Files.Read.All` | OneDrive/SharePoint dosya erişimi |
   | `Sites.Read.All` | Site/sürücü keşfi |
   | `Calendars.Read` | Takvim penceresi |
   | `offline_access` | Refresh token (OIDC izni, admin onayı gerekmez) |

   **`Mail.Send` istenmez ve hiçbir fazda istenmeyecektir** — bu, `GraphScopes.cs`'de bir sabitle
   (`ForbiddenMailSendScope`) belgelenmiş ve birim testleriyle (`GraphScopesTests.cs`) korunan bir
   mimari karardır (ADR-0008): gönderim her zaman kullanıcı tarafından Outlook'tan yapılır.
6. **Grant admin consent for `<kiracı adı>`** düğmesine basın (yönetici iseniz doğrudan; değilseniz
   yöneticinizden isteyin). Bunun **neden gerekli** olduğu: Microsoft'un yeni kiracılarda varsayılan
   yönetilen onay politikası (`microsoft-user-default-recommended`) `Mail.*`, `Calendars.*`,
   `Files.Read.All`, `Sites.Read.All` gibi izinler için **kullanıcı kendi kendine onay veremez**
   şekilde kısıtlar ([`m365-integration.md`](../architecture/m365-integration.md) §2). Admin
   consent atlanırsa `/auth/callback` "consent gerekli" hatasıyla (AADSTS65001) döner — bkz. §6.
7. **Overview** sayfasından **Application (client) ID** ve **Directory (tenant) ID** değerlerini
   not edin; §3'te `CLIENT_ID`/`TENANT_ID` olarak kullanılacaklar.
8. **Conditional Access / Token Protection notu:** Kiracınızda "tüm uygulamalar" için Token
   Protection veya cihaz uyumluluğu zorunlu kılan bir CA politikası varsa, bu kayıt Microsoft'un
   Token Protection desteklenen-uygulama listesinde olmadığından (yalnızca Microsoft'un kendi
   istemcileri) engellenebilir (ADR-0007 açık risk). Gerçek bir CA politikasını değiştirmeden önce,
   test için ayrı bir **report-only** CA politikası altında (bu uygulamayı hedefleyen, ama
   engellemeyen) deneyin ve Entra'nın **Sign-in logs** → **Conditional Access** sekmesinden hangi
   politikaların "Report-only: Failure" verdiğine bakın.

## 3. MSI'yi edinme ve kurma

### MSI'yi indirme

MSI, `.github/workflows/installer.yml`'deki `build-msi` işinden **`OpsIntel-x64-msi`** adlı
GitHub Actions artifact'i olarak üretilir (GitHub → Actions → ilgili workflow çalıştırması →
Artifacts). Bu MSI **imzasız**dır: `SIGNING_THUMBPRINT` secret'ı henüz ayarlanmadığı için imzalama
adımı atlanır (workflow'daki "Skip-signing notice" adımı, ADR-0006 — imza sağlayıcısı henüz
sözleşilmedi). Bu nedenle:

- Kurulumda Windows SmartScreen **"Windows protected your PC"** uyarısı gösterecektir. Devam etmek
  için **More info → Run anyway**. Bu, Faz 0'da beklenen bir durumdur, güvenlik açığı değildir —
  ama kendi PC'niz dışında dağıtmayın.
- Kurumsal ortamlarda Smart App Control imzasız/itibarsız dosyaları engelleyebilir
  ([`installation.md`](installation.md) §8); kendi test PC'nizde Smart App Control kapalıysa
  sorun yaşamazsınız.

### Sessiz kurulum

MSI'nin genel (public) özellikleri `installer/Package.wxs`'te tanımlıdır; adları alt çizgilidir: **`TENANT_ID`** ve **`CLIENT_ID`**:

```powershell
msiexec /i OpsIntel-x64.msi /qn PORT=6500 TENANT_ID=<directory-tenant-id> CLIENT_ID=<application-client-id> /l*v install.log
```

Diğer ilgili özellikler (`installer/Package.wxs`): `CERT_THUMBPRINT` (boşsa makineye özel
kendinden imzalı sertifika üretilir, bkz. §4), `ALLOW_LAN` (varsayılan `0`, LAN modunu ve güvenlik
duvarı kuralını açar — test için `0` bırakın), `REMOVE_DATA` (yalnızca kaldırmada kullanılır, §7).

### Etkileşimli alternatif

MSI dosyasına çift tıklayıp sihirbazı takip edin. **Önemli:** bu Faz 0 MSI'sinde TENANT_ID/CLIENT_ID
girecek bir kurulum arayüzü ekranı olduğu koda göre doğrulanamadı (Package.wxs yalnızca
launch condition'ları ve özellik varsayılanlarını tanımlıyor, özel bir kullanıcı arayüzü dizisi
görülmedi) — **[DOĞRULANMADI]**. En güvenilir yol, etkileşimli kurulumdan sonra §4'teki kayıt
defteri anahtarlarını elle düzenlemek ya da `msiexec /i ... CLIENT_ID=... TENANT_ID=...` ile
komut satırından geçmektir (özellikler etkileşimli kurulumda da `/i msi.exe CLIENT_ID=... TENANT_ID=...`
şeklinde verilebilir, sihirbaz yine de gösterilir).

### Yapılandırmanın nereye yazıldığı ve nasıl güncellenir

Değerler `HKLM\SOFTWARE\OpsIntel` altına yazılır (`installer/Config.wxs`):

| Kayıt defteri değeri | Kaynak özellik | Karşılık gelen appsettings anahtarı |
|---|---|---|
| `Port` | `PORT` | `OpsIntel:Kestrel:Port` |
| `CertThumbprint` | `CERT_THUMBPRINT` | `OpsIntel:Kestrel:CertificateThumbprint` |
| `TenantId` | `TENANT_ID` | `OpsIntel:Graph:Auth:TenantId` |
| `ClientId` | `CLIENT_ID` | `OpsIntel:Graph:Auth:ClientId` |
| `AllowLan` | `ALLOW_LAN` | — |

Bu değerler `Program.cs`'de `builder.Configuration.AddOpsIntelWindowsRegistryConfiguration()` ile
okunur (yalnızca Windows'ta; `appsettings.json`'dan **daha yüksek**, ortam değişkenlerinden **daha
düşük** öncelikli). Kayıt defterini kurulumdan sonra elle değiştirirseniz (örn. `CLIENT_ID`'yi ilk
kez veya yeniden ayarlarken), **`OpsIntel.Host` servisini yeniden başlatmadan değişiklik etkili
olmaz** — konfigürasyon yalnızca süreç başlangıcında okunur, canlı yeniden yükleme (reload) yoktur.

```powershell
Restart-Service OpsIntel.Host
```

`CLIENT_ID` boş bırakılırsa (varsayılan), `GraphAuthServiceCollectionExtensions.cs` gerçek MSAL
işlem hattı yerine `NotConfiguredGraphAuthService`'i kaydeder: Host normal başlar,
`/health/live`, statik SPA gibi her şeyi sunar; yalnızca `/auth/login` ve `/auth/callback`
**HTTP 503** ile "Microsoft Graph sign-in is not configured..." mesajı döner (crash yerine).

## 4. Doğrulama

### Servisler

MSI iki servis kurar (`installer/Services.wxs`): **`OpsIntel.Host`** (`NT SERVICE\OpsIntel.Host`
hesabıyla) ve **`OpsIntel.AI`** (`NT SERVICE\OpsIntel.AI` hesabıyla, görünen adı "OpsIntel
Intelligence"). Her ikisi de `Start="auto"`, arıza durumunda otomatik yeniden başlatma
yapılandırılmıştır (`util:ServiceConfig FirstFailureActionType="restart"`).

```powershell
Get-Service OpsIntel.Host, OpsIntel.AI | Select-Object Name, Status, StartType
```

Her ikisinin de `Running` olduğunu doğrulayın. `OpsIntel.AI` bu testte kimlik doğrulamaya dahil
değildir (Graph token'ı hiç görmez, ADR-0001/0008), ama `Get-Service` ile birlikte kontrol etmek
kurulumun bütünlüğünü doğrular.

### Sağlık uç noktası

```powershell
curl.exe -k https://localhost:6500/health/live
```

`-k` gereklidir: kurulum PKI sertifikası verilmediyse (`CERT_THUMBPRINT` boş bırakıldıysa), Host
kendinden imzalı bir sertifika kullanır (`installation.md` §5); tarayıcı/curl bunu varsayılan
olarak güvenmez. Tarayıcıda `https://localhost:6500/` açtığınızda "Bağlantınız gizli değil" /
"NET::ERR_CERT_AUTHORITY_INVALID" uyarısı **beklenen** bir durumdur (kurulum, kendinden imzalı
sertifikayı `LocalMachine\Root`'a eklemesi gerekiyor —`installation.md` §5 madde 3; bu adımın
gerçek kurulumda çalıştığı bu ortamda yeniden doğrulanmadı, tarayıcı uyarısını "Advanced → Proceed"
ile geçin).

### Oturum açma akışı

1. Tarayıcıda `https://localhost:6500/auth/login` açın (`AuthEndpoints.cs`). Host bir
   `opsintel-auth-state` çerezi (HttpOnly, Secure, SameSite=Lax, 10 dk ömür) bırakır ve sizi
   Entra'nın yetkilendirme sayfasına 302 ile yönlendirir.
2. Entra'da hesabınızla oturum açın (MFA/CA varsa uygulanır).
3. Entra sizi `https://localhost:6500/auth/callback?code=...&state=...`'a geri yönlendirir. Host
   `state`'i çerezle karşılaştırır, MSAL'in arka planda bekleyen `AcquireTokenInteractive`
   çağrısını tamamlar (`GraphAuthService.CompleteInteractiveLoginAsync`) ve başarılıysa `/`'a 302
   yönlendirir.
4. `https://localhost:6500/auth/me` adresine gidin — `HandleMeAsync` şunu döner:
   ```json
   { "signedIn": true, "username": "...", "displayName": "..." }
   ```
   (Kod not: `GetCurrentAccountAsync` şu an `DisplayName` alanına da `Username`'i koyuyor —
   `GraphAuthService.cs` satır 101 — yani `displayName` alanı gerçek ad değil e-posta adresini
   gösterecektir; bu bir eksiklik/kusur olabilir, üründeki gösterim davranışını yansıtmaz.)

### Toplanacak sonuçlar (spike A/B git/gitme kararı için)

- [ ] `OpsIntel.Host` ve `OpsIntel.AI` servisleri hangi hesap altında `Running`? (`Get-Service`,
      ayrıca Hizmetler (services.msc) → özellikler → Log On sekmesinden hesap adını doğrulayın.)
- [ ] Sertifika güveni: Edge, Chrome, Firefox'ta ayrı ayrı `https://localhost:6500/` açıp uyarı
      olup olmadığını not edin (Chrome/Edge Windows kök deposunu otomatik kullanır; Firefox 120+
      varsayılan olarak OS köklerini içe aktarır — `installation.md` §5).
- [ ] Oturum açma başarı/başarısızlık, hata mesajı (varsa tam AADSTS kodu).
- [ ] Conditional Access davranışı: Entra **Sign-in logs**'ta bu oturum için hangi CA
      politikalarının uygulandığı/rapor-only olarak tetiklendiği.
- [ ] Token cache dosyasının varlığı: `ISecretStore`'un DPAPI deposu, `OpsIntel:Secrets:StorageDirectory`
      (varsayılan `secrets`, `ConfigDir` altına çözümlenir — `Program.cs`) altında
      `graph-msal-token-cache` anahtarıyla saklanır (`GraphAuthOptions.TokenCacheSecretKey`).
      `ProgramData\OpsIntel\config` altında ilgili dosyanın oluştuğunu ve ACL'inin yalnızca
      `NT SERVICE\OpsIntel.Host` + SYSTEM + Administrators'a açık olduğunu (`icacls`) doğrulayın
      (`Folders.wxs`'in `ConfigDirAcl` bileşeni — Intelligence servisine bilerek erişim
      verilmemiştir).
- [ ] Olay Günlüğü / log hataları: `Get-WinEvent -LogName Application -FilterHashtable
      @{ProviderName='OpsIntel.Host'}` ve `ProgramData\OpsIntel\logs` altındaki Serilog dosyaları
      (workflow'un `installer-build-logs`/`install-smoke-diagnostics` adımlarının CI'da topladığı
      aynı kaynaklar).

## 5. Sorun giderme

| Belirti | Olası neden | Çözüm |
|---|---|---|
| `AADSTS50011: reply URL … does not match` | Entra'ya kayıtlı redirect URI, Host'un gerçek portuyla eşleşmiyor (`https://localhost:<PORT>/auth/callback`) | Entra kaydındaki URI'yi ve MSI'nin `PORT` değerini birbirine eşitleyin; `GraphAuthRedirectUriResolver` portu appsettings/registry'den türetir |
| `AADSTS65001: … consent …` / "onay gerekli" | Yönetilen onay politikası kullanıcı onayını engelliyor, admin consent verilmemiş | Uygulama kaydında **Grant admin consent** verin (§2 madde 6) |
| `/auth/login` veya `/auth/callback` → **HTTP 503** | `CLIENT_ID` boş/ayarlanmamış (`NotConfiguredGraphAuthService`) | `CLIENT_ID`/`TENANT_ID`'yi kayıt defterine yazın ve `OpsIntel.Host`'u yeniden başlatın (§3) |
| `https://localhost:6500` açılmıyor / bağlantı reddedildi | Port 6500 başka bir süreç tarafından kullanılıyor, ya da servis çalışmıyor | `Get-NetTCPConnection -LocalPort 6500`, `Get-Service OpsIntel.Host`; farklı port için MSI'yi `PORT=<başka>` ile kurup Entra redirect URI'yi de güncelleyin |
| Tarayıcı "sertifika güvenilir değil" uyarısı | Kendinden imzalı uç sertifika `LocalMachine\Root`'a eklenmemiş/bulunamıyor | `-k`/"Proceed anyway" ile geçin (§4); kurumsal PKI sertifikası kullanmak isterseniz `CERT_THUMBPRINT` ile kurun |
| Entra oturum açma sayfasında "bu uygulama engellendi" veya sessiz hata | Conditional Access / Token Protection politikası bu genel istemciyi reddediyor | Entra Sign-in logs → Conditional Access sekmesinden hangi politikanın uyguladığını görün; report-only modda test edin (§2 madde 8) |
| `state mismatch.` (400) | `/auth/login`'den geçen 10 dakikadan uzun süre geçti (çerez süresi doldu) ya da farklı bir tarayıcı sekmesi/profili kullanıldı | Akışı `/auth/login`'den yeniden başlatın |

## 6. Kaldırma

```powershell
msiexec /x OpsIntel-x64.msi /qn REMOVE_DATA=1
```

`REMOVE_DATA=1` verilmezse (`installer/Package.wxs` varsayılanı `0`), `ProgramData\OpsIntel`
altındaki veri (config, secrets/token cache dahil) ve makineye özel HTTPS sertifikası **korunur**
(`Folders.wxs`'in `RemoveDataOnUninstall` bileşeni, `Cert.wxs`'in `CertRemove` eylemi). `1`
verildiğinde `util:RemoveFolderEx` tüm `ProgramData\OpsIntel` alt ağacını (dolayısıyla token
cache'i de) siler ve sertifika kaldırılır — bu, testten sonra Entra hesabınıza ait yerel izleri
temizlemek istiyorsanız kullanılmalıdır. Not: bu, Entra tarafındaki uygulama kaydını veya verdiğiniz
admin consent'i **silmez**; onları ayrıca Entra admin merkezinden kaldırmanız gerekir.
