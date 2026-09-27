# KVKK Uyum Çerçevesi

> **Bu doküman hukuki tavsiye değildir.** Araştırma notlarının mühendislik ve ürün tasarımı açısından özetidir. Tüm hukuki yorumlar, özellikle "hukuk danışmanı" ile işaretlenenler, Türk veri koruma ve iş hukuku alanında uzman bir avukat tarafından teyit edilmelidir.

*Kaynak: [araştırma raporu §4](../../research/rapor-m365-operasyon-zekasi-platform-plani.md), [güvenlik ve uyum notları §1–§2, §8–§9](../../research/notes/guvenlik_uyum.md). Tarih: 27 Eylül 2026.*

## 1. Özet: yerel önce, bulut istisnayla

OpsIntel'in işlediği e-posta ve belge içeriği çalışanların ve üçüncü kişilerin kişisel verilerini, zaman zaman da özel nitelikli kişisel verileri (sağlık, sendika üyeliği, ceza kaydı) içerir. KVKK bu içeriğe tam olarak uygulanır. Tasarımı belirleyen iki hukuki nokta:

1. **Bulut LLM çağrısı yurt dışı aktarımdır.** 7499 sonrası md. 9'a göre düzenli aktarım yeterlilik kararı veya uygun güvence (pratikte Kurul'un ilan ettiği **standart sözleşme, SS-2**) gerektirir ve sözleşme **imzadan itibaren 5 iş günü içinde** Kurum'a bildirilir. Açık rıza yalnızca "arızi" aktarımlar için kullanılabilir.
2. **Meşru menfaat özel nitelikli veri için işleme şartı değildir** (md. 6). Bu nedenle İK, sağlık ve hukuk içeriğini dışlamak "iyi olur" değil, hukuki bir gerekliliktir.

Yerel çıkarım yalnızca çıkarım adımı için aktarım sorusunu ortadan kaldırır; md. 4, 10, 11 ve 12 yükümlülükleri devam eder.

## 2. Hukuki çerçeve (yürürlükteki hukuk)

| Madde | Özet | Ürüne yansıması |
|---|---|---|
| Md. 4 | Hukuka ve dürüstlük kuralına uygunluk, doğruluk, belirli/açık/meşru amaç, amaçla bağlantılı/sınırlı/ölçülü olma, gerekli süre kadar saklama | Amaçla sınırlılık; saklama kuralı (türetilmiş kayıt kaynaktan uzun yaşamaz) |
| Md. 5(2)(f), (e) | Meşru menfaat (temel hak ve özgürlüklere zarar vermemek kaydıyla); bir hakkın tesisi/kullanılması/korunması | Çalışanların olağan kişisel verileri için savunulabilir dayanak (hukuk danışmanı teyidi) — [meşru menfaat testi](mesru-menfaat-testi.md) |
| Md. 6 | Özel nitelikli veriler; meşru menfaat bu şartlar arasında değil; 7499 ile yeni şartlar (ör. istihdam/İSG/sosyal güvenlik yükümlülükleri) | Politika kapısı: hariç tutma, etiket kuralları, yerel sınıflandırıcı |
| Md. 9(1), (4)–(5) | Yeterlilik kararı; yoksa uygun güvence (standart sözleşme vb.); SS 5 iş günü içinde bildirim | Bulut katmanı kilidi — [SS-2 takip](ss2-takip.md) |
| Md. 9(6) | Açık rıza vb. istisnalar yalnızca "arızi" aktarım için | Düzenli bulut LLM akışı açık rızaya dayandırılamaz |
| Md. 10 | Aydınlatma yükümlülüğü | [Aydınlatma şablonu](aydinlatma-sablonu.md) |
| Md. 11, 13 | İlgili kişi hakları; md. 11(1)(g) münhasıran otomatik analiz aleyhine sonuca itiraz; başvuruya en geç 30 gün içinde yanıt | [Veri sahibi talepleri](veri-sahibi-talepleri.md); kişi hakkında AI kararı yok |
| Md. 12 | Teknik ve idari tedbirler; ihlalin "en kısa sürede" bildirimi | Şifreleme, ACL, içeriksiz log, ihlal runbook'u |
| Md. 16 | VERBİS kaydı: amaçlar, veri kategorileri, alıcılar, **yurt dışına aktarılması öngörülen veriler**, güvenlik tedbirleri, azami saklama süreleri; değişiklikler "derhâl" | [VERBİS girdileri](verbis-girdileri.md) |
| Md. 18 | İdari para cezaları (7499 ile standart sözleşme bildirmeme cezası eklendi; tutarlar her yıl yeniden değerlenir — 2026 tutarları doğrulanmadı) | Uyum paketi önceliği |

Kaynak: [6698 sayılı Kanun](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf).

## 3. Yurt dışı aktarım ve SS-2

