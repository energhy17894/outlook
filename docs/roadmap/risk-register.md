# Risk Kaydı

*Kaynak: [araştırma raporu §6 — Risk kaydı](../research/rapor-m365-operasyon-zekasi-platform-plani.md). Tarih: 27 Eylül 2026. Olasılık/Etki: Düşük / Orta / Yüksek. Bu kayıt her sprint gözden geçirilir.*

| # | Risk | Olasılık / Etki | Azaltım | İlgili |
|---|---|---|---|---|
| R1 | Servis tarafında token edinimi Conditional Access veya Token Protection nedeniyle engellenir | Orta / Yüksek | Faz 0 spike B; WAM tray yardımcısı; CA politikasını report-only modda test etmek; kiracıya özel uygulama kaydı | [ADR-0007](../adr/0007-delegated-auth-bff-pkce.md) |
| R2 | Yerel modellerin Türkçe kalitesi veya hızı CPU'lu makinelerde yetersiz kalır | Yüksek / Yüksek | Donanım profiline göre katman seçimi, küçük şemalar, bulut katman 2 (KVKK sonrası), Faz 3 LoRA ince ayarı ([Seeth vd.](https://arxiv.org/html/2609.01320)) | [ADR-0014](../adr/0014-foundry-local-model-hosting.md), spike E |
| R3 | Bulut sağlayıcı SS-2 imzalamaz veya bildirim gecikir | Orta / Yüksek | Yerel önce varsayılanı; katman kilidi; sağlayıcı değerlendirme kaydı | [KVKK](../compliance/kvkk/README.md), [SS-2 takip](../compliance/kvkk/ss2-takip.md) |
| R4 | Çalışan izleme itirazı (AYM / Bărbulescu) | Orta / Yüksek | İmzalı aydınlatma, amaçla sınırlılık, bireysel puanlama yok, "Kişisel" klasörlerle çıkış, ortak/proje posta kutularıyla başlama ([ECHR Bărbulescu](https://www.echr.coe.int/documents/d/echr/press_q_a_barbulescu_eng)) | [ADR-0023](../adr/0023-no-individual-performance-scoring.md) |
| R5 | Prompt enjeksiyonu veya veri sızdırma | Yüksek / Yüksek | Araçsız çıkarıcı, spotlighting, CSP, egress izin listesi, CI'da kırmızı takım testleri | [ADR-0019](../adr/0019-untrusted-content-quarantine.md), [tehdit modeli](../architecture/threat-model.md) |
| R6 | sqlite-vec'te kırıcı değişiklik veya Vec1 gecikmesi | Orta / Orta | `IVectorIndex` soyutlaması, kaba kuvvet yedeği, sürüm sabitleme | [ADR-0010](../adr/0010-sqlite-fts5-vector-blob-storage.md) |
| R7 | MSI kenar durumları (sanal hesap ACL'i, gecikmeli başlatma, tarayıcı güveni) | Orta / Orta | Pester matrisi, SetupHelper, Sandbox testleri | [ADR-0004](../adr/0004-https-certificate-strategy.md), [ADR-0005](../adr/0005-single-msi-wix-v7.md) |
| R8 | Kod imzalama ve SmartScreen / Smart App Control | Yüksek / Orta | OV bulut HSM'in erken satın alınması; kurum içi dağıtımda AD CS; tutarlı imza kimliği | [ADR-0006](../adr/0006-code-signing.md) |
| R9 | Graph değişiklikleri (sharedWithMe, SDK v6 dokümantasyon gecikmesi) | Orta / Orta | Ham HTTP delta yolu, sürüm sabitleme, sözleşme testleri | [ADR-0009](../adr/0009-delta-polling-change-detection.md), [ADR-0025](../adr/0025-shared-content-discovery.md) |
| R10 | Yönetici onayının alınamaması | Yüksek / Orta | Admin consent bağlantısı, yayıncı doğrulaması, Sites.Selected modu, onay iş akışı belgeleri | [ADR-0008](../adr/0008-delegated-graph-no-mail-send.md) |
| R11 | Dizüstü kaybı veya veri ihlali | Orta / Yüksek | BitLocker, DB şifrelemesi, crypto-shred, Intune wipe runbook'u, ihlal prosedürü | [ADR-0011](../adr/0011-encryption-at-rest.md) |
| R12 | Microsoft'un "proje hafızası" ile boşluğu kapatması | Orta / Yüksek | Kanıtlı kayıt, denetim, yerel çalışma ve Türkçe derinliği; MCP ile birlikte çalışma | [Rakip analizi](../product/competitive-analysis.md) |
| R13 | Kapsam kayması | Orta / Orta | Katı MVP tablosu, feature flag'ler, sprint başına kabul kriterleri | [MVP kapsamı](mvp-scope.md) |

## Isı haritası

| Etki \ Olasılık | Düşük | Orta | Yüksek |
|---|---|---|---|
| **Yüksek** | — | R1, R3, R4, R11, R12 | R2, R5 |
| **Orta** | — | R6, R7, R9, R13 | R8, R10 |
| **Düşük** | — | — | — |

## Gözden geçirme notları

- R1 ve R2, Faz 0 sonunda (23 Ekim 2026) spike B ve E sonuçlarıyla yeniden puanlanacaktır.
- Yeni riskler bu tabloya eklenir; kapanan riskler silinmez, "Kapandı" notuyla bırakılır.
