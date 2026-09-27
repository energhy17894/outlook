# ADR-0013: AI soyutlama — Microsoft.Extensions.AI; MAF 1.x yalnızca Soru-Cevap; yeni kodda Semantic Kernel yok

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, AI/NLP mühendisi
- **İlgili ADR'ler:** [0014](0014-foundry-local-model-hosting.md), [0015](0015-extraction-contract-evidence.md), [0017](0017-approval-state-machine.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

- Sağlayıcıdan bağımsız `IChatClient` / `IEmbeddingGenerator` soyutlamaları (Microsoft.Extensions.AI) GA'dır ([MS Learn](https://learn.microsoft.com/en-us/dotnet/ai/microsoft-extensions-ai)).
- **Microsoft Agent Framework 1.0** Nisan 2026'da GA oldu ([MAF 1.0](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/)); Semantic Kernel bakım modundadır ([SK #13215](https://github.com/microsoft/semantic-kernel/discussions/13215)).
- Çekirdeğin otonom ajan olmaması için üç kanıt: ExtractBench'te öncü modeller gerçekçi şemalarda "remain unreliable" (369 alanlı şemada %0 geçerli çıktı; [arXiv 2602.12247](https://arxiv.org/abs/2602.12247)); gelen e-posta saldırgan girdisidir (EchoLeak); OWASP Agentic Top 10 "Agent Goal Hijack" ve "Tool Misuse"ı ilk sıralara koyar.
- MAF checkpoint deposu "is a trust boundary … Never load checkpoints from untrusted or potentially tampered sources" ([MAF Checkpoints](https://learn.microsoft.com/en-us/agent-framework/workflows/checkpoints)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **MEAI + deterministik boru hattı; MAF yalnız Soru-Cevap (seçilen)** | Öngörülebilir, test edilebilir, sağlayıcı değiştirilebilir | Ajan esnekliğinden vazgeçilir |
| Tam otonom ajan (MAF) | Esnek | Enjeksiyon/aşırı yetki riski, şema güvenilmezliği |
| Semantic Kernel | Tanıdık | Bakım modu; yeni özellikler yalnız MAF'ta |
| LangGraph / Pydantic AI | Olgun | Python; ikinci çalışma zamanı |

## Karar

- Tüm model çağrıları **Microsoft.Extensions.AI** (`IChatClient`, `IEmbeddingGenerator`) üzerinden yapılır; yerel ve bulut sağlayıcılar aynı arayüzle takılır.
- Çekirdek, **deterministik bir iş akışıdır**: ingest → temizle → triage → çıkar → doğrula → sakla → öner. İçerik okuyan çağrılara araç verilmez.
- **MAF 1.x** yalnızca bilgi tabanı üzerinde **salt-okunur araçlarla** etkileşimli Soru-Cevap ajanı için kullanılır; ajan M365'e yazamaz ve aksiyon yürütemez.
- MAF checkpoint'leri yalnızca operasyonel durumdur; onay akışının kaynak-gerçeği kendi tablolarımızdır ([ADR-0017](0017-approval-state-machine.md)).
- Yeni kodda Semantic Kernel kullanılmaz.

## Sonuçlar

### Olumlu

- Sağlayıcı katmanları (Foundry Local, Azure OpenAI, Anthropic/OpenAI) yapılandırmayla değişir.
- Boru hattı adımları ayrı ayrı değerlendirilebilir ve sürümlenebilir.

### Olumsuz

- FIDES (bilgi akışı denetimi) yalnızca Python'da ve deneysel; .NET'te aynı ilke süreç ayrımı ve şema kısıtıyla uygulanır.
- MAF .NET'te dayanıklı checkpoint deposu (yalnızca `CreateInMemory` gösteriliyor) doğrulanmadı; gerekirse DB tabanlı özel depo.

## Doğrulama / açık noktalar

- S7 teslimatı: "Sor" (iddia bazında atıflı Soru-Cevap); ajan araç listesinin salt-okunur olduğu mimari testiyle doğrulanır.

## Kaynaklar

- [Araştırma raporu §1 — Yapay zekâ katmanı](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [AI mimarisi notları §7](../research/notes/ai_agent_mimarisi.md), [Teknoloji yığını notları §2, §5](../research/notes/teknoloji_yigini.md)
- [OWASP Agentic Top 10 2026](https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications-for-2026/)
