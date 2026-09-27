# Features, UX Patterns and Domain Models for an M365 "Operations Intelligence" Web App + Mail Agent (state as of Sept 2026)

Scope: a Windows PC-local web app (https://localhost:6500) that reads Microsoft 365 mail threads, SharePoint/OneDrive documents and optionally Teams meetings/calendar, and turns them into projects, phases, tasks, commitments, decisions, RAID items, timelines and BI. Every item is backed by evidence, and every outbound action is a draft that needs approval. Legend used in the Inferences: **[V]** value, **[F]** feasibility, each rated H/M/L.

---

## 1. Domain model: entities, established frameworks (RAID, PMBOK/PRINCE2, RACI, decision records) and how tools/APIs represent them

### Takeaway
The PM frameworks give an established vocabulary: RAID/RAIDD for the register, PMBOK/PRINCE2 for phases and stage gates, RACI for roles, and ADR-style immutable decision records with "superseded-by". Microsoft's own products and research already split mail-derived tasks into **commitments / requests / follow-ups**, which maps directly to "my tasks" and "waiting on". For grounding, model every extracted item as an object linked to qualified evidence anchors (OCEL 2.0-style event–object relations plus W3C Web Annotation text selectors), not as free text.

### Cited Findings
**RAID / RAIDD**
- A RAID log tracks Risks, Assumptions, Issues and Dependencies. A risk is an uncertain future event (it can be positive or negative). An issue is a problem that has already happened and needs attention. An assumption is something believed true but not yet confirmed. A dependency is a task or event the project relies on. — [Asana RAID log guide](https://asana.com/resources/raid-log); [Smartsheet RAID guide](https://www.smartsheet.com/content/raid-project-management)
- Many practitioners extend RAID with Decisions (RAIDD), and some with Actions, and there are "RAAIDD" variants that stress assumptions and dependencies. So "A" and "D" are ambiguous across organizations (Actions vs. Assumptions; Dependencies vs. Decisions). — [Resulting-IT: RAID vs RAAIDD](https://www.resulting-it.com/raid-vs-raaidd-log-pmo); [PMP-practitioners RAIDD log](https://pmp-practitioners.com/product/risk-assumptions-issues-dependencies-decision-log-raidd-log/); [ProjectManagement.com RAID Register template](https://www.projectmanagement.com/deliverables/834600/raid-register--risks--assumptions--issues-and-dependencies-)

**Phases / lifecycle**
- PMBOK 7th edition replaced the 10 knowledge areas with 8 performance domains: Stakeholders, Team, Development Approach & Life Cycle, Planning, Project Work, Delivery, Measurement, Uncertainty. — [Twproject on PMBOK 7](https://twproject.com/blog/team-performance-domain-whats-new-pmbok-7/)
- PMBOK defines a phase as "a collection of logically related project activities that culminates in the completion of one or more deliverables". A stage gate ("phase review" / "kill point") blocks progress until an authority approves after review. The life-cycle domain covers predictive, adaptive and hybrid approaches. — [Pearson IT Certification, PMBOK 7 Life Cycle domain](https://www.pearsonitcertification.com/articles/article.aspx?p=3178910&seqNum=2)
- PRINCE2 has 7 processes: Starting up, Directing, Initiating, Controlling a Stage, Managing Product Delivery, Managing Stage Boundaries, and Closing a Project. Initiation produces the Project Initiation Documentation (PID) and the business case. Closing prepares input for the Project Board to confirm closure. — [PRINCE2 processes (prince2.com)](https://www.prince2.com/usa/prince2-processes); [PRINCE2 wiki](https://prince2.wiki/processes/)

**Roles**
- RACI = Responsible (does the work), Accountable (the single owner answerable for the outcome), Consulted (input sought before a decision), Informed (told afterwards). A common rule is exactly one Accountable per task or deliverable. — [Atlassian RACI chart](https://www.atlassian.com/work-management/project-management/raci-chart); [Wikipedia: Responsibility assignment matrix](https://en.wikipedia.org/wiki/Responsibility_assignment_matrix)

**Decisions**
- In Nygard-style decision records, status is Proposed, Accepted, Deprecated, or "Superseded by ADR-NNNN". Accepted records are treated as immutable: a changed decision gets a new record that supersedes and links to the old one. — [Nygard template (joelparkerhenderson/architecture-decision-record)](https://github.com/joelparkerhenderson/architecture-decision-record/blob/main/locales/en/templates/decision-record-template-by-michael-nygard/index.md); [hidekazu-konishi ADR operations guide](https://hidekazu-konishi.com/entry/architecture_decision_records_templates_and_operations.html)

**Mail-derived task taxonomy (Microsoft)**
- Viva Insights' Briefing email sorts mail tasks into three types. A **Commitment** is something you promised to do for someone. A **Request** is something someone asked you to do. A **Follow-up** is something you asked someone else for. Users can mark an item Done or choose "Remind me", which adds it to To Do. Microsoft has paused sending Briefing emails "to make improvements"; the Outlook add-in and Teams app remain. — [Microsoft Learn: Follow up on tasks with the Briefing email](https://learn.microsoft.com/en-us/viva/insights/personal/briefing/be-tasks) (now redirects to [Microsoft Support](https://support.microsoft.com/viva/insights/personal-insights-in-viva-insights)); [Briefing email overview](https://learn.microsoft.com/en-us/viva/insights/personal/Briefing/be-overview)
- Microsoft Research's email intent work groups workplace email intents into **Request Information, Schedule Meeting, Promise Action** (Wang, Hosseini, Awadallah, Bennett, Quirk, SIGIR 2019). — [MSR publication page](https://www.microsoft.com/en-us/research/publication/context-aware-intent-identification-in-email-conversations/)
- The Teams Meeting AI Insights API returns `meetingNotes` (title, text, nested subpoints), `actionItems` (title, text, `ownerDisplayName`) and `viewpoint.mentionEvents` (timestamp, transcript utterance, speaker identity). This is a ready-made shape for meeting-derived tasks with evidence. — [Microsoft Learn: Meeting AI Insights API](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/meeting-insights)

**Object-centric events and evidence anchoring**
- OCEL 2.0 (Berti, Koren, Adams, Park et al., March 2024) models events, objects, event types, object types and attributes. It adds **qualified event-to-object (E2O) and object-to-object (O2O) relationships** and **dynamic object attributes** (values that change over time). Exchange formats are SQLite, XML and JSON. — [arXiv 2403.01975](https://arxiv.org/abs/2403.01975); [ocel-standard.org](https://www.ocel-standard.org)
- W3C Web Annotation (Recommendation, 23 Feb 2017) defines **TextQuoteSelector** (exact text plus optional prefix and suffix to disambiguate) and **TextPositionSelector** (start/end character offsets). Selectors can be chained with `refinedBy` so there is a fallback when content changes. — [W3C Web Annotation Data Model](https://www.w3.org/TR/annotation-model/)

**Contracts / obligations**
- CUAD v1 has 13,000+ expert labels over 510 commercial contracts in **41 clause categories**, including Effective/Renewal/Expiration Date, Governing Law, Termination for Convenience, Post-Termination Services, Revenue/Profit Sharing, Price Restrictions, Indemnity and Confidentiality. It is a ready taxonomy for an "Obligation/Contract term" entity. — [CUAD on Zenodo](https://zenodo.org/records/4595826); [CUAD paper (Hendrycks et al.)](https://www.worldcc.com/portals/iaccm/Resources/10045_0_CUADpaper.pdf)

### Inferences
- **Proposed core schema** (SQLite locally; column names are suggestions):
  - `Project` (id, name, aliases[], customer_org_id, vendor_org_ids[], phase_id, health {score, band, drivers[]}, start/end estimates, confidence, status {active/on-hold/closed}, created_by {auto|user}, merged_into_id)
  - `Phase` (project_id, kind ∈ {lead/proposal, negotiation/contract, kickoff/initiation, design, execution, test/UAT, delivery/go-live, invoicing/payment, warranty/support, closure}, entered_at, exited_at, gate_evidence_ids[], inferred|confirmed). The enum is a pragmatic merge of the PRINCE2 processes and a commercial B2B lifecycle (offer→contract→delivery→invoice).
  - `Milestone` (project_id, title, due_at, achieved_at, source).
  - `WorkItem` supertype with `kind` ∈ {task, commitment, request, follow_up, decision, risk, assumption, issue, dependency, open_question, obligation}. Shared fields: title, description, project_id, owner_person_id, counterparty_person_id/org_id, due_at, due_text (the original phrase, e.g. "Cuma'ya kadar"), status, priority, confidence, extractor_version, review_state ∈ {suggested, accepted, edited, rejected}, evidence_ids[], supersedes_id/superseded_by_id. A single supertype keeps one list/board/queue UI for everything, and typed extension tables hold the extra fields:
    - `Commitment`: direction ∈ {i_owe, owed_to_me, third_party}, promise_text, fulfilled_evidence_id, state machine `open → at_risk (due soon, no activity) → overdue → fulfilled | cancelled | renegotiated`.
    - `Decision`: rationale, alternatives_considered, decided_by[], decided_at, status (Proposed/Accepted/Superseded/Reversed), superseded_by_id. Accepted decisions are immutable, following ADR practice.
    - `Risk`: probability (1–5), impact (1–5), exposure = P×I, trend, owner, mitigation, trigger, risk_vs_issue flag (a risk becomes an issue when it materialises).
    - `Assumption`: validate_by date. `Dependency`: upstream/downstream party. `OpenQuestion`: asked_by, asked_to, asked_at, answered_evidence_id.
    - `Obligation` (from contracts): CUAD clause type, trigger/date, party, amount.
  - `Person` / `Stakeholder` (email aliases, org_id, role, RACI per project), `Organization` (domain(s), type ∈ {customer, vendor, partner, internal}).
  - `Evidence` (id, source_type ∈ {mail, attachment, sharepoint_file, onedrive_file, teams_transcript, calendar_event, meeting_ai_insight}, graph_ids {internetMessageId, conversationId, driveItemId, webUrl}, text_quote {exact, prefix, suffix}, text_position {start, end}, page/slide/cell locator, source_timestamp, author, content_hash, extracted_at, model_id). Use W3C-style quote+position selectors so the UI can deep-link and highlight the quote, and so an anchor survives HTML/plain-text re-rendering.
  - `Event` table in an OCEL 2.0-compatible shape (event_id, activity, timestamp, attributes) plus E2O links with qualifiers (e.g. "sender", "about-project", "fulfils-commitment") and O2O links (project↔org "customer-of", decision↔decision "supersedes"). This gives the timeline and process mining for free (see §3).
- **RAID naming:** make the register configurable (RAID / RAIDD / RAAIDD) because organizations disagree on what "A" and "D" mean. **[V:H][F:H]**
- **RACI inference:** derive R/A/C/I suggestions from mail roles (To vs. Cc, who approves, who is asked for input) and flag "no Accountable" or "2+ Accountable" on tasks as a governance warning. **[V:M][F:M]**

### Gaps
- I found no PMI primary source that defines "RAID" (PMBOK does not name it; the definitions come from vendor and practitioner guides). The claim seen in a search snippet that "projects with an active RAID log have 31% lower major schedule slip (PMI Pulse 2024)" could not be verified in any PMI source and should **not** be used.
- No published schema standard exists for "commitment" objects beyond Microsoft's product taxonomy and the research datasets (Enron/Avocado annotations).

---

## 2. Project phase and health inference from communication signals

### Takeaway
Phases are best inferred from **artefact and speech-act signals**: offer or quote documents, PO/contract signature, kickoff invites, UAT threads, delivery/acceptance notes, invoices. These should be confirmed by the user at "gates". Health is best computed as an **explainable composite** of latency, silence, open-question age, overdue commitments, stakeholder breadth and escalation/negative language. Sales-intelligence products (Gong deal warnings) and escalation-prediction research provide validated analogues. Absolute latency thresholds must be **relationship- and calendar-aware**, because reply behavior varies hugely by time of day, weekday and internal vs. external recipient.

### Cited Findings
- Gong's configurable deal warnings: **No Activity** (no emails/calls by either side for X days); **Ghosted** (no prospect-side activity for X days); **Overdue** (close date in the past); **Insufficient Contacts** (≤N active contacts for X days); **Lack of Seniority** (no Director/VP/C-level active and closing within X days); **Pricing Not Discussed**; **Red Flags** (an email labelled "red flag" in the last X days); **Stalled Progress** (no stage change for X days). — [Gong Help: Customize deal warning settings](https://help.gong.io/docs/customize-your-deal-warning-settings)
- Gong also recommends multi-threading (several stakeholders) over single-threaded deals as a risk mitigation. — [Gong Deal Warnings guide (PDF)](https://www.gong.io/wp-content/uploads/2023/01/Deal-Warnings-Guide.pdf); [Gong: 5 deal warnings](https://www.gong.io/resources/guides/5-deal-warnings)
- Enterprise email reply behavior (Yang, Dumais, Bennett, Awadallah; SIGIR 2017; Avocado corpus):
  - 52.99% of enterprise emails are non-dyadic (more than one recipient).
  - Reply rate is highest in the afternoon (7.77%) and lowest at night (3.63%). Median reply time is under 1 hour for morning/afternoon mail and over 7 hours for evening/night mail.
  - Weekend emails have 13× (Sunday) to 30× (Saturday) longer reply latency.
  - Emails with requests are about twice as likely to get a reply (14.81% vs 7.45%) but take longer (median 81 vs 55 min).
  - External-recipient emails have a 2.26% reply rate vs 7.76% internal, with a median of 134.7 vs 65.5 min.
  - Historical interaction features (individual and pairwise) were among the most important predictors.
  — [SIGIR'17 paper PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2017/04/sigir17a.pdf)
- Email deferral study (Sarrafzadeh et al., WSDM 2019; 40,000 anonymous users plus 15 interviews): deferral is driven by lack of time/resources, competing tasks, sender identity and recipient-list size. Useful features include message length, the number of unhandled emails, and whether mail is human- or machine-generated. — [MSR blog: Email overload](https://www.microsoft.com/en-us/research/blog/email-overload-using-machine-learning-to-manage-messages-commitments/)
- Support-ticket escalation prediction at IBM (Montgomery, Damian, Bulmer, Quader; Requirements Engineering journal 2020): over 2.5M tickets and 10,000 escalations; recall 87.36%; 88.23% less analyst workload to find at-risk tickets. Feature engineering based on analyst knowledge beat the baseline. — [arXiv 2010.06145](https://arxiv.org/abs/2010.06145)
- Sentiment differs considerably between escalated and non-escalated support tickets. — [arXiv 2010.13684](https://arxiv.org/html/2010.13684v1)
- Wolf et al. (ICSE 2009) found that no single communication-network measure predicts build failure, but combinations of communication-structure measures can. — [IEEE Xplore](https://ieeexplore.ieee.org/document/5070503/); [ResearchGate](https://www.researchgate.net/publication/221553828_Predicting_build_failures_using_social_network_analysis_on_developer_communication)
- Asana uses the status values On track / At risk / Off track / On hold / Complete. Its AI "smart status" drafts status updates by scanning tasks, milestones and blockers, surfacing open questions and roadblocks for the user to edit before posting. — [Asana status updates](https://asana.com/features/project-management/status-updates); [Asana Smart Status help](https://help.asana.com/s/article/smart-status?language=en_US) (help-page body did not render; details are from the search-result snippet)
- Stage gates ("phase review points / kill points") are formal approval checkpoints between phases in PMBOK. — [Pearson IT Certification](https://www.pearsonitcertification.com/articles/article.aspx?p=3178910&seqNum=2)

### Inferences
- **Phase signal catalogue** (rules plus LLM classification per thread and document; bilingual TR/EN keywords are illustrative):
  - Proposal/offer: attachments named "teklif/offer/quotation/RFQ/RFP", pricing tables, "fiyat", "revize teklif".
  - Contract: "sözleşme/contract/NDA/PO/sipariş", DocuSign/e-imza notifications, legal reviewers on Cc.
  - Kickoff: calendar invite titled "kick-off/açılış", RACI or plan documents, new domain participants.
  - Design: "tasarım/spec/şartname/analysis", design document versions on SharePoint.
  - Execution: status threads, daily/weekly progress, supplier logistics.
  - Test/UAT: "UAT/test/kabul testi/bug/hata", test-report files.
  - Delivery: "teslim/delivery/go-live/sevk/irsaliye", acceptance protocol ("kabul tutanağı").
  - Invoicing: "fatura/invoice/ödeme/payment/vade".
  - Closure: "kapanış/lessons learned/warranty/garanti", archive.

  Model the phase as a **probability distribution with an argmax plus the evidence list**. Only move the displayed phase when confidence clears a threshold **or** the user confirms at a "gate" card ("Looks like Project X entered UAT: 3 signals. Confirm?"). **[V:H][F:M]**
- **Health composite** (0–100 with coloured bands, always showing its drivers, Gong-style):
  - (a) *Silence*: days since last counterparty message vs. that relationship's own baseline, not a global constant (latency depends heavily on external vs. internal and on day/time).
  - (b) *Ghosting*: we sent the last N messages without reply.
  - (c) *Open-question age*: count and oldest unanswered question.
  - (d) *Commitment slippage*: overdue commitments by us or by them.
  - (e) *Stakeholder breadth*: single-threaded project, or senior sponsor silent.
  - (f) *Escalation language / negative sentiment*: e.g. "urgent/acil", "disappointed", "escalate", legal or management added to Cc.
  - (g) *Stalled phase*: no phase change for X days compared with typical phase duration.
  - (h) *Deadline proximity* with unresolved blockers.

  Every driver links to evidence. Thresholds are user-tunable like Gong's X-days settings. **[V:H][F:H]** for (a)–(e) and (g), which are metadata/rule-based. **[F:M]** for (f), which is LLM-based and needs calibration.
- **Business-hours and holiday calendars** (Turkey plus counterparties' countries) must normalise latency metrics, or weekend mail will produce false "silence" alarms. **[V:M][F:H]**
- A later-stage ML model (after labels accumulate from user confirmations) can combine drivers, following the escalation-prediction and Wolf et al. finding that combinations beat single signals. **[V:M][F:L–M]**

### Gaps
- I found no peer-reviewed study that validates **email-signal-based phase detection for general B2B projects**. The phase catalogue above is design inference. Sales-stage inference (Gong/CRM) is the closest commercial analogue, but its accuracy figures are not public.
- No public benchmark exists for "stakeholder silence" thresholds. The SIGIR'17 medians come from one tech-company corpus (Avocado, early 2000s) and may not transfer to Turkish industrial firms.

---

## 3. Event flows, timelines and process mining from email

### Takeaway
Academic work since about 2016 converts email into event logs by classifying messages into (process, instance/case, activity) and using timestamps. The LLM era (2024–2026) shows **fine-tuned models beat zero/few-shot prompting** for text-to-event-log. An object-centric representation (OCEL 2.0) fits this app better than a single "case ID", because one email can touch a project, a contract, a PO, a customer and several commitments at once.

### Cited Findings
- Laga, Elleuch, Gaaloul, Alaoui Ismaili (Orange Labs / Telecom SudParis, ATAED 2019): mine business processes from email by incrementally building an annotated corpus collaboratively with users, then classifying emails into process, instance and activity IDs. Features include exchange history, correspondents, references and named entities. Instance clustering assumes that emails in the same process instance have close timestamps and share correspondents. The authors note that content-only features degraded activity recognition and that earlier methods needed considerable human effort. — [CEUR-WS Vol-2371 paper](https://ceur-ws.org/Vol-2371/ATAED2019-54-70.pdf)
- Other pre-LLM approaches:
  - supervised text classification (fastText) combined with process discovery — [Preprints.org](https://www.preprints.org/manuscript/202005.0007/v2)
  - unsupervised frameworks for mining process models from email logs — [ResearchGate](https://www.researchgate.net/publication/308361731_A_framework_for_mining_process_models_from_emails_logs)
  - pattern-discovery activity detection — [Springer](https://link.springer.com/chapter/10.1007/978-3-030-58638-6_6)
  - a reproducible approach for mining business activities from emails — [Springer](https://link.springer.com/chapter/10.1007/978-3-031-14135-5_6)
- Semantic role labelling of event-log text attributes (up to eight semantic roles per event) using language models. — [arXiv 2103.11761](https://arxiv.org/pdf/2103.11761)
- LLM event-log extraction:
  - CoopIS 2024 paper "Event Log Extraction for Process Mining Using Large Language Models". — [Springer](https://link.springer.com/chapter/10.1007/978-3-031-81375-7_4) (abstract not accessible, paywall)
  - Seeth, Tavares, Schuster (arXiv, 1 Sept 2026): fine-tuned LLMs as "automated data translators" from unstructured text to event logs, built on a new text-to-log dataset. Fine-tuning "outperforms few-shot or zero-shot prompting by a large amount" and is called "a necessary pre-condition for generating reliable event data". — [arXiv 2609.01320](https://arxiv.org/abs/2609.01320)
- OCEL 2.0 supports qualified E2O/O2O relationships and changing object attributes, in SQLite/XML/JSON. It is described as more expressive and more readable than XES for object-centric process mining. — [arXiv 2403.01975](https://arxiv.org/abs/2403.01975). The website ocel-standard.org hosts the spec, example logs and tool support. — [arXiv 2403.01982](https://arxiv.org/abs/2403.01982)
- A proposed simpler core model for object-centric event data (OCED) discusses the design space. — [arXiv 2410.14495](https://arxiv.org/pdf/2410.14495)

### Inferences
- **Case notion:** use the *object-centric* view. Object types are Project, Thread (conversationId), Document, Organization, Person, Commitment, Decision, PO/Invoice. Activity labels are a controlled vocabulary, for example: `OfferSent, OfferRevised, POReceived, ContractSigned, KickoffHeld, DesignDocShared, QuestionAsked, QuestionAnswered, CommitmentMade, CommitmentFulfilled, DecisionMade, RiskRaised, EscalationRaised, UATStarted, DefectReported, Delivered, InvoiceSent, PaymentReceived, ProjectClosed`. Each event links to its evidence. **[V:H][F:M]**
- **Visualizations:**
  - (1) *Project timeline* with **swimlanes per organization or stakeholder**, milestone diamonds, phase bands and decision/risk markers; clicking any marker opens the evidence quote. **[V:H][F:H]**
  - (2) *Directly-follows process map* across projects of the same type, to show the typical path (offer→PO→kickoff…) and deviations. **[V:M][F:M]**
  - (3) *Sankey of phase transitions* (including loops such as UAT→Execution rework). **[V:M][F:H]**
  - (4) *Phase-duration box plots* to benchmark the current project against history. **[V:M][F:H]**
  - (5) *Thread "story" view*: condensed chronological narrative of a thread with speech acts (request / commitment / decision) as chips. **[V:H][F:H]**
- **Export OCEL 2.0 (SQLite/JSON)** so advanced users can analyse the data in external process-mining tools. This is low cost, because the same event table powers the UI. **[V:M][F:H]**
- Because fine-tuning beats prompting for text-to-log, plan to collect user-validated events (from the approval queue) as a **local fine-tuning or few-shot exemplar dataset** over time. **[V:M][F:M]**

### Gaps
- I did not find a published end-to-end evaluation of LLM-based **OCEL** extraction from **enterprise email** specifically. The Sept 2026 paper does not mention XES/OCEL compliance in its abstract.
- There is no Turkish-language email process-mining dataset that I could find.

---

## 4. Personal productivity: my tasks, waiting-on, nudges, briefings, meeting prep, SLA, priority

### Takeaway
Two decades of Microsoft research and products validate the core loop: detect **commitments/requests/follow-ups** in email, remind, and let users mark Done or snooze. Users value AI reminders most for the small, easy-to-forget items, not for the high-stakes work they already track formally. Copilot now offers inbox prioritization with explanations and meeting prep, but it is **gated by an M365 Copilot licence**. A local app can deliver a comparable, explainable experience without that licence.

### Cited Findings
- **Commitment detection:** Azarbonyad, Sim, White (WSDM 2019) show commitments can be extracted reliably within a domain, but performance drops across domains (Enron vs. Avocado). Adversarial domain adaptation helps, and commitment vocabulary is largely domain-independent. — [MSR publication](https://www.microsoft.com/en-us/research/publication/domain-adaptation-for-commitment-detection-in-email/); [MSR blog](https://www.microsoft.com/en-us/research/blog/email-overload-using-machine-learning-to-manage-messages-commitments/)
- **Smart To-Do** (Mukherjee et al., ACL 2020): generates to-do items from emails in which the sender promises an action, using a two-stage approach (detect, then generate). Built on Avocado (≈938k emails, 279 employees) with 9,349 human-annotated instances; BLEU 0.23 / ROUGE 0.63. — [arXiv 2005.06282](https://arxiv.org/abs/2005.06282)
- **Product lineage:** Cortana (2017) suggested reminders for commitments found in Outlook email ("I'll send you the report by Friday") and pinged before the deadline. — [Windows Experience Blog](https://blogs.windows.com/windowsexperience/2017/03/06/windows-10-tip-cortana-can-automatically-remind-commitments/). The Viva Briefing email later surfaced commitments, requests and follow-ups with Done / Remind me → To Do; it is currently paused. — [Microsoft Learn Briefing overview](https://learn.microsoft.com/en-us/viva/insights/personal/Briefing/be-overview)
- **CSCW 2024 study** (Morrison, Iqbal, Horvitz) of knowledge workers' experience with the Viva daily Briefing email (interviews plus surveys) studies what people want AI reminders for, how they want to interact with them, and which work styles benefit. — [arXiv 2403.01365](https://arxiv.org/abs/2403.01365); [MSR page](https://www.microsoft.com/en-us/research/publication/ai-powered-reminders-for-collaborative-tasks-experiences-and-futures/). A search-result summary (not verified against the full text) says users preferred reminders for low-to-medium-importance tasks, because high-stakes items live in formal PM tools, and that power users rephrased their own emails so the model would catch action items. — [author PDF](https://erichorvitz.com/AI_powered_collaborative_reminders_CSCW_2024.pdf)
- **Time cost:** reading and answering email takes up to 28% of enterprise workers' time (a McKinsey figure cited by Microsoft Research). — [SIGIR'17 paper](https://www.microsoft.com/en-us/research/wp-content/uploads/2017/04/sigir17a.pdf); [MSR blog](https://www.microsoft.com/en-us/research/blog/email-overload-using-machine-learning-to-manage-messages-commitments/)
- **Copilot "Prioritize my inbox"** (Outlook):
  - marks high-priority mail with an up arrow and shows a short explanation of *why* in the reading pane
  - users add natural-language rules ("Emails from [manager] are High Priority"), up to 15 instructions across High and Low
  - rules take up to 15 minutes to apply
  - only mail received after enabling is prioritized
  — [Microsoft Support](https://support.microsoft.com/en-us/outlook/copilot-outlook/prioritize-my-inbox); [Practical365](https://practical365.com/prioritize-my-inbox/) (details are from search-result summaries of these pages)
- **Copilot meeting prep** ("Prepare with Copilot"):
  - summarizes purpose, related context, tasks and documents, using documents shared with attendees, prior emails and chats among participants
  - "Summarized Insights are only available for 1:1 meetings and meetings that have related content"
  - the page advises users to "Always check the information provided"
  — [Microsoft Support](https://support.microsoft.com/en-us/outlook/prepare-for-your-meeting-with-copilot)
- **Meeting Insights retirement:** secondary reporting says Outlook's free Meeting Insights began retiring in mid-August 2026, replaced by the licensed Copilot prep feature. — [Windows News (secondary)](https://windowsnews.ai/article/outlooks-meeting-insights-goes-dark-in-august-2026-and-copilot-wont-fill-the-gap-for-everyone.439221)
- **Inbox-wide Copilot Chat:** a secondary source says that as of July 2026, Copilot Chat in Outlook reasons across the entire inbox, calendar and meetings to surface action items and follow-ups. — [A Guide to Cloud & AI, July 2026 (secondary)](https://www.aguidetocloud.com/blog/microsoft-365-copilot-july-2026-updates/)
- **Gmail nudges:** Gmail resurfaces received mail you haven't answered and sent mail with no reply after a few days (e.g. "Sent 4 days ago. Follow up?"), and users can turn nudges off in Settings. — [Google Gmail Help](https://support.google.com/mail/answer/6585?hl=en); [Mailmeteor explainer](https://mailmeteor.com/blog/gmail-nudges)
- **Low-cost importance labelling:** Argo (Ray et al., incl. Microsoft authors; arXiv, May 2026) reports 148–167× inference-cost reduction versus GPT-class labelling for email importance, with "negligible quality degradation". This is relevant for running priority scoring locally or cheaply. — [arXiv 2605.21604](https://arxiv.org/abs/2605.21604)
- **Reply-time predictability:** reply rate and latency depend on requests, attachments, internal/external recipients, time of day and pairwise history. — [SIGIR'17 paper](https://www.microsoft.com/en-us/research/wp-content/uploads/2017/04/sigir17a.pdf)

### Inferences
- **"My Work" hub** (value/feasibility highest):
  - **My tasks** = requests to me + my commitments + tasks assigned to me in meetings (Meeting AI Insights `ownerDisplayName` or transcript). **[V:H][F:H]**
  - **Waiting on** = my follow-ups (requests I made) + others' commitments to me, each with an age badge and a "nudge draft" button. **[V:H][F:H]**
  - **Promised by me, at risk**: due within 48h and no related outgoing activity. **[V:H][F:H]**
  - One-click Done / Snooze / Not a task / Wrong owner. These feedback signals feed extraction quality (see §5).
- **Nudges personalised by relationship baseline:** "Customer X usually replies in about 1 day; it has been 4 business days." This beats Gmail's generic "few days". **[V:H][F:H]**
- **Daily briefing** (morning, local toast plus an in-app page, optional email to self):
  - overdue and due-today commitments
  - new requests
  - waiting-on items older than baseline
  - today's meetings with prep links
  - projects that changed health band
  - decisions made yesterday

  Keep it short and scannable. Weekly: a per-project digest. **[V:H][F:H]**
- **Meeting prep brief** (local, no Copilot licence needed): attendees and their orgs, last N threads with them, open commitments both ways, open questions, recent decisions, relevant SharePoint docs (recently modified or shared), and suggested agenda points. This fills the gap left by the Meeting Insights retirement for unlicensed users. **[V:H][F:M]**
- **SLA/response tracking:** customer-facing threads carry a configurable SLA (e.g. 1 business day for the first reply); show a breach countdown. Aggregate SLA metrics only at team or organization level (see §6 on privacy). **[V:M][F:H]**
- **Priority scoring with explanation chips:** "From key customer", "Contains request to you", "Deadline Friday", "Escalation words", "Project at risk". Natural-language user rules in the Copilot style (up to about 15). **[V:H][F:M]**
- Focus on the small items: per the CSCW 2024 findings, the feature should excel at the forgettable small asks ("send the link", "share the file"), not try to replace a formal PM system for critical tasks. Integrate with Planner/Jira for those instead (§7).

### Gaps
- No public accuracy numbers exist for Copilot's or Viva's current commitment/priority detection.
- I could not verify the CSCW 2024 findings beyond the abstract; the low/medium-importance preference comes from a search-result summary.
- There is no Turkish-language commitment-detection dataset or benchmark that I could find.

---

## 5. Human-in-the-loop and trust UX: citations, confidence, approve/edit/reject, feedback, undo, explanations

### Takeaway
The main guidance points the same way: Microsoft HAX's 18 guidelines, Google PAIR's Explainability + Trust and Feedback + Control chapters, and NN/g's 2025 research on citations. **Show claim-level, labelled, deep-linked evidence**; prefer categorical confidence over raw percentages; make dismissal and correction cheap; collect granular feedback; and don't assume citations get checked, since users rarely click them. For agentic actions, permission policies should be **user-level and per-action-type**, not one-size-fits-all.

### Cited Findings
- **HAX Guidelines:** Microsoft's 18 Guidelines for Human-AI Interaction (Amershi et al., CHI 2019, Honorable Mention) were synthesized from 168+ candidate guidelines and validated in a study with 49 design practitioners across 20 AI products. They are grouped by phase: *initially*, *during interaction*, *when wrong*, *over time*. — [MSR publication](https://www.microsoft.com/en-us/research/publication/guidelines-for-human-ai-interaction/); [HAX Toolkit](https://www.microsoft.com/en-us/haxtoolkit/ai-guidelines/); [MSR blog](https://www.microsoft.com/en-us/research/blog/guidelines-for-human-ai-interaction-design/)
  - The guidelines are:
    - G1 Make clear what the system can do; G2 Make clear how well it can do it
    - G3 Time services based on context; G4 Show contextually relevant information
    - G5 Match social norms; G6 Mitigate social biases
    - G7 Support efficient invocation; G8 Support efficient dismissal; **G9 Support efficient correction**; **G10 Scope services when in doubt** (disambiguate or degrade gracefully)
    - G11 Make clear why the system did what it did; G12 Remember recent interactions; G13 Learn from user behavior; G14 Update and adapt cautiously
    - **G15 Encourage granular feedback**; G16 Convey the consequences of user actions; G17 Provide global controls; G18 Notify users about changes
  - The G9, G10 and G15 wording was verified on HAX pages ([G10](https://www.microsoft.com/en-us/haxtoolkit/guideline/scope-services-when-in-doubt/), [G15](https://www.microsoft.com/en-us/haxtoolkit/guideline/encourage-granular-feedback/)). The other names come from the CHI 2019 guideline set listed in the [AAAI 2020 tutorial deck](https://www.microsoft.com/en-us/research/wp-content/uploads/2020/01/HAI_Guidelines_AAAI_Tutorial_2020_distribution.pdf).
- **Google PAIR Guidebook** (chapters: User Needs + Defining Success, Data Collection + Evaluation, Mental Models, Explainability + Trust, Feedback + Control, Errors + Graceful Failure) — [PAIR Explainability + Trust](https://pair.withgoogle.com/chapter/explainability-trust/):
  - Aim for *calibrated* trust, not maximal trust. People show both algorithm aversion and over-trust.
  - Explain data sources (scope, reach, removal).
  - Use partial explanations: general-system, specific-output, example-based and counterfactual.
  - Show confidence only when it changes user decisions. Formats: **categorical (High/Med/Low)**, **N-best alternatives** (useful at low confidence), numeric % (risky without context), visualizations (best for experts).
  - Trust must be established early, grown, maintained and recovered through error-recovery plans.
- **NN/g "Explainable AI in Chat Interfaces"** (Megan Chan, 12 Dec 2025) — [NN/g](https://www.nngroup.com/articles/explainable-ai/):
  - citations are often hallucinated or inaccurate, yet "people rarely click citation links", so the mere presence of citations inflates trust
  - step-by-step "reasoning" explanations are often unfaithful to the model's actual computation
  - recommendations: place sources next to specific claims; use meaningful labels instead of a generic "Source"; deep-link to the relevant section; state that sources may be wrong; put specific, actionable disclaimers near the input rather than in footers; avoid anthropomorphic language
- **Agent permission research:** Michael & Roesner (arXiv, July 2026) surveyed 21 academic proposals and 5 commercial agents on how agent permissions are specified (UI), derived (internal policy) and enforced (runtime). They argue for **user-level** permission policies because users differ, and cite prompt injection and hallucination as core risks. — [arXiv 2607.13718](https://arxiv.org/abs/2607.13718)
- **Evidence anchoring:** W3C TextQuoteSelector (exact + prefix/suffix) and TextPositionSelector enable robust deep-linking and highlighting of quoted evidence. — [W3C Web Annotation](https://www.w3.org/TR/annotation-model/)
- Microsoft's own meeting-prep documentation tells users to "Always check the information provided". Vendor guidance thus assumes human verification. — [Microsoft Support](https://support.microsoft.com/en-us/outlook/prepare-for-your-meeting-with-copilot)

### Inferences
- **Evidence-first item cards:** every extracted item shows 1–3 evidence chips labelled "Mail · Ahmet Y. · 12 Sep · 'Cuma'ya kadar revize teklifi göndereceğim'". Clicking opens a side drawer with the highlighted quote in context (thread view or document page), plus "Open in Outlook/SharePoint" via webLink/webUrl. Because users rarely click, **show the quote inline** on the card, not just a link. **[V:H][F:H]**
- **Categorical confidence** (High/Medium/Low, or "Needs review") driven by extractor score plus corroboration count (how many independent evidence items). At low confidence, use **N-best** alternatives for owner, due date or project assignment (for example "Which project? A · B · New"), following G10. **[V:H][F:H]**
- **Review queue ("Inbox of suggestions")** with keyboard-first triage: Accept (A), Edit (E), Reject (R) with reason codes such as "not a task", "wrong owner", "wrong project", "duplicate", "already done". Support bulk actions and an undo toast lasting about 10 seconds, plus a full undo history (G8, G9, G15). **[V:H][F:H]**
- **Feedback → learning loop:** store every correction as a labelled example. Use accepted/rejected pairs as few-shot exemplars per user/tenant, per-sender rules ("mails from noreply@… never create tasks"), and threshold tuning. Periodically show "What I learned from your corrections" (G13, G14, G18). **[V:H][F:M]**
- **Action drafts behind approval gates**, with autonomy per action type:
  - Level 0: suggest only
  - Level 1: draft for approval (default for reply drafts, meeting requests, escalations, external mail)
  - Level 2: auto-execute with notification (e.g. creating a *local* task, adding a Planner task to my own plan)
  - Never auto-send external email.

  Show a **consequence preview** before approval (recipients including external domains, attachments, which thread, what will change: G16) and a global kill switch (G17). The rationale for user-level, per-action policy is Michael & Roesner. **[V:H][F:H]**
- **Explanations:** "Why this is flagged" lists the detected signals (e.g. request phrase + due date + you are in To), not a chain-of-thought narrative, given NN/g's warning about unfaithful step-by-step explanations. **[V:M][F:H]**
- **Capability disclosure (G1/G2):** onboarding and an "About this extraction" panel stating what sources are read, known weaknesses (sarcasm, implicit commitments, Turkish/English mixing, scanned PDFs) and measured precision on the user's own reviewed items. **[V:M][F:H]**
- **Audit log:** an immutable log of every agent action and approval (who approved, when, the before/after diff). This is essential for enterprise trust and for the decision-log export. **[V:H][F:H]**
- **Prompt-injection hygiene:** treat email and document content as untrusted data. Extraction outputs cannot trigger actions without the approval gate, which is a direct consequence of the injection risk highlighted by Michael & Roesner. **[V:H][F:H]**

### Gaps
- Statistics seen in search snippets attributed to "NN/g 2024" (e.g. "72% of users say AI language affects trust" and "63% more likely to rely on AI showing confidence") could not be traced to an NN/g primary source and should **not** be used.
- I did not find a controlled study measuring how inline quotes (vs. links) change verification behavior in enterprise email tools specifically.
- A claim attributed to NN/g about "progressive delegation after 40 approved suggestions" came from a non-NN/g blog and was not verified.

---

## 6. Business intelligence: KPIs, dashboards, natural-language Q&A, scheduled reports, privacy-safe analytics

### Takeaway
Portfolio BI should aggregate the same evidence-backed objects: projects, phases, commitments, decisions, RAID. Microsoft Viva Insights is the benchmark for privacy-safe collaboration analytics: minimum group size of 5 (cannot go lower), de-identification, hashed subjects, domain/term exclusions and differential privacy. For "ask your projects" questions spanning the whole corpus, graph-based retrieval (GraphRAG) clearly beats plain vector RAG on comprehensiveness and diversity.

### Cited Findings
- Viva Insights **minimum group size defaults to 5 and cannot be set lower**. Charts hide groups below the threshold. Admins can **hash subject lines** and **exclude domains, specific email addresses and subject-line terms** (e.g. "confidential; ACP; privileged") from analysis. — [Microsoft Learn: Viva Insights privacy considerations](https://learn.microsoft.com/en-us/viva/insights/Privacy/Privacy-considerations)
- Viva de-identifies data through pseudonymization (email addresses replaced with cryptographic strings) and aggregation. Organization insights enforce minimum group sizes, **differential privacy** and **masked distributions**. — [Microsoft Learn: De-identification](https://learn.microsoft.com/en-us/viva/insights/Privacy/de-identify-data); [Advanced insights privacy](https://learn.microsoft.com/en-us/viva/insights/advanced/privacy/privacy); [Viva privacy page](https://www.microsoft.com/en-us/microsoft-viva/privacy)
- **GraphRAG** (Edge et al., Microsoft Research, 2024) builds an LLM-derived entity graph and pre-generates community summaries. On "global sensemaking" questions over corpora around 1M tokens, it beat naive RAG on comprehensiveness and diversity with roughly 70–80% win rates. Later work added dynamic community selection to cut cost. — [arXiv 2404.16130](https://arxiv.org/abs/2404.16130); [MSR blog: dynamic community selection](https://www.microsoft.com/en-us/research/blog/graphrag-improving-global-search-via-dynamic-community-selection/)
- Microsoft's own Meeting AI Insights use cases frame post-meeting insights as feeding PM tools (classifying insights into decisions, tasks and risk items and pushing them into Azure DevOps/Jira/Notion) and executive daily briefings that "improve executive focus and decision velocity". — [Microsoft Learn: Meeting AI Insights](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/meeting-insights)
- Asana's standard project status vocabulary (On track / At risk / Off track / On hold / Complete) is a de-facto portfolio heatmap scale. — [Asana status updates](https://asana.com/features/project-management/status-updates)
- The Planner Agent writes status reports from plan data (only for plans with at least 10 tasks, with a configurable period and detail level) and requires a Microsoft 365 Copilot licence. — [Microsoft Support: Access Planner Agent](https://support.microsoft.com/en-us/planner/copilot/access-planner-agent)

### Inferences
- **KPI catalogue** (all drill down to evidence):
  - *Portfolio overview:* number of active projects by phase; health heatmap (project × week; bands per Asana vocabulary); projects entering or leaving the "at risk" band this week. **[V:H][F:H]**
  - *Commitment reliability:* % of commitments fulfilled on time, split **by direction** (ours vs. counterparties') and **by organization** (customer/vendor reliability). Show at org level by default. **[V:H][F:H]**
  - *Decision velocity:* median time from first question/proposal to Accepted decision; count of decisions per phase; decisions later superseded or reversed (churn). **[V:M][F:M]**
  - *Open-question aging:* count, median age, and oldest per project. **[V:H][F:H]**
  - *Risk exposure trend:* Σ(P×I) of open risks per project over time; risk→issue conversion rate; risks without an owner or mitigation. **[V:H][F:M]**
  - *Bottlenecks:* phases with the longest dwell time vs. history; waiting-on items grouped by counterparty org (e.g. "Vendor Y blocks 7 items"). **[V:H][F:H]**
  - *Customer/vendor communication load:* message volume, thread count, response latency and escalation count **per organization**, never per employee by default. **[V:M][F:H]**
  - *Pipeline-to-cash lens:* offer→PO→delivery→invoice→payment cycle times per customer, if invoice/payment mail is present. **[V:M][F:M]**
- **Privacy-safe defaults** (following Viva):
  - aggregate person-level metrics only for the user's own data ("my response times" is visible only to me)
  - team/org metrics apply a minimum group of 5
  - exclusion lists for domains, addresses and subject terms (legal/HR/"gizli")
  - an option to hash subjects in analytics exports
  - a clear, visible statement that the app is not for performance evaluation

  This also matters for EU/Turkish data-protection expectations (KVKK), but legal analysis is outside this research scope. **[V:H][F:H]**
- **"Ask your projects" natural-language Q&A:**
  - *local* questions ("What did we agree with X about delivery date?") use hybrid retrieval over evidence plus structured objects
  - *global* questions ("What are the recurring risks across supplier projects this quarter?") use GraphRAG-style community summaries over the entity graph (projects, orgs, people, decisions, risks)
  - answers always carry claim-level citations (§5)
  - where possible, the question is compiled into structured queries over the SQLite object tables (text-to-SQL) instead of free generation, for numeric questions

  **[V:H][F:M]**
- **Scheduled reports:**
  - weekly portfolio digest; per-project status draft (Asana smart-status style: auto-drafted, human-edited, then sent)
  - monthly RAID review pack; decision-log export (Markdown/Word/PDF/CSV) with evidence links
  - reports are drafts in the approval queue before they go to anyone **[V:H][F:H]**

### Gaps
- I found no standard, validated definition of a "decision velocity" KPI in PM literature. The definition above is a proposal.
- The privacy mechanics (differential-privacy parameters in Viva) are not published in detail.
- Legal requirements (KVKK/GDPR, works councils) for locally processing colleagues' mail are not covered here.

---

## 7. Additional and advanced ideas: Teams transcripts, contracts, customer/vendor views, TR/EN, Outlook/Teams surfaces, integrations, knowledge graph, alerts, decision-log export, voice briefing, MCP server, Windows toasts

### Takeaway
Most advanced ideas are feasible with first-party APIs. The important caveat as of Sept 2026 is that Microsoft's own AI endpoints are **Copilot-licence-gated**: the Meeting AI Insights API, the Work IQ MCP servers and the Planner/Project Manager agents. The local app should therefore rely on **raw data APIs** (mail delta, transcripts as VTT, drive files) with its own extraction, and treat Copilot-derived insights as optional enrichments. Exposing the local knowledge base via an MCP server is well aligned with where Microsoft itself is going (Work IQ MCP).

### Cited Findings
**Teams transcripts**
- `getAllTranscripts` returns transcripts for meetings **organized by** a given user. It supports delta (full and incremental sync), does **not** support channel meetings, and transcript content comes as .vtt. — [Microsoft Learn: onlineMeeting getAllTranscripts](https://learn.microsoft.com/en-us/graph/api/onlinemeeting-getalltranscripts?view=graph-rest-1.0); [Transcripts GA announcement](https://devblogs.microsoft.com/microsoft365dev/microsoft-graph-apis-for-microsoft-teams-meeting-transcripts-now-generally-available/)

**Meeting AI Insights API**
- Returns notes, action items with owner, and mention events. It **requires the user to have an M365 Copilot licence**. Insights are available only after the meeting ends, "might take up to four hours", and channel meetings are not supported. Transcription or recording must be on. — [Microsoft Learn](https://learn.microsoft.com/en-us/microsoftteams/platform/graph-api/meeting-transcripts/meeting-insights)
- A secondary source says it went GA in Graph v1.0 in December 2025. — [SPKnowledge (secondary)](https://spknowledge.com/2026/09/07/microsoft-365-copilot-meeting-ai-insights-api/)

**Teams intelligent recap**
- Provides AI notes, suggested tasks, speaker timeline markers and personalized mention markers. It requires Teams Premium (or Copilot) and transcription/recording. — [Microsoft Support: Recap in Teams](https://support.microsoft.com/en-us/teams/meetings/recap-in-microsoft-teams); [Tech Community](https://techcommunity.microsoft.com/blog/microsoftteamsblog/intelligent-meeting-recap-in-teams-premium-now-available/3832541)

**Mail sync**
- Graph **delta query** on `/mailFolders/{id}/messages/delta` maintains a local store incrementally via `@odata.deltaLink`. It is per-folder, and can be combined with change notifications. — [Microsoft Learn: delta query for messages](https://learn.microsoft.com/en-us/graph/delta-query-messages)

**Work IQ MCP**
- Microsoft's Work IQ MCP server exposes M365 through **10 generic tools**:
  - entity tools: `fetch`, `create_entity`, `update_entity`, `delete_entity`, `do_action`, `call_function`
  - Copilot tools: `ask`, `list_agents`
  - schema tools: `get_schema`, `search_paths`
- Its design principles are "fewer tools, more paths", "introspection over enumeration" and "policy over scopes" (four broad OAuth permissions, fine-grained policy per path/method/tenant). The page was updated 20 Aug 2026. — [Microsoft Learn: Work IQ MCP overview](https://learn.microsoft.com/en-us/microsoft-365/copilot/extensibility/work-iq/mcp/overview)
- Secondary sources say the Work IQ Mail and Calendar MCP servers were in preview mid-2026, restricted to tenants with M365 Copilot licences and requiring admin registration in Entra. — [Scalekit blog (secondary)](https://www.scalekit.com/blog/outlook-mcp-vs-api); [Work IQ Mail MCP marketplace listing](https://marketplace.microsoft.com/en-us/product/saas/wa200009543?tab=overview)
- MCP itself is an open protocol for connecting AI agents to tools and data. — [modelcontextprotocol.io](https://modelcontextprotocol.io)

**Planner / Project Manager agent**
- The agent can execute tasks, act on feedback and write status reports. It needs a Copilot licence, and premium features need Planner Plan 1 / Project Plan 3/5. — [Microsoft Support](https://support.microsoft.com/en-us/planner/copilot/access-planner-agent)
- Per Microsoft's blog snippet, meeting tasks from Facilitator sync to Planner, and the Project Manager Agent structures tasks with owners and due dates. — [Tech Community: Project Manager Agent](https://techcommunity.microsoft.com/blog/plannerblog/power-up-project-management-in-teams-with-the-project-manager-agent/4454813)

**Outlook add-in surfaces**
- Add-in content URLs should be HTTPS. Self-signed certificates work for development if trusted on the machine, and Office on the web and Marketplace require SSL. Network-share sideloading is **not supported for production**. — [Microsoft Learn: Office Add-ins manifest](https://learn.microsoft.com/en-us/office/dev/add-ins/develop/add-in-manifests); [Microsoft Learn: network share sideloading](https://learn.microsoft.com/en-us/office/dev/add-ins/testing/create-a-network-shared-folder-catalog-for-task-pane-and-content-add-ins)

**Windows toasts**
- Since Windows App SDK 1.1, unpackaged Win32 apps can send app (toast) notifications. For desktop apps, `activationType="background"` is ignored, so the app must handle activation arguments itself. Unpackaged apps need an AUMID (and a CLSID on the shortcut for activation). — [Windows Developer Blog: WinAppSDK 1.1](https://blogs.windows.com/windowsdeveloper/2022/06/03/whats-new-in-windows-app-sdk-1-1/); [Microsoft Learn: App notifications quickstart](https://learn.microsoft.com/en-us/windows/apps/develop/notifications/app-notifications/app-notifications-quickstart); [Toast activation from desktop apps](https://learn.microsoft.com/en-us/windows/apps/design/shell/tiles-and-notifications/toast-desktop-apps)

**Knowledge graph caution**
- Microsoft retired Viva Topics (a curated, AI-built topic/knowledge graph over SharePoint) on 22 Feb 2025 and shifted to Copilot-based knowledge experiences. Published topic pages became plain SharePoint pages. — [Microsoft Learn: Changes coming to Topics](https://learn.microsoft.com/en-us/microsoft-365/topics/changes-coming-to-topics?view=o365-worldwide); [Office365ITPros](https://office365itpros.com/2024/02/23/viva-topics-retirement/)

**Turkish / multilingual**
- Benchmarks report a significant performance gap between Turkish and English LLM performance. Turkish's agglutinative morphology makes translated English benchmarks inadequate, and native Turkish benchmarks exist (TurkishMMLU, TR-MMLU, TurkBench, and TUMLU for Turkic languages). — [TurkBench, arXiv 2601.07020](https://arxiv.org/html/2601.07020v1); [TUMLU (ACL 2025) notes](https://en.papernotes.org/ACL2025/llm_evaluation/tumlu_a_unified_and_native_language_understanding_benchmark_for_turkic_languages/)

**Contracts**
- CUAD's 41 clause types support contract/obligation extraction. — [CUAD Zenodo](https://zenodo.org/records/4595826)
- LLM norm extraction from contracts shows both opportunities and challenges. — [arXiv 2404.02269](https://arxiv.org/pdf/2404.02269)

### Inferences
Prioritized "additional developments" (value × feasibility):

**Tier 1: high value, high feasibility (next 1–2 releases)**
1. **Customer/vendor (Organization) 360 view:** all projects, open commitments both directions, waiting-on, SLA, last contact, escalations, key stakeholders and a relationship-health band, per organization, derived from email domains. **[V:H][F:H]**
2. **Decision log and RAID register export:** Markdown/DOCX/XLSX/CSV with evidence links and quotes, plus immutable decision history with superseded-by chains. **[V:H][F:H]**
3. **Windows toast notifications** from the local service: due-today commitments, a new high-priority request, health-band changes, and approval-queue items. Actions on the toast (Open / Snooze / Mark done) deep-link to https://localhost:6500. Rate-limit toasts and support quiet hours (HAX G3 "time services based on context"). **[V:H][F:H]**
4. **Teams transcripts (raw VTT) ingestion** for meetings the user organizes, with local extraction of decisions, actions and risks, each linked to a transcript timestamp. Optionally enrich with Meeting AI Insights when a Copilot licence is present. **[V:H][F:M]** (organizer-only and no-channel-meeting limits apply)
5. **Anomaly alerts:**
   - sudden silence from a normally active counterparty
   - a spike in escalation language
   - a new legal/management participant on a thread
   - a due date moved more than twice (renegotiation churn)
   - a commitment due with no activity
   - a document version churned near a deadline
   **[V:H][F:M]**

**Tier 2: high value, medium feasibility**
6. **Local MCP server** exposing the knowledge base read-only by default. Tools such as `search_evidence`, `get_project`, `list_commitments`, `list_decisions`, `get_timeline`, `ask_projects`; write tools (`create_draft`) go to the approval queue only. This lets Claude Desktop/Claude Code or other MCP clients query the local operations graph. Follow Work IQ's "few generic tools + introspection" pattern and bind to localhost with a per-client token. **[V:H][F:M]**
7. **Integrations (push, approval-gated):** Planner / To Do (via Graph) for my tasks; Jira and Azure DevOps for engineering follow-ups; Dynamics 365 / Salesforce for customer activity and opportunity stage sync (the phase model maps onto the opportunity stage); ERP (e.g. SAP/Logo/Netsis, common in Turkey) read-only for PO/invoice status to confirm the invoicing/payment phases. Start with one-way push of approved items, with bidirectional status sync later. **[V:H][F:M]**
8. **Contract/obligation extraction** from SharePoint/OneDrive contracts, using CUAD categories for dates, renewal, termination, SLA, penalties and payment terms. Obligations become commitments with due dates and renewal reminders. **[V:H][F:M]**
9. **Bilingual TR/EN handling:** per-message language detection; extract in the source language but normalise entities and dates ("Cuma'ya kadar", "ay sonu", "hafta içi" → absolute dates relative to the send date and the Turkish business calendar); keep bilingual labels in the UI. Build a small **Turkish evaluation set** from reviewed items, given the documented Turkish performance gap. **[V:H][F:M]**
10. **Meeting prep brief plus post-meeting follow-up draft** (a recap mail with decisions and actions for approval). **[V:H][F:M]**

**Tier 3: medium value or lower feasibility**
11. **Outlook add-in (task pane)** showing "this thread's project, commitments and evidence" and linking to the local app. HTTPS-to-localhost works on the desktop with a trusted certificate, but Office on the web and Marketplace distribution need a public HTTPS host. Treat it as a *desktop-only, sideloaded/centrally deployed* companion. **[V:M][F:M]**
12. **Teams surface:** a personal tab or bot pointing to the local app. This has the same public-hosting constraints, so a lower priority. **[V:M][F:L]**
13. **Knowledge graph explorer** (projects ↔ orgs ↔ people ↔ decisions ↔ documents) with GraphRAG-backed global Q&A. Given Viva Topics' retirement, favour *auto-derived and user-corrected* graphs over curated topic pages that need "knowledge editors". **[V:M][F:M]**
14. **Voice briefing:** TTS of the daily briefing (Windows speech or a local TTS engine), hands-free during a commute. **[V:L–M][F:H]**
15. **OCEL 2.0 export and process-map analytics** for power users/PMO. **[V:M][F:H]**
16. **Fine-tuning or adapter training** on locally reviewed items (text→event, commitment, decision), following the Sept 2026 finding that fine-tuning beats prompting for event-log extraction. **[V:M][F:L–M]**

**Cross-cutting guardrails:** treat every mail and document as untrusted input (prompt-injection risk); approval gates for all outbound actions; audit log; privacy defaults (§6); and a "licence-aware" feature matrix that shows which enrichments need M365 Copilot and which are fully local.

### Gaps
- I did not verify the exact GA date of the Meeting AI Insights API from a Microsoft primary source (the Microsoft Learn page does not state it; Dec 2025 comes from a secondary blog).
- Work IQ MCP preview/licensing status as of Sept 2026 comes from secondary sources. The Microsoft Learn overview does not state licensing.
- I found no documentation of an officially supported pattern for an Outlook add-in whose production content is served from `https://localhost` on each user's PC. Feasibility depends on enterprise certificate deployment and centralized add-in deployment, so it should be tested.
- No product case study was found for "voice briefing" or "local MCP over M365 data" with usage metrics. These are forward-looking ideas.
- ERP integrations common in Turkey (Logo, Netsis, SAP Business One) were not researched for API details.
