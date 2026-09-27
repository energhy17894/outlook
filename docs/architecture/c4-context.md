# C4 Bağlam Diyagramı (Seviye 1)

*Kaynak: [araştırma raporu §1 ve §4](../research/rapor-m365-operasyon-zekasi-platform-plani.md). Diyagram GitHub uyumluluğu için Mermaid `flowchart` ile C4 tarzında çizilmiştir.*

Bu diyagram OpsIntel'i tek bir sistem kutusu olarak ve onunla etkileşen kişileri/dış sistemleri gösterir.

```mermaid
flowchart TB
  USER["Kişi: Bilgi çalışanı / proje yöneticisi<br/>İşlerim, Beklediklerim, inceleme kuyruğu,<br/>projeler ve panoları kullanır"]
  ADMIN["Kişi: BT yöneticisi<br/>MSI/Intune ile kurar, Entra admin consent verir,<br/>politika ve sağlayıcı katmanını yönetir"]
  DPO["Kişi: KVKK danışmanı / DPO<br/>Aydınlatma, DPIA, VERBİS, SS-2 onayı"]

  OPS["Yazılım sistemi: OpsIntel<br/>Yerel Windows servisleri + https://localhost:6500<br/>Kanıta bağlı operasyon kaydı, onaylı aksiyon taslakları"]

  ENTRA["Dış sistem: Microsoft Entra ID<br/>OIDC, Conditional Access, CAE"]
  GRAPH["Dış sistem: Microsoft Graph v1.0<br/>Mail, Calendar, Drives/Sites, Search, To Do, Planner"]
  CATALOG["Dış sistem: Foundry Local model kataloğu<br/>Yalnızca model indirme; içerik gönderilmez"]
  CLOUD["Dış sistem: Opsiyonel bulut LLM (Katman 2/3)<br/>Azure OpenAI EU DataZone vb.<br/>Yalnızca SS-2 + politika onayıyla"]
  EH["Dış sistem: Müşterinin Azure Event Hub'ı<br/>Opsiyonel, kurumsal; yalnızca delta tetikler"]
  MDM["Dış sistem: Intune / GPO / SCCM / winget<br/>Dağıtım ve yükseltme"]

  USER -->|"Tarayıcı ile HTTPS"| OPS
  ADMIN -->|"Kurulum, yapılandırma"| OPS
  ADMIN -->|"Admin consent, uygulama kaydı"| ENTRA
  DPO -.->|"Canlıya geçiş kontrol listesini onaylar"| OPS
  OPS -->|"Delegated oturum açma (PKCE)"| ENTRA
  OPS -->|"Delta polling, onaylı yazma (Faz 2)"| GRAPH
  OPS -->|"İlk çalıştırmada model indirme"| CATALOG
  OPS -.->|"Politika izin verirse"| CLOUD
  EH -.->|"Değişiklik bildirimi (outbound tüketici)"| OPS
  MDM -->|"MSI dağıtımı"| OPS
```

## Sınırlar ve güven notları

| Etkileşim | Not |
|---|---|
| Kullanıcı → OpsIntel | Yalnızca loopback; LAN modu Faz 2'de ve kurumsal PKI ile ([ADR-0003](../adr/0003-kestrel-loopback-https-6500.md)) |
| OpsIntel → Graph | Yalnızca delegated; `Mail.Send` hiçbir fazda yok ([ADR-0008](../adr/0008-delegated-graph-no-mail-send.md)) |
| OpsIntel → bulut LLM | Varsayılan kapalı; SS-2 imzalanıp bildirilmeden açılamaz ([KVKK](../compliance/kvkk/README.md)) |
| Event Hub → OpsIntel | Genel URL gerekmez; bildirim içeriğine güvenilmez, yalnızca delta turu tetiklenir ([ADR-0009](../adr/0009-delta-polling-change-detection.md)) |
| Model kataloğu | Model indirme kişisel veri aktarımı değildir; tanı paylaşımı politikayla kapatılır ([güvenlik notları §8](../research/notes/guvenlik_uyum.md)) |

Bir alt seviye için bkz. [c4-container.md](c4-container.md).
