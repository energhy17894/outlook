# Kalite Hedefleri (Pilot Kabulü)

*Kaynak: [araştırma raporu §6 — Kalite hedefleri](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [AI mimarisi notları §10](../research/notes/ai_agent_mimarisi.md). Aşağıdaki eşikler **öneridir ve Faz 0 ölçümüyle kalibre edilmelidir.***

## Ölçüm yaklaşımı

ROUGE ve BERTScore, e-posta özetlerinde insan yargısıyla zayıf ilişkilidir ([EmailSum](https://aclanthology.org/2021.acl-long.537/)). Bu yüzden ölçüm **öğe ve atıf düzeyinde** yapılır (ALCE ve ExtractBench tarzı): tür + sahip + normalize nesne + termin (± tolerans) eşleşmesiyle madde düzeyi P/R/F1; alıntının varlığı ve iddiayı desteklemesiyle atıf kesinliği/duyarlılığı.

## Pilot hedefleri

| Metrik | Hedef |
|---|---|
| Atıf geçerliliği (gösterilen öğede alıntının kaynakta birebir bulunması) | %100 (yapısal garanti) |
| Atıf kesinliği (alıntının iddiayı desteklemesi, NLI veya insan değerlendirmesiyle) | ≥ 0,90 |
| "İşlerim" kesinliği | ≥ 0,80 |
| Taahhüt/talep madde düzeyi F1 | ≥ 0,70 (pilot sonunda) |
| Proje atama doğruluğu / aşama doğruluğu | ≥ 0,85 / ≥ 0,75 |
| Termin tarihi ±1 gün | ≥ 0,85 |
| Enjeksiyon saldırı başarı oranı (kırmızı takım korpusu) | < %2 |
| İlk tam senkronizasyon (10 bin e-posta, GPU'lu makine) | < 4 saat |
| Kurulumdan ilk öneriye kadar geçen süre | < 30 dk (model indirme hariç) |

## Sprint başlangıç çizgileri (MVP sırasında)

| Sprint | Metrik | Eşik |
|---|---|---|
| S3 | Başlık yeniden kurma doğruluğu (altın/sentetik set) | ≥ %95 |
| S3 | Soyucunun TR/EN yanıtlarda alıntı geçmişini temizlemesi | ≥ %90 |
| S4 | Triage "aksiyon gerektiren" kesinliği | ≥ 0,85 |
| S4 | Hariç tutulan test öğelerinin LLM'e ulaşmaması | %100 |
| S4 | 100 bin parçada arama gecikmesi | p95 < 1 sn |
| S5 | Taahhüt/talep F1 / karar/risk F1 | ≥ 0,65 / ≥ 0,60 |
| S5 | Termin ±1 gün | ≥ 0,80 |
| S5 | Kırmızı takımda enjeksiyon kaynaklı sahte öğe | < %5 |
| S6 | Bilinen projelerde atama doğruluğu | ≥ 0,85 |
| S7 | Sayfa yükleme | p95 < 2 sn |

## Operasyonel ve kurulum hedefleri

| Hedef | Eşik | Kaynak sprint |
|---|---|---|
| Sessiz kurulum çıkış kodu | 0 veya 3010 | S1, Pester |
| SCM yeniden başlatma | Süreç öldürüldüğünde ≤ 60 sn | S1 |
| İlk senkronizasyon (10.000 e-posta) | Hatasız; ele alınmamış 429 yok | S2 |
| Loglarda içerik | Log taramasında gövde metni yok | S2 |
| Parser hata enjeksiyonu | Servisler ayakta | S3 |
| Pester kurulum matrisi | %100 yeşil | S8 |
| Localhost pen testi | Açık yüksek/kritik bulgu yok | S8 |

## Ek izlenecek metrikler (hedefsiz)

AI notlarından: sahip doğruluğu, termin tam eşleşmesi, çekimser kalma (abstention) kesinliği, e-posta başına maliyet ve gecikme.

## Değerlendirme verisi

- Depoda yalnızca **sentetik** TR/EN korpus bulunur (`tests/eval/`).
- Gerçek e-postalardan oluşan altın veri seti (öneri: 200–500 başlık, TR/EN, ekli) KVKK kapsamında kişisel veridir; **depoya konmaz**, şifreli ve erişimi kısıtlı ayrı depolamada tutulur; `eval.yml` self-hosted runner'da çalışır. Rıza ve maskeleme prosedürü Faz 0'da hazırlanır.
- LLM-as-judge yalnızca tamamlayıcıdır; çıkarıcıdan farklı model ailesiyle ve insan etiketli alt kümeye karşı kalibre edilerek kullanılır.
