# Araştırma

Bu dizin OpsIntel planının dayandığı araştırmayı içerir. Araştırma **27 Eylül 2026** tarihinde yapılmıştır.

> **Kaynak güncelliği:** Kaynaklar (Microsoft Learn sayfaları, resmi bloglar, NuGet sürümleri, mevzuat metinleri, makaleler) bu tarihte doğrudan erişilerek doğrulanmıştır. Bazı öğeler notlarda **preview/beta** veya **ikincil kaynak** (üçüncü taraf blog, basın, arama özeti) olarak işaretlenmiştir; bazı notlarda arama bütçesi tükendiği için doğrulanamayan noktalar "Gaps" başlıkları altında listelenmiştir. Bu dizindeki dosyalar olduğu gibi korunur; güncellemeler yeni doküman veya ADR ile yapılır.

## Ana rapor

| Dosya | Açıklama |
|---|---|
| [rapor-m365-operasyon-zekasi-platform-plani.md](rapor-m365-operasyon-zekasi-platform-plani.md) | Sentezlenmiş Türkçe araştırma raporu: §1 mimari (iki Windows servisli .NET 10 modüler monolit), §2 MSI ve ön gereksinimler, §3 kanıt zinciri veri modeli, §4 KVKK ve güvenlik, §5 depo yapısı ve 25 ADR, §6 yol haritası, §7 rakip ayrışması, §8 ek geliştirme önerileri |

## Araştırma notları (İngilizce, kaynak URL'leriyle)

| Dosya | Açıklama |
|---|---|
| [notes/teknoloji_yigini.md](notes/teknoloji_yigini.md) | Mimari stil, çalışma zamanı, Kestrel/HTTPS, ön uç, arka plan işleme, veri katmanı, gözlemlenebilirlik, güncelleme ve önerilen yığın |
| [notes/graph_entegrasyonu.md](notes/graph_entegrasyonu.md) | Microsoft Graph kimlik doğrulama (WAM sınırı), değişiklik bildirimleri vs delta, throttling, izinler, Copilot API'leri/MCP, kullanımdan kalkanlar, güvenli geri yazma, etiketler, SDK |
| [notes/msi_kurulum_dagitim.md](notes/msi_kurulum_dagitim.md) | WiX v7/Burn, ön gereksinimler, servis kaydı, HTTPS sertifikası, özel eylemler, yükseltme/kaldırma, kod imzalama, kurumsal dağıtım, CI kurulum testleri |
| [notes/guvenlik_uyum.md](notes/guvenlik_uyum.md) | KVKK (7499 sonrası), Türk içtihadı ve Bărbulescu, GDPR, AB AI Act ve Digital Omnibus, M365 kontrolleri, yerel uç nokta güvenliği, AI'ya özgü tehditler, sağlayıcı veri işleme, sertifikasyonlar (hukuki tavsiye değildir) |
| [notes/ai_agent_mimarisi.md](notes/ai_agent_mimarisi.md) | Model barındırma ve Türkçe yeteneği, çıkarım boru hattı, atıflı yapılandırılmış çıkarım, arama, proje/aşama tespiti, taahhüt çıkarımı, orkestrasyon ve HITL, maliyet, AI güvenliği, değerlendirme |
| [notes/ozellikler_ux.md](notes/ozellikler_ux.md) | Alan modeli (RAID, RACI, ADR, OCEL, W3C Web Annotation), aşama/sağlık çıkarımı, süreç madenciliği, kişisel verimlilik, güven UX'i, BI ve ileri fikirler |
| [notes/rakip_analizi.md](notes/rakip_analizi.md) | Microsoft Copilot ailesi, kurumsal arama asistanları, AI e-posta istemcileri, toplantı zekâsı, süreç madenciliği ve pazar boşlukları |

## Türetilmiş dokümanlar

Rapordan türetilen dokümanların dizini: [docs/README.md](../README.md). Mimari kararlar: [adr/README.md](../adr/README.md).
