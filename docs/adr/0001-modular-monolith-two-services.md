# ADR-0001: Modüler monolit; iki Windows servisi ve sandbox parser alt süreci

- **Durum:** Kabul edildi (named pipe uyandırma tasarımı Faz 0'da doğrulanacak)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider / mimar
- **İlgili ADR'ler:** [0002](0002-dotnet-10-lts-self-contained.md), [0012](0012-db-job-outbox-quartz.md), [0014](0014-foundry-local-model-hosting.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

OpsIntel tek bir Windows PC'de, ek sunucu altyapısı olmadan çalışacak; web arayüzü, Graph senkronizasyonu, yapay zekâ işleme, onay akışı ve denetim kaydı içerir. Araştırma notları iki eğilim gösterdi:

- **Tek servis:** 2–6 kişilik ekip için dağıtık sistem yükünden kaçınan tek bir .NET servisi, en fazla bir LLM yan süreci ([teknoloji yığını §1](../research/notes/teknoloji_yigini.md)).
- **Ayrılmış süreçler:** Güvenilmeyen içeriği işleyen süreci yetkili süreçten ayırmak (CaMeL/FIDES tarzı karantinaya alınmış çıkarıcı).

Ek olgular:

- Foundry Local, GPU/NPU sürücülerine bağlı yerel bir kütüphanedir; çökmesi web arayüzünü düşürmemelidir.
- EchoLeak, güvenilmeyen içeriği okuyan bağlamın ayrıcalıklı veriyle karışmasının sonucudur ([arXiv 2509.10540](https://arxiv.org/html/2509.10540v1)).
- .NET 6'dan beri `BackgroundService` hatası host'u temiz biçimde durdurur; SCM kurtarma eylemleri yalnızca sıfır dışı çıkış kodunda (`Environment.Exit(1)`) devreye girer ([MS Learn](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)). Her ek servis yönetilecek yaşam döngüsü sayısını artırır.

## Karar etkenleri

- Çökme yalıtımı (yerel model çalışma zamanı)
- En az yetki (içerik okuyan süreçte token yok)
- Güvenilmeyen belge ayrıştırıcılarının yalıtımı
- Küçük ekip için işletim basitliği; mesaj aracısı kurmamak

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **İki servis + parser alt süreci (seçilen)** | Çökme yalıtımı, token/içerik ayrımı, ayrıştırıcı sandbox'ı | İki servis yaşam döngüsü, IPC tasarımı |
| Tek servis + LLM yan süreci | En basit işletim | İçerik okuyan kod token sahibi süreçte; ayrıştırıcı çöküşü tüm servisi etkiler |
| Mikroservisler + mesaj aracısı | Bağımsız ölçekleme | PC'de RabbitMQ vb. kurulumu, işletim yükü; tek MSI kısıtını zorlar |

## Karar

- Tek depodan üretilen **.NET modüler monolit**, iki Windows servisi olarak dağıtılır: `OpsIntel.Host` (`NT SERVICE\OpsIntel.Host`) ve `OpsIntel.Intelligence` (`NT SERVICE\OpsIntel.AI`, Host'a bağımlı, **Graph token tutmaz**).
- Belge ayrıştırma, Intelligence'ın başlattığı düşük yetkili, Job Object ile sınırlanmış `OpsIntel.Parser` alt sürecinde yapılır.
- Kalıcı iş devri SQLite iş/outbox tablosuyla; düşük gecikmeli uyandırma servis SID'lerine ACL'lenmiş named pipe ile yapılır. Mesaj aracısı kurulmaz.
- Modüller kendi tablolarının sahibidir; modül sınırları mimari testleriyle korunur. Üçüncü bir servis ancak ölçülen bir ihtiyaçla eklenir.
- Her servis ölümcül hatada sıfır dışı kodla çıkar; SCM kurtarma: restart/restart/none.

## Sonuçlar

### Olumlu

- Foundry Local veya ayrıştırıcı çöküşü arayüzü ve senkronizasyonu düşürmez.
- Prompt enjeksiyonu başarılı olsa bile saldırganın ulaştığı süreçte Graph token'ı yoktur.
- Modül sınırları temiz kalırsa bölme bir dağıtım değişikliğidir, yeniden yazım değil.

### Olumsuz

- İki servisin kurulum, yükseltme ve kurtarma davranışı ayrı ayrı test edilmelidir (Pester matrisi).
- Named pipe ACL tasarımı ve servis bağımlılığı sıralaması ek karmaşıklık getirir.
- Alt süreç iletişim protokolü henüz tanımlanmadı.

## Doğrulama / açık noktalar

- Faz 0 spike A: iki servis + `/health` walking skeleton.
- Named pipe'ın servis SID'lerine ACL'lenmesi ve iki servis arasında çalışması Faz 0'da doğrulanacak.
- S1 kabul kriteri: süreç öldürüldüğünde SCM 60 sn içinde yeniden başlatır.
- S3 kabul kriteri: Parser'a hata enjekte edildiğinde servisler ayakta kalır.

## Kaynaklar

- [Araştırma raporu §1 — Süreç topolojisi](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §1](../research/notes/teknoloji_yigini.md), [AI mimarisi notları §9](../research/notes/ai_agent_mimarisi.md)
- [MS Learn: Windows service with BackgroundService](https://learn.microsoft.com/en-us/dotnet/core/extensions/windows-service)
- [EchoLeak, arXiv 2509.10540](https://arxiv.org/html/2509.10540v1)
