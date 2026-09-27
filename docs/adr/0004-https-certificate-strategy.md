# ADR-0004: HTTPS sertifikası — kurumsal PKI öncelikli; yoksa makineye özel CA=false uç sertifika; ortak kök CA yok

- **Durum:** Önerildi (Faz 0 spike A'da Edge/Chrome/Firefox tarayıcı güven testleri bekleniyor)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, güvenlik mühendisi, DevOps/kurulum mühendisi
- **İlgili ADR'ler:** [0003](0003-kestrel-loopback-https-6500.md), [0005](0005-single-msi-wix-v7.md)

## Bağlam

`https://localhost:6500` tarayıcıda uyarısız açılmalı, ancak kurulum bir MITM riski yaratmamalıdır. İki not seti farklı yön gösteriyordu:

- **İsim kısıtlı yerel kök CA** önerisi ([MSI notları §4](../research/notes/msi_kurulum_dagitim.md)); isim kısıtı desteği tarayıcıya göre değişir ve doğrulanmadı.
- **Hiç kök CA dağıtmamak** ([güvenlik notları §6](../research/notes/guvenlik_uyum.md)); gerekçe: Dell'in özel anahtarıyla birlikte kök sertifika kurduğu ve MITM'e kapı açtığı olay ([CERT/CC VU#925497](https://www.kb.cert.org/vuls/id/925497)).

Tarayıcı tarafı: Chrome Windows'ta LocalMachine ve CurrentUser kök depolarını otomatik tüketir ([Chrome Root Store FAQ](https://chromium.googlesource.com/chromium/src/+/main/net/data/ssl/chrome_root_store/faq.md)); Firefox 120+ Windows'ta işletim sistemi köklerini varsayılan olarak içe aktarır ([Firefox 120](https://www.firefox.com/en-US/firefox/120.0/releasenotes/)). `New-SelfSignedCertificate` varsayılan olarak bir yıllık sertifika üretir.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Kurumsal PKI; yoksa CA=false uç sertifika LocalMachine\Root'ta (seçilen)** | Başka sertifika imzalayamaz; kök CA anahtarı yok | Yenilemede yeni uç sertifika yeniden güvenilir kılınmalı |
| İsim kısıtlı yerel kök CA | Yeniden güven gerektirmeden yeniden verme | İsim kısıtı tarayıcı desteği doğrulanmadı; CA anahtarı saldırı hedefi |
| mkcert / dev-cert | Kolay | Geliştirme amaçlı; servis için desteklenmez; kök anahtar tam MITM gücü verir |
| Düz HTTP loopback | Sertifika yok | Tarayıcıların `http://localhost`'u güvenli bağlam sayması doğrulanmadı; `Secure` çerez sorunları |

## Karar

1. `CERT_THUMBPRINT` verilirse **kurumsal PKI** sertifikası kullanılır (LAN modu için fiilen zorunlu).
2. Verilmezse kurulum, `OpsIntel.SetupHelper` (ertelenmiş, `Impersonate="no"`, rollback karşılıklı CA) ile makineye özel, kendinden imzalı bir **uç sertifika** üretir: `basicConstraints CA=false`, EKU serverAuth, SAN `localhost`, `127.0.0.1`, `::1`, makine adı ve FQDN; anahtar dışa aktarılamaz; özel anahtar ACL'i yalnızca `NT SERVICE\OpsIntel.Host`.
3. **Yalnızca bu uç sertifika** `LocalMachine\Root`'a eklenir. Ortak veya satıcıya ait kök CA hiç dağıtılmaz; kurulum paketi özel anahtar taşımaz.
4. Yenileme: SYSTEM zamanlanmış görevi aylık `setup-helper cert renew` çalıştırır; süresine 30 günden az kalan sertifikayı yeniler.
5. Kaldırmada sertifika silinir; yükseltmede (`UPGRADINGPRODUCTCODE`) silinmez.

## Sonuçlar

### Olumlu

- CA=false olduğu için sertifika başka ad için sertifika imzalamakta kullanılamaz.
- Kurumsal ortamlarda mevcut PKI güveniyle uyumlu.

### Olumsuz

- CA=false uç sertifikanın Root deposunda güven çapası olarak kabul edilmesi standart X.509 yol doğrulamasına dayanan bir **tasarım çıkarımıdır** ve test edilmelidir.
- Yenilenen her sertifika yeniden güvenilir kılınır; eski sertifika Root'tan temizlenmelidir.

## Doğrulama / açık noktalar

- Spike A git kriteri: temiz Win11 23H2 ve Server 2022'de `https://localhost:6500/health/ready` Edge, Chrome ve Firefox'ta uyarısız açılır.
- Yenileme sonrası Kestrel'in yeni sertifikayı yüklemesi (yalnızca depo değişikliğinde yeniden yükleme doğrulanmadı).
- Pester: kaldırmadan sonra sertifika kalmaz.

## Kaynaklar

- [Araştırma raporu §2 — HTTPS sertifikası](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [MSI notları §4](../research/notes/msi_kurulum_dagitim.md), [Güvenlik notları §6](../research/notes/guvenlik_uyum.md)
- [MS Learn: New-SelfSignedCertificate](https://learn.microsoft.com/en-us/powershell/module/pki/new-selfsignedcertificate)
- [Kurulum dokümanı](../operations/installation.md)
