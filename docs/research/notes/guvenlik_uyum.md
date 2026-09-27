# Security, privacy and regulatory compliance for a locally installed M365 "operations intelligence" AI app in Turkey (as of 27 Sept 2026)

Scope: a Turkish company runs an app on Windows PCs (Windows services, local HTTPS UI on port 6500, data stored on the PC). The app reads Outlook, SharePoint and OneDrive content through Microsoft Graph, uses an LLM (local via Foundry Local/Ollama, or cloud via Azure OpenAI/Foundry EU, Anthropic or OpenAI) to extract decisions, risks, tasks and project status, and drafts actions for a human to approve. These notes are not legal advice. Items marked "counsel" need a Turkish data-protection or employment lawyer. Every claim below is labelled either as law in force or as a proposal/soft guidance. Note: the session's web-search budget ran out part-way through. Some points were then checked only by fetching known primary URLs; anything that could not be checked is listed under Gaps.

## 1. KVKK (Law No. 6698, as amended by Law No. 7499): lawful bases, cross-border transfers to cloud LLMs, VERBİS, generative-AI guidance, retention, data-subject rights

### Takeaway
KVKK applies in full to the email and document content the app processes. That content contains personal data of employees and third parties, and it will sometimes include special-category data (health, union membership, criminal records). The two most important legal points for the design are:
- Any cloud LLM call that sends that content abroad is a cross-border transfer. Under the post-7499 Article 9, a regular transfer needs either an adequacy decision or an "appropriate safeguard". In practice that means a Board-issued standard contract (SS-2, controller to processor) with each LLM provider, notified to the Authority within 5 business days. Explicit consent is only available for "arızi" (occasional) transfers, not for a regular data flow.
- Legitimate interest (Art. 5(2)(f)) can support processing ordinary personal data, but it is not a basis for special-category data under Art. 6. This makes content filtering and exclusion of HR, health and legal mail a legal requirement, not just a nice-to-have.

Local inference avoids the transfer question for the inference step only.

