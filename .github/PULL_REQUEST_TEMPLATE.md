## Özet

<!-- Bu PR ne yapıyor ve neden? 1-3 cümle. -->

## Değişiklikler

<!-- Madde madde ana değişiklikler. -->

-
-

## İlgili ADR / Issue

<!-- Örn: Refs ADR-0007, Closes #12. Yeni bir mimari karar içeriyorsa ADR eklenmiş olmalı. -->

- ADR:
- Issue:

## Test

<!-- Nasıl doğrulandı? Birim/entegrasyon/e2e/Pester/eval; manuel adımlar. Doküman PR'ında: bağlantı kontrolü. -->

- [ ] Birim / entegrasyon testleri eklendi veya güncellendi
- [ ] Kurulum etkileniyorsa Pester matrisi çalıştırıldı
- [ ] Çıkarım/prompt değiştiyse eval ve kırmızı takım korpusu çalıştırıldı
- [ ] Doküman değiştiyse göreli bağlantılar kontrol edildi

## KVKK / güvenlik etkisi kontrol listesi

- [ ] **Gerçek e-posta, belge, transkript veya kişisel veri eklenmedi** (test verileri sentetik; ekran görüntüleri maskeli)
- [ ] Sır (secret, anahtar, PFX, `.env`) eklenmedi
- [ ] Loglara içerik (e-posta gövdesi, prompt, model çıktısı) yazılmıyor
- [ ] Yeni bir Graph izni (scope) istenmiyor; isteniyorsa ADR ile gerekçelendirildi. `Mail.Send` istenmiyor
- [ ] LLM'e giden veri akışı değişmedi; değiştiyse politika kapısı (hariç tutma, etiket, özel nitelikli veri) ve sağlayıcı katmanı kuralları korunuyor
- [ ] Veri makineden dışarı çıkmıyor; çıkıyorsa (bulut katmanı, yeni egress uç noktası) KVKK md. 9 / SS-2 etkisi değerlendirildi
- [ ] Güvenilmeyen içerik (e-posta/belge) talimat olarak işlenmiyor; model çıktısı güvenli biçimde gösteriliyor
- [ ] Yerel uç nokta korumaları (Host/Origin denetimi, CSRF, CSP) zayıflatılmadı
- [ ] Onay gerektiren aksiyonlar onaysız yürütülmüyor; denetim kaydı yazılıyor
- [ ] Kişi bazlı performans/duygu puanlaması eklenmedi ([ADR-0023](../docs/adr/0023-no-individual-performance-scoring.md))
- [ ] Saklama/silme davranışı (kaynak silinince türetilmiş kaydın silinmesi) korunuyor
