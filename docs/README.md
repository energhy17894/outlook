# Doküman Dizini

OpsIntel planlama dokümanlarının tam dizini. Proje özeti için bkz. [kök README](../README.md). Tüm dokümanlar Türkçedir; teknik terimler, API ve ürün adları özgün hâlindedir. Tarih: 27 Eylül 2026.

## Araştırma

| Doküman | Açıklama |
|---|---|
| [research/README.md](research/README.md) | Araştırma raporu ve notların açıklaması |
| [research/rapor-m365-operasyon-zekasi-platform-plani.md](research/rapor-m365-operasyon-zekasi-platform-plani.md) | Sentezlenmiş araştırma raporu (tüm dokümanların ana kaynağı) |

## Mimari

| Doküman | Açıklama |
|---|---|
| [architecture/overview.md](architecture/overview.md) | Mimari karar özeti, süreç topolojisi, bileşen listesi, katman katman seçimler |
| [architecture/c4-context.md](architecture/c4-context.md) | C4 bağlam diyagramı (seviye 1) |
| [architecture/c4-container.md](architecture/c4-container.md) | C4 konteyner diyagramı (seviye 2) |
| [architecture/data-model.md](architecture/data-model.md) | Kanıt zinciri veri modeli, ER diyagramı, varlık tablosu |
| [architecture/threat-model.md](architecture/threat-model.md) | Yapay zekâya özgü ve yerel uç nokta tehditleri, kontroller |
| [architecture/m365-integration.md](architecture/m365-integration.md) | Kimlik (BFF/WAM sınırı), izinler, delta, throttling, yazma kuralları, kullanımdan kalkanlar |

## Mimari karar kayıtları (ADR)

| Doküman | Açıklama |
|---|---|
| [adr/README.md](adr/README.md) | ADR dizini (0001–0025), durumlar, Faz 0 spike eşlemesi |
| [adr/template.md](adr/template.md) | MADR tarzı Türkçe ADR şablonu |

## Yol haritası

| Doküman | Açıklama |
|---|---|
| [roadmap/roadmap.md](roadmap/roadmap.md) | Fazlar, takvim, ekip, Faz 0 spike'ları ve git/gitme kriterleri, MVP sprintleri, Faz 2/3 |
| [roadmap/mvp-scope.md](roadmap/mvp-scope.md) | MVP kapsam içi / dışı |
| [roadmap/quality-targets.md](roadmap/quality-targets.md) | Pilot kabul kalite hedefleri ve sprint eşikleri |
| [roadmap/risk-register.md](roadmap/risk-register.md) | Risk kaydı ve ısı haritası |

## Uyum

> Uyum dokümanları hukuki tavsiye değildir; hukuk danışmanı onayı gerektirir.

| Doküman | Açıklama |
|---|---|
| [compliance/kvkk/README.md](compliance/kvkk/README.md) | KVKK çerçevesi: yurt dışı aktarım/SS-2, özel nitelikli veri, çalışan e-postası içtihadı, gerekli belgeler |
| [compliance/kvkk/mesru-menfaat-testi.md](compliance/kvkk/mesru-menfaat-testi.md) | Meşru menfaat denge testi (taslak) |
| [compliance/kvkk/aydinlatma-sablonu.md](compliance/kvkk/aydinlatma-sablonu.md) | Aydınlatma metni ve kullanım politikası şablonu (taslak) |
| [compliance/kvkk/dpia.md](compliance/kvkk/dpia.md) | DPIA / KVKK risk değerlendirmesi (taslak) |
| [compliance/kvkk/verbis-girdileri.md](compliance/kvkk/verbis-girdileri.md) | VERBİS girdileri (taslak) |
| [compliance/kvkk/ss2-takip.md](compliance/kvkk/ss2-takip.md) | SS-2 standart sözleşme takibi (taslak) |
| [compliance/kvkk/veri-sahibi-talepleri.md](compliance/kvkk/veri-sahibi-talepleri.md) | İlgili kişi talepleri ve ihlal süreci (taslak) |
| [compliance/ai-act-kapsam.md](compliance/ai-act-kapsam.md) | AB AI Act Annex III 4(b) kapsam dışı tasarım gerekçesi, Digital Omnibus tarihleri |

## İşletim

| Doküman | Açıklama |
|---|---|
| [operations/installation.md](operations/installation.md) | MSI'ın yaptıkları, ön gereksinimlerin kaldırılması, sertifika stratejisi, sessiz kurulum, Burn, kod imzalama |
| [operations/intune.md](operations/intune.md) | Intune Win32/LOB dağıtımı, eşlik eden politikalar, kayıp cihaz runbook'u |
| [operations/troubleshooting.md](operations/troubleshooting.md) | Sorun giderme (taslak başlıklar) |

## Ürün

| Doküman | Açıklama |
|---|---|
| [product/features.md](product/features.md) | Özellik kataloğu (İşlerim, Beklediklerim, inceleme kuyruğu, zaman çizelgeleri, panolar, Sor) |
| [product/competitive-analysis.md](product/competitive-analysis.md) | Rakip analizi ve ayrışma |
| [product/additional-developments.md](product/additional-developments.md) | Önceliklendirilmiş ek geliştirme önerileri (değer/fizibilite) |
| [product/personas.md](product/personas.md) | Kullanıcı personaları |

## Kod dizinleri (yer tutucu)

| Dizin | Açıklama |
|---|---|
| [../src/README.md](../src/README.md) | Planlanan .NET projeleri ve SPA |
| [../installer/README.md](../installer/README.md) | Planlanan WiX projeleri |
| [../tests/README.md](../tests/README.md) | Planlanan test katmanları |
| [../prompts/README.md](../prompts/README.md) | Sürümlenmiş prompt şablonları ve JSON şemaları |
| [../models/README.md](../models/README.md) | Model manifesti |
| [../tools/README.md](../tools/README.md) | Derleme ve geliştirici betikleri |
