# Özellik Kataloğu

*Kaynak: [araştırma raporu §3, §6, §8](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [özellikler ve UX notları](../research/notes/ozellikler_ux.md). Faz etiketleri [MVP kapsamı](../roadmap/mvp-scope.md) ile uyumludur. Değer (D) / fizibilite (F) derecelendirmeleri (Y/O/D) notlardan veya rapor §8'den alınmıştır.*

## İlkeler (tüm özellikler için)

1. **Kanıt önce:** Her öğe `{mesaj/belge ID, alıntı}` kanıtı taşır; alıntı kartta **satır içi** gösterilir (kullanıcılar atıf bağlantılarına nadiren tıklar — [NN/g](https://www.nngroup.com/articles/explainable-ai/)).
2. **Doğrulanamayan gösterilmez:** "needs review" durumunda kalır ([ADR-0015](../adr/0015-extraction-contract-evidence.md)).
3. **İnsan onaylar:** Her dışa dönük aksiyon bir taslaktır; harici e-posta asla otomatik gönderilmez ([ADR-0017](../adr/0017-approval-state-machine.md)).
4. **Kişi değil iş:** Kişi bazlı performans/duygu puanı yok; ekip/org metriklerinde ≥5 kişi ([ADR-0023](../adr/0023-no-individual-performance-scoring.md)).
5. **Kullanıcı kontrolü:** Proaktif özellikler kullanıcı ve kiracı düzeyinde kapatılabilir.

## Kanıtlı kayıt: WorkItem türleri

| Tür | Açıklama | Görünüm |
|---|---|---|
| `commitment` | Birinin verdiği söz ("Cuma'ya kadar revize teklifi göndereceğim") — yön: benim borcum / bana olan borç / üçüncü taraf | İşlerim, Beklediklerim |
| `request` | Birinin istediği iş | İşlerim |
| `follow_up` | Benim başkasından istediğim | Beklediklerim |
| `task` | Görev | İşlerim |
| `decision` | Karar (gerekçe, karar veren, tarih; kabul edilmiş karar değişmez, yerine geçme zinciri) | Proje sayfası, karar zinciri |
| `risk` | Risk (olasılık 1–5 × etki 1–5, trend, azaltım) | RAID/RAIDD |
| `assumption`, `issue`, `dependency` | RAID öğeleri | RAID/RAIDD |
| `open_question` | Cevap bekleyen soru (yaşı sağlık sürücüsüdür) | Proje sayfası |
| `obligation` | Sözleşme yükümlülüğü (CUAD kategorileri) | Faz 3 |

Taksonomi Microsoft'un Viva Briefing ayrımına (Commitment, Request, Follow-up) eşlenir. RAID adlandırması yapılandırılabilir (RAID / RAIDD / RAAIDD). Veri modeli: [data-model.md](../architecture/data-model.md).

## Kullanıcı özellikleri

| # | Özellik | Açıklama | Faz | D / F |
|---|---|---|---|---|
| 1 | **İşlerim (My Work)** | Bana gelen talepler + verdiğim sözler + bana atanan görevler; **riskteki sözlerim** (48 saat içinde termin, ilgili giden etkinlik yok); tek tıkla Tamam / Ertele / Görev değil / Yanlış sahip | MVP (S7) | Y / Y |
| 2 | **Beklediklerim (Waiting on)** | Benim takip isteklerim + başkalarının bana verdiği sözler; yaş rozeti; "hatırlatma taslağı" düğmesi (yerel taslak) | MVP (S7) | Y / Y |
| 3 | **İnceleme kuyruğu** | Klavye öncelikli triage: Kabul (A), Düzenle (E), Reddet (R) + gerekçe kodu ("görev değil", "yanlış sahip", "yanlış proje", "yinelenen", "zaten yapıldı"); toplu işlem; geri al | MVP (S7) | Y / Y |
| 4 | **Kanıt kartları** | Satır içi alıntı, etiketli kanıt çipleri ("Mail · Ahmet Y. · 12 Eyl · '…'"), bağlamda vurgulu alıntı çekmecesi, "Outlook'ta aç" / "SharePoint'te aç" | MVP (S7) | Y / Y |
| 5 | **Güven UX'i** | Kategorik güven (Yüksek/Orta/Düşük); düşük güvende N-best alternatifler ("Hangi proje? A · B · Yeni"); "neden işaretlendi" sinyal listesi | MVP (temel) → Faz 2 ("düzeltmelerinizden öğrendiklerim") | Y / Y |
| 6 | **Projeler** | Proje kayıt defteri (takma adlar, kodlar, üye listesi); otomatik proje önerileri (onaylı); proje sayfası (RAID/RAIDD, karar zinciri, açık sorular) | MVP (S6–S7) | Y / Y |
| 7 | **Aşamalar ve kapı kartları** | Yapılandırılabilir aşama FSM'i; "X projesi UAT'ye girmiş görünüyor: 3 sinyal. Onayla?" | MVP (S6) | Y / O |
| 8 | **Proje sağlığı** | Açıklanabilir bileşik skor; sürücüler: ilişki bazına göre sessizlik, cevapsız kalma, açık soru yaşı, geciken taahhüt, duran aşama; her sürücü kanıta gider; eşikler ayarlanabilir (Gong tarzı) | MVP (temel) → Faz 2 | Y / Y |
| 9 | **Zaman çizelgesi ve olay akışı** | Organizasyon/paydaş kulvarları, kilometre taşları, aşama bantları, karar/risk işaretleri; OCEL biçimli olaylar; başlık "hikâye" görünümü | MVP (S7) | Y / Y |
| 10 | **Portföy panosu** | Aşamaya göre aktif projeler, sağlık ısı haritası (proje × hafta), riskli banda giren/çıkan projeler | MVP (temel, S7) → Faz 3 KPI kataloğu | Y / Y |
| 11 | **Sor (Ask)** | İddia bazında atıflı Soru-Cevap; yerel sorular için hibrit arama (FTS5 + vektör, RRF); ajan yalnızca salt-okunur araçlar | MVP (S7) → Faz 3 küresel özetler (LazyGraphRAG) ve text-to-SQL | Y / O |
| 12 | **Arama** | FTS5 trigram + vektör hibrit arama, kanıt ve yapılandırılmış nesneler üzerinde | MVP (S3–S4) | Y / Y |
| 13 | **Yerel aksiyon önerileri** | Yanıt taslağı, görev, hatırlatma → onay → denetim; dışa aktarım (kopyala / .eml / .md) | MVP (S7) | Y / Y |
| 14 | **Türkçe tarih normalizasyonu** | "Cuma'ya kadar", "ay sonu" → mutlak tarih; TR iş takvimi; tr-TR İ/ı duyarlı normalizasyon | MVP (S5) | Y / Y |
| 15 | **Canlı güncellemeler** | SSE ile "3 yeni öneri, 1 aşama geçişi" | MVP (S7) | Y / Y |

## Yönetim ve kurulum özellikleri

| # | Özellik | Açıklama | Faz |
|---|---|---|---|
| 16 | **İlk çalıştırma sihirbazı** | Sağlık/sertifika kontrolü, oturum açma, admin consent bağlantısı, klasör/site seçimi, saklama süresi, model indirme | MVP (S2) |
| 17 | **Politika yönetimi** | Hariç tutulan posta kutusu/klasör/site/alan adı/konu terimi; etiket kuralları; özel nitelikli veri sınıflandırıcı eşikleri; sağlayıcı katmanı (bulut kilidi ve SS-2 kontrol listesi) | MVP (S4) |
| 18 | **Yönetici sayfası** | Servis sağlığı, senkronizasyon durumu, dead-letter işler, OTel metrikleri, BitLocker durumu | MVP (S8) |
| 19 | **Tanı paketi** | İçeriksiz tanı paketi | MVP (S8) |
| 20 | **Denetim kaydı görüntüleme** | Onay/ret/yürütme geçmişi, önce/sonra farkı | MVP (S7–S8) |

## Faz 2 özellikleri (özet)

| Özellik | Sprint |
|---|---|
| M365'e yazma: `createReply` taslakları, To Do/Planner görevleri, katılımcısız takvim "hold" | S9–S10 |
| Eylem türü başına otonomi seviyesi (0: yalnız öneri, 1: onaylı taslak, 2: bildirimli otomatik yerel eylem), çok adımlı onay, acil durdurma anahtarı | S10 |
| Tray yardımcısı: WAM token köprüsü, Windows bildirimleri, sessiz saatler | S11 |
| Günlük brifing (kapatılabilir, zamanlaması ayarlanabilir) | S11 |
| Organizasyon 360 (müşteri/tedarikçi): açık taahhütler iki yönde, SLA, eskalasyonlar, ilişki sağlığı | S12 |
| İlişki bazına göre kişiselleştirilmiş hatırlatmalar ("Müşteri X genelde 1 günde yanıtlar, 4 iş günü geçti") | S12 |
| Karar günlüğü ve RAID dışa aktarımı (DOCX/XLSX/MD) | S12 |
| Bulut katman 2, takma adlandırma, maliyet tavanları; Purview entegrasyonu | S13 |
| Teams transkriptleri ve toplantı hazırlık brifingi; LAN modu | S14 |

Faz 3 ve ek öneriler: [additional-developments.md](additional-developments.md).

## Bilinçli olarak sunulmayanlar

- E-posta gönderme (`Mail.Send` yok)
- Kişi bazlı verimlilik, yanıt hızı sıralaması, duygu puanı, bireyleri sıralayan yönetici panosu
- Kontrol edilemeyen proaktif özetler
- Doğrulanamayan öğelerin "gerçek" olarak gösterilmesi
