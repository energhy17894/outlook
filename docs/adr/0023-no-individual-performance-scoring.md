# ADR-0023: Kişi bazlı performans/duygu puanlaması yok; organizasyon metriklerinde en az 5 kişilik grup

- **Durum:** Kabul edildi (**hukuk danışmanı onayı bekleniyor**)
- **Tarih:** 2026-09-27
- **Karar vericiler:** Ürün sahibi, KVKK danışmanı/DPO, teknik lider
- **İlgili ADR'ler:** [0016](0016-project-registry-phase-fsm.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

- **AB AI Act Annex III 4(b)**, "monitor and evaluate the performance and behaviour of persons" amaçlı sistemleri yüksek riskli sayar ([Annex III](https://artificialintelligenceact.eu/annex/3/)). Digital Omnibus ile Annex III yükümlülükleri **2 Aralık 2027**'ye ertelendi ([Hunton](https://www.hunton.com/privacy-and-cybersecurity-law-blog/eu-digital-omnibus-on-ai-enters-into-force)). Profilleme yapan Annex III sistemi her zaman yüksek risklidir (Art. 6(3)).
- **KVKK md. 11(1)(g):** münhasıran otomatik analiz sonucu kişinin aleyhine bir sonuç çıkmasına itiraz hakkı.
- **AYM 12 Ocak 2021** ölçütleri: önceden bilgilendirme, meşru amaç, ölçülülük, daha az müdahaleci yöntem, amaçla sınırlılık; içerik izleme daha ağır gerekçe ister.
- **Viva Insights** en az 5 kişilik grup eşiği uygular ve bu eşik düşürülemez ([Viva gizlilik](https://learn.microsoft.com/en-us/viva/insights/Privacy/Privacy-considerations)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Kişi bazlı puanlama yok; ekip/org metriklerinde ≥5 kişi (seçilen)** | Annex III 4(b) dışında kalma; AYM ölçülülüğü | Bazı "yönetici panosu" talepleri karşılanmaz |
| Kişi bazlı verimlilik/yanıt hızı panosu | Talep edilebilir | Yüksek riskli AI; KVKK itiraz ve çalışan izleme riski |
| Duygu/ton puanı | "Erken uyarı" | Kişi hakkında çıkarım; Art. 5(1)(f) kapsamı doğrulanmadı |

## Karar

- Ürün **kişi bazlı verimlilik, yanıt hızı, güvenilirlik veya duygu puanı üretmez**; **bireyleri sıralayan yönetici panosu yoktur**; kişilerin geçmiş davranışına göre otomatik görev ataması yapılmaz.
- Görev sahibi yalnızca metinde açıkça adlandırıldığında çıkarılır ve proje bağlamında gösterilir.
- Kişisel metrikler ("benim yanıt sürelerim") yalnızca kullanıcının kendisine görünür.
- Ekip ve organizasyon metriklerinde **en az 5 kişilik grup** eşiği uygulanır; daha küçük gruplar gizlenir.
- Organizasyon metrikleri (taahhüt güvenilirliği, iletişim yükü) varsayılan olarak **karşı taraf kurum** (müşteri/tedarikçi) düzeyindedir.
- Eskalasyon dili gibi sinyaller proje/başlık düzeyinde sağlık sürücüsüdür, kişiye atfedilmez.
- Kullanım amacı belgelerde "proje/operasyon bilgisi çıkarımı ve insan onayına sunulan taslaklar" olarak yazılır; çıktılar disiplin, performans veya fesih amacıyla kullanılmaz (aydınlatma metnine girer).

## Sonuçlar

### Olumlu

- AI Act Annex III 4(b) kapsamı dışında kalma gerekçesi güçlenir ([ai-act-kapsam.md](../compliance/ai-act-kapsam.md)).
- Çalışan izleme itirazı riskini azaltır (risk kaydı: Orta/Yüksek).

### Olumsuz

- Rakiplerin bazı "ekip performansı" özellikleri bilinçli olarak sunulmaz.
- Yeni özellikler (ör. Faz 3 KPI kataloğu, anomali uyarıları) bu ADR'ye karşı incelenmelidir.

## Doğrulama / açık noktalar

- **Hukuk danışmanı onayı** (KVKK/iş hukuku): kararın aydınlatma metni, meşru menfaat testi ve DPIA ile tutarlılığı.
- PR şablonundaki kontrol listesi maddesi her özellikte uygulanır.
- Metinden duygu çıkarımının Art. 5(1)(f) iş yerinde duygu tanıma yasağı dışında kalıp kalmadığı doğrulanmadı.

## Kaynaklar

- [Araştırma raporu §4 — AB AI Act bağlamı](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Güvenlik notları §2, §4](../research/notes/guvenlik_uyum.md), [Özellikler/UX notları §6](../research/notes/ozellikler_ux.md)
- [KVKK](../compliance/kvkk/README.md)
