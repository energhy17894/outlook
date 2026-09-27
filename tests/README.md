# tests/

> **Durum:** Faz 0 iskeleti — Birim, mimari ve değerlendirme testleri eklendi (112 test geçti).

Bu dizin birim, entegrasyon, mimari, sözleşme, e2e, performans, kurulum ve değerlendirme testlerini barındırır. Skeleton validasyonu tamamlanmıştır.

Kaynak: [araştırma raporu §5, §6](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md). Kalite hedefleri: [docs/roadmap/quality-targets.md](../docs/roadmap/quality-targets.md).

## Planlanan yapı

```text
tests/
├─ unit/          # birim testleri
├─ integration/   # Graph kayıt/oynatma, SQLite
├─ architecture/  # modül sınırları (modüller arası doğrudan tablo erişimi yok, Mail.Send kapsamı yok vb.)
├─ contract/      # API sözleşme testleri
├─ e2e/           # Playwright; CSP/Host/CSRF testleri
├─ perf/          # performans (ör. 100 bin parçada arama p95 < 1 sn)
├─ installer/     # Pester 5 kurulum matrisi + Windows Sandbox .wsb
└─ eval/          # sentetik TR/EN korpus, metrik koşucusu, kırmızı takım korpusu
```

## Kurallar

- **Gerçek e-posta verisi bu dizine (ve depoya) asla konmaz.** Tüm test verileri sentetiktir.
- Gerçek e-postalardan oluşan altın veri seti KVKK kapsamında kişisel veridir; şifreli, erişimi kısıtlı ayrı bir depolamada tutulur ve `eval.yml` iş akışı self-hosted runner'da çalışır.
- Kırmızı takım korpusu: LLMail-Inject örnekleri ve Türkçeye çevrilmiş enjeksiyonlar; her sürümde çalışır ([ADR-0019](../docs/adr/0019-untrusted-content-quarantine.md)).
- Pester kurulum matrisi maddeleri: [installation.md §10](../docs/operations/installation.md).
