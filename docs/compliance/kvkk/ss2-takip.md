# SS-2 Standart Sözleşme Takibi

> **Bu doküman hukuki tavsiye değildir.** Taslak başlık yapısıdır. **Hukuk danışmanı onayı gerekir.**

**Amaç:** Bulut LLM sağlayıcılarına (katman 2/3) yapılacak düzenli yurt dışı aktarım için KVKK md. 9(4)(c) kapsamında standart sözleşmelerin (SS-2; gerekirse alt işleyenler için SS-3) imzalanmasını, **imzadan itibaren 5 iş günü içinde** Kurum'a bildirilmesini ve değişiklik/fesih bildirimlerini takip etmek. Bu tablo, OpsIntel'in bulut katmanı kilidinin (yönetici kontrol listesi) hukuki dayanağıdır ([ADR-0014](../../adr/0014-foundry-local-model-hosting.md)).

*Dayanak: [KVKK README](README.md) §3; [güvenlik notları §1, §8](../../research/notes/guvenlik_uyum.md).*

## 1. Sağlayıcı envanteri ve değerlendirme kaydı

| Sağlayıcı | Sözleşme tarafı (tüzel kişi) | Veri işleyen rolü | Alt işleyenler | Saklama (varsayılan / ZDR / işaretli içerik) | İnsan incelemesi konumu | Eğitimde kullanım | Çıkarım/depolama coğrafyası | SS-2 durumu | Sertifikalar |
|---|---|---|---|---|---|---|---|---|---|
| Microsoft (M365 kiracısı) | | | | | | | | Teyit edilecek | |
| Azure OpenAI (katman 2) | | | | | | | | | |
| Anthropic (katman 3) | | | | | | | | | |
| OpenAI (katman 3) | | | | | | | | | |

## 2. Sözleşme ve bildirim takibi

| Sağlayıcı | Sözleşme türü | İmza tarihi | Bildirim son günü (5 iş günü) | Bildirim yöntemi (KEP / modül / fiziksel) | Bildirim tarihi | Kanıt |
|---|---|---|---|---|---|---|
| | SS-2 | | | | | |

## 3. Değişiklik ve fesih bildirimleri

| Sağlayıcı | Olay | Tarih | Bildirim tarihi |
|---|---|---|---|
| | | | |

## 4. Yıllık sağlayıcı gözden geçirmesi

## 5. Bilinen belirsizlikler

- Microsoft'un Türk müşterilerle SS imzalama pratiği Aralık 2025 itibarıyla belirsizdi.
- "Claude in Microsoft Foundry"de veri işleyen Anthropic'tir; Microsoft taahhütleri kapsamaz.
- Anthropic "Covered Models" 30 gün saklamayı zorunlu kılar; birinci taraf API'sinde AB çıkarım bölgesi yoktur.
