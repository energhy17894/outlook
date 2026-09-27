# Sorun Giderme

*Durum: Taslak (stub). Ürün henüz mevcut değildir; bu doküman S8'de (sessiz kurulum, Intune ve GPO belgeleriyle birlikte) doldurulacaktır. Başlıklar [araştırma raporu §1–§2](../research/rapor-m365-operasyon-zekasi-platform-plani.md) ve [MSI notları](../research/notes/msi_kurulum_dagitim.md) temel alınarak belirlenmiştir.*

> **Destek taleplerine veya issue'lara gerçek e-posta içeriği, kişisel veri ya da içerik barındıran log eklemeyin.** Tanı paketi içeriksiz olacak şekilde tasarlanmıştır ([ADR-0021](../adr/0021-observability.md)).

## 1. Hızlı kontroller

- [ ] İki servis çalışıyor mu? (`OpsIntel.Host`, `OpsIntel.Intelligence`)
- [ ] `https://localhost:6500/health/live` ve `/health/ready` ne döndürüyor?
- [ ] Windows Event Log'da OpsIntel kaynağından Warning/Error var mı?
- [ ] `%ProgramData%\OpsIntel\logs\` altındaki son log dosyası

## 2. Kurulum sorunları

### 2.1 Port 6500 dolu

*Doldurulacak: SetupHelper port kontrol mesajı, portu kullanan süreci bulma, `PORT` özelliği.*

### 2.2 MSI hata kodları ve `/l*v` logu

*Doldurulacak: 0/3010/1603 vb.; logda "Return value 3" araması.*

### 2.3 Servis başlamıyor

*Doldurulacak: servis hesabı (`NT SERVICE\…`) ve "Log on as a service" hakkı; çalışma dizininin `System32` olması; ACL'ler.*

### 2.4 Yükseltme / onarım / kaldırma

*Doldurulacak: MajorUpgrade davranışı, `REMOVE_DATA`, kalan servis/sertifika/kural temizliği.*

## 3. Sertifika ve tarayıcı güveni

*Doldurulacak: Edge/Chrome/Firefox'ta uyarı; `LocalMachine\Root` ve `LocalMachine\My` kontrolü; `CERT_THUMBPRINT`; yenileme görevi (`setup-helper cert renew`).*

## 4. Oturum açma ve Graph

### 4.1 Admin consent gerekli

*Doldurulacak: yönetilen onay politikası, admin-consent bağlantısı, onay iş akışı.*

### 4.2 Conditional Access / Token Protection engeli

*Doldurulacak: report-only test, tray yardımcısı (Faz 2), CAE claims challenge.*

### 4.3 Yeniden oturum açma uyarısı (token iptali)

### 4.4 Throttling (429) ve yavaş senkronizasyon

*Doldurulacak: posta kutusu başına eşzamanlılık, `Retry-After`, ilk senkronizasyon süresi.*

### 4.5 410 Gone / syncStateNotFound — tam yeniden senkronizasyon

## 5. Yapay zekâ ve modeller

### 5.1 Model indirme başarısız / çevrimdışı site

*Doldurulacak: `MODEL_SOURCE`, model manifesti hash doğrulaması.*

### 5.2 GPU/NPU algılanmıyor, CPU'da yavaş çıkarım

### 5.3 Öneriler "needs review" durumunda kalıyor

*Doldurulacak: alıntı doğrulama başarısızlığı, güven kategorileri.*

## 6. Veri ve depolama

### 6.1 Disk alanı, blob deposu büyümesi

### 6.2 Veritabanı bütünlüğü ve yedekten dönüş

*Doldurulacak: `backup\` klasöründeki `VACUUM INTO` yedekleri; servis durdur/değiştir/başlat.*

### 6.3 BitLocker uyarısı

## 7. Tanı paketi

*Doldurulacak: yönetici arayüzünden veya CLI'dan tanı paketi oluşturma; içeriği (loglar, redakte yapılandırma, sürümler, health, DB bütünlük kontrolü).*

## 8. Destek iletişimi

*Doldurulacak.*