### Cited Findings
**General principles and lawful bases (law in force)**
- Art. 4 requires processing to be lawful and fair, accurate and up to date, for specified, explicit and legitimate purposes, "relevant, limited and proportionate", and kept only as long as the law or the purpose requires. — [Law 6698 consolidated text, mevzuat.gov.tr](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 5(1): personal data may not be processed without explicit consent, unless one of the Art. 5(2) grounds applies. The grounds include:
  - (c) necessity for a contract with the data subject;
  - (ç) a legal obligation of the controller;
  - (e) establishing, exercising or protecting a right;
  - (f) the controller's legitimate interests, "provided the data subject's fundamental rights and freedoms are not harmed".
  — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 6(1) lists the special categories: race, ethnic origin, political opinion, philosophical belief, religion, sect or other beliefs, appearance and dress, membership of associations, foundations or unions, health, sexual life, criminal convictions and security measures, and biometric and genetic data. Law 7499 repealed Art. 6(2) and rewrote Art. 6(3). — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- The new Art. 6 conditions for special-category data are:
  - explicit consent;
  - explicitly provided by law;
  - protection of life or bodily integrity;
  - made public by the data subject;
  - establishing, exercising or protecting a right;
  - processing by persons under a confidentiality obligation (e.g. health professionals);
  - legal obligations in employment, occupational health and safety, social security and social services (a new condition);
  - foundations, associations and unions, for their members.
  Legitimate interest is not among them. The Board's "adequate measures" must also be taken. — [Erdem & Erdem, "KVKK'da neler değişti"](https://www.erdem-erdem.av.tr/bilgi-bankasi/kisisel-verilerin-korunmasi-kanununda-neler-degisti); [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)

**Law 7499 facts (law in force)**
- Law 7499 was published in Resmî Gazete No. 32487 on 12 March 2024 and took effect on 1 June 2024.
- Transfers under the old Art. 9 remained valid until 1 September 2024.
- Appeals against administrative fines moved from the criminal courts of peace to the administrative courts.
— [Erdem & Erdem](https://www.erdem-erdem.av.tr/bilgi-bankasi/kisisel-verilerin-korunmasi-kanununda-neler-degisti)

**Cross-border transfers (Art. 9 as amended, law in force)**
- Art. 9(1): transfer abroad is allowed if an Art. 5/6 condition exists **and** there is an adequacy decision for the country, sector or international organisation. Adequacy decisions are published in the Resmî Gazete and reviewed at least every four years. — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 9(4): without an adequacy decision, transfer is allowed if an Art. 5/6 condition exists, the data subject can exercise rights and has effective remedies in the destination country, and one of these appropriate safeguards is in place:
  - (a) an agreement between public bodies, with Board permission;
  - (b) binding corporate rules approved by the Board;
  - (c) a **standard contract announced by the Board**;
  - (ç) a written undertaking with Board permission.
  — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 9(5): "Standart sözleşme, imzalanmasından itibaren beş iş günü içinde veri sorumlusu veya veri işleyen tarafından Kuruma bildirilir." (The standard contract must be notified to the Authority by the controller or processor within five business days of signature.) — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 9(6): only if there is neither adequacy nor a safeguard may data be transferred "arızi olmak kaydıyla" (on an occasional basis) under listed exceptions. These include explicit consent after being informed of the possible risks, contract necessity, and establishing or protecting a right. — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- The KVKK Transfer Guide (KVKK Publication No. 48, January 2025) says "arızi" means one-off or a few times. Regular, recurring transfers cannot rely on the Art. 9(6) exceptions; the controller must fall back to adequacy or a safeguard. — [KVKK, Kişisel Verilerin Yurt Dışına Aktarılması Rehberi](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi)
- The implementing regulation ("Kişisel Verilerin Yurt Dışına Aktarılmasına İlişkin Usul ve Esaslar Hakkında Yönetmelik") was published in RG No. 32598 on 10 July 2024. Its Art. 4(e) defines a transfer as transmitting personal data to a controller or processor abroad, or otherwise making it accessible to one. — [KVKK Transfer Guide](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi)
- The guide states that remote access from a third country, even just viewing on a screen for support, and "yurt dışında bulunan bir bulutta depolama" (storage in a cloud abroad) count as transfers when the three criteria are met. — [KVKK Transfer Guide](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi)
- Board decision 2024/959 of 4 June 2024 adopted four standard contracts:
  - SS-1: controller to controller;
  - SS-2: controller to processor;
  - SS-3: processor to processor;
  - SS-4: processor to controller.
  — [KVKK Transfer Guide](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi); [KVKK Standart Sözleşmeler](https://www.kvkk.gov.tr/Icerik/7929/Standart-Sozlesmeler)
- Who notifies and how:
  - The contract can say whether the exporter or the importer notifies. If it is silent, the exporter notifies.
  - Notification can be physical, by KEP (registered e-mail), or through other Board-set channels such as the "Standart Sözleşme Bildirim Modülü".
  - The notification includes the signed final contract, proof of the signatories' authority, and notarised translations of foreign-language documents.
  - SS-1 and SS-2 must include the exporter's VERBİS details, consistent with its VERBİS registration.
  — [KVKK Transfer Guide](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi); [KVKK Standard Contract Notification Module announcement](https://www.kvkk.gov.tr/Icerik/8043/Standart-Sozlesme-Bildirim-Modulu-Hakkinda-Kamuoyu-Duyurusu)
- Changes to the parties or content, or termination of the contract, must also be notified within 5 business days, per law-firm and consultancy summaries. — [Birasyo summary](https://birasyo.com/tr/blog/kvkk-yurt-disi-veri-aktarimi-yeni-yonetmelik-standart-sozlesme/) (secondary; confirm against the Regulation)
- Background to the reform: under the old Art. 9, more than 80 undertaking applications were made but very few approved. In practice, transfers depended on explicit consent, which made lawful use of most foreign cloud software "neredeyse imkânsız" (almost impossible). — [KVKK Transfer Guide](https://www.kvkk.gov.tr/Icerik/8142/Kisisel-Verilerin-Yurt-Disina-Aktarilmasi-Rehberi)
- **Microsoft KVKK standard contract status is unclear.** In a December 2025 Microsoft Q&A post, a Turkish customer said that despite a LinkedIn statement by Microsoft Türkiye's general manager that standard contracts were included in corporate packages, they had not seen one signed in practice. They asked for a roadmap. There was no Microsoft answer at the time of fetching. — [Microsoft Q&A, "KVKK – Standart Sözleşme hk."](https://learn.microsoft.com/tr-tr/answers/questions/5664711/kvkk-standart-s-zle-me-hk)
- A Turkish Microsoft partner blog says Microsoft had no Turkish cloud datacenter at end-2025 and that M365 data for Turkish tenants sits in EU/EMEA datacenters. — [Micro Bilgi blog](https://www.microsoftkurumsal.com/blog/kvkk-microsoft-365-veri-ikametgahi-shared-responsibility/) (secondary; verify in the tenant's M365 admin "Data location")

**Transparency, rights, security, VERBİS (law in force)**
- Art. 10 (aydınlatma, the duty to inform): when data is obtained, the controller must state its identity, the purposes, the recipients and the purpose of any transfer, the collection method and legal basis, and the Art. 11 rights. — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 11 rights: to learn whether data is processed, request information, learn the purposes, know the domestic and foreign recipients, request correction or deletion (with notice to recipients), and seek compensation. Art. 11(1)(g) gives a right to object to "işlenen verilerin münhasıran otomatik sistemler vasıtasıyla analiz edilmesi suretiyle kişinin kendisi aleyhine bir sonucun ortaya çıkmasına" (an adverse result arising solely from automated analysis). — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 13(2): requests must be answered "en geç otuz gün içinde" (within 30 days at the latest), free of charge. — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 12 (security):
  - The controller must take "gerekli her türlü teknik ve idari tedbirleri" (all necessary technical and administrative measures).
  - It is jointly responsible with processors acting on its behalf.
  - It must audit its own compliance.
  - Breaches must be notified to the data subject and the Board "en kısa sürede" (as soon as possible).
  — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- Art. 16 (VERBİS registration) must cover purposes, data-subject groups and data categories, recipients, **data planned to be transferred abroad**, security measures and maximum retention periods. Changes must be notified "derhâl" (immediately). — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)
- VERBİS exemptions:
  - General rule: exempt if fewer than 50 employees **and** annual balance sheet under 100 million TL (cumulative), and the main activity is not processing special-category data.
  - Board decision 2025/1572 of 4 September 2025: controllers whose main activity is special-category processing are exempt only below 10 employees and 10 million TL.
  — [KVKK announcement](https://www.kvkk.gov.tr/Icerik/8388/KAMUOYU-DUYURUSU); [Mondaq/Esenyel 2026 VERBİS exemptions](https://www.mondaq.com/turkey/data-protection/1736820/2026-verb%C4%B0s-kay%C4%B1t-%C4%B0stisnalar%C4%B1-100-milyon-tl-e%C5%9Fi%C4%9Fi-ve-g%C3%BCncel-kurallar)
- Art. 18 fines as written in the statute (base amounts; revalued every year):

  | Breach | Fine range (TL) |
  |---|---|
  | Duty to inform (aydınlatma) | 5,000–100,000 |
  | Data security | 15,000–1,000,000 |
  | Not complying with Board decisions | 25,000–1,000,000 |
  | VERBİS | 20,000–1,000,000 |
  | Not notifying a standard contract (added by 7499; applies to controllers **or processors**) | 50,000–1,000,000 |

  — [Law 6698](https://mevzuat.gov.tr/MevzuatMetin/1.5.6698.pdf)

**KVKK generative-AI guidance (soft law / guidance)**
- "Üretken Yapay Zekâ ve Kişisel Verilerin Korunması Rehberi (15 Soruda)" (Generative AI and personal data protection, in 15 questions) was published on 24 November 2025. It covers the genAI lifecycle, risks, determining controller status, processing conditions, cross-border transfers and data-subject rights under Law 6698. — [KVKK](https://www.kvkk.gov.tr/Icerik/8547/uretken-yapay-zeka-ve-kisisel-verilerin-korunmasi-rehberi-15-soruda); [SRP-Legal (date)](https://www.srp-legal.com/tr/uretken-yapay-zeka-ve-kisisel-verilerin-korunmasi-rehberi-24-11-2025-tarihinde-kisisel-verileri-koruma-kurumunun-internet-sitesinde-yayimlanmistir/); [Afyonluoğlu (64 pages)](https://afyonluoglu.org/news/kvkk-uretken-yapay-zeka-ve-kisisel-verilerin-korunmasi-rehberi/)
- According to Hergüner's summary, the guide says:
  - Controller/processor roles must be assessed case by case, on the actual activity.
  - "Simply informing users that a GenAI system is being used is not sufficient" for valid explicit consent.
  - It recommends privacy by default ("only necessary data be processed by default"), accessible privacy notices, telling users when they are interacting with genAI, DPIAs, red-team testing, and data mapping/labelling so rights can be exercised.
  — [Hergüner Bilgen Üçer summary](https://herguner.av.tr/en/generative-artificial-intelligence-and-personal-data-protection-guide-published/)
- On 5 March 2026 KVKK published "İş Yerlerinde Üretken Yapay Zekâ Araçlarının Kullanımı" (use of generative AI tools in the workplace). It warns that workplace genAI adoption often happens through individual employee decisions rather than an institutional strategy ("shadow AI"), which makes corporate oversight hard. — [KVKK](https://www.kvkk.gov.tr/Icerik/8674/is-yerlerinde-uretken-yapay-zeka-araclarinin-kullanimi)

### Inferences
**Lawful basis (counsel to confirm)**
- For employees' ordinary personal data in corporate mail and documents, the defensible basis is Art. 5(2)(f), legitimate interest in running projects and operations. It should be backed by a documented balancing test ("meşru menfaat dengesi testi") and, for some purposes, Art. 5(2)(e).
- Consent-based processing is fragile in employment, and it is useless for regular cross-border flows because of Art. 9(6) "arızi".
- For external correspondents (customers, suppliers), the same legitimate-interest basis applies. Transparency can be given through updated privacy notices and website or email-footer links.

**Special-category data must be kept out**
- Because legitimate interest is not an Art. 6 condition, the pipeline should detect and exclude special-category content before any LLM call. This is especially important before a cloud call. Examples include health reports, sick-leave mails, union matters and criminal-record checks.
- Practical controls:
  - Exclude mailboxes and sites (HR, Legal, occupational health, executive).
  - Exclude items carrying sensitivity labels.
  - Run a local classifier for health, union, religion and criminal terms.
  - Discard rather than store such content.

**Cross-border**
- Any Azure OpenAI/Foundry (EU regions or EU DataZone), Anthropic or OpenAI call is a transfer from Turkey. EU hosting does not help unless Turkey issues an adequacy decision for the EU; none was found.
- Required steps per provider:
  - SS-2 (controller → processor) signed with each provider, or with its Turkish/EU contracting entity.
  - Notification to KVKK within 5 business days.
  - SS-3 for the provider's sub-processors, or confirmation that the provider handles onward transfers.
  - Mapping of the provider's DPA terms onto SS-2 (the contract is used as issued).
- Plan the contracting lead time. If a provider will not sign SS-2, only local inference is lawful for real content.
- M365 itself is already a transfer, because the tenant data sits in EU datacenters. The company's existing Microsoft SS-2 status should be confirmed as part of the same workstream.

**VERBİS and records**
- Update the VERBİS inventory with:
  - the new processing purpose (AI-supported project and operations analysis);
  - the data categories (communication content, professional experience, and so on);
  - foreign recipients (the LLM providers);
  - security measures;
  - retention periods for the derived data (extracted decisions, tasks, risks) held on PCs.

**Duty to inform and rights**
- Add an Art. 10 employee notice and policy (see section 2).
- Build per-person search, export, correct and delete over the local stores (derived records, embeddings, caches, logs) so Art. 11 requests can be answered within 30 days.
- Because of Art. 11(1)(g), keep humans deciding. The AI must not produce conclusions about individual employees.

**Security and breaches**
- Laptops holding extracted mail content widen the Art. 12 breach surface. A lost, unencrypted PC is a notifiable breach, which is another reason BitLocker plus app-level encryption is mandatory (section 6).

### Gaps
- The 2026 revalued Art. 18 fine amounts were not verified. The statute shows base amounts, which are revalued annually under the Misdemeanours Law.
- The deletion/destruction regulation ("Kişisel Verilerin Silinmesi, Yok Edilmesi veya Anonim Hale Getirilmesi Hakkında Yönetmelik", RG 28.10.2017) could not be fetched. The commonly cited rules (storage & destruction policy for VERBİS registrants; periodic destruction at most every 6 months; records kept 3 years) come from background knowledge and are not verified here. Counsel should confirm.
- Board decision 2019/10 on breach notification (the widely cited 72-hour deadline to the Board) could not be fetched. Art. 12(5) itself only says "en kısa sürede".
- No KVKK adequacy decision (e.g., for the EU) was found. This absence could not be exhaustively confirmed because search was exhausted.
- Whether Microsoft, Anthropic and OpenAI will sign or pre-sign KVKK SS-2 contracts as of September 2026 is unconfirmed; the Microsoft signal is contradictory. Ask the vendors directly.
- The full text of the KVKK genAI guide (the PDF) was not reviewed. Its exact statements on legitimate interest and on cross-border transfers to genAI services are unverified.

## 2. Turkish case law (Anayasa Mahkemesi, Yargıtay, KVKK Board) and ECHR Bărbulescu on employer access to employee corporate email

### Takeaway
Turkish constitutional and KVKK practice follows Bărbulescu. Employers may process corporate email content only with:
1. clear and complete prior notice of the monitoring and its scope;
2. a legitimate aim;
3. proportionality, including a preference for less intrusive methods and a distinction between flow and content;
4. data limited to the stated purpose.

Content analysis without prior notice has led to violations (AYM 2016/13010) and fines (KVKK 2021/1187, 250,000 TL). An AI system that systematically reads mail content is "content monitoring" and needs an explicit, signed policy before go-live.

### Cited Findings
- **AYM (Constitutional Court) decisions**:

  | Application no. | Decision date | Resmî Gazete | Outcome | Reason |
  |---|---|---|---|---|
  | 2013/4825 | 24 Mar 2016 | 10 May 2016 | No violation | Employees had been given prior notice |
  | 2016/13010 | 17 Sep 2020 | 14 Oct 2020 | **Violation** | Corporate email examined without prior explicit notice |
  | 2018/31036 | 12 Jan 2021 | 5 Feb 2021 | No violation | Clear contractual notice; proportionate use |

  — [ProCompliance analysis of AYM decisions](https://www.procompliance.net/kurumsal-e-posta-hesaplarinin-isverence-incelenmesinin-anayasa-mahkemesi-kararlari-isiginda-analizi/); AYM press releases: [no-violation](https://www.anayasa.gov.tr/tr/haberler/bireysel-basvuru-basin-duyurulari/kurumsal-e-posta-hesabinin-isveren-tarafindan-denetlenmesinin-kisisel-verilerin-korunmasini-isteme-hakki-ve-haberlesme-hurriyetini-ihlal-etmedigi/), [violation](https://www.anayasa.gov.tr/tr/haberler/bireysel-basvuru-basin-duyurulari/calisanin-kurumsal-e-posta-hesabinin-incelenerek-is-akdinin-feshedilmesi-nedeniyle-kisisel-verilerin-korunmasini-isteme-hakkinin-ve-haberlesme-hurriyetinin-ihlal-edilmesi/) (AYM site returned 503 when fetched; content taken from search excerpts and secondary analysis)
- Facts of the 2021 no-violation case: a private-bank employee's contract said corporate email was for business only and could be monitored without notice. The bank dismissed the employee after finding the account used for the spouse's business. — [AYM press release (search excerpt)](https://www.anayasa.gov.tr/tr/haberler/bireysel-basvuru-basin-duyurulari/kurumsal-e-posta-hesabinin-isveren-tarafindan-denetlenmesinin-kisisel-verilerin-korunmasini-isteme-hakki-ve-haberlesme-hurriyetini-ihlal-etmedigi/)
- The AYM said that owning the communication tools does not give the employer "unlimited and absolute" surveillance power. It distinguished monitoring the communication flow from monitoring content, and said content requires weightier justification. — [AYM press releases, via search excerpts](https://www.anayasa.gov.tr/tr/haberler/bireysel-basvuru-basin-duyurulari/kurumsal-e-posta-hesabinin-isveren-tarafindan-denetlenmesinin-kisisel-verilerin-korunmasini-isteme-hakki-ve-haberlesme-hurriyetini-ihlal-etmedigi/)
- Criteria from the 12 January 2021 decision:
  1. Monitoring is limited to management-right purposes (operations, order, security).
  2. There must be legitimate grounds.
  3. Employees must be informed in advance, including of the legal basis, scope, retention and their rights ("Çalışanlar işveren tarafından önceden bilgilendirilmelidir").
  4. The measure must be relevant and suitable to the aim.
  5. It must be necessary, with no less intrusive means available.
  6. Data must be limited to the stated purpose.
  7. There must be a fair balance of the interests involved.

  — [Erdem & Erdem](https://www.erdem-erdem.av.tr/bilgi-bankasi/isverenin-calisanin-e-postalarini-denetlemesi-1212021-tarihli-anayasa-mahkemesi-karari-ile-getirilen-kistaslar)
- **Yargıtay** 22. Hukuk Dairesi (22nd Civil Chamber), E. 2017/21857, K. 2019/9884, 7 May 2019: employer monitoring requires prior notification to the employee. — [ProCompliance](https://www.procompliance.net/kurumsal-e-posta-hesaplarinin-isverence-incelenmesinin-anayasa-mahkemesi-kararlari-isiginda-analizi/)
- **KVKK Board 2021/1187 (25 November 2021)**: an employer accessed a former employee's corporate mailbox, which contained private correspondence with a fiancé and bank statements, without prior notice.
  - The Board rejected the arguments that corporate emails are not personal data, that workplace email is "public", and that business interest justifies inspection without notice.
  - Fine: 250,000 TL.
  — [KVKK decision summary 2021/1187](https://www.kvkk.gov.tr/Icerik/7269/2021-1187)
- **KVKK Board 2023/86 (19 January 2023)**: no violation where:
  - the employee had signed documents referring to the email monitoring policy (Art. 10 duty to inform satisfied);
  - the processing rested on Art. 5(2)(e) (establishing or protecting a right) and (f) (legitimate interest in protecting trade secrets and proper use of corporate systems);
  - monitoring was targeted and proportionate.
  — [KVKK decision summary 2023/86](https://www.kvkk.gov.tr/Icerik/7593/2023-86)
- **ECHR Bărbulescu v. Romania** (Grand Chamber, no. 61496/08, 5 September 2017) found a violation of Art. 8. The factors it set out are:
  - whether the employee was notified of possible monitoring, with notice that is "clear about the nature of the monitoring and be given in advance";
  - the extent of monitoring and the degree of intrusion (flow vs content, all or only part of the communications, limits in time and space, who had access to the results);
  - legitimate reasons, with content monitoring requiring "weightier justification";
  - whether less intrusive methods could have achieved the aim without accessing full content;
  - the consequences for the employee and whether the results were used for the declared aim;
  - adequate safeguards, especially that the employer cannot access actual content unless the employee was notified in advance.
  — [ECHR Q&A on Bărbulescu](https://www.echr.coe.int/documents/d/echr/press_q_a_barbulescu_eng)

### Inferences
- The app processes content, not just metadata, continuously and at scale. That is the most intrusive end of the Bărbulescu/AYM spectrum, so every factor should be designed in.

**Before rollout: signed notice and policy (counsel)**
- An Art. 10 aydınlatma notice plus an updated IT and acceptable-use policy should say that corporate email, SharePoint and OneDrive content is analysed by AI for project and operations purposes. It should list the providers and countries, retention, who sees the outputs, the exclusions, and how to object.
- Collect employee acknowledgements. Decision 2023/86 shows signed policy references were decisive.

**Purpose limitation**
- Write down that outputs will not be used for disciplinary, performance or termination purposes. That also helps with EU AI Act scoping (section 4).
- Any investigation use needs a separate, case-specific, proportionate process.

**Less intrusive design**
- Start with shared or project mailboxes and project SharePoint sites rather than all personal mailboxes.
- Allow opt-out folders or labels (e.g., a "Personal/Private" category) that the app never reads.
- Use metadata-first triage (Mail.ReadBasic returns no body) before any content read.
- Minimise what is kept: store extracted facts with source references, not full copies of mail bodies.

**Access control on outputs**
- A user should see only analysis derived from content they could already access. The app must not become a way for managers to read subordinates' mail.

**Works councils and unions**
- Turkey has no German-style co-determination on this point. Collective agreements or union consultation should still be checked where they exist (counsel).

### Gaps
- The full AYM decision texts could not be fetched (503). The applicant names for 2016/13010 and 2018/31036 are not confirmed here.
- No Yargıtay decision specifically on AI or automated analysis of employee email was found. The case law concerns human inspection. How courts would treat continuous automated analysis is untested.
- KVKK Board decisions after 2023 on email monitoring were not surveyed, because search was exhausted.

## 3. GDPR (if EU data subjects, EU establishments or EU hosting)

### Takeaway
GDPR applies if the Turkish company has EU establishments, such as subsidiaries or branches whose mail is processed (Art. 3(1)). It may also apply under Art. 3(2)(b) if the processing monitors the behaviour of people in the EU, which is arguable for EU-based employees of the group. EU correspondents' emails landing in a Turkish mailbox do not by themselves trigger GDPR for a Turkish-only controller. Where GDPR applies:
- a DPIA is effectively required (systematic, extensive automated evaluation and workplace monitoring);
- Art. 22 limits solely automated decisions with significant effects;
- Art. 88 national employment rules, including works-council co-determination in countries such as Germany, apply.

### Cited Findings
- Art. 3(1): GDPR applies to processing "in the context of the activities of an establishment of a controller or a processor in the Union, regardless of whether the processing takes place in the Union or not". — [GDPR Art. 3](https://gdpr-info.eu/art-3-gdpr/)
- Art. 3(2): GDPR applies to controllers outside the EU processing data of people in the EU where the processing relates to (a) offering goods or services to them or (b) "the monitoring of their behaviour as far as their behaviour takes place within the Union". — [GDPR Art. 3](https://gdpr-info.eu/art-3-gdpr/)
- Art. 35(1) requires a DPIA where processing is "likely to result in a high risk", in particular using new technologies. Art. 35(3) mandates one for "systematic and extensive evaluation of personal aspects… based on automated processing, including profiling" with legal or similarly significant effects, for large-scale special-category processing, and for large-scale systematic monitoring of public areas. — [GDPR Art. 35](https://gdpr-info.eu/art-35-gdpr/)
- Art. 22(1): the right not to be subject to a decision "based solely on automated processing, including profiling" that produces legal or similarly significant effects. Art. 22(3) requires safeguards including human intervention and the right to contest. — [GDPR Art. 22](https://gdpr-info.eu/art-22-gdpr/)
- Art. 88 lets Member States set specific employment-context rules. These must include safeguards for dignity and rights, "with particular regard to the transparency of processing… and monitoring systems at the work place". — [GDPR Art. 88](https://gdpr-info.eu/art-88-gdpr/)
- EU AI Act Art. 26(9): deployers of high-risk AI must use the provider's Art. 13 information when doing their GDPR Art. 35 DPIA. — [AI Act Art. 26](https://artificialintelligenceact.eu/article/26/)

### Inferences
- If any EU subsidiary's mailboxes, sites or employees are in scope, do the following:
  - Run a GDPR DPIA. The KVKK genAI guide also recommends DPIAs (section 1), so one combined DPIA/KVKK risk assessment can serve both regimes.
  - Consult the DPO.
  - Check national monitoring and works-council rules before rollout, e.g., German Betriebsrat co-determination on technical systems capable of monitoring performance or behaviour (counsel).
- The simplest scoping choice for phase 1 is to limit the tenant scope (via Exchange RBAC for Applications and site selection, section 5) to Turkish-entity users and resources.
- Art. 22 is avoided by design: the app only drafts, a human approves, and no decisions are made about individuals.
- EU data subjects (external correspondents) processed by a Turkish controller with no EU establishment: GDPR most likely does not apply, but KVKK does (counsel).

### Gaps
- The EDPB/WP29 DPIA criteria (WP248: "systematic monitoring", "vulnerable data subjects" including employees) and the EDPB employment-monitoring opinion (WP249) could not be fetched. They are not cited as verified.
- German BetrVG §87(1) No. 6 text could not be fetched (503). The works-council point is from background knowledge.

## 4. EU AI Act classification and timeline (including the Digital Omnibus) and Turkey's AI law status

### Takeaway
**Status of the AI Act (law in force)**
- The AI Act reaches a Turkish company only when there is an EU nexus: it places the system on the EU market, deploys it in an EU establishment, or the output is used in the EU (Art. 2(1)(c)).
- The Digital Omnibus on AI (Regulation (EU) 2026/1744, in the OJ on 24 July 2026, in force since 27 July 2026) moved stand-alone Annex III high-risk obligations to **2 December 2027**.
- Article 50 transparency applies from 2 August 2026.
- Prohibitions have applied since February 2025 and GPAI obligations since August 2025.

**Classification of this app**
- It sits right next to Annex III point 4(b): "to allocate tasks based on individual behaviour or personal traits… or to monitor and evaluate the performance and behaviour of persons".
- Extracting tasks and project status is probably outside that, if the intended purpose is documented as project/operations support.
- Any per-person performance or responsiveness scoring, or task allocation based on behaviour, would pull it into high-risk. Profiling blocks the Art. 6(3) escape route.

**Turkey**: there is no AI-specific law in force. Bills and a March 2026 parliamentary commission report exist.

### Cited Findings
- Annex III point 4(b) (high-risk): AI systems "intended to be used to make decisions affecting terms of work-related relationships, the promotion or termination of work-related contractual relationships, to allocate tasks based on individual behaviour or personal traits or characteristics or to monitor and evaluate the performance and behaviour of persons in such relationships". — [AI Act Annex III](https://artificialintelligenceact.eu/annex/3/)
- Art. 6(3) derogation: an Annex III system is not high-risk if it performs (a) a narrow procedural task, (b) improves the result of a completed human activity, (c) detects decision patterns without replacing human assessment, or (d) performs a preparatory task. But an Annex III system that performs **profiling of natural persons is always high-risk**. Providers relying on the derogation must document the assessment and register it (Art. 6(4), Art. 49(2)). — [AI Act Art. 6](https://artificialintelligenceact.eu/article/6/)
- Art. 26(7): employer-deployers must inform workers' representatives and affected workers before putting a high-risk AI system into use at the workplace. Art. 26(2): deployers must assign human oversight to competent people. — [AI Act Art. 26](https://artificialintelligenceact.eu/article/26/)
- Art. 2(1): the Act applies to (a) providers placing on the market or putting into service in the Union, regardless of where they are established, (b) deployers established in the Union, and (c) providers and deployers in third countries "where the output produced by the AI system is used in the Union". — [AI Act Art. 2](https://artificialintelligenceact.eu/article/2/)
- Art. 50:
  - 50(1): providers must ensure systems that interact directly with people inform them that they are interacting with AI.
  - 50(2): providers of systems generating synthetic text, audio, image or video must mark outputs as machine-readable. This does not apply to "an assistive function for standard editing" or to systems that do not substantially alter the input.
  - 50(4): deployers must disclose AI-generated text published to inform the public, unless it underwent human review or editorial control.
  — [AI Act Art. 50](https://artificialintelligenceact.eu/article/50/)
- Digital Omnibus on AI:
  - Regulation (EU) 2026/1744, published in the OJ on 24 July 2026, in force 27 July 2026.
  - Annex III high-risk application moved from 2 Aug 2026 to **2 Dec 2027**; Annex I (product-embedded) to **2 Aug 2028**.
  - Art. 50 transparency still applies from 2 Aug 2026, with a grace period to **2 Dec 2026** for Art. 50(2) machine-readable marking by systems placed on the market before 2 Aug 2026.
  — [Hunton](https://www.hunton.com/privacy-and-cybersecurity-law-blog/eu-digital-omnibus-on-ai-enters-into-force); [Gibson Dunn](https://www.gibsondunn.com/eu-ai-act-omnibus-agreement-postponed-high-risk-deadlines-and-other-key-changes/); [lawandtechnology.eu](https://lawandtechnology.eu/en/digital-omnibus-on-ai-official-journal-regulation-2026-1744/)
- Conflict: one summary (NicFab, as extracted) gave a "fixed" Annex III date of 2 August 2028. Hunton, Gibson Dunn and CSA all say 2 Dec 2027 for Annex III and 2 Aug 2028 for Annex I, so the NicFab reading is treated as an extraction error. — [NicFab](https://www.nicfab.eu/en/posts/digital-omnibus-ai-official-journal/); [CSA research note](https://labs.cloudsecurityalliance.org/research/csa-research-note-eu-ai-act-high-risk-deadline-omnibus-20260/)
- Other Omnibus changes:
  - Art. 4 AI literacy changed from "ensure" to taking measures to "support" AI literacy, with no guaranteed level.
  - Registration of Art. 6(3) non-high-risk Annex III systems kept, with reduced content.
  - New Art. 4a allows special-category data for bias detection under strict necessity (from 27 July 2026).
  - New prohibitions on non-consensual intimate imagery and CSAM from 2 Dec 2026.
  - SME and small mid-cap simplifications.
  — [NicFab](https://www.nicfab.eu/en/posts/digital-omnibus-ai-official-journal/); [Gibson Dunn](https://www.gibsondunn.com/eu-ai-act-omnibus-agreement-postponed-high-risk-deadlines-and-other-key-changes/)
- The Art. 5 prohibitions have applied since February 2025 and GPAI provider obligations since August 2025. Neither was changed by the Omnibus's high-risk deferral. — [CSA research note](https://labs.cloudsecurityalliance.org/research/csa-research-note-eu-ai-act-high-risk-deadline-omnibus-20260/)
- **Turkey (not law):**
  - No AI-specific law is in force.
  - Three bills have been tabled: 2024, a 2025 criminal-code-focused bill, and a 2025 bill focused on Law 5651.
  - The TBMM AI Research Commission report (March 2026, sıra sayısı 260) recommends a Türkiye AI Authority (possibly by converting the Cybersecurity Presidency) and ratifying the Council of Europe AI Framework Convention. Separate legislation would still be needed.
  — [N Partners comparison of the three bills](https://npartners.com.tr/tr/turkiyede-2026-yapay-zeka-kanunu-tbmmye-sunulan-uc-kanun-teklifinin-detayli-karsilastirilmasi); [SETA analysis of TBMM report](https://www.setav.org/turk-yapay-zeka-kanununa-dogru-tbmm-raporunun-hukuki-degerlendirmesi)
- A Türkiye AI Action Plan 2026–2030 was reportedly published through a presidential circular in the Resmî Gazete in mid-2026. It is a policy plan, not binding AI regulation of private deployers. — [Foundern](https://foundern.com/2026/08/turkiye-yapay-zeka-eylem-plani-resmi-gazetede/) (secondary; verify the RG reference)

### Inferences
**Keep the product out of Annex III 4(b) by design and in documentation**
- State the intended purpose as "project/operational information extraction and drafting for human approval".
- Do not provide:
  - per-person productivity, responsiveness, sentiment or "reliability" scores;
  - rankings or dashboards of individuals' overdue tasks meant for managers;
  - automatic task assignment based on people's past behaviour or traits.
- Task owners should be extracted only where the text explicitly names them, and shown in a project context.
- Aggregate risk and status at project level.
- Record an Art. 6(3)-style assessment even if the AI Act does not apply today. It is cheap insurance and matches the KVKK genAI guide's documentation expectations.

**Applicability**
- If the tool is used only inside the Turkish entity on Turkish staff, the AI Act likely does not apply directly (counsel).
- If EU subsidiaries use it, or it is sold to EU customers, the company becomes a provider or deployer in scope. Then:
  - Art. 4 literacy (support measures) and Art. 50(1) (disclose AI interaction in the UI) apply now.
  - Art. 50(2) marking of drafted text may be exempt as "assistive editing". Label AI drafts in any case.
  - High-risk obligations (if the design drifts into 4(b)) would apply from 2 Dec 2027.

**Avoid emotion and sentiment inference about individual employees**
- This keeps it clear of the workplace emotion-recognition prohibition (see Gaps) and of profiling.

**Turkey**
- Monitor the TBMM bill process. Design to the EU AI Act principles as the likely template.

### Gaps
- The exact text of Art. 5(1)(f) (emotion recognition in the workplace) and whether text-based sentiment falls outside it (the definition relies on biometric data) was not fetched. Treat it as "verify with counsel".
- The Commission's Art. 6 high-risk classification guidelines (and their status after the Omnibus) were not retrieved.
- No bill number or committee stage for a 2026 comprehensive Turkish AI bill was confirmed.

## 5. Microsoft 365-side controls: Entra consent, app scoping, Conditional Access, Purview, Defender app governance, honouring labels

### Takeaway
Least privilege in the tenant matters as much as the app's own security. The main controls are:
- **Consent**: disable or limit user consent, route requests through the admin consent workflow, and publish the app as verified and single-tenant.
- **Scoping**: if app-only permissions are used, scope them with **Exchange Online RBAC for Applications** (which replaces Application Access Policies), and remove the unscoped Entra grant.
- **Conditional Access**: require compliant devices; token protection is available for supported native apps.
- **Purview APIs in Graph** (protectionScopes, processContent, contentActivities, sensitivityLabels): honour labels and DLP inline, and put AI interactions into Purview Audit, eDiscovery and Communication Compliance.
- **Defender for Cloud Apps app governance**: monitor the app's Graph usage.

### Cited Findings
**Entra consent (configuration)**
- By default, all users can consent to permissions that do not need admin consent; for example, a user can let an app access their own mailbox. Microsoft recommends allowing user consent only for apps from **verified publishers**.
- The built-in policies are `microsoft-user-default-low` (verified publishers or apps registered in your tenant, low-impact permissions only) and `microsoft-user-default-legacy` (any app).
- The admin consent workflow lets users request admin review. Apps that require user assignment always need admin consent.
- Changes to consent settings do not revoke existing grants.
— [Microsoft Learn: Configure user consent](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/configure-user-consent)

**Exchange Online RBAC for Applications**
- It grants app-only permissions scoped to management scopes (mailbox property filters) or administrative units. It "replaces Application Access Policies".
- Entra grants and RBAC grants are a **union**. You "need to ensure that you removed the organization-wide unscoped permissions assigned in Microsoft Entra ID", otherwise there is no effective scoping.
- Permission changes take 30 minutes to 2 hours to apply because of caching. `Test-ServicePrincipalAuthorization` checks scope.
- Supported roles include `Application Mail.ReadBasic` (no body, preview, attachments or extended properties), `Mail.Read`, `MailboxItem.Read`, `Calendars.Read` and `Mail.Send`.
- Supported protocols are Graph and EWS. Up to 10,000 apps per organisation. Nested group members are out of scope when a group-based scope is used.
— [Microsoft Learn: RBAC for Applications in Exchange Online](https://learn.microsoft.com/en-us/exchange/permissions-exo/application-rbac)

**Token protection (Conditional Access session control)**
- It accepts only device-bound sign-in tokens (PRT), so a stolen token cannot be replayed from another device.
- It is GA for native apps on Windows. Enforceable resources are Exchange Online, SharePoint Online and Teams (plus AVD and Windows 365 on Windows).
- Devices must be Windows 10+ and Entra joined, hybrid joined or registered.
- Microsoft recommends a pilot in report-only mode first.
— [Microsoft Learn: Token protection](https://learn.microsoft.com/en-us/entra/identity/conditional-access/concept-token-protection)

**Defender for Cloud Apps app governance**
- It governs OAuth apps registered in Entra, with predefined policies.
- It detects unusual increases in data usage by overprivileged or highly privileged apps (for example, full mailbox access), and apps that create suspicious inbox rules or run unusual email searches through Graph.
— [App governance overview](https://learn.microsoft.com/en-us/defender-cloud-apps/app-governance-manage-app-governance); [Threat detection alerts](https://learn.microsoft.com/en-us/defender-cloud-apps/app-governance-anomaly-detection-alerts); [Predefined policies](https://learn.microsoft.com/en-us/defender-cloud-apps/app-governance-investigate-predefined-policies)

**Microsoft Purview APIs in Microsoft Graph (for custom and third-party AI apps)**
- `protectionScopes/compute` finds which policies apply and whether to evaluate inline or offline.
- `processContent` evaluates prompts and responses against DLP, allowing inline blocking.
- `contentActivities` sends AI interactions for Audit, eDiscovery, Data Lifecycle Management and Communication Compliance.
- `sensitivityLabels` APIs list labels and compute user rights on labelled content.
- Stated purposes include "Address oversharing concerns by honoring sensitivity labels applied to data" and supporting Insider Risk Management alerts.
— [Microsoft Learn: Overview of Microsoft Purview APIs](https://learn.microsoft.com/en-us/purview/developer/microsoft-purview-sdk-documentation-overview); [Use Purview for other AI apps](https://learn.microsoft.com/en-us/purview/ai-other-apps)

**Protected content and labels**
- Third-party apps that need to read labels or handle protected (encrypted) content are expected to integrate the MIP SDK. Graph cannot apply encryption that travels with the file.
— [Microsoft Tech Community on sensitivity labels](https://techcommunity.microsoft.com/blog/microsoft-security-blog/use-sensitivity-labels-on-all-e-mail-messages-use-encryption-and-protection-wher/3821549); [MIP SDK label metadata](https://learn.microsoft.com/en-us/information-protection/develop/concept-mip-metadata); [Princeton IT decision matrix (secondary)](https://princetonits.com/sensitivity-labels-decision-matrix/)

### Inferences
**Identity model**
- Per-user local install fits **delegated** permissions: `Mail.Read`, `Files.Read`/`Sites.Read.All` as needed, through the user's own sign-in. The app then sees only what the user can see, which matches the output access-control principle in section 2.
- Avoid tenant-wide application permissions on every PC. Never ship client secrets to PCs.
- If a central, app-only mode is needed, confine it to one managed host with a certificate credential. Scope it with Exchange RBAC for Applications to project, shared or pilot mailboxes, exclude HR, Legal and executive mailboxes through the scope filter, and remove the Entra-level `Mail.Read` grant.

**Consent governance**
- Register a single-tenant app and complete publisher verification.
- Set user consent to "verified publishers, low-impact" or disabled, and enable the admin consent workflow.
- Tenant-wide admin consent for the app should be granted by Global Admin/Privileged Role Admin after a documented review.
- Review the app periodically in app governance and set alerts for data-usage spikes.

**Conditional Access**
- Require compliant, Intune-enrolled and BitLocker-encrypted devices, plus MFA, for the app's sign-ins.
- Test whether token protection can be enforced for this custom public client (see Gaps). If not, rely on compliant-device plus sign-in risk policies.

**Honouring sensitivity**
- Before any LLM call, read the item's sensitivity label (`sensitivityLabels` / message label metadata).
- Skip items that are:
  - labelled Confidential or Highly Confidential, or HR, Legal or Privileged;
  - encrypted (RMS/OME);
  - tagged with a Purview retention or confidentiality marker;
  - in excluded folders.
- Call `processContent` to let tenant DLP decide whether content may be sent to an external LLM, blocking cloud routing and falling back to local inference or skipping.
- Emit `contentActivities` so the organisation's Purview Audit, eDiscovery and Communication Compliance see the AI interactions. This also supports the "who saw what" evidence needed for KVKK Art. 12 audits.

**Retention alignment**
- Derived records on the PC should not outlive the source. When the source mail is deleted, or a retention policy removes it, the extracted items should be purged or de-referenced.
- Use Graph change notifications or delta queries to propagate deletes.

**Drafted actions**
- Keep them as Outlook drafts or planner suggestions. Do not request `Mail.Send` at all in phase 1, so the app cannot send even if it is subverted. This reduces excessive agency (section 7).

### Gaps
- It was not verified whether token protection can be enforced for a third-party public client calling Microsoft Graph, as opposed to Microsoft's own native apps. The documentation lists resources (Exchange, SharePoint, Teams), not arbitrary clients.
- SharePoint `Sites.Selected` (per-site app permissions) was not verified in this session. It is the SharePoint analogue to Exchange RBAC for Apps.
- How Graph returns RMS-encrypted mail to a non-MIP app (e.g., as an `.rpmsg` attachment) was not verified. Treat encrypted items as "skip".
- Purview API licensing and billing (pay-as-you-go versus E5) was not retrieved.
- Communication Compliance policy specifics for AI interactions were not retrieved.

## 6. Local-endpoint security (Windows services, local HTTPS UI on port 6500, data at rest on the PC)

### Takeaway
A PC that holds extracted email intelligence and Graph refresh tokens is a high-value target. The baseline is:
- **Encryption**: BitLocker, enforced by Intune compliance, plus app-level encryption of the database (SQLCipher or similar) with keys protected by DPAPI or TPM.
- **Tokens**: MSAL's encrypted token cache or the WAM broker, never plain-text files.
- **Local web UI hardening**:
  - bind to loopback only;
  - validate the Host header strictly (anti-DNS-rebinding);
  - require authentication even on localhost;
  - add CSRF and Origin checks, no permissive CORS, and a strict CSP.
- **No custom root CA**, or at least never one whose private key is exportable or shared.
- **Least-privilege** service accounts.
- **Content-free logs**.
- **Intune wipe** for lost devices.

### Cited Findings
**DNS rebinding**
- DNS rebinding lets a malicious website's JavaScript reach services on 127.0.0.1 or the LAN. The browser keeps treating requests as same-origin after the attacker's DNS switches to a local IP.
- A real example: an unauthenticated Deluge WebUI endpoint was exploited this way to read files and escalate.
- Mitigations: "Check the Host header of the request and deny if it doesn't strictly match an allow list of expected values"; "Always enforce strong, password-based authentication—even for internal services"; use TLS.
- "a permanently deployed local network web application that doesn't require authentication and TLS is a red flag."
— [GitHub Security Blog: DNS rebinding attacks explained](https://github.blog/security/application-security/dns-rebinding-attacks-explained-the-lookup-is-coming-from-inside-the-house/)

**Chrome Local Network Access (LNA)**
- LNA requires a user permission before public websites can send requests to loopback or private addresses. The permission can only be requested from secure contexts.
- Opt-in from Chrome 138; launched with the prompt on by default in Chrome 142.
— [Chrome for Developers: Local Network Access](https://developer.chrome.com/blog/local-network-access)

**Root CA risk**
- Dell System Detect installed a root certificate ("DSDTestProvider") together with its private key in the Windows Trusted Root store. Anyone with the key could forge certificates that the machine would trust and man-in-the-middle HTTPS.
- Remediation was to distrust and remove the certificate, plus Microsoft Certificate Trust List updates.
— [CERT/CC VU#925497](https://www.kb.cert.org/vuls/id/925497)

**MSAL token cache**
- For desktop apps MSAL.NET recommends the cross-platform token cache (`Microsoft.Identity.Client.Extensions.Msal`).
- Microsoft's sample protects the cache file with DPAPI (`ProtectedData.Protect(... DataProtectionScope.CurrentUser)`).
- A "plain-text fallback mode" (ACL-restricted unencrypted file) exists for when encryption fails. It should be disabled or alerted on.
— [Microsoft Learn: MSAL.NET token cache serialization](https://learn.microsoft.com/en-us/entra/msal/dotnet/how-to/token-cache-serialization)

**SQLCipher**
- Provides "Strong 256-bit AES" full-database encryption for SQLite.
- Community (open-source) and commercial editions; the commercial edition is up to 4x faster. There is an Enterprise FIPS edition with an embedded FIPS 140-3 module.
— [Zetetic SQLCipher](https://www.zetetic.net/sqlcipher/)

**Intune Wipe**
- Factory-resets the device.
- The Windows option "Wipe device, and continue to wipe even if device loses power" (`doWipeProtected`) overwrites free space and is meant for lost or stolen devices, though it can leave some devices unbootable.
- There is a tenant limit of 500 wipes per day, and Multi-Admin Approval can gate the action.
— [Microsoft Learn: Wipe devices with Intune](https://learn.microsoft.com/en-us/intune/device-management/actions/wipe)

### Inferences
**Architecture for the port-6500 UI**
- Bind only to `127.0.0.1` and `::1`, never `0.0.0.0`.
- Reject any request whose `Host` is not exactly `localhost:6500` or `127.0.0.1:6500`.
- Require `Origin` and `Sec-Fetch-Site: same-origin` on state-changing requests, plus a synchronizer CSRF token and `SameSite=Strict`, `HttpOnly`, `Secure` session cookies.
- Send no `Access-Control-Allow-Origin` header; never reflect Origin or use `*`.
- Serve a strict CSP (`default-src 'self'; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'`). This also blocks EchoLeak-style image exfiltration (section 7).
- Rate-limit the endpoint.

**Authentication on localhost**
- Other local processes, other Windows users on shared PCs, and malware can all reach the loopback port.
- Authenticate the browser session through an Entra sign-in (auth code + PKCE with a loopback redirect) or a one-time launch token passed from the tray app. Bind the session to the Windows user SID.
- On multi-user machines, reject sessions whose SID does not own the data store.

**TLS**
- Preferred options:
  - (a) Serve plain HTTP only on loopback with strong session auth, if the browser treats `http://localhost` as a secure context (verify per browser).
  - (b) Generate a per-machine self-signed certificate for `localhost` whose private key is non-exportable (CNG/TPM-backed) and trust only that leaf in the current user's store.
- Never install a shared or vendor root CA, and never ship a private key in the installer. The Dell case shows why.

**Services**
- Run the sync and analysis services under a virtual service account (`NT SERVICE\<name>`) or a dedicated low-privilege account, not LocalSystem.
- Separate the attachment parser and document converter into a sandboxed, low-integrity process, since documents are untrusted input.
- ACL the data directory to the service SID and the owning user.

**Data at rest**
- BitLocker is mandatory, enforced through an Intune compliance policy tied to Conditional Access.
- Add app-level encryption:
  - SQLCipher (or SQL Server Express with encryption) for the database;
  - a key wrapped with DPAPI (machine or user scope, per the service design), ideally TPM-bound;
  - encrypted vector-index files.
- This lets "crypto-shred" on offboarding delete the key and so destroy the data.

**Tokens**
- Use the MSAL cache extension with DPAPI, or the WAM broker (which also enables device-bound tokens). Refuse the plain-text fallback.
- Store LLM API keys in Windows Credential Manager or DPAPI, or better, avoid static keys: use Entra ID auth to Azure OpenAI through a central proxy.

**EDR interplay**
- Code-sign all binaries.
- Do not ask for broad antivirus exclusions for the data folder; scanning attachments is desirable.
- Document expected behaviour (Graph polling, local model loading) so Defender for Endpoint baselines are clean.
- Do not disable AMSI or script scanning.

**Logging**
- Log IDs, hashes, counts, timings, policy decisions and errors, never message bodies, prompts or completions.
- Turn off verbose LLM request logging in SDKs.
- Forward security events (auth failures, Host/Origin rejects, policy blocks) to the SIEM.
- Record AI interactions through Purview `contentActivities` instead of local text logs.

**Device loss**
- Run an Intune wipe (protected wipe for lost devices).
- Revoke the user's Entra sessions and refresh tokens.
- Rotate any keys.
- Treat the event as a potential KVKK Art. 12 breach until encryption status is confirmed from Intune/BitLocker recovery evidence.

**Local LLM runtimes**
- Ollama and the Foundry Local optional server expose local HTTP endpoints. The same loopback-bind, Host-check and auth rules apply.
- Prefer the in-process SDK. Foundry Local recommends the SDK over the optional server for embedded scenarios (section 8).

### Gaps
- Microsoft docs for BitLocker/Intune encryption compliance, DPAPI-NG, and Windows virtual service accounts were not fetched in this session. The recommendations reflect standard practice, not cited text.
- Ollama's default bind address and authentication model were not verified.
- Whether each target browser treats `http://localhost` as a secure context was not verified.

## 7. AI-specific security: indirect prompt injection from inbound email, exfiltration through rendering, excessive agency (OWASP LLM Top 10 2025, OWASP Agentic Top 10)

### Takeaway
The app's core input, inbound email and shared documents, is attacker-controlled. EchoLeak (CVE-2025-32711) showed that one crafted email could make M365 Copilot exfiltrate internal data with zero clicks. It did so by combining a classifier bypass, reference-style Markdown links and images, and an allowlisted Microsoft proxy that got around CSP.

Required controls:
- treat all content as data, never as instructions;
- separate trust tiers and scopes;
- keep the model's tool access minimal, with human approval for every action;
- render outputs without remote fetches;
- constrain egress;
- red-team continuously.

### Cited Findings
**EchoLeak: what happened**
- EchoLeak (CVE-2025-32711) was a zero-click indirect prompt injection in Microsoft 365 Copilot.
- A single crafted email could make Copilot pull internal data (chats, OneDrive, SharePoint, Teams) and send it to an attacker server.
- Aim Security called it an "LLM Scope Violation": untrusted external content was mixed into the same context as privileged internal data.
- Microsoft patched it server-side and reported no exploitation in the wild.
— [The Hacker News](https://thehackernews.com/2025/06/zero-click-ai-vulnerability-exposes.html); [Reco](https://www.reco.ai/blog/echoleak-vulnerability)
- Secondary sources give a CVSS score of 9.3. — [Reco](https://www.reco.ai/blog/echoleak-vulnerability)

**EchoLeak: attack chain**
1. The payload was phrased as an instruction to the human recipient, which evaded Microsoft's XPIA (cross-prompt injection) classifier.
2. Reference-style Markdown links (`[text][ref]` with `[ref]: https://evil…`) got past inline-link redaction.
3. Reference-style images auto-fetched by the browser gave zero-click exfiltration.
4. CSP was bypassed through an allowlisted Microsoft Teams asynchronous preview API that fetched attacker URLs.

- Timeline: discovered January 2025 (Aim Labs), fixed server-side May 2025, disclosed 11 June 2025.
— [arXiv 2509.10540, "EchoLeak"](https://arxiv.org/html/2509.10540v1)

**EchoLeak: recommended defences**
- Strict prompt partitioning of untrusted and trusted content.
- Provenance-based access control and least privilege (default to internal sources, trust tiers).
- Input filtering.
- Output validation (URL allowlists, schema validation, secret/PII scanning).
- Default-deny CSP with egress constrained through proxies and HTML/Markdown sanitised to a safe subset.
- Guardrails with continuous red-teaming.
- The paper concludes: "only a layered, defense-in-depth approach can contain this class of threats".
— [arXiv 2509.10540](https://arxiv.org/html/2509.10540v1)

**OWASP Top 10 for LLM Applications 2025**
- LLM01 Prompt Injection
- LLM02 Sensitive Information Disclosure
- LLM03 Supply Chain
- LLM04 Data and Model Poisoning
- LLM05 Improper Output Handling
- LLM06 Excessive Agency
- LLM07 System Prompt Leakage
- LLM08 Vector and Embedding Weaknesses
- LLM09 Misinformation
- LLM10 Unbounded Consumption

— [OWASP GenAI Security Project](https://genai.owasp.org/llm-top-10/)

**OWASP Top 10 for Agentic Applications (2026 edition, released December 2025)**
- ASI01 Agent Goal Hijack (EchoLeak is the cited example)
- ASI02 Tool Misuse
- ASI03 Identity & Privilege Abuse
- ASI04 Agentic Supply Chain
- ASI05 Unexpected Code Execution
- ASI06 Memory & Context Poisoning
- ASI07 Insecure Inter-Agent Communication
- ASI08 Cascading Failures
- ASI09 Human-Agent Trust Exploitation
- ASI10 Rogue Agents

- It builds on the February 2025 "Agentic AI – Threats and Mitigations" taxonomy, updated to v1.1.
— [OWASP GenAI](https://genai.owasp.org/2025/12/09/owasp-top-10-for-agentic-applications-the-benchmark-for-agentic-security-in-the-age-of-autonomous-ai/); [Cycode summary](https://cycode.com/blog/owasp-top-10-agentic-applications/)

### Inferences
Design controls mapped to the threats:

**Injection and goal hijack (LLM01 / ASI01)**
- Wrap every email or document chunk in delimited "untrusted data" blocks.
- The system prompt should say that content must never be followed as instructions.
- Use **structured-output-only** extraction (a JSON schema for decisions, risks, tasks and status) so the model has no free-form channel to act.
- Run a separate lightweight injection classifier. Treat it as a signal, not a guarantee, since EchoLeak bypassed one.

**Scope violation**
- Never mix untrusted external mail and internal privileged documents in one prompt unless the task needs both.
- Keep per-user and per-project context isolation.
- Enforce ACLs at retrieval time for the vector store (LLM08), so a user's query can only retrieve chunks that user could open in M365.

**Excessive agency (LLM06 / ASI02–03)**
- The model gets no tools that act on M365.
- Actions (reply drafts, meeting proposals, task creation) are generated as proposals, shown with a diff and their source citations, and executed only after explicit human approval by deterministic code.
- Do not grant `Mail.Send`, and give no write scopes in phase 1.
- Keep a kill switch and per-user rate limits.

**Improper output handling and exfiltration (LLM05)**
- Render model output as plain text or a sanitised Markdown subset with no remote images. Disable link auto-fetching and previews.
- Show links only if their domain is on an allowlist, and never auto-open them.
- Apply the strict CSP from section 6.
- Add an egress firewall rule so the service can reach only Graph, the login endpoints and the chosen LLM endpoint.

**Sensitive information disclosure (LLM02)**
- Redact or pseudonymise PII before cloud calls where feasible.
- Keep secrets out of prompts, and keep nothing in the system prompt that would matter if leaked (LLM07).

**Misinformation (LLM09)**
- Every extracted decision, risk or task must cite its source message or document ID and a quoted span. The UI marks items as "AI-extracted, unverified" until a user confirms them.
- This also supports KVKK accuracy (Art. 4(2)(b)) and Art. 11 correction.

**Memory and context poisoning (ASI06)**
- Do not treat previously LLM-generated summaries as ground truth for later runs without their provenance.
- Allow per-item deletion, and re-derive from sources.

**Supply chain (LLM03 / ASI04)**
- Pin model versions and hashes for local models.
- Pin SDK versions and keep an SBOM.
- Vet any MCP or tool plug-ins; prefer none.

**Unbounded consumption (LLM10)**
- Put token and cost caps on cloud calls.
- Put batch-size and time limits on local inference so the PC stays usable.

**Assurance**
- Build a red-team corpus of malicious emails (EchoLeak-style reference links, hidden text, instructions in attachments, Unicode tricks). Run it in CI and before each model change.
- The KVKK genAI guide also recommends red-team testing (section 1).

### Gaps
- MSRC's advisory page for CVE-2025-32711 was not fetched directly; the CVSS score comes from secondary sources.
- No published incident was found of a locally installed (non-Copilot) email-AI tool being exploited. The threat model is extrapolated from EchoLeak and the OWASP guidance.

## 8. Model provider data handling: Azure OpenAI/Foundry, Anthropic, OpenAI, and local inference

### Takeaway
- **Azure ("Models sold by Azure")**: prompts and completions are not used for training, not shared with OpenAI, and models are stateless. Processing stays in the chosen geography unless Global or DataZone deployments are used; EU DataZone means any EU member state. Abuse monitoring can involve human review, by EEA-based staff for EEA deployments, unless the customer is approved for **modified abuse monitoring** (verifiable through `ContentLogging=false`).
- **Anthropic**: training is excluded, and ZDR is available by arrangement. But since **9 June 2026 the most capable "Covered Models" (Claude Fable 5/5.1, Mythos 5/5.1) require 30-day retention that overrides ZDR on every platform**. First-party inference geography options are only "us" and "global", with no EU option.
- **OpenAI**: default API retention is 30 days, with ZDR for eligible business customers (August 2026 news).
- **Local inference** (Foundry Local) keeps prompts on the device, apart from model downloads and optional diagnostics. It removes the transfer question for inference but not other KVKK duties.

### Cited Findings
**Azure / Microsoft Foundry ("Models sold by Azure", including Azure OpenAI)**
- Prompts, completions, embeddings and training data are not available to other customers, not available to OpenAI or other model providers, not used to improve their models, and not used to train foundation models without permission.
- Processing is governed by the Microsoft Products and Services DPA.
- "The models are stateless: no prompts or completions are stored in the model."
— [Microsoft Learn: Data, privacy, and security for Models sold by Azure](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/openai/data-privacy)
- Location of processing:
  - Standard deployments: processing stays in the customer-specified geography, but may move between regions within it.
  - "Global": processing may happen in any geography where the model is deployed.
  - "DataZone" in an EU member state: processing may happen "in that or any other European Union Member Nation".
  - Data at rest, including the abuse-monitoring store, stays in the customer geography.
  - Batch is a Global deployment type.
  — [Microsoft Learn: data privacy](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/openai/data-privacy)
- Stateful features (Responses API, Assistants threads, Stored completions, Files/vector stores) store data in the resource's geography, encrypted with AES-256 (customer-managed key optional), and deletable by the customer. Preview features may differ. — [Microsoft Learn: data privacy](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/openai/data-privacy)
- Abuse monitoring:
  - Flagged prompts and completions may be sampled for review. Automated LLM review stores nothing.
  - Human review is by authorised Microsoft employees via Secure Access Workstations with Just-In-Time approval. "For Models sold by Azure deployed in the European Economic Area, the authorized Microsoft employees are located in the European Economic Area."
  - Customers meeting "additional Limited Access eligibility criteria" can apply for **modified abuse monitoring**. Then human review and storage are not performed, though automated review may still run. Some advanced models may have stricter criteria.
  - Approval can be checked in the resource JSON: `"ContentLogging": "false"`.
  — [Microsoft Learn: Abuse monitoring](https://learn.microsoft.com/en-us/azure/ai-foundry/openai/concepts/abuse-monitoring); [Microsoft Learn: data privacy](https://learn.microsoft.com/en-us/azure/ai-foundry/responsible-ai/openai/data-privacy)
- EU Data Boundary:
  - It covers EU and EFTA countries.
  - Azure regional services deployed in an EU Data Boundary region are in scope.
  - For **Microsoft 365, only tenants with an EU or EFTA sign-up location are in scope**; Multi-Geo customers are excluded.
  - Limited transfers outside the boundary still occur and are documented.
  — [Microsoft Learn: What is the EU Data Boundary](https://learn.microsoft.com/en-us/privacy/eudb/eu-data-boundary-learn)

**Anthropic**
- Anthropic's docs cover the Claude API, Claude Platform on AWS and **Claude in Microsoft Foundry, "where Anthropic is the data processor"**. On Bedrock and Google Cloud the cloud provider is the processor.
- "Retained data is never used for model training without your express permission."
- ZDR and HIPAA arrangements exist.
- Features that are not ZDR-eligible: Batch (29-day retention), Files API (kept until deleted or expired), code execution and programmatic tool calling (up to 30 days).
- Even under ZDR, flagged content may be kept up to 2 years, or longer where legally required.
— [Claude Platform Docs: API and data retention](https://platform.claude.com/docs/en/manage-claude/api-and-data-retention)
- Anthropic's commercial retention policy, per the search summary, is deletion of API inputs and outputs within 30 days. The platform doc also says conversation content "is not retained by default" apart from Covered Models. **This is an apparent inconsistency to clear up in the contract or DPA.** — [Anthropic Privacy Center: commercial retention](https://privacy.claude.com/en/articles/7996866-how-long-do-you-store-my-organization-s-data); [Claude Platform Docs](https://platform.claude.com/docs/en/manage-claude/api-and-data-retention)
- **Covered Models** (effective 9 June 2026):
  - Covered: Claude Fable 5, Fable 5.1, Mythos 5 and Mythos 5.1.
  - Prompts and outputs are retained for 30 days for safety work.
  - This overrides ZDR. ZDR organisations must enable 30-day retention per workspace to use these models.
  - It applies on the Claude API, AWS Bedrock, Google Cloud, Microsoft Azure Foundry, Claude Code and Claude Enterprise.
  — [Anthropic Privacy Center: Covered Models](https://privacy.claude.com/en/articles/15425996-data-retention-practices-for-covered-models); [Claude Platform Docs](https://platform.claude.com/docs/en/manage-claude/api-and-data-retention)
- Anthropic data residency:
  - `inference_geo` supports only `"global"` (the default) and `"us"`, at 1.1x price for US-only on Claude 4.6+ models.
  - Workspace storage geo is currently only `"us"`.
  - On Claude in Microsoft Foundry, `inference_geo` does not apply; the "US Data Zone Standard" deployment keeps inference in the US.
  — [Claude Platform Docs: Data residency](https://platform.claude.com/docs/en/manage-claude/data-residency)

**OpenAI**
- The standard API practice is a 30-day retention window.
- On 20 August 2026 OpenAI announced "Private Safety Processing" for ZDR business deployments (automated safety scanning without retaining data) and customer-controlled encryption keys for data stored on OpenAI infrastructure.
— [The Register, 20 Aug 2026](https://www.theregister.com/ai-and-ml/2026/08/20/openai-chases-anthropics-biz-customers-with-zero-data-retention-pledge/5290609) (news report; verify on OpenAI's enterprise privacy pages)

**Foundry Local**
- "Your data never leaves the device."
- The network is used only for model and execution-provider downloads and for optional, user-initiated diagnostic log sharing.
- No Azure subscription is needed.
- It has an OpenAI-compatible API, runs in-process through the SDK, and has an optional local web server.
- Models are versioned and can be pinned.
- It supports Windows, macOS and Linux, with GPU/NPU acceleration.
— [Microsoft Learn: What is Foundry Local](https://learn.microsoft.com/en-us/azure/ai-foundry/foundry-local/what-is-foundry-local)

### Inferences
**Provider policy for the plan: "local-first, cloud by exception"**

| Tier | Model location | What it may process | Conditions |
|---|---|---|---|
| 1 | Foundry Local (in-process SDK) | All content by default, including anything flagged by DLP or labels | — |
| 2 | Azure OpenAI in an EU region, Standard or EU DataZone deployment | Only content that passes label/DLP checks and has been pseudonymised where feasible | Modified abuse monitoring approved (`ContentLogging=false`); `store=false` / no Stored completions / no Assistants threads; no Global or Batch deployments; SS-2 signed and notified |
| 3 | Anthropic/OpenAI direct | Optional | Only with ZDR in place, SS-2 signed and notified, and **no Covered Models** where 30-day retention is unacceptable |

- Tier 3 should be judged against the fact that Anthropic's first-party API offers no EU inference geography.

**Contract nuance**
- "Claude in Microsoft Foundry" makes **Anthropic** the processor, not Microsoft. Using Claude through Azure therefore needs its own KVKK SS-2 and DPA review; it is not covered by Microsoft's "Models sold by Azure" commitments.

**EU Data Boundary does not help a Turkish tenant**
- The tenant's M365 data is outside the EUDB commitment unless its sign-up country is EU or EFTA. For KVKK, EU hosting is still "abroad".

**Local inference is still processing under KVKK**
- Arts. 4, 10, 11 and 12 still apply. What local inference removes is the Art. 9 transfer for the inference step and the need for provider ZDR terms.
- Model downloads are not a personal-data transfer. Diagnostics sharing should be disabled by policy.

**Keep an evaluation record per provider**
- Processor role, sub-processors, retention (default, ZDR, flagged content), human-review location, training exclusion, inference and storage geography, KVKK SS-2 availability, breach-notification SLA, and certifications (ISO 27001/27701/42001, SOC 2).

### Gaps
- OpenAI's current API data-residency options (e.g., EU regional storage and processing) and ZDR eligibility were not verified from primary OpenAI pages.
- The exact eligibility criteria for Azure "modified abuse monitoring" (the Limited Access form) and any stricter rules for newer models were not retrieved.
- Whether Anthropic models on AWS Bedrock or Google Cloud EU regions give EU-only processing, and how Covered Models retention is handled there, was not verified.
- The Anthropic retention inconsistency (30-day default versus "not retained by default") needs contract confirmation.

## 9. Relevant certifications and frameworks (ISO 27001, ISO/IEC 42001, SOC 2, Microsoft 365 App Compliance, NIST AI RMF)

### Takeaway
None of these is legally required in Turkey, but they give the compliance workstream its structure:
- **ISO 27001**: extend the ISMS to cover the new app and endpoints.
- **ISO/IEC 42001**: an AI management system for AI governance.
- **NIST AI RMF** (Govern / Map / Measure / Manage) plus the GenAI Profile (NIST AI 600-1): the risk register.
- **Microsoft 365 App Compliance**: Publisher Attestation, then M365 Certification, relevant if the app is ever distributed to other tenants.
- **SOC 2 Type II**: relevant for a commercial offering.

### Cited Findings
- The Microsoft 365 App Compliance Program has three routes:
  - **Publisher Attestation**: self-assessment of security, data handling and compliance.
  - **Microsoft 365 Certification**: a yearly independent audit including penetration testing, assessed against controls based on SOC 2, PCI DSS and ISO 27001 across Application Security, Operational Security, and Data Handling, Security & Privacy.
  - **ACAT** (App Compliance Automation Tool) for automating evidence.
- Certification is "especially recommended for apps that handle sensitive data… or interact with Microsoft Graph", and "in some cases, certification may be required for apps to be enabled within a Microsoft tenant".
- The program covers apps and agents integrating with Teams, Copilot, Outlook, SharePoint and others, plus SaaS. Audits are contracted through Claranet.
— [Microsoft Learn: M365 App Compliance Program overview](https://learn.microsoft.com/en-us/microsoft-365-app-certification/overview)
- Microsoft recommends allowing user consent only for apps from verified publishers. Publisher verification is therefore a practical prerequisite for smooth tenant adoption. — [Microsoft Learn: Configure user consent](https://learn.microsoft.com/en-us/entra/identity/enterprise-apps/configure-user-consent)
- NIST AI RMF:
  - AI RMF 1.0 was released on 26 January 2023. It is voluntary, with four functions: Govern, Map, Measure and Manage.
  - The Generative AI Profile (NIST AI 600-1) was released on 26 July 2024.
  - NIST states "The AI RMF 1.0 is being revised as part of the White House AI Action Plan".
  - On 7 April 2026 NIST announced a concept note for a critical-infrastructure profile.
  — [NIST AI RMF](https://www.nist.gov/itl/ai-risk-management-framework)
- The KVKK genAI guide recommends privacy by design and by default, DPIAs, red-team testing and data mapping. These map naturally onto ISO 42001 and NIST AI RMF controls. — [Hergüner summary of the KVKK guide](https://herguner.av.tr/en/generative-artificial-intelligence-and-personal-data-protection-guide-published/)

### Inferences
**Internal-only tool**
- Bring the app into the existing ISO 27001 ISMS scope (asset inventory, risk assessment, Annex A controls for endpoint, cryptography, access control, supplier relationships for the LLM providers, logging).
- Use ISO 42001 or NIST AI RMF as the AI governance layer: AI policy, impact assessment, model and provider lifecycle, human oversight, incident handling.
- Certification is optional; alignment is enough.

**If productised**
- Complete publisher verification, then Publisher Attestation, then M365 Certification (annual).
- Add SOC 2 Type II. Add ISO 27001 and ISO 42001 certification for enterprise buyers.
- Provide a KVKK package: SS-2 templates, a VERBİS input sheet, and an aydınlatma template.

**Suggested compliance and security workstream deliverables (for the plan)**
1. Legal basis memo and legitimate-interest balancing test (KVKK Art. 5(2)(f)/(e)), plus a special-category exclusion design (Art. 6) — counsel.
2. Employee aydınlatma notice, IT/email monitoring and AI-use policy update, and signed acknowledgements. Check any union or collective agreement (AYM/Bărbulescu criteria).
3. Cross-border transfer pack: provider inventory, SS-2 (and SS-3 where relevant) signed per cloud LLM provider, KVKK notification within 5 business days, and a tracker for amendments and terminations.
4. VERBİS update (purpose, categories, foreign recipients, security measures, retention) and storage and destruction policy update for the derived data.
5. A combined DPIA/KVKK risk assessment, including GDPR if EU entities are in scope, and an AI Act applicability and Annex III 4(b) scoping memo (with no individual performance scoring).
6. M365 tenant hardening: consent policy, admin consent workflow, app registration and publisher verification, Exchange RBAC for Apps scoping (if app-only), Conditional Access (compliant device, MFA, token-protection test), app governance policies, and Purview integration (labels, DLP `processContent`, `contentActivities` audit).
7. Endpoint security baseline:
   - loopback-only UI with Host/Origin/CSRF/CSP controls and authentication;
   - no root CA;
   - least-privilege service accounts;
   - BitLocker, SQLCipher and DPAPI;
   - MSAL encrypted cache or WAM;
   - content-free logging;
   - Intune wipe runbook;
   - a pen test of the local web UI.
8. AI security:
   - an OWASP LLM and Agentic threat model;
   - structured-output extraction;
   - untrusted-content partitioning;
   - no-auto-send human approval;
   - sanitised rendering and egress allowlist;
   - a red-team corpus including EchoLeak-style payloads, run in CI.
9. Provider governance: Azure modified abuse monitoring application, ZDR agreements, Covered Models avoidance, `store=false`, and an annual provider review.
10. Data-subject rights tooling (per-person search, export and delete across local stores, within 30 days) and a breach runbook that includes lost laptops.

### Gaps
- The ISO/IEC 42001 page (iso.org) returned 403. Its details (published 2023, a certifiable AI management system standard) come from background knowledge, not verified here.
- SOC 2 (AICPA Trust Services Criteria) and ISO 27001:2022 sources were not fetched in this session.
- Whether the M365 App Compliance program applies to a locally installed desktop app that only calls Graph (rather than a Teams app, add-in or SaaS app) is unclear from the overview. The listed product scope includes "SaaS" but not desktop clients.
