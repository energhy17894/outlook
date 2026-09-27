# ADR-0019: Güvenilmeyen içerik karantinası, spotlighting, katı CSP, egress izin listesi

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Güvenlik mühendisi, AI/NLP mühendisi, teknik lider
- **İlgili ADR'ler:** [0001](0001-modular-monolith-two-services.md), [0003](0003-kestrel-loopback-https-6500.md), [0013](0013-microsoft-extensions-ai-maf.md), [0015](0015-extraction-contract-evidence.md), [0017](0017-approval-state-machine.md)

## Bağlam

Ürünün çekirdek girdisi (gelen e-posta, paylaşılan belgeler) saldırgan kontrolündedir. EchoLeak (CVE-2025-32711), tek bir e-postayla M365 Copilot'tan sıfır tıklamalı veri sızdırdı: sınıflandırıcıyı atlatan talimatlar → referans tarzı Markdown bağlantıları → otomatik yüklenen görseller → CSP'deki izinli proxy ([arXiv 2509.10540](https://arxiv.org/html/2509.10540v1)). OWASP Agentic Top 10 (2026) "Agent Goal Hijack"ı ilk sıraya koyar. Spotlighting/datamarking saldırı başarısını %50'nin üzerinden %2'nin altına indirmiştir ([arXiv 2403.14720](https://arxiv.org/abs/2403.14720)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Katmanlı savunma: karantina + spotlighting + güvenli gösterim + egress + yeniden denetim + CI kırmızı takım (seçilen)** | EchoLeak zincirinin her halkasını hedefler | Birden çok kontrolün bakımı |
| Yalnızca enjeksiyon sınıflandırıcısı | Basit | EchoLeak sınıflandırıcıyı atlattı |
| Yalnızca insan onayı | Aksiyonları durdurur | Sızdırma gösterim/egress yoluyla yine olabilir |

## Karar

- **Karantinaya alınmış çıkarıcı:** İçerik okuyan LLM çağrılarına araç verilmez; yalnızca şemaya uygun JSON döner. İçerik okuyan süreçte Graph token'ı yoktur ([ADR-0001](0001-modular-monolith-two-services.md)).
- **Spotlighting / datamarking:** Güvenilmeyen içerik işaretli bloklara sarılır; sistem talimatı içerikteki talimatların komut değil veri olduğunu belirtir. Olası enjeksiyon tespit edilirse risk öğesi olarak kaydedilir.
- **Güvenli çıktı gösterimi:** Model çıktısı düz metin; uzak görsel yüklenmez; bağlantılar otomatik açılmaz; referans tarzı Markdown silinir.
- **Katı CSP:** `default-src 'self'` ile başlar (ör. `img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'` — güvenlik notlarındaki öneri).
- **Egress izin listesi:** Servis SID'ine göre giden bağlantılar yalnızca Graph, oturum açma uç noktaları ve seçilmiş LLM uç noktası.
- **Politikanın yeniden denetimi:** Her aksiyon yürütülmeden önce politika tekrar kontrol edilir; yeni harici alıcı açık onay gerektirir; yapay zekâ ek iliştiremez.
- **Politika kapısı:** Hariç tutulan posta kutuları/klasörler (İK, Hukuk, İSG), etiket kuralları ve yerel özel nitelikli veri sınıflandırıcısı içerik LLM'e gitmeden uygulanır.
- **CI'da kırmızı takım:** LLMail-Inject korpusundan (839 katılımcı, 208.095 saldırı; [arXiv 2506.09956](https://arxiv.org/html/2506.09956v1)) örnekler ve Türkçeye çevrilmiş enjeksiyonlar her sürümde.

## Sonuçlar

### Olumlu

- Enjeksiyon başarılı olsa bile ne araç ne token ne de sızdırma kanalı vardır.
- Kalite hedefi ölçülebilir: enjeksiyon saldırı başarı oranı < %2.

### Olumsuz

- MAF FIDES yalnızca Python/deneysel; .NET'te ilke süreç ayrımı ve şema kısıtıyla uygulanır.
- Türkçe enjeksiyonlarda spotlighting etkinliği yayımlanmış değerlendirmeyle doğrulanmadı.
- Egress kısıtının teknik mekanizması henüz belirlenmedi.

## Doğrulama / açık noktalar

- S5 kabul kriteri: kırmızı takım korpusunda enjeksiyon kaynaklı sahte öğe < %5; pilot hedefi saldırı başarısı < %2.
- S7: CSP testlerinde uzak içerik yüklenmez. S8: localhost pen testi.

## Kaynaklar

- [Araştırma raporu §4 — Yapay zekâya özgü tehditler](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Güvenlik notları §7](../research/notes/guvenlik_uyum.md), [AI mimarisi notları §9](../research/notes/ai_agent_mimarisi.md)
- [OWASP LLM Top 10](https://genai.owasp.org/llm-top-10/), [Tehdit modeli](../architecture/threat-model.md)
