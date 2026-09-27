# ADR-0024: OS desteği — Win11 23H2+, Server 2022/2025; Win10 22H2 en iyi çaba

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider, DevOps + QA mühendisi
- **İlgili ADR'ler:** [0002](0002-dotnet-10-lts-self-contained.md), [0005](0005-single-msi-wix-v7.md)

## Bağlam

- .NET 10'un resmi desteklenen OS listesinde Windows 10 yalnızca Enterprise LTSC/IoT kanallarıyla yer alır; tüketici ve Pro **Windows 10 22H2 listede yoktur** ([.NET 10 desteklenen OS](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)).
- Windows 10 destek sonu 14 Ekim 2025'tir; ESU Windows 10 22H2 ister ([teknoloji yığını §2](../research/notes/teknoloji_yigini.md)).
- Foundry Local'ın yeni NPU'lar için Windows 24H2 veya sonrası gerektirdiği ikincil bir kaynakta geçer (doğrulanmadı).

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **Win11 23H2+, Server 2022/2025 destekli; Win10 22H2 en iyi çaba (seçilen)** | .NET 10 matrisiyle uyumlu; test yükü makul | Win10 22H2 kullanan müşteriler için garanti yok |
| Win10 22H2'yi tam desteklemek | Daha geniş kurulu taban | .NET 10 resmi desteği yok; OS desteği bitmiş |
| Yalnızca Win11 | En basit | Sunucu kurulumları dışarıda kalır |

## Karar

- Destek beyanı: **"Windows 11 23H2+ ve Windows Server 2022/2025 desteklenir; Windows 10 22H2 en iyi çaba (best effort)."**
- Mimari: x64 birincil; Arm64 ikincil ([ADR-0002](0002-dotnet-10-lts-self-contained.md)).
- MSI başlatma koşulları: x64, Windows 11 23H2+/Server 2022+, yönetici, TCP 6500 boş. Windows 10 22H2'de kurulumun engellenip engellenmeyeceği (uyarı mı, engel mi) Faz 0'da kararlaştırılacaktır (kaynaklar yalnızca "en iyi çaba" der).
- Kurulum test matrisi: temiz Win11 23H2 ve Server 2022 (S1 kabul kriteri); Server 2025 ve Win11 24H2+ matrise eklenir.

## Sonuçlar

### Olumlu

- Destek taahhüdü çalışma zamanının resmi matrisine dayanır.

### Olumsuz

- Windows 10 filosu olan kurumlarda pilot seçimi kısıtlanır.

## Doğrulama / açık noktalar

- Pester kurulum matrisinin işletim sistemi kapsamı `installer.yml`'de tanımlanacak.
- Foundry Local'ın donanım/OS gereksinimleri spike C'de doğrulanacak.

## Kaynaklar

- [Araştırma raporu §2 — İşletim sistemi desteği](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §2](../research/notes/teknoloji_yigini.md), [AI mimarisi notları §1](../research/notes/ai_agent_mimarisi.md)
