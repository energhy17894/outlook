# Personalar

*Kaynak: Personalar raporun ekip, kapsam ve özellik tanımlarından ([araştırma raporu §4, §6, §7](../research/rapor-m365-operasyon-zekasi-platform-plani.md)) ve [özellikler/UX notlarından](../research/notes/ozellikler_ux.md) **türetilmiştir**. Kaynaklarda ayrıntılı persona araştırması (görüşme, anket) bulunmamaktadır; bu personalar pilotta (10–20 kullanıcı) doğrulanmalıdır. Ad ve kurumlar kurgusaldır.*

## P1 — Proje yöneticisi / operasyon sorumlusu (birincil kullanıcı)

- **Bağlam:** Orta ölçekli bir Türk şirketinde müşteri ve tedarikçilerle yürütülen birden çok projeyi takip eder; yazışmalar TR/EN karışık, Outlook ve SharePoint yoğun.
- **İhtiyaçlar:** Kimin neye söz verdiğini, neyin beklendiğini, hangi kararın ne zaman alındığını ve hangi projenin hangi aşamada olduğunu kanıtıyla görmek; unutulan küçük talepleri yakalamak.
- **Kullandığı özellikler:** İşlerim, Beklediklerim, inceleme kuyruğu, proje sayfası (RAID/RAIDD, karar zinciri), zaman çizelgesi, Sor.
- **Güven beklentisi:** Her öğede alıntı; yanlış öğeyi hızla reddedip düzeltebilmek.
- **Endişeler:** Yanlış pozitif gürültü; izleniyor hissi.

## P2 — Bilgi çalışanı / ekip üyesi

- **Bağlam:** Günlük e-posta yükü yüksek; formal PM aracı kullanmayan küçük işler (dosya gönder, bağlantı paylaş) kaybolabiliyor.
- **İhtiyaçlar:** Kendi verdiği sözleri ve kendisinden istenenleri kaçırmamak; hatırlatma taslakları.
- **Kullandığı özellikler:** İşlerim, riskteki sözlerim, hatırlatma taslakları; (Faz 2) günlük brifing ve bildirimler.
- **Not:** Araştırma notlarına göre kullanıcılar yapay zekâ hatırlatmalarını en çok düşük/orta önemdeki unutulabilir işler için istiyor (CSCW 2024 özeti; tam metin doğrulanmadı).

## P3 — Yönetici / PMO (portföy görünümü)

- **Bağlam:** Birden çok projenin sağlığını ve darboğazlarını görmek ister.
- **İhtiyaçlar:** Portföy panosu, sağlık ısı haritası, riskteki projeler, karar ve risk dışa aktarımı.
- **Sınır:** OpsIntel bireyleri sıralayan veya kişi performansı gösteren pano **sunmaz**; metrikler proje ve karşı taraf kurum düzeyindedir, ekip metriklerinde en az 5 kişilik grup uygulanır ([ADR-0023](../adr/0023-no-individual-performance-scoring.md)). Kullanıcı yalnızca zaten erişebildiği içerikten türetilen analizi görür.

## P4 — BT yöneticisi (kurulum ve yönetişim)

- **Bağlam:** Intune/GPO/SCCM ile yazılım dağıtır, Entra'da uygulama onaylarını yönetir.
- **İhtiyaçlar:** Tek imzalı MSI, sessiz kurulum parametreleri, tespit kuralı, yükseltme/kaldırma davranışı; admin consent bağlantısı, yayıncı doğrulaması; en az yetki (Mail.Send yok); içeriksiz loglar ve tanı paketi.
- **Kullandığı özellikler:** İlk çalıştırma sihirbazı, politika yönetimi, yönetici sayfası; [kurulum](../operations/installation.md), [Intune](../operations/intune.md).

## P5 — KVKK danışmanı / DPO

- **Bağlam:** İşlemenin hukuka uygunluğundan ve canlıya geçiş onayından sorumludur.
- **İhtiyaçlar:** Meşru menfaat testi, aydınlatma ve imzalı onaylar, DPIA, VERBİS güncellemesi, SS-2 takibi; bulut katmanının kilitli olduğunun kanıtı; denetim kaydı; ilgili kişi taleplerinin 30 gün içinde karşılanabilmesi.
- **Kullandığı belgeler:** [KVKK uyum çerçevesi](../compliance/kvkk/README.md), [AI Act kapsamı](../compliance/ai-act-kapsam.md).

## Persona dışı: çalışanlar ve dış yazışma tarafları (veri sahipleri)

Uygulamayı kullanmayan ama verisi işlenen kişilerdir. Haklarına (bilgilendirme, itiraz, silme, "Kişisel/Özel" klasörle çıkış) ürün tasarımında birinci sınıf gereksinim olarak yer verilir.
