# Katkı Rehberi

OpsIntel'e katkı yaptığınız için teşekkürler. Proje şu anda **planlama aşamasındadır**; kod Faz 0 (5–23 Ekim 2026) ile birlikte eklenecektir. Bu rehber hem doküman hem de ileride kod katkıları için geçerlidir.

## Temel kural: gerçek e-posta verisi depoya konmaz

**Gerçek e-posta verisi depoya konmaz.** Bu kural istisnasızdır ve şunları kapsar:

- Gerçek e-postalar (`.eml`, `.msg`), ekler, SharePoint/OneDrive belgeleri, Teams transkriptleri
- Uygulamanın ürettiği veritabanı dosyaları (`*.db`, `*.sqlite`), blob klasörleri, tanı paketleri, loglar
- Gerçek kişilere ait ad, e-posta adresi, telefon numarası veya kurum içi bilgiler (issue, PR açıklaması, test verisi, ekran görüntüsü dahil)
- Değerlendirme için kullanılan gerçek "altın veri seti" (ayrı, şifreli ve erişimi kısıtlı depolamada tutulur)

Test ve örnekler için yalnızca **sentetik** veri kullanın. Ekran görüntülerinde gerçek veri varsa maskeleyin. `.gitignore` bir güvenlik ağıdır, tek savunma değildir. Ayrıntı: [SECURITY.md](SECURITY.md).

## Dal (branch) stratejisi

- `main` her zaman yayımlanabilir/incelenmiş durumdadır; doğrudan push yapılmaz.
- Çalışma dalları kısa ömürlüdür ve şu önekleri kullanır:

| Önek | Kullanım | Örnek |
|---|---|---|
| `feat/` | Yeni özellik | `feat/review-queue-keyboard` |
| `fix/` | Hata düzeltme | `fix/delta-410-resync` |
| `docs/` | Yalnızca doküman | `docs/kvkk-dpia-outline` |
| `spike/` | Faz 0 veya sonraki keşif çalışması | `spike/foundry-local-service` |
| `adr/` | Yeni veya değişen ADR | `adr/0026-lan-mode` |
| `chore/` | Araç, CI, bağımlılık | `chore/pin-wix-7-0-1` |

## Commit kuralları

[Conventional Commits](https://www.conventionalcommits.org/tr/v1.0.0/) biçimi kullanılır:

```text
<tür>(<kapsam>): <kısa özet, emir kipi, küçük harf>

<isteğe bağlı gövde: neden ve nasıl>

<isteğe bağlı altbilgi: Refs: ADR-0007, Closes #12>
```

- **Türler:** `feat`, `fix`, `docs`, `refactor`, `test`, `build`, `ci`, `chore`, `perf`, `security`
- **Kapsam örnekleri:** `host`, `intelligence`, `parser`, `setup-helper`, `graph`, `policy`, `extraction`, `web`, `installer`, `adr`, `kvkk`
- Commit mesajı ve kod yorumları Türkçe veya İngilizce olabilir; API ve ürün adları özgün hâliyle yazılır.

Örnek: `feat(graph): posta kutusu başına eşzamanlılık semaforu ekle`

## Pull request süreci

1. İlgili issue'yu açın veya mevcut olana bağlanın (şablonlar: özellik, hata, spike, ADR).
2. Dalınızı açın, küçük ve odaklı değişiklikler yapın.
3. PR açarken [PR şablonunu](.github/PULL_REQUEST_TEMPLATE.md) eksiksiz doldurun; **KVKK/güvenlik etkisi kontrol listesi** zorunludur.
4. En az bir onaylayıcı incelemesi gerekir. Güvenlik, kimlik, politika kapısı, denetim kaydı veya kurulum dosyalarına dokunan değişikliklerde teknik lider incelemesi gerekir (CODEOWNERS Faz 0'da eklenecek).
5. CI (Faz 0'dan itibaren) yeşil olmalıdır.
6. Birleştirme "squash merge" ile yapılır; squash mesajı commit kurallarına uyar.

## ADR (Mimari Karar Kaydı) süreci

Mimari açıdan önemli her karar bir ADR ile kaydedilir. Ayrıntı ve şablon: [docs/adr/README.md](docs/adr/README.md), [docs/adr/template.md](docs/adr/template.md).

- **Ne zaman ADR yazılır?** Yeni bir bağımlılık, çalışma zamanı, depolama, protokol, güvenlik sınırı, izin (Graph scope), veri akışı veya kurulum davranışı değiştiğinde.
- **Numara:** Bir sonraki boş numara (`0026-...`). Dosya adı İngilizce/ASCII kebab-case.
- **Durumlar:** `Önerildi` → `Kabul edildi` / `Reddedildi`; sonradan `Kullanımdan kalktı` veya `Yerine geçti: ADR-NNNN`.
- **Değişmezlik:** Kabul edilmiş bir ADR'nin kararı yeniden yazılmaz. Karar değişirse yeni bir ADR açılır ve eskisinin durumu "Yerine geçti: ADR-NNNN" olarak güncellenir.
- Faz 0 spike sonuçları ilgili ADR'yi "Önerildi"den "Kabul edildi"ye taşır veya yeni ADR doğurur. Spike issue'su git/gitme kriterlerini içermelidir.

## Doküman kuralları

- Dokümanlar **Türkçe** yazılır; teknik terimler, API ve ürün adları özgün hâliyle kalır.
- Dosya ve dizin adları İngilizce/ASCII kebab-case'dir (Türkçe karakter kullanılmaz).
- Dokümanlar arası bağlantılar göreli yol kullanır; yeni bağlantı eklediğinizde hedef dosyanın var olduğunu kontrol edin.
- Kaynağa dayanmayan iddia yazılmaz; doğrulanmamış bilgi "doğrulanmadı" olarak işaretlenir. Kaynaklar `docs/research/` altındaki rapor ve notlardır.
- Mermaid diyagramlarında parantez veya özel karakter içeren etiketler tırnak içine alınır.

## Kod kuralları (Faz 0'dan itibaren)

- `.editorconfig` kurallarına uyulur; kültür duyarlı string işlemlerinde Türkçe İ/ı sorunlarına dikkat edilir (`StringComparison.Ordinal` / açık `CultureInfo`).
- Loglara e-posta gövdesi, prompt veya model çıktısı yazılmaz ([ADR-0021](docs/adr/0021-observability.md)).
- Prompt ve JSON şemaları `prompts/` altında sürümlenir ve kod incelemesinden geçer.
- Modül sınırları mimari testleriyle korunur; modüller birbirinin tablolarına doğrudan erişmez.