- KVKK Aktarım Rehberi (Ocak 2025) "arızi" kavramını tek seferlik veya birkaç kez olarak tanımlar; **yurt dışındaki bir bulutta depolamayı** ve üçüncü ülkeden uzaktan erişimi de aktarım sayar ([KVKK Aktarım Rehberi](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi)).
- Kurul kararı 2024/959 ile dört standart sözleşme kabul edildi: SS-1 (VS→VS), **SS-2 (VS→veri işleyen)**, SS-3 (Vİ→Vİ), SS-4 (Vİ→VS). Bildirim KEP, fiziksel veya "Standart Sözleşme Bildirim Modülü" ile yapılabilir; SS-1/SS-2 veri ihracatçısının VERBİS bilgilerini içermelidir.
- Sözleşmede taraf/içerik değişikliği veya fesih de 5 iş günü içinde bildirilmelidir (ikincil kaynak; Yönetmelik ile teyit edilmeli).
- AB'de barındırma tek başına yardımcı olmaz: AB için bir KVKK yeterlilik kararı bulunamamıştır (kapsamlı olarak doğrulanamadı).
- **Microsoft'un Türk müşterilerle KVKK standart sözleşmesi imzalama pratiği Aralık 2025 itibarıyla belirsizdi** ([Microsoft Q&A](https://learn.microsoft.com/tr-tr/answers/questions/5664711/kvkk-standart-s-zle-me-hk)). M365'in kendisi de (kiracı verisi AB veri merkezlerinde) bir aktarımdır; şirketin mevcut Microsoft SS-2 durumu aynı iş paketinde teyit edilmelidir.
- "Claude in Microsoft Foundry" kullanımında veri işleyen Anthropic'tir; ayrı SS-2 ve DPA incelemesi gerekir.

**Ürün kuralı:** Bulut katmanı (2/3), yönetici ilgili sağlayıcıyla SS-2'nin imzalandığını ve bildirildiğini bir kontrol listesiyle onaylamadan açılamaz ([ADR-0014](../../adr/0014-foundry-local-model-hosting.md)).

## 4. Özel nitelikli veriler

- Md. 6(1) listesi: ırk, etnik köken, siyasi düşünce, felsefi inanç, din, mezhep veya diğer inançlar, kılık ve kıyafet, dernek/vakıf/sendika üyeliği, sağlık, cinsel hayat, ceza mahkûmiyeti ve güvenlik tedbirleri, biyometrik ve genetik veriler.
- Kontroller (LLM çağrısından **önce**):
  - İK, Hukuk, İSG ve üst yönetim posta kutularının/sitelerinin hariç tutulması
  - Duyarlılık etiketi taşıyan veya şifreli öğelerin atlanması
  - Sağlık, sendika, din, ceza terimleri için yerel sınıflandırıcı
  - "Kişisel/Özel" kategorisi veya klasörüyle çıkış (opt-out)
  - Bu içerik saklanmaz, atılır
- S4 kabul kriteri: hariç tutulan test öğelerinin %100'ü LLM'e ulaşmaz.

## 5. Çalışan e-postası ve içtihat

| Karar | Tarih | Sonuç | Belirleyici husus |
|---|---|---|---|
| AYM 2013/4825 | 24 Mar 2016 | İhlal yok | Önceden bilgilendirme yapılmıştı |
| AYM 2016/13010 | 17 Eyl 2020 | **İhlal** | Açık önceden bilgilendirme olmadan inceleme |
| AYM 2018/31036 | 12 Oca 2021 | İhlal yok | Açık sözleşmesel bildirim; ölçülü kullanım |
| Yargıtay 22. HD E. 2017/21857 K. 2019/9884 | 7 May 2019 | — | İşveren izlemesi önceden bildirim gerektirir |
| KVKK Kurulu 2021/1187 | 25 Kas 2021 | **250.000 TL** idari para cezası | Önceden bilgilendirme olmadan kurumsal posta kutusuna erişim |
| KVKK Kurulu 2023/86 | 19 Oca 2023 | İhlal yok | Çalışanın imzalı politika referansı belirleyici oldu; md. 5(2)(e) ve (f) |
| AİHM Bărbulescu / Romanya (BD) | 5 Eyl 2017 | İhlal | Bildirim, kapsam, meşru gerekçe, daha az müdahaleci yöntem, sonuçlar, güvenceler |

