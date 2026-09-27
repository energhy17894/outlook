# ADR-0014: Model barındırma — Foundry Local süreç içi varsayılan; katman 2/3 politika ve KVKK kontrol listesiyle

- **Durum:** Önerildi (Faz 0 spike C: Foundry Local'ın servis içinde çalışması; spike E: Türkçe çıkarım ön ölçümü)
- **Tarih:** 2026-09-27
- **Karar vericiler:** AI/NLP mühendisi, teknik lider, KVKK danışmanı
- **İlgili ADR'ler:** [0005](0005-single-msi-wix-v7.md), [0013](0013-microsoft-extensions-ai-maf.md), [0015](0015-extraction-contract-evidence.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

- **Foundry Local** 9 Nisan 2026'da GA oldu; uygulamanın içine gömülen yerel kütüphanedir: "no separate CLI or service required", "small enough to bundle directly inside your application installer" ([Foundry blog](https://devblogs.microsoft.com/foundry/foundry-local-ga/)). Microsoft'a göre "Your data never leaves the device" ([What is Foundry Local](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/what-is-foundry-local)).
- Ollama'nın Windows yükleyicisi kullanıcı başınadır; servis olarak çalıştırmak ayrı sarmalayıcı ister ([Ollama Windows](https://docs.ollama.com/windows)).
- **KVKK md. 9:** bulut LLM'e giden her e-posta içeriği düzenli yurt dışı aktarımdır; SS-2 + 5 iş günü içinde bildirim gerekir ([KVKK](../compliance/kvkk/README.md)).
- **Türkçe:** TurkBench'te Qwen3-30B-A3B-Instruct 73,4; Türkçeye özel TR-Gemma-9b 65,3 ([TurkBench](https://arxiv.org/html/2601.07020v1)). Türkçe metin İngilizceye göre 1,40–2,21 kat daha fazla token tüketir.
- **Kapasite:** RTX 4070 sınıfı GPU'da 8B Q4 ≈ 76 token/sn; günde 200 e-posta ≈ 40 GPU-dakikası. Yalnızca CPU'lu dizüstülerde 30B sınıfı çıkarım pratik değildir.
- Phi Silica Kasım 2026'da Aion ile değiştirilir; Windows AI API'leri çekirdek bağımlılık yapılmaz.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Foundry Local süreç içi (seçilen)** | Ayrı servis/kurulum yok; veri cihazda | Servis hesabı altında çalışması ve VC++ bağımlılığı doğrulanmadı |
| Ollama standalone zip + WinSW | Geniş model desteği | Ek servis sarmalayıcı; yerel HTTP uç noktası |
| Varsayılan bulut (hibrit) | En iyi kalite | KVKK md. 9 aktarımı; SS-2 belirsizliği |
| Windows AI API'leri (Phi Silica/Aion) | İşletim sistemi entegrasyonu | Geçiş döneminde; çekirdek bağımlılık için riskli |

## Karar

- Varsayılan çalışma zamanı **süreç içi Foundry Local** (Intelligence servisinde). Modeller MSI'a gömülmez; ilk çalıştırma sihirbazı indirir veya `MODEL_SOURCE=\\paylaşım\models` ile çevrimdışı yüklenir; `models/model-manifest.json` model ID + SHA-256 sabitler.
- Donanım profiline (GPU/NPU/CPU) göre model katmanı: triage ve embedding için 4–12B sınıfı (Qwen3.5-9B veya Apache 2.0 lisanslı Gemma 4 12B) + `qwen3-embedding-0.6b`; GPU'lu iş istasyonlarında 30B-A3B sınıfı MoE ile tam yerel çıkarım.
- **Sağlayıcı katmanları:**

| Katman | Model konumu | İşleyebileceği içerik | Ön koşullar |
|---|---|---|---|
| 1 (varsayılan) | Foundry Local, süreç içi | Politika kapısını geçen tüm içerik | — |
| 2 | Azure OpenAI, AB bölgesi, Standard veya EU DataZone | Etiket/DLP'den geçmiş, mümkünse takma adlandırılmış içerik | SS-2 imzalı ve bildirilmiş; modified abuse monitoring (`ContentLogging=false`); `store=false`; Global/Batch-Global yok |
| 3 | Anthropic / OpenAI doğrudan | Opsiyonel | ZDR + SS-2; Anthropic "Covered Models" 30 gün saklamayı zorunlu kılar; Anthropic birinci taraf API'sinde AB çıkarım bölgesi yok |

- Katman 2/3, yönetici SS-2'nin imzalandığını ve bildirildiğini kontrol listesiyle onaylamadan **açılamaz**. MVP'de bulut katmanı kapalıdır.
- Tanı paylaşımı politikayla kapatılır.

## Sonuçlar

### Olumlu

- Varsayılan kurulumda çıkarım için yurt dışı aktarım yoktur.
- Tek MSI hedefi korunur (VC++ gerekmezse).

### Olumsuz

- CPU'lu makinelerde Türkçe kalite/hız yetersiz kalabilir (risk kaydı: Yüksek/Yüksek); çözüm KVKK paketi sonrası katman 2 veya Faz 3 LoRA.
- Model indirme GB'larca veri; çevrimdışı siteler için paylaşım gerekir.

## Doğrulama / açık noktalar

- **Spike C git/gitme:** Foundry Local'ın `NT SERVICE` hesabıyla servis içinde çalışması, model önbellek yolu, VC++ bağımlılığı. **Başarısızsa Ollama standalone + WinSW'ye geçilir.**
- **Spike E:** 50 başlıkta Qwen3.5-9B / Gemma 4 12B / Qwen3-30B-A3B ve bulut modeli; sonuç CPU'lu makineler için katman politikasını belirler. Qwen3.5 ve Gemma 4 için Türkçe benchmark sonucu bulunamamıştır.
- Bulut maliyet kestirimi (liste fiyatları, Batch): Haiku 4.5 ≈ 14 $/kullanıcı/ay, Sonnet 5 ≈ 28 $ — rapordaki kabaca hesap; doğrulanmalı.

## Kaynaklar

- [Araştırma raporu §1 — Model barındırma ve Türkçe; §4 — Hukuki çerçeve](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [AI mimarisi notları §1, §8](../research/notes/ai_agent_mimarisi.md), [Güvenlik notları §8](../research/notes/guvenlik_uyum.md)
- [Foundry Local 1.1](https://devblogs.microsoft.com/foundry/foundry-local-v1-1/), [Azure veri gizliliği](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/openai/data-privacy), [Anthropic Covered Models](https://privacy.claude.com/en/articles/15425996-data-retention-practices-for-covered-models)
