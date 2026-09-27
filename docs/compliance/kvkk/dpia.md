# Veri Koruma Etki Değerlendirmesi (DPIA) / KVKK Risk Değerlendirmesi

> **Bu doküman hukuki tavsiye değildir.** Taslak başlık yapısıdır. **Hukuk danışmanı onayı gerekir.**

**Amaç:** OpsIntel'in işleme faaliyetleri için birleşik bir etki değerlendirmesi yapmak. KVKK'nın üretken yapay zekâ rehberi DPIA önerir; AB kuruluşlarının posta kutuları/siteleri veya çalışanları kapsamdaysa GDPR md. 35 kapsamında DPIA fiilen zorunludur. Tek bir birleşik belge iki rejime de hizmet edebilir.

*Dayanak: [KVKK README](README.md); [güvenlik notları §3, §9](../../research/notes/guvenlik_uyum.md); [tehdit modeli](../../architecture/threat-model.md); [AI Act kapsamı](../ai-act-kapsam.md).*

## 1. İşlemenin sistematik tanımı

- Veri akışı diyagramı (Graph → Host → Intelligence → onay → yürütme)
- Veri kategorileri, veri sahipleri, saklama

## 2. Gereklilik ve ölçülülük değerlendirmesi

- Meşru menfaat testine atıf ([mesru-menfaat-testi.md](mesru-menfaat-testi.md))
- Minimizasyon ve kapsam seçimleri

## 3. Uygulanabilir rejimler

- KVKK
- GDPR (AB kuruluşu/çalışanı var mı? md. 3(1), 3(2)(b))
- AB AI Act (AB bağlantısı var mı? [ai-act-kapsam.md](../ai-act-kapsam.md))

## 4. Risk tanımlama

- Çalışan izleme algısı / itirazı
- Özel nitelikli verinin işlenmesi
- Yurt dışı aktarım
- Cihaz kaybı / ihlal
- Prompt enjeksiyonu ve veri sızdırma
- Yanlış çıkarım (misinformation)

## 5. Risk azaltıcı önlemler

- Teknik (şifreleme, ACL, politika kapısı, egress, içeriksiz log)
- İdari (aydınlatma, politika, eğitim, erişim yetkileri)

## 6. Artık risk ve kabul

## 7. DPO görüşü

## 8. Gözden geçirme takvimi

## 9. Onaylar

| Rol | Ad | Tarih | İmza |
|---|---|---|---|
| Hukuk danışmanı | | | |
| DPO | | | |
| Güvenlik mühendisi | | | |