AYM 12 Ocak 2021 ölçütleri: yönetim hakkı amaçlarıyla sınırlılık; meşru gerekçe; kapsam, hukuki dayanak, saklama ve haklar dahil **önceden bilgilendirme**; ilgililik ve elverişlilik; gereklilik (daha az müdahaleci yol yoksa); amaçla sınırlılık; menfaatler arasında adil denge ([Erdem & Erdem](https://www.erdem-erdem.av.tr/bilgi-bankasi/isverenin-calisanin-e-postalarini-denetlemesi-1212021-tarihli-anayasa-mahkemesi-karari-ile-getirilen-kistaslar)).

**Sonuç:** Uygulama içeriği sürekli ve ölçekli işler; bu Bărbulescu/AYM yelpazesinin en müdahaleci ucudur. Bu yüzden **imzalı aydınlatma ve BT/AI kullanım politikası canlıya geçişin önkoşuludur** ve yol haritasında bir sprint kabul kriteridir (S8). Daha az müdahaleci tasarım: ortak/proje posta kutuları ve proje siteleriyle başlamak; önce meta veri triage'ı; kaynağın tam kopyası yerine kaynak referanslı çıkarımlar; kullanıcı yalnızca zaten erişebildiği içerikten türetilen analizi görür.

Not: Otomatik (AI) analize ilişkin Yargıtay kararı bulunamamıştır; içtihat insan incelemesine ilişkindir.

## 6. Üretken yapay zekâ rehberleri (yol gösterici)

- **"Üretken Yapay Zekâ ve Kişisel Verilerin Korunması Rehberi (15 Soruda)"**, 24 Kasım 2025 ([KVKK](https://www.kvkk.gov.tr/Icerik/8547/uretken-yapay-zeka-ve-kisisel-verilerin-korunmasi-rehberi-15-soruda)). İkincil özete göre: rollerin somut faaliyete göre belirlenmesi, "yalnızca üretken AI kullanıldığını bildirmek" geçerli açık rıza için yeterli değildir, varsayılan olarak gizlilik, DPIA, kırmızı takım testleri, veri haritalama. Rehberin tam metni incelenmedi.
- **"İş Yerlerinde Üretken Yapay Zekâ Araçlarının Kullanımı"**, 5 Mart 2026 ([KVKK](https://www.kvkk.gov.tr/Icerik/8674/is-yerlerinde-uretken-yapay-zeka-araclarinin-kullanimi)) — "gölge AI" uyarısı.

## 7. Gerekli belgeler (iş paketi)

Tümü **hukuk danışmanı onayı** gerektirir. Faz 0'da başlatılır, S8'de (pilot öncesi) tamamlanır.

| # | Belge | Dosya | Durum |
|---|---|---|---|
| 1 | Hukuki dayanak notu ve meşru menfaat denge testi (md. 5(2)(f)/(e)); özel nitelikli veri dışlama tasarımı (md. 6) | [mesru-menfaat-testi.md](mesru-menfaat-testi.md) | Taslak başlıklar |
| 2 | Çalışan aydınlatma metni, BT/e-posta izleme ve AI kullanım politikası güncellemesi, imzalı onaylar | [aydinlatma-sablonu.md](aydinlatma-sablonu.md) | Taslak başlıklar |
| 3 | Birleşik DPIA / KVKK risk değerlendirmesi (AB kuruluşları varsa GDPR dahil); AI Act kapsam notu | [dpia.md](dpia.md), [ai-act-kapsam.md](../ai-act-kapsam.md) | Taslak başlıklar |
| 4 | VERBİS güncellemesi; saklama ve imha politikası güncellemesi | [verbis-girdileri.md](verbis-girdileri.md) | Taslak başlıklar |
| 5 | Yurt dışı aktarım paketi: sağlayıcı envanteri, SS-2 (gerekirse SS-3), 5 iş günü bildirim, değişiklik/fesih takibi | [ss2-takip.md](ss2-takip.md) | Taslak başlıklar |
| 6 | İlgili kişi hakları süreci (kişi bazında arama/dışa aktarma/silme, 30 gün) ve kayıp dizüstü dahil ihlal runbook'u | [veri-sahibi-talepleri.md](veri-sahibi-talepleri.md) | Taslak başlıklar |
| 7 | DPO'nun canlıya geçiş kontrol listesi (S8 kabul kriteri) | Bu dizinde Faz 1'de oluşturulacak | Planlandı |

## 8. Açık noktalar (kaynaklarda doğrulanamayanlar)

- 2026 yılı için yeniden değerlenmiş md. 18 ceza tutarları.
- Silme/yok etme yönetmeliğinin (RG 28.10.2017) ayrıntıları (periyodik imha süresi vb.) — arka plan bilgisi, doğrulanmadı.
- Kurul'un ihlal bildirimi için yaygın olarak atıf yapılan 72 saat kararı (2019/10) alınamadı; md. 12(5) yalnızca "en kısa sürede" der.
- Microsoft, Anthropic ve OpenAI'ın Eylül 2026 itibarıyla SS-2 imzalayıp imzalamadığı; doğrudan sağlayıcılara sorulmalı.
- AB için KVKK yeterlilik kararı yokluğu kapsamlı olarak doğrulanamadı.
