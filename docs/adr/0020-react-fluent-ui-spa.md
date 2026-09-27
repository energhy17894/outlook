# ADR-0020: UI — React/Vite/TS + Fluent UI v9 + ECharts/React Flow/vis-timeline/AG Grid Community + SSE

- **Durum:** Kabul edildi
- **Tarih:** 2026-09-27
- **Karar vericiler:** Frontend geliştirici, UX tasarımcı, teknik lider
- **İlgili ADR'ler:** [0003](0003-kestrel-loopback-https-6500.md), [0019](0019-untrusted-content-quarantine.md)

## Bağlam

Arayüz; projeler, İşlerim, inceleme kuyruğu, zaman çizelgesi/olay akışı, panolar, Arama/Sor ve yönetim ekranlarını içerir. Hedef makinede ek çalışma zamanı kurulmamalıdır; M365 ile tutarlı görünüm ve erişilebilirlik istenir.

- Next.js hedef makinede Node sunucu çalışma zamanı gerektirir.
- React Flow (MIT) ve AG Grid Community (MIT) ticari kullanım için serbesttir ([xyflow LICENSE](https://github.com/xyflow/xyflow/blob/main/LICENSE), [AG Grid](https://www.ag-grid.com/eula/community/)).
- .NET 10 yerel Server-Sent Events desteği sunar ([SSE in .NET 10](https://milanjovanovic.tech/blog/server-sent-events-in-aspnetcore-and-dotnet-10)).
- Blazor yalnızca C# bilen ekip için meşru alternatiftir, ancak zaman çizelgesi ve akış bileşenlerinde yine JS kütüphanelerini sarmak gerekir.

## Değerlendirilen alternatifler

| Seçenek | Artılar | Eksiler |
|---|---|---|
| **React + Vite + TS SPA, Kestrel'den statik (seçilen)** | Ek çalışma zamanı yok; zengin görselleştirme ekosistemi | İki dil (C# + TS) |
| Blazor Server + Fluent UI Blazor | Tek dil | Kullanıcı başına devre; JS kütüphanelerini sarmak gerekir |
| Next.js | SSR | Node çalışma zamanı |
| shadcn/ui | Hızlı özelleştirme | M365 görünümü yeniden yapılmalı |

## Karar

- **React + Vite + TypeScript** SPA; `wwwroot` içinde Kestrel tarafından statik sunulur.
- Bileşenler: **Fluent UI React v9** (M365 tutarlılığı, erişilebilirlik, tema), **Apache ECharts** (BI), **React Flow** (olay akışı, kanıt→öngörü→aksiyon grafikleri), **vis-timeline** (proje zaman çizelgeleri), **AG Grid Community** (tablolar), **TanStack Query** (önbellek).
- API istemcisi OpenAPI'den üretilir (`src/shared/api-client`).
- Canlı güncellemeler **SSE** ile (`/api/events`); SignalR yalnızca çift yönlü ihtiyaç doğarsa.
- i18n: tr/en; Türkçe öncelikli.
- Güvenli gösterim: e-posta içeriği ve model çıktısı düz metin; `dangerouslySetInnerHTML` benzeri yollar kullanılmaz (CSP ile birlikte, [ADR-0019](0019-untrusted-content-quarantine.md)).
- Özellik klasörleri: `projects`, `my-work`, `review-queue`, `timeline`, `dashboards`, `ask`, `admin`, `setup`.

## Sonuçlar

### Olumlu

- Hedef makinede Node gerekmez; MSI yalnızca statik dosya taşır.
- Klavye öncelikli inceleme kuyruğu ve kanıt kartları için olgun bileşenler.

### Olumsuz

- ECharts ve vis-timeline lisansları kaynaklarda yeniden doğrulanmadı (genel bilgi: Apache-2.0 / Apache-2.0-MIT); THIRD-PARTY-NOTICES'ta teyit edilmeli.
- Fluent UI v9 grafik bileşenlerinin 2026 durumu doğrulanmadı (bu yüzden ECharts).

## Doğrulama / açık noktalar

- S7 kabul kriterleri: 5 pilot kullanıcıyla kullanılabilirlik testi; sayfa yükleme p95 < 2 sn; CSP testlerinde uzak içerik yüklenmez.

## Kaynaklar

- [Araştırma raporu §1 — Arayüz](../research/rapor-m365-operasyon-zekasi-platform-plani.md)
- [Teknoloji yığını notları §4](../research/notes/teknoloji_yigini.md), [Özellikler/UX notları §3, §5](../research/notes/ozellikler_ux.md)
- [microsoft/fluentui](https://github.com/microsoft/fluentui)
