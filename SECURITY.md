# Güvenlik Politikası

## Durum

OpsIntel şu anda **planlama aşamasındadır; henüz yayımlanmış bir sürüm veya çalıştırılabilir kod yoktur.** Bu nedenle desteklenen sürüm tablosu henüz boştur.

| Sürüm | Destek |
|---|---|
| — | Henüz sürüm yok |

## Güvenlik açığı bildirimi (sorumlu ifşa)

> **Yer tutucu:** Resmî güvenlik iletişim kanalı (ör. özel e-posta adresi veya GitHub Private Vulnerability Reporting) Faz 0'da belirlenecek ve buraya eklenecektir.

Bu kanal belirlenene kadar:

1. Güvenlik açıklarını **herkese açık issue olarak açmayın.**
2. Mümkünse GitHub'ın "Report a vulnerability" (özel güvenlik danışmanlığı) özelliğini kullanın; etkin değilse depo sahipleriyle özel olarak iletişime geçin.
3. Bildiriminize etkilenen bileşeni, yeniden üretme adımlarını ve olası etkiyi ekleyin. **Bildirime gerçek e-posta içeriği veya kişisel veri eklemeyin**; gerekirse sentetik örnek kullanın.

Hedeflenen yanıt süreleri, açıklama takvimi ve ödül politikası ilk sürümden önce tanımlanacaktır.

## Veri kuralı: gerçek e-posta verisi asla depoya konmaz

- Gerçek e-postalar, ekler, belgeler, Teams transkriptleri, veritabanı dosyaları (`*.db`, `*.sqlite`), tanı paketleri ve log dosyaları **KVKK kapsamında kişisel veri** içerebilir ve bu depoya **hiçbir koşulda** eklenmez.
- Değerlendirme (eval) için kullanılan gerçek "altın veri seti" şifreli, erişimi kısıtlı ayrı bir depolamada tutulur; `eval.yml` iş akışı self-hosted runner'da çalışır ([rapor §5](docs/research/rapor-m365-operasyon-zekasi-platform-plani.md)).
- Depodaki testler yalnızca **sentetik** TR/EN korpus kullanır (`tests/eval/`).
- Sırlar (client secret, API anahtarı, PFX, `.env`) depoya konmaz. PC'lere client secret dağıtılmaz ([ADR-0007](docs/adr/0007-delegated-auth-bff-pkce.md)).
- Yanlışlıkla kişisel veri veya sır commit edilirse: derhal bildirin; yalnızca yeni commit ile silmek yeterli değildir, geçmişin temizlenmesi ve olası KVKK ihlal değerlendirmesi gerekir.

## Tasarım gereği güvenlik ilkeleri

Ürünün güvenlik tasarımı [docs/architecture/threat-model.md](docs/architecture/threat-model.md) dosyasında ve ilgili ADR'lerde belgelenmiştir. Öne çıkanlar:

- Yalnızca loopback (`127.0.0.1`/`::1`) dinleme, katı Host/Origin denetimi, CSRF, katı CSP ([ADR-0003](docs/adr/0003-kestrel-loopback-https-6500.md))
- `Mail.Send` hiçbir fazda istenmez ([ADR-0008](docs/adr/0008-delegated-graph-no-mail-send.md))
- Güvenilmeyen içerik karantinası, araçsız çıkarıcı, egress izin listesi ([ADR-0019](docs/adr/0019-untrusted-content-quarantine.md))
- Hash zincirli, append-only denetim kaydı ([ADR-0018](docs/adr/0018-hash-chained-audit-log.md))
- İçerik içermeyen loglar ([ADR-0021](docs/adr/0021-observability.md))
