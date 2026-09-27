# models/

> **Durum:** Faz 0 iskeleti — `model-manifest.json` eklendi (spike C ve E validasyondadır).

Bu dizin **yalnızca model manifestini** tutar: sabitlenmiş model kimlikleri ve **SHA-256** hash'leri. **Model ikili dosyaları depoya konmaz** (`.gitignore` `*.onnx`, `*.gguf`, `*.safetensors` dosyalarını dışlar). Kaynak: [araştırma raporu §1, §5](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md), [ADR-0014](../docs/adr/0014-foundry-local-model-hosting.md).

## Planlanan içerik

```text
models/
└─ model-manifest.json   # sabitlenmiş model ID + SHA-256 (ikili dosya yok)
```

Manifest, donanım profiline (GPU/NPU/CPU) göre model katmanlarını tanımlayacaktır. Rapordaki aday modeller (spike E ile doğrulanacak):

- Triage ve embedding: 4–12B sınıfı (Qwen3.5-9B veya Gemma 4 12B) + `qwen3-embedding-0.6b`
- GPU'lu iş istasyonlarında tam yerel çıkarım: 30B-A3B sınıfı MoE (ör. Qwen3-30B-A3B)

## Çalışma zamanı davranışı

- Modeller MSI'a gömülmez; ilk çalıştırma sihirbazı Foundry Local kataloğundan indirir veya `MODEL_SOURCE=\\paylaşım\models` ile çevrimdışı yüklenir.
- İndirilen modeller `%ProgramData%\OpsIntel\models` altında önbelleğe alınır ve manifest hash'iyle doğrulanır (tedarik zinciri kontrolü, [tehdit modeli](../docs/architecture/threat-model.md)).
- Her model kendi lisansına tabidir; lisanslar THIRD-PARTY-NOTICES'ta listelenecektir.
