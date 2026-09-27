# Rakip Analizi ve Ayrışma

*Kaynak: [araştırma raporu §7](../research/rapor-m365-operasyon-zekasi-platform-plani.md), [rakip analizi notları](../research/notes/rakip_analizi.md). Durum: Eylül 2026. Fiyatlar kaynaklardaki liste/alıcı raporu değerleridir ve değişebilir; "ikincil" işaretli iddialar üçüncü taraf kaynaklıdır.*

## 1. Pazar durumu

Eylül 2026 itibarıyla Microsoft bu alanın büyük kısmını bulutta kapsıyor:

- **Copilot in Outlook** numaralı atıflarla başlık özeti çıkarır, önceliklendirme ve taslak yazma yapar. Kurumsal fiyatı kullanıcı başına **30 $/ay**, Business eklentisi 21 $/ay ([Microsoft 365 Copilot pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing)).
- **Copilot Notebooks** 25 Ağustos 2026'dan beri Outlook e-postalarını proje bağlamı olarak tutabiliyor.
- **Copilot Cowork** 16 Haziran 2026'da GA oldu ve uzun görevleri buluttaki Anthropic modelleriyle çalıştırıyor ([Microsoft 365 Blog](https://www.microsoft.com/en-us/microsoft-365/blog/2026/06/16/copilot-cowork-is-now-generally-available/)).

Belgelenmiş boşluklar:

