# ADR-0016: Proje/aşama — kayıt defterine karşı sınıflandırma + yapılandırılabilir aşama FSM'i; kümeleme yalnızca öneri üretir

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** AI/NLP mühendisi, ürün sahibi, teknik lider
- **İlgili ADR'ler:** [0015](0015-extraction-contract-evidence.md), [0017](0017-approval-state-machine.md), [0023](0023-no-individual-performance-scoring.md)

## Bağlam

- Taahhüt tespit modelleri alan kaymasında (domain shift) belirgin biçimde bozulur ([Azarbonyad vd., WSDM 2019](https://www.microsoft.com/en-us/research/publication/domain-adaptation-for-commitment-detection-in-email/)); kiracıya özgü bir kayıt defteri yaklaşımı daha sağlamdır.
- E-posta sinyalinden genel B2B proje aşaması tespitini doğrulayan hakemli bir çalışma bulunamadı; aşama kataloğu bir tasarım çıkarımıdır ([özellikler/UX notları §2](../research/notes/ozellikler_ux.md)).
- Aşama geçişleri artefakt ve söz eylemi sinyallerinden (teklif, PO/sözleşme, kickoff daveti, UAT, teslim, fatura) çıkarılır; PMBOK "stage gate" kavramı onay noktalarını tanımlar.
- Gong tarzı açıklanabilir sağlık uyarıları (No Activity, Ghosted, Stalled Progress) eşikleri kullanıcı ayarlıdır ([Gong](https://help.gong.io/docs/customize-your-deal-warning-settings)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Kayıt defterine karşı sınıflandırma + FSM + onaylı öneriler (seçilen)** | Kararlı, açıklanabilir, kullanıcı kontrolünde | Kullanıcı kayıt defterini düzenlemeli |
| Serbest kümeleme ile otomatik proje | Sıfır kurulum | Kararsız kümeler, alan kayması, açıklanamaz |
| LLM'in aşamayı doğrudan yazması | Basit | Kural dışı sıçramalar, kanıtsız değişiklik |

## Karar

- **Proje ataması** kullanıcının düzenlediği bir **proje kayıt defterine** (ad, takma adlar, PO/sözleşme kodları, müşteri org, üye listesi) karşı sınıflandırmadır. Adaylar katılımcı örtüşmesi, konu kodları ve embedding kNN ile seçilir; LLM ilk 3–5 aday ile "yeni/hiçbiri" arasından seçer.
- Atanamayan başlıklar bir havuzda birikir; periyodik kümeleme **yalnızca kullanıcı onayına sunulan yeni proje önerileri** üretir.
- **Aşama**, yapılandırılabilir bir yaşam döngüsü şablonuna bağlı **sonlu durum makinesidir**; kural dışı sıçramalar reddedilir. Aşama olasılık dağılımı + kanıt listesi olarak modellenir; gösterilen aşama yalnızca güven eşiği aşıldığında veya kullanıcı bir "kapı kartında" onayladığında değişir.
- Her geçiş `PhaseTransition` (from→to, olasılık, evidence_ids, inferred/confirmed) ve OCEL biçimli `Event` olarak kaydedilir.
- **Sağlık** açıklanabilir bir bileşiktir; sürücüler: ilişki bazına göre sessizlik, cevapsız kalma, açık soru yaşı, geciken taahhüt, duran aşama. Her sürücü kanıtına bağlanır; eşikler ayarlanabilir; iş günü/tatil takvimiyle normalize edilir.
- RAID/RAIDD adlandırması yapılandırılabilir.

## Sonuçlar

### Olumlu

- Kullanıcı kontrolü ve açıklanabilirlik; yanlış atama tek tıkla düzeltilir ve etiket olur.
- Zaman çizelgesi, süreç haritası ve OCEL dışa aktarımı aynı olay tablosundan beslenir.

### Olumsuz

- İlk kurulumda kayıt defteri oluşturma zahmeti (öneriler bunu azaltır).
- Aşama kataloğu ve eşikler kuruma göre ayarlanmalı; doğruluğu pilotta ölçülmeli.

## Doğrulama / açık noktalar

- S6 kabul kriterleri: bilinen projelerde atama doğruluğu ≥ 0,85; güven eşiği aşılmadan veya kullanıcı onayı olmadan aşama değişmez; her sağlık sürücüsü tıklanınca kanıtına gider.
- Pilot hedefleri: proje atama ≥ 0,85, aşama ≥ 0,75.
- Sağlık metrikleri kişi değil proje/organizasyon düzeyindedir ([ADR-0023](0023-no-individual-performance-scoring.md)).

## Kaynaklar

- [Araştırma raporu §3 — Proje ataması; §6 — S6](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Özellikler/UX notları §1–§3](../research/notes/ozellikler_ux.md), [AI mimarisi notları §5](../research/notes/ai_agent_mimarisi.md)
- [OCEL 2.0, arXiv 2403.01975](https://arxiv.org/abs/2403.01975)
