# İlgili Kişi (Veri Sahibi) Talepleri ve İhlal Süreci

> **Bu doküman hukuki tavsiye değildir.** Taslak başlık yapısıdır. **Hukuk danışmanı onayı gerekir.**

**Amaç:** KVKK md. 11 kapsamındaki ilgili kişi başvurularının (bilgi talebi, düzeltme, silme, itiraz) OpsIntel'in yerel depolarında **en geç 30 gün içinde** (md. 13) karşılanabilmesi için süreci ve teknik gereksinimleri tanımlamak; md. 12 kapsamında kayıp dizüstü dahil ihlal müdahale adımlarını belgelemek.

*Dayanak: [KVKK README](README.md) §2; [güvenlik notları §1, §6](../../research/notes/guvenlik_uyum.md); [veri modeli](../../architecture/data-model.md).*

## 1. Başvuru kanalları ve kayıt

## 2. Kimlik doğrulama

## 3. Teknik gereksinimler (ürün)

- Kişi bazında arama: türetilmiş kayıtlar, kanıtlar, embedding'ler, önbellekler, loglar
- Dışa aktarma
- Düzeltme
- Silme (kaynaktan yeniden türetmenin engellenmesi dahil)
- Denetim kaydıyla ilişki (payload'da kişisel veri tutulmaması, [ADR-0018](../../adr/0018-hash-chained-audit-log.md))
- Çok PC'li kurulumlarda talebin tüm cihazlara uygulanması (açık nokta)

## 4. md. 11(1)(g) otomatik analize itiraz

## 5. Yanıt şablonları ve süre takibi (30 gün)

## 6. İhlal müdahale runbook'u

- Kayıp/çalıntı dizüstü: Intune protected wipe, Entra oturum/refresh token iptali, anahtar rotasyonu
- Şifreleme durumunun BitLocker/Intune kanıtıyla teyidi; teyit edilene kadar potansiyel ihlal olarak ele alma
- Kurul'a ve ilgili kişilere "en kısa sürede" bildirim (süre yorumu hukuk danışmanıyla netleştirilecek)

## 7. Onaylar

| Rol | Ad | Tarih | İmza |
|---|---|---|---|
| Hukuk danışmanı | | | |
| DPO | | | |
