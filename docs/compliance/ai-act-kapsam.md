# AB AI Act Kapsam Değerlendirmesi

> **Bu doküman hukuki tavsiye değildir.** Mühendislik ve ürün tasarımı gerekçesidir. **Hukuk danışmanı onayı gerekir.**

*Kaynak: [araştırma raporu §4 — AB AI Act bağlamı](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [güvenlik ve uyum notları §4](../research/notes/guvenlik_uyum.md). Tarih: 27 Eylül 2026.*

## 1. Uygulanabilirlik

AI Act bir Türk şirketine ancak **AB bağlantısı** olduğunda uygulanır (Art. 2(1)): sistemi AB pazarına sunmak, bir AB kuruluşunda kullanmak veya çıktının AB'de kullanılması. Yalnızca Türk kuruluşu içinde Türk çalışanlarla kullanımda doğrudan uygulanmaması muhtemeldir (hukuk danışmanı teyidi). AB iştiraklerinde kullanım veya AB müşterilerine satış durumunda şirket kapsamdaki sağlayıcı/uygulayıcı olur.

Türkiye'de yürürlükte yapay zekâya özgü bir kanun yoktur; TBMM'de teklifler ve Mart 2026 araştırma komisyonu raporu vardır ([güvenlik notları §4](../research/notes/guvenlik_uyum.md)).

## 2. Önemli tarihler (Digital Omnibus sonrası)

| Tarih | Yükümlülük |
|---|---|
| Şubat 2025 | Art. 5 yasakları yürürlükte |
| Ağustos 2025 | GPAI sağlayıcı yükümlülükleri |
| 27 Temmuz 2026 | Digital Omnibus on AI (Tüzük (AB) 2026/1744) yürürlüğe girdi (AB Resmî Gazetesi: 24 Temmuz 2026) |
| 2 Ağustos 2026 | Art. 50 şeffaflık yükümlülükleri |
| 2 Aralık 2026 | Art. 50(2) makinece okunabilir işaretleme için 2 Ağustos 2026 öncesi piyasaya sunulan sistemlere geçiş süresi sonu |
| **2 Aralık 2027** | **Annex III (bağımsız) yüksek risk yükümlülükleri** ([Hunton](https://www.hunton.com/privacy-and-cybersecurity-law-blog/eu-digital-omnibus-on-ai-enters-into-force)) |
| 2 Ağustos 2028 | Annex I (ürüne gömülü) yüksek risk yükümlülükleri |

Not: Bir ikincil özet Annex III için 2 Ağustos 2028 tarihini vermiştir; Hunton, Gibson Dunn ve CSA 2 Aralık 2027 dediği için bu bir çıkarma hatası olarak değerlendirilmiştir.

## 3. Annex III 4(b) ve kapsam dışı tasarım gerekçesi

Annex III 4(b), iş ilişkilerinde "to allocate tasks based on individual behaviour or personal traits or characteristics or to monitor and evaluate the performance and behaviour of persons" amaçlı sistemleri yüksek riskli sayar ([Annex III](https://artificialintelligenceact.eu/annex/3/)). Art. 6(3) istisnası (dar prosedürel görev, hazırlayıcı görev vb.) vardır, ancak **gerçek kişilerin profillemesini yapan bir Annex III sistemi her zaman yüksek risklidir** ([Art. 6](https://artificialintelligenceact.eu/article/6/)).

OpsIntel bu kapsamın **dışında** tasarlanır ([ADR-0023](../adr/0023-no-individual-performance-scoring.md)):

| Tasarım kararı | Gerekçe |
|---|---|
| Kullanım amacı: "proje/operasyon bilgisi çıkarımı ve insan onayına sunulan taslaklar" | Amaç beyanı sınıflandırmayı belirler |
| Kişi bazlı verimlilik, yanıt hızı, güvenilirlik veya duygu puanı yok | "monitor and evaluate the performance and behaviour of persons" dışında kalmak |
| Bireyleri sıralayan yönetici panosu yok | Aynı |
| Geçmiş davranışa/özelliklere göre otomatik görev ataması yok | "allocate tasks based on individual behaviour" dışında kalmak |
| Görev sahibi yalnızca metinde açıkça adlandırıldığında, proje bağlamında | Profilleme yapmamak |
| Risk ve durum proje düzeyinde toplulaştırılır; ekip/org metriklerinde en az 5 kişilik grup (Viva Insights örneği) | Kişi düzeyinde değerlendirme yapmamak |
| Çıktılar disiplin/performans/fesih amacıyla kullanılmaz (aydınlatma metninde) | Amaçla sınırlılık |
| Kişiler hakkında duygu/ton çıkarımı yok | İş yerinde duygu tanıma yasağı ve profilleme riskinden uzak durmak (Art. 5(1)(f) metni doğrulanmadı) |

KVKK md. 11(1)(g)'deki "münhasıran otomatik analiz" itiraz hakkı da aynı yöne işaret eder.

## 4. Kapsam içindeyse geçerli olacak yükümlülükler

AB bağlantısı varsa:

- **Art. 4** yapay zekâ okuryazarlığı (Omnibus sonrası "destekleme" tedbirleri) — şimdiden.
- **Art. 50(1)** kullanıcıya yapay zekâ ile etkileşimde olduğunu bildirmek (arayüzde) — şimdiden.
- **Art. 50(2)** taslak metinlerin işaretlenmesi "assistive function for standard editing" istisnasına girebilir; yine de AI taslakları etiketlenir.
- Tasarım 4(b)'ye kayarsa: 2 Aralık 2027'den itibaren yüksek risk yükümlülükleri; uygulayıcı işveren için Art. 26(7) çalışan temsilcilerini bilgilendirme ve Art. 26(2) insan gözetimi.

## 5. Planlanan belgeler

- AI Act uygulanmasa bile **Art. 6(3) tarzı bir değerlendirme** kayda geçirilir (ucuz bir sigorta ve KVKK üretken AI rehberinin belgeleme beklentisiyle uyumlu).
- Faz 3'te (S22–S23) "AI Act md. 6(3) belgesi" ve **2 Aralık 2027'den önce** AI Act değerlendirmesi ([yol haritası](../roadmap/roadmap.md)).
- Yeni özellikler (KPI kataloğu, anomali uyarıları, sağlık skorları) bu belgeye karşı PR incelemesinde kontrol edilir.

## 6. Açık noktalar

- Art. 5(1)(f) (iş yerinde duygu tanıma) metni ve metin tabanlı duygu analizinin kapsam dışında kalıp kalmadığı doğrulanmadı.
- Komisyonun Art. 6 sınıflandırma kılavuzları ve Omnibus sonrası durumu alınamadı.
- 2026 kapsamlı Türk AI kanun teklifinin numarası veya komisyon aşaması doğrulanmadı.
