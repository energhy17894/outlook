# ADR-0002: .NET 10 LTS, self-contained x64; Arm64 ikincil

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Teknik lider / mimar
- **İlgili ADR'ler:** [0001](0001-modular-monolith-two-services.md), [0005](0005-single-msi-wix-v7.md), [0024](0024-os-support-matrix.md)

## Bağlam

Çalışma zamanı; Windows servisi barındırma, Graph/MSAL, yerel LLM, web sunucusu ve arka plan işçilerini tek pakette taşımalı ve MSI'a dış ön gereksinim eklememelidir.

- **.NET 10 LTS** Kasım 2025'te yayımlandı, **14 Kasım 2028'e kadar** desteklenir ([.NET destek politikası](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core)). ASP.NET Core IIS olmadan `AddWindowsService()` ile Windows servisi olarak barındırılabilir.
- `Microsoft.Graph` 6.7.0 net10.0'ı hedefler; MSAL.NET 4.90.1; Microsoft.Extensions.AI GA; Microsoft Agent Framework 1.0 GA (Nisan 2026); Foundry Local GA (9 Nisan 2026, C# SDK).
- Self-contained yayın paylaşımlı framework gerektirmez; ASP.NET Core Hosting Bundle yalnızca IIS için gereklidir.

## Karar etkenleri

- Yerel SCM entegrasyonu ve Event Log
- Graph/MSAL/WAM/MAF/Foundry Local SDK kapsamı
- Ürün ömrüyle uyumlu LTS
- Sıfır ön gereksinim (tek MSI)

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **.NET 10 LTS self-contained (seçilen)** | Yerel Windows Service, en eksiksiz M365/AI SDK'ları, LTS | Ekipte C# yetkinliği gerekir |
| Node.js | Ön/arka uç tek dil | SEA hâlâ "Stability: 1.1 – Active development"; servis için sarmalayıcı gerekir ([Node.js SEA](https://nodejs.org/api/single-executable-applications.html)) |
| Python | Zengin AI/ayrıştırma ekosistemi | Servis için NSSM gibi sarmalayıcılar (NSSM son kararlı 2014); paketleme riski |
| Go / Rust | Tek statik ikili | Rust için resmi Graph SDK yok; Go'da MSAL.NET düzeyinde Entra/WAM desteği yok |

## Karar

- Tüm servisler ve yardımcılar **.NET 10 LTS (C#)** ile yazılır, `win-x64` için **self-contained** yayımlanır.
- Arm64 derlemesi ikincil hedeftir (opsiyonel).
- Python yalnızca Faz 2'de, gerekirse, .NET host'unun denetlediği opsiyonel alt süreç olarak (ör. Docling) kullanılır.
- Aspire yalnızca geliştirme zamanında kullanılabilir; ürün Aspire çalışma zamanına bağımlı olmaz (evergreen, LTS yok).

## Sonuçlar

### Olumlu

- Tek çalışma zamanı web sunucusunu, işçileri, Graph istemcisini, kimliği ve yerel LLM'i taşır; MSI sade kalır.
- Paylaşımlı .NET runtime kurulumu gerekmez.

### Olumsuz

- Self-contained yayın paket boyutunu artırır.
- .NET 10 EOL (Kasım 2028) öncesinde .NET 12 LTS'e geçiş planlanmalıdır (Faz 4 önerisi; .NET 12 çıkış tarihi kaynaklarda doğrulanmadı).
- Windows 10 22H2 .NET 10 resmi listesinde yok ([ADR-0024](0024-os-support-matrix.md)).

## Doğrulama / açık noktalar

- Faz 0 walking skeleton'da self-contained publish + MSI boyutu ölçülecek.
- Single-file yayının Foundry Local yerel kütüphaneleriyle uyumu Faz 0'da kontrol edilecek (kaynaklarda doğrulanmadı).

## Kaynaklar

- [Araştırma raporu §1 — Çalışma zamanı seçimi, Karar özeti](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §2](../research/notes/teknoloji_yigini.md), [MSI notları §2](../research/notes/msi_kurulum_dagitim.md)
- [MS Learn: Host ASP.NET Core in a Windows Service](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/windows-service?view=aspnetcore-10.0)
- [.NET 10 desteklenen OS](https://github.com/dotnet/core/blob/main/release-notes/10.0/supported-os.md)