- Triage "English-only" çalışıyor ([justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook)).
- Proaktif sabah/akşam özetleri kullanıcı veya kiracı düzeyinde kapatılamıyor ([Office365ITPros](https://office365itpros.com/2026/09/15/new-outlook-copilot/)).
- Copilot Studio'nun e-posta tetikleyicileri yalnızca ajanı oluşturanın kimlik bilgileriyle çalışıyor ([MS Learn](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-triggers-about)).
- Türkiye, Copilot'un ülke içi işleme listesinde yer almıyor ([Computerworld](https://www.computerworld.com/article/4085303/m365-copilot-data-processing-goes-local-to-meet-sovereignty-demands.html)).
- Viva'nın taahhüt takibi yapan Briefing e-postası duraklatıldı ve yapılandırılmış bir taahhüt kaydıyla değiştirilmedi.

Diğer rakipler:

- **Glean:** Güçlü atıflı arama ve onay seçenekli ajanlar; alıcı raporlarına göre kullanıcı başına ayda yaklaşık 50–75 $ ve asgari 100 koltuk ([Vendr](https://www.vendr.com/marketplace/glean)).
- **Onyx:** Açık kaynak, air-gapped kurulabilen yerel alternatif; ancak sohbet ve arama aracı — proje modeli, e-posta ajanı veya onay akışı yok ([Onyx](https://onyx.app/)).
- **Notion Mail:** 22 Eylül 2026'da kapandı ([TechCrunch](https://techcrunch.com/2026/06/25/notion-mail-shuts-down-amid-agent-takeover/)).

## 2. Karşılaştırma tablosu

| Rakip | Güçlü yanı | Bu ürüne göre boşluğu | OpsIntel'in karşılığı |
|---|---|---|---|
| M365 Copilot (Outlook, Notebooks, Cowork, Planner Agent) | En derin Graph entegrasyonu, atıflı özetler, otonom görevler | Kalıcı ve yaşam döngülü karar/risk/açık soru kaydı yok; başlıklar arası aşama çıkarımı yok; bulutta çalışır; Türkiye'de ülke içi işleme yok; koltuk + kredi maliyeti | Kanıta bağlı kayıtlar, aşama FSM'i, yerel çalışma, sabit lisans |
| Copilot Studio e-posta ajanları | Olay tetikleyicileri | Oluşturanın kimlik bilgisi riski; ~15 eylemden sonra güvenilirlik düşer | Kullanıcı başına delegated erişim, deterministik boru hattı |
| Glean / Dust | Kurumsal arama, konektörler, atıflı yanıtlar | Yüksek fiyat ve koltuk alt sınırı; soruya cevap verir ama kalıcı bir operasyon kaydı tutmaz | Kayıt ve panolar birinci sınıf nesnedir; orta ölçekli şirketlere uygun lisans |
| Onyx | Self-hosted, yerel LLM | E-posta ajanı, proje/aşama modeli, onay ve denetim yok | Bunların hepsi |
| Superhuman / Fyxer / Shortwave | Triage, "waiting on" etiketleri, taslaklar | Kullanıcı başına ve bulutta; organizasyon katmanı yok; Outlook desteği zayıf veya hiç yok | Organizasyon ve proje düzeyinde görünüm, Outlook/Exchange öncelikli |
| Read AI / Otter / Fireflies | Toplantı aksiyon maddeleri | E-posta ve belgeyle birleşik proje zaman çizelgesi zayıf | Posta + belge + (Faz 2) transkript birleşik zaman çizelgesi |
| Celonis / PA Process Mining | Log tabanlı süreç madenciliği | ERP günlüklerine dayalı ve pahalı | İletişimden süreç ve olay akışı çıkarımı |
| Claude for M365 | Arama ve yazma araçları (Temmuz 2026) | Bulutta çalışır, yapılandırılmış kayıt tutmaz | MCP ile OpsIntel kayıtlarını okuyan bir istemci olabilir |

## 3. Konumlandırma

> **"Şirketin operasyonel hafızası, kendi makinenizde."**

Ayrışma yedi başlıkta toplanır:

1. Her karar, risk, açık soru ve taahhüt için alıntı, kaynak ID'si, güven düzeyi ve durum geçmişi taşıyan **kalıcı kayıtlar**.
2. Başlıklar arası **proje ve aşama çıkarımı** ile sapma uyarıları.
3. **İki yönlü taahhüt takibi:** benim borcum ve bana olan borç.
4. Çok adımlı, **denetim izli onay akışı**.
5. **KVKK'ya uygun yerel çalışma** ve Türkçe öncelikli dil işleme.
6. İletişimden türetilen **olay akışı ve darboğaz analizi**.
7. Kullanıcı ve kiracı düzeyinde **kapatılabilen proaktif özellikler**.

## 4. Savunulabilir hendek

Özet ve taslak yazımı hızla metalaşıyor. Microsoft'un Notebooks, Cowork ve Planner Agent yönündeki hızı göz önüne alındığında savunulabilir hendek **yönetişimli kayıt + denetim + yerel dağıtım** üçlüsüdür. Microsoft'un bu üçlüyü yerel (on-prem) olarak sunması yapısal olarak olası görünmüyor.

**Maliyet modeli (rakip notlarından öneri):** Kullanıcı başına AI kredisi ölçümü yerine sunucu başına veya süresiz lisans; Copilot (koltuk başına 21–30 $ + kredi) ve Glean (koltuk başına ~50–75 $, 100 koltuk alt sınırı) ile karşıtlık. Kesin lisans modeli kaynaklarda kararlaştırılmamıştır.

**Birlikte yaşama:** Planner, To Do, Jira'ya dışa aktarım; Teams transkriptlerini içe alma; MCP/API ile Copilot, Claude veya Glean'in kayıtları okuyabilmesi; BT kabulü için Agent 365/Intune envanterine görünürlük.

## 5. Konumlandırmaya yönelik riskler

- Microsoft hızlı ilerliyor; gelecekteki bir "Copilot proje hafızası" boşluğu daraltabilir (risk kaydı R12: Orta/Yüksek). Yanıt: kayıt, denetim ve yerel dağıtımda derinlik.
- Uzun bağlamlı Türkçe çıkarımda yerel LLM kalitesi ve donanım sınırları ölçülmeli (spike E).

## 6. Araştırılamayanlar

Rakip notlarına göre araştırma bütçesi tükendiği için Guru, Writer, Hebbia, Credal, Slack AI ve Missive incelenmedi. Türkiye'de yerel/on-prem e-posta AI talebini ölçen birincil kaynak bulunamadı. Yapılandırılmış karar/risk kaydı sunan niş PMO/RAID araçları taranmadı.
