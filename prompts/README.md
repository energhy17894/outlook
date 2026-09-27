# prompts/

> **Durum:** Planlama aşaması — bu dizinde henüz prompt yoktur. İlk şablonlar **S4–S5**'te (triage ve kanıtlı çıkarım) eklenecektir; biçim Faz 0'da kararlaştırılacaktır.

Bu dizin **sürümlenmiş prompt şablonlarını ve çıkarım JSON şemalarını** tutar. Promptlar kod gibi ele alınır ve **kod incelemesinden geçer**. Kaynak: [araştırma raporu §1, §5](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md), [ADR-0015](../docs/adr/0015-extraction-contract-evidence.md).

## Planlanan içerik

- Triage promptu (aksiyon gerektiren / bildirim / bülten)
- Üç küçük, düz çıkarım şeması ve promptu:
  1. karar / risk / açık soru
  2. taahhüt / talep / aksiyon
  3. proje sinyali
- Proje ataması (ilk 3–5 aday + "yeni/hiçbiri")
- Soru-Cevap sistem talimatı (salt-okunur araçlar)

## Kurallar

- Her öğe `{message_id, quote}` kanıtı taşımak zorundadır; şemalar bunu zorunlu kılar.
- Güvenilmeyen içerik **spotlighting/datamarking** ile işaretli bloklara sarılır; talimat metni içerikteki talimatların veri olduğunu belirtir ([ADR-0019](../docs/adr/0019-untrusted-content-quarantine.md)).
- Prompt sürümü, `ExtractionRun` kaydına ve idempotency anahtarına (içerik hash'i + prompt sürümü) girer; prompt değişikliği yeniden çıkarım demektir.
- Promptlarda sır veya gerçek e-posta örneği bulunmaz; few-shot örnekleri sentetiktir.
- Prompt değişiklikleri eval ve kırmızı takım korpusunu çalıştırmayı gerektirir (PR şablonu).
