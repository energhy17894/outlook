# ADR-0015: Çıkarım sözleşmesi — küçük düz şemalar, zorunlu alıntı kanıtı, deterministik doğrulama, "needs review" durumu

- **Durum:** Kabul edildi (eşikler Faz 0 spike E ölçümüyle kalibre edilecek)
- **Tarih:** 2026-09-27
- **Karar vericiler:** AI/NLP mühendisi, teknik lider, ürün sahibi
- **İlgili ADR'ler:** [0013](0013-microsoft-extensions-ai-maf.md), [0014](0014-foundry-local-model-hosting.md), [0016](0016-project-registry-phase-fsm.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

- ExtractBench: öncü modeller gerçekçi şemalarda güvenilmez; 369 alanlı şemada %0 geçerli çıktı ([arXiv 2602.12247](https://arxiv.org/abs/2602.12247)).
- Anthropic Citations API geçerli işaretçiler garanti eder, ancak yapılandırılmış çıktıyla birlikte HTTP 400 döner ([Claude Citations](https://platform.claude.com/docs/en/build-with-claude/citations)).
- MiniCheck sınıfı küçük NLI denetleyici, GPT-4 düzeyinde topraklama doğruluğuna yaklaşık 400 kat düşük maliyetle ulaşır ([arXiv 2404.10774](https://arxiv.org/abs/2404.10774)).
- Kullanıcılar atıf bağlantılarına nadiren tıklar ([NN/g](https://www.nngroup.com/articles/explainable-ai/)); alıntı kartta satır içi gösterilmelidir.
- Ürünün kalitesi modelden çok **doğrulama disiplinine** bağlıdır (rapor, Sonuç).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Küçük düz şemalar + zorunlu alıntı + yerel deterministik doğrulama (seçilen)** | Sağlayıcıdan bağımsız; yerel zayıf modellerle bile güvenilir kayıt | Birden çok çağrı; alıntı eşleştirme normalizasyonu gerekir |
| Tek büyük şema | Tek çağrı | Güvenilmez (ExtractBench) |
| Sağlayıcı Citations API | Geçerli işaretçi garantisi | Yapılandırılmış çıktıyla birleşmez; sağlayıcıya bağımlı |

## Karar

- Çıkarım **küçük ve düz JSON şemalarıyla** yapılır; ayrı çağrılar: (1) karar/risk/açık soru; (2) taahhüt/talep/aksiyon; (3) proje sinyali.
- Her öğe **`{message_id, quote}` kanıtı** taşımak zorundadır.
- **Doğrulama merdiveni:** alıntı, temizlenmiş metinde **tr-TR normalizasyonuyla** (İ/ı duyarlı) deterministik olarak eşleşir → mesajın başlığa aitliği → yazar kontrolü → (opsiyonel) MiniCheck sınıfı NLI.
- Karakter ofsetleri yerelde hesaplanır; `Evidence` W3C seçicileriyle saklanır.
- **Doğrulanamayan öğe asla "gerçek" olarak gösterilmez**; `review_state = suggested` / "needs review" durumunda kalır.
- Güven kategorik gösterilir (High/Med/Low); düşük güvende N-best alternatifler.
- Prompt ve şemalar `prompts/` altında sürümlenir; her çalıştırma `ExtractionRun` (model_id, model_hash, prompt_version) kaydı üretir. İdempotency anahtarı: içerik hash'i + prompt sürümü.
- Türkçe tarih normalizasyonu ("Cuma'ya kadar", "ay sonu") ve TR iş takvimi sözleşmenin parçasıdır.

## Sonuçlar

### Olumlu

- Atıf geçerliliği yapısal olarak %100 garanti edilir.
- İnceleme kuyruğundaki her kabul/ret gelecekteki ince ayar için ücretsiz etikettir.

### Olumsuz

- Birden çok çağrı token maliyetini ve süreyi artırır.
- `uniqueBody` bazen tüm konuşmayı içerebilir; kendi TR/EN soyucumuz gerekir.

## Doğrulama / açık noktalar

- S5 kabul kriterleri: gösterilen öğelerin %100'ünün alıntısı kaynakta birebir; madde düzeyi F1 taahhüt/talep ≥ 0,65, karar/risk ≥ 0,60; termin ±1 gün ≥ 0,80; enjeksiyon kaynaklı sahte öğe < %5.
- Pilot hedefleri: [kalite hedefleri](../roadmap/quality-targets.md).

## Kaynaklar

- [Araştırma raporu §1 — Yapay zekâ katmanı; §6 — Kalite hedefleri](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [AI mimarisi notları §2, §3, §10](../research/notes/ai_agent_mimarisi.md), [Özellikler/UX notları §5](../research/notes/ozellikler_ux.md)
- [Veri modeli](../architecture/data-model.md)
