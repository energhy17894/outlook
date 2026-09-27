# tools/

> **Durum:** Faz 0 iskeleti — Derleme ve imzalama betikleri eklendi (Faz 0 tamamlandı).

Bu dizin yerel derleme, imzalama, geliştirici ortamı kurulumu ve sentetik test verisi üretimi betiklerini barındırır.

Kaynak: [araştırma raporu §5](../docs/research/rapor-m365-operasyon-zekasi-platform-plani.md).

## Planlanan betikler

| Betik | Amaç |
|---|---|
| `build.ps1` | Yerel derleme: .NET projeleri, SPA (`npm ci && npm run build` → `wwwroot`), self-contained publish, WiX MSI |
| `sign.ps1` | İmzalama sırası: kendi exe/dll → SetupHelper → MSI → (bundle) detach/engine/reattach/bundle; SHA-256 + RFC 3161 zaman damgası ([ADR-0006](../docs/adr/0006-code-signing.md)) |
| `dev-setup.ps1` | Geliştirici ortamı kurulumu (SDK'lar, WiX EULA kabulü, yerel sertifika — yalnızca geliştirme için) |
| `seed-synthetic.ps1` | **Sentetik** TR/EN test korpusu üretimi |

## Kurallar

- Betikler gerçek posta kutularına bağlanıp veri çekmez; test verisi yalnızca sentetiktir.
- Sırlar betiklere gömülmez; CI'da OIDC/ortam sırları kullanılır.
- PowerShell betikleri CRLF satır sonu kullanır (`.editorconfig`).
