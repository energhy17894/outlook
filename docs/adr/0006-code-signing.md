# ADR-0006: Kod imzalama — bulut HSM'de OV sertifika veya kurum AD CS; Artifact Signing Türkiye'de uygun değil

- **Durum:** Önerildi (satın alma süreci Faz 0'da başlar; sağlayıcı seçimi bekleniyor)
- **Tarih:** 2026-09-27
- **Karar vericiler:** DevOps + kurulum mühendisi, ürün sahibi (bütçe)
- **İlgili ADR'ler:** [0005](0005-single-msi-wix-v7.md), [0022](0022-msi-major-upgrade-updates.md)

## Bağlam

Tüm PE dosyaları (exe/dll, SetupHelper), MSI ve Burn bundle imzalanmalıdır. Burn bundle'larda imzalama iki parçalıdır: engine ayrılır (detach), imzalanır, geri eklenir (reattach), ardından nihai bundle imzalanır ([FireGiant signing](https://docs.firegiant.com/wix/tools/signing/)).

- **Azure Artifact Signing** (eski adıyla Trusted Signing) GitHub Actions ile OIDC üzerinden entegre olur ve aylık 9,99 $'dan başlar ([Azure pricing](https://azure.microsoft.com/en-us/pricing/details/artifact-signing/)). Ancak Public Trust sertifikaları yalnızca ABD, Kanada, AB, İngiltere, Avustralya, Yeni Zelanda, Japonya, Güney Kore, Singapur, İsviçre, Norveç ve İsrail'deki kuruluşlara verilir; **Türkiye listede yoktur** ([Artifact Signing quickstart](https://learn.microsoft.com/en-us/azure/artifact-signing/quickstart)).
- 1 Mart 2026'dan itibaren yeni kod imzalama sertifikalarının geçerliliği en fazla **460 gündür** ([DigiCert](https://www.digicert.com/blog/understanding-the-new-code-signing-certificate-validity-change)).
- EV sertifikalar artık SmartScreen uyarısını atlatmaz; Windows 11 Smart App Control olumlu itibarı olmayan imzasız dosyaları engeller ([SmartScreen itibarı](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/smartscreen-reputation)).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **OV sertifika, bulut HSM (Türk tüzel kişiliği için)** | Genel güven; CI'dan imzalama | Maliyet; 460 gün geçerlilik; itibar birikmesi zaman alır |
| **Kurum AD CS kod imzalama sertifikası (yalnızca kurum içi)** | Ek maliyet yok; GPO/Intune ile güvenilir yayıncı | Yalnızca yönetilen cihazlarda güvenilir |
| Azure Artifact Signing (Public Trust) | Ucuz, OIDC entegrasyonu | Türkiye'deki kuruluşa verilmez |
| EV sertifika | — | SmartScreen'i artık atlatmaz; primi haklı çıkarmaz |

## Karar

- **Türk tüzel kişiliği tarafından dağıtılan sürümler:** bulut HSM üzerinde OV kod imzalama sertifikası.
- **Yalnızca kurum içi dağıtım:** kurumun AD CS kod imzalama sertifikası; GPO/Intune ile güvenilir yayıncı olarak dağıtılır.
- Azure Artifact Signing, uygun ülkede bir tüzel kişilik imzalamadıkça kullanılmaz.
- Tüm imzalar SHA-256 + RFC 3161 zaman damgalıdır. İmza sırası: kendi exe/dll'ler → SetupHelper → MSI → (bundle) detach/engine/reattach/bundle.
- Sürümler arasında tutarlı imza kimliği korunur (SmartScreen itibarı için).
- Satın alma Faz 0'da başlar; S1'de test imzası, S8'de üretim imzası.

## Sonuçlar

### Olumlu

- Intune/GPO/SCCM ile dağıtılan MSI'larda SmartScreen büyük ölçüde devreye girmez (Mark-of-the-Web yok — MSI notlarında çıkarım).
- İmzalı SetupHelper, PowerShell CA yerine güvenilir özel eylem sağlar.

### Olumsuz

- Doğrudan indirmelerde itibar birikene kadar SmartScreen uyarıları beklenir.
- Türk kuruluşları için bulut HSM OV sağlayıcı seçenekleri ve fiyatları **araştırılmadı** (MSI notları Gaps).
- 460 günlük geçerlilik düzenli yenileme süreci gerektirir.

## Doğrulama / açık noktalar

- Sağlayıcı ve fiyat karşılaştırması (DigiCert KeyLocker, SSL.com eSigner, Sectigo gibi seçenekler notlarda yalnızca adıyla geçer, doğrulanmadı).
- Burn engine detach/reattach akışının CI'da (`installer.yml`) otomasyonu.

## Kaynaklar

- [Araştırma raporu §2 — Kod imzalama](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [MSI notları §7](../research/notes/msi_kurulum_dagitim.md)
- [MS Learn: Artifact Signing FAQ](https://learn.microsoft.com/en-us/azure/artifact-signing/faq)
