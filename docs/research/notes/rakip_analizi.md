# Competitive Landscape (as of 27 Sep 2026): AI that turns corporate email and documents into projects, decisions, risks, tasks and drafted actions, with a focus on M365 and local/privacy-first options

Scope note: this covers research done on 27 Sep 2026. "Secondary" marks a claim that comes from a third-party blog or reseller rather than the vendor. Preview or Frontier (early-access) status is flagged. Web-search budget ran out before a few vendors (Guru, Writer, Hebbia, Credal, Slack AI, Missive) could be researched; see Gaps.

---

## 1. Microsoft's own offerings: Copilot in Outlook, Copilot agents, Copilot Studio, Agent 365 / Work IQ, Viva Insights, Planner, pricing, and what they don't do well

### Takeaway
By September 2026 Microsoft covers a lot of this ground in the cloud. Copilot in Outlook summarizes threads with numbered citations, prioritizes and triages the inbox, and drafts and schedules. Copilot Notebooks now hold Outlook emails and Teams meetings as a persistent, per-project context. Copilot Cowork (GA 16 Jun 2026) runs long multi-step tasks across mail, meetings and files. Planner Agent manages tasks, and Agent 365 governs agents. It is still assistant- and prompt-centric and cloud-only: there is no persistent, structured, evidence-linked register of decisions, risks and open questions, and no project-phase inference across threads. Approval workflows before actions are limited to confirmation prompts. Processing is never local or offline. Microsoft has no Turkish datacenter or in-country Copilot processing, and several features are English-only, Frontier-only, or impossible to switch off.

### Cited Findings

**Pricing and licensing (Sep 2026)**
- Microsoft 365 Copilot (enterprise) costs $30/user/month paid yearly and requires a qualifying M365 plan. Copilot Studio agents built inside M365 are for internal use only. — [Microsoft Copilot Studio pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing/copilot-studio)
- Enterprise Copilot is also sold at $31.50/user/month on monthly billing (secondary). — [justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook)
- Microsoft 365 Copilot Business (add-on, for SMBs):
  - Regular price $21/user/month on annual billing, or $25.20 on monthly billing.
  - Promotional price $18/user/month for annual commitments bought between 1 Jul and 31 Dec 2026.
  - It includes Work IQ grounding and the prebuilt agents Researcher, Analyst and Facilitator.
  - Bundles: Business Standard with Copilot is $23.50 and Business Premium with Copilot is $32 (annual).
  - Source: [Microsoft 365 Copilot pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing)
- Copilot Business was announced at Ignite 2025 at $21/user/month from December 2025, for businesses with fewer than 300 users. — [Microsoft 365 Blog, Ignite 2025](https://www.microsoft.com/en-us/microsoft-365/blog/2025/11/18/microsoft-ignite-2025-copilot-and-agents-built-to-power-the-frontier-firm/)
- Conflict on price: some secondary sources say the $30 enterprise SKU is discounted to $18 through 31 Dec 2026 ([coworker.ai](https://coworker.ai/blog/microsoft-copilot-enterprise-pricing)). Computerworld cites "$20 for Microsoft 365 Copilot for Business" ([Computerworld](https://www.computerworld.com/article/4186190/microsoft-launches-copilot-cowork-with-usage-based-pricing.html)). Microsoft's own pricing page shows the $18 promo only on the Business add-on, at $21 list ([Microsoft](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing)). Treat the official page as authoritative.
- Microsoft 365 Copilot Chat has no extra per-user cost with eligible M365 subscriptions. — [Microsoft pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing); [justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook)
- Copilot Studio pricing:
  - A credit pack is $200/month for 25,000 Copilot Credits, tenant-wide.
  - Pay-as-you-go bills consumed credits and needs an Azure subscription.
  - Pre-purchase plans are also offered.
  - Source: [Microsoft Copilot Studio pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing/copilot-studio)
- The pay-as-you-go rate is $0.01 per Copilot Credit. — [Computerworld](https://www.computerworld.com/article/4186190/microsoft-launches-copilot-cowork-with-usage-based-pricing.html)
- Secondary sources warn that premium tools, grounding and voice multiply credit burn 5–30x. — [search summary citing GoSearch/Braincuber](https://www.braincuber.com/blog/microsoft-copilot-agent-cost-2026-real-math)
- Microsoft 365 E7 "Frontier Suite" costs $99/user/month and has been GA since 1 May 2026. It bundles E5, Copilot, Entra Suite and Agent 365. On 1 Jul 2026, E3 rose from $36 to $39 and E5 from $57 to $60. Agent compute is not included and is billed separately through Copilot Studio or Foundry. (Secondary.) — [Orchestry](https://www.orchestry.com/insight/m365-licensing-changes-july2026); [Red River](https://redriver.com/technology-solutions/microsoft-365-e7-license-and-upgrade)
- Microsoft reported more than 30M paid Copilot seats at the close of FY26 (secondary). — [empowering.cloud Aug 2026](https://empowering.cloud/microsoft-365-ai-workplace-update-august-2026/)

**Copilot in Outlook: features and license split**

The in-Outlook features below need the paid M365 Copilot license. Copilot Chat (free) only answers questions about the inbox, calendar and meetings.

| Feature | What it does | Limits |
|---|---|---|
| Summarize thread | A "Summary by Copilot" box that "pulls the key points, with numbered citations" | |
| Draft with Copilot | Drafts in the compose box | HTML format only |
| Coaching | Tone and clarity feedback | |
| Prioritize | Rates each new email high, low or normal, with explanations | Only rates mail received after it is turned on, never re-rates old mail, ignores mail that rules route to subfolders |
| Triage and rules | Plain-English commands | "English-only right now" |
| Schedule with Copilot | Creates the invite | |
| Attachment summarization | Summarizes attachments | PDF, Word and PowerPoint only |

Source for the table: [justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook).

- Mailbox and security limits:
  - The Outlook FAQ says Copilot works only on the primary mailbox, while a Sep 2026 requirements page claims support for archive, shared and delegate mailboxes. Group mailboxes are unsupported in both.
  - S/MIME, Double Key Encryption, IRM-protected mail and some sensitivity-labeled mail are excluded.
  - Chat only works in new Outlook for Windows and Outlook on the web.
  - Source: [justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook)
- 23 Sep 2026 release: more natural-language triage actions (delete, move, copy, categorize, manage rules and folders) "with confirmation prompts for bulk or mailbox changes". It works across classic and new Outlook for Windows, web, Mac and mobile. — [M365 Copilot release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
- 11 Aug 2026 releases:
  - Coaching feedback in chat, applied to the draft in place.
  - "Schedule with Copilot" in classic Outlook for Windows: finds times, books rooms, drafts agendas and sends invites. Copilot license only.
  - "Prepare for meetings", which summarizes relevant context, tasks and documents.
  - Source: [M365 Copilot release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
- The Outlook agent mode ("Allow actions": create folders and rules, categorize, set out-of-office, bulk invites) was in the Frontier early-access program. The author notes "rules created aren't automatically executed" and says to expect quirks. — [M365 Copilot Connection (Substack)](https://m365copilotconnection.substack.com/p/copilot-in-outlook-a-full-deep-dive)
- Agentic Copilot for Outlook (triage, drafts, calendar preferences) began rolling out to Frontier users in spring 2026 in Outlook on the web and new Outlook for Windows. — [Windows Forum](https://windowsforum.com/threads/agentic-copilot-in-outlook-spring-2026-triage-drafts-and-calendar-automation.421226)
- Proactive "Morning Briefing" and "Daily Wrap-up" summaries in new Outlook:
  - Tony Redmond (15 Sep 2026): "These features are just annoying. I never asked Copilot to become so proactive."
  - "There's no way to disable the morning or evening offers at a user level or for the tenant."
  - The wrap-up was "surprisingly sparse and did not include the broader Inbox activity".
  - Source: [Office365ITPros](https://office365itpros.com/2026/09/15/new-outlook-copilot/)
- A secondary review notes that Copilot in Outlook requires a prompt for each action and "lacks autonomous email triage, task extraction, and follow-up tracking". This predates the Sep 2026 triage release. — [Maestro Labs](https://www.maestrolabs.com/blog-detail/in-depth-review-of-copilot-for-outlook-and-alternative-ai-email-assistants)

**Persistent context: Copilot Notebooks (the closest Microsoft gets to project memory)**
- 25 Aug 2026: "Outlook Emails as References in Copilot Notebooks … users can ground Copilot in the conversations, decisions, and context that drive their work." Teams meetings (transcripts, notes, chats) can also be attached "directly to their projects". — [M365 Copilot release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
- 23 Sep 2026: the new Notebooks design is "a persistent AI workspace". "Copilot uses the Notebook's accumulated context to ground responses, so work continues across sessions." — [M365 Copilot release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)

**Agents**
- Ignite 2025 (18 Nov 2025) statuses:
  - Researcher and Analyst were GA in M365 Copilot.
  - Facilitator was GA; it "drives the agenda, takes notes … helps manage actions from the meeting".
  - Workflows agent was Frontier-only; it automates tasks "on a schedule or in response to events".
  - Outlook inbox and calendar understanding was due in preview in early 2026.
  - Source: [Microsoft 365 Blog](https://www.microsoft.com/en-us/microsoft-365/blog/2025/11/18/microsoft-ignite-2025-copilot-and-agents-built-to-power-the-frontier-firm/)
- Researcher produces "comprehensive, source-cited reports". With Sources set to "Work" it can use work email, meetings and chats. — [Microsoft Learn: Researcher](https://learn.microsoft.com/en-us/microsoft-365/copilot/researcher-agent); [Microsoft Support](https://support.microsoft.com/en-us/topic/get-started-using-researcher-with-computer-use-in-microsoft-365-copilot-frontier-1f274537-6648-46e8-8264-052a49b92af4)
- 25 Aug 2026: users can choose Researcher models and modes in Chat. — [release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
- Researcher and Computer Use: "Frontier's Researcher: Retired 1 July; Computer Use moved to Cowork" (secondary, wording ambiguous). — [empowering.cloud](https://empowering.cloud/microsoft-365-ai-workplace-update-august-2026/)
- Planner Agent timeline:
  - Mid-March 2026: the "Project Manager agent" was renamed "Planner agent" and extended to Copilot users on premium and basic plans. — [M365 Admin (handsontek)](https://m365admin.handsontek.net/microsoft-365-copilot-planner-agent-rename-rollout-premium-basic-plans/)
  - 11 Aug 2026: available in all group-based Planner plans, including basic. — [release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
  - 23 Sep 2026: new Planner Agent chat with natural-language Q&A, "smart task discovery" and in-plan updates. — [release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)
  - A search snippet (source page not verified; the June 2026 "What's New" page returned only its title) says the agent can create structured plans with goals and buckets and deliver interactive task cards. Treat this as unverified. — [Microsoft Community Hub, What's New June 2026](https://techcommunity.microsoft.com/blog/Microsoft365CopilotBlog/what%E2%80%99s-new-in-microsoft-365-copilot--june-2026/4529572)
- Project Online retires on 30 Sep 2026, with investment moving to Planner and Copilot. — [Microsoft Tech Community Planner blog](https://techcommunity.microsoft.com/blog/plannerblog/microsoft-project-online-is-retiring-what-you-need-to-know/4450558)
- Several Planner features retired in early 2026 (MC1193421), including old task comments, the iCal feed and Planner in Viva Goals. — [mc.merill.net](https://mc.merill.net/message/MC1193421)

**Copilot Cowork (agentic, long-running tasks)**
- GA on 16 Jun 2026 to M365 Copilot customers worldwide:
  - "You define the work and Cowork runs it end-to-end and returns a completed result, not just a draft."
  - It runs in the cloud: "tasks keep running even when your laptop is off".
  - It runs on Anthropic models "including Opus 4.8 and Sonnet 4.6".
  - The GA post does not mention human approval checkpoints before actions.
  - Source: [Microsoft 365 Blog, 16 Jun 2026](https://www.microsoft.com/en-us/microsoft-365/blog/2026/06/16/copilot-cowork-is-now-generally-available/)
- Billing and controls:
  - Usage is billed in Copilot Credits at $0.01 each, via pay-as-you-go or a P3 commitment. Credits depend on model use, context retrieval, tool calls and runtime.
  - Cowork is off by default. Spend limits can be set at tenant, group and user level.
  - Unlike Claude Cowork, which works on local files and apps, it acts only on tenant data in the cloud.
  - Source: [Computerworld](https://www.computerworld.com/article/4186190/microsoft-launches-copilot-cowork-with-usage-based-pricing.html)
- Task tiers are roughly light (100–300 credits), medium (400–700) and heavy (700+) (secondary). — [Quisitive](https://quisitive.com/copilot-cowork-pricing-2026-how-usage-based-billing-works/)
- 23 Sep 2026: "Cowork gathers context from your emails, meetings, chats, files … and returns completed results." — [release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)

**Copilot Studio autonomous agents with email triggers**
- Event triggers let agents "act autonomously" and need generative orchestration. Examples include SharePoint item created, Planner task completed, and recurrence; Outlook "when a new email arrives" is widely used. — [Microsoft Learn, updated 9–10 Sep 2026](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-triggers-about); [Matthew Devaney](https://www.matthewdevaney.com/copilot-studio-build-email-agents-to-automate-replies/)
- Limits documented on the same Microsoft Learn page ([Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-triggers-about)):
  - "Event triggers can use only the agent author's credentials for authentication." Users of a published agent may therefore reach data through the maker's credentials.
  - Each trigger payload counts as a billable message.
  - Frequent triggers can hit quotas and be throttled.
  - Complex agents that run many actions in one sequence (Microsoft advises fewer than 15) "can struggle to run them reliably".
  - Admins can block triggers through data policies.
- With the Outlook trigger, the mailbox is accessed through the creator's connection, so sharing the agent risks exposing the creator's mail. — [ippu-biz](https://ippu-biz.com/en/development/powerplatform/copilot-studio/event-trigger/)

**Agent 365 and Work IQ**
- Work IQ is "the intelligence layer that enables Microsoft 365 Copilot to know you, your job, and your company". It combines work data (email, files, meetings, chats), memory and inference, and is exposed to custom agents through Copilot Studio or an API. — [Microsoft 365 Blog, Ignite 2025](https://www.microsoft.com/en-us/microsoft-365/blog/2025/11/18/microsoft-ignite-2025-copilot-and-agents-built-to-power-the-frontier-firm/)
- Agent 365 went GA on 1 May 2026 ([Microsoft Tech Community](https://techcommunity.microsoft.com/discussions/agent-365-discussions/agent-365-will-be-generally-available-on-may-1-2026/4500380); [Microsoft Security Blog](https://www.microsoft.com/en-us/security/blog/2026/05/01/microsoft-agent-365-now-generally-available-expands-capabilities-and-integrations/)):
  - It costs $15/user/month or comes with E7.
  - It is a control plane to "observe, govern, and secure agents".
  - It discovers local agents (OpenClaw, GitHub Copilot CLI, Claude Code) through Defender and Intune.
  - Windows 365 for Agents is in public preview, US only.
- Agent 365 had about 40M registered agents at the close of FY26 (secondary). — [empowering.cloud](https://empowering.cloud/microsoft-365-ai-workplace-update-august-2026/)

**Viva Insights (the old commitments and briefing features)**
- The Briefing email surfaced "outstanding commitments and requests" by "scanning messages for key words and phrases that indicate when the recipient or sender might be committing to an action". — [Microsoft Learn Briefing docs (search snippet)](https://learn.microsoft.com/en-us/viva/insights/personal/briefing/)
- Microsoft paused Briefing emails "to make improvements". — [Office365ITPros, Dec 2022](https://office365itpros.com/2022/12/23/viva-briefing-pause/); [Microsoft Tech Community: Pausing the Briefing email](https://techcommunity.microsoft.com/blog/viva_insights_blog/pausing-the-briefing-email-from-microsoft-viva/3795322)
- On 27 Sep 2026 the old Learn overview URL returned a 301 redirect to a generic Viva Insights support page that does not mention Briefing (observed during this research). — [old URL](https://learn.microsoft.com/en-us/viva/insights/personal/Briefing/be-overview)
- MC1477996 retires the Viva Insights personal-insights active-user trend chart from the M365 admin center between early October and mid-November 2026, with no replacement. — [mc.merill.net](https://mc.merill.net/message/MC1477996)

**Data residency and sovereignty (relevant to KVKK)**
- In-country processing of Copilot interactions:
  - End of 2025: Australia, India, Japan and the UK.
  - 2026: Canada, Germany, Italy, Malaysia, Poland, South Africa, Spain, Sweden, Switzerland, UAE and the US.
  - Turkey is not listed.
  - Source: [Computerworld](https://www.computerworld.com/article/4085303/m365-copilot-data-processing-goes-local-to-meet-sovereignty-demands.html)
- The Microsoft Learn data residency page (updated Sep 2026) lists "No modifications" for M365 Copilot and Copilot Chat. The at-rest commitment for Copilot Cowork covers 15 countries (AU, BR, CA, FR, DE, IN, JP, NO, ZA, KR, SE, CH, UAE, UK, US); Turkey is not among them. — [Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-365/enterprise/m365-dr-service-copilot?view=o365-worldwide)
- A Turkish Microsoft-partner blog says Microsoft had no cloud datacenter in Turkey at end-2025 and the Turkey DC project had no opening date. Under KVKK Article 9 the data controller must provide the legal basis for transfer, and the EU datacenter is the current option (secondary). — [microsoftkurumsal.com](https://www.microsoftkurumsal.com/blog/microsoft-365-copilot-kvkk-uyumu-eu-data-boundary-rehberi/)

**Local and on-device AI on Windows (platform, not a competitor app)**
- Build 2026 (June 2026) moved the Windows AI pitch from Copilot+ PCs toward local agents and models on CPU, GPU and NPU. "Microsoft Foundry on Windows" is the unified local AI platform, and Foundry Local handles on-device inference and fine-tuning. Microsoft Execution Containers give agents defined permissions and identity. (Secondary.) — [Windows Developer Blog, 2 Jun 2026](https://blogs.windows.com/windowsdeveloper/2026/06/02/build-2026-furthering-windows-as-the-trusted-platform-for-development/); [Redmondmag](https://redmondmag.com/articles/2026/06/02/microsoft-uses-build-2026-to-put-ai-agents-at-the-center-of-windows.aspx)

### Inferences
- **Microsoft's gaps** (inferred from the feature list above):
  - **Structured registers.** Copilot produces prose summaries, chat answers and Cowork deliverables. No source shows persistent, queryable decision, risk or open-question registers where each item is linked to its email or document evidence and has a lifecycle (open, mitigated, closed). Notebooks keep context but are user-curated and not structured.
  - **Project state and phases.** There is no automatic inference of which project a thread belongs to, what phase the project is in, or whether it has drifted. The Planner Agent manages tasks people enter; it does not mine them from mail. Viva's commitment detection, the closest earlier feature, has been paused since about 2022–23.
  - **Approval gates.** Confirmation prompts exist only for bulk mailbox changes. The Cowork GA post mentions no approval checkpoints. Copilot Studio triggers run under the maker's credentials, which is a governance weakness a per-user, approval-gated local agent could turn into a selling point.
  - **Local and offline.** None of Copilot, Cowork or Copilot Studio runs locally. There is no Turkish in-country processing, and triage is English-only.
  - **Cost opacity.** Charges stack: $21–30 per seat, plus credits for Cowork and agents, plus $15 for Agent 365 or $99 for E7.
- **Microsoft's direction.** Notebooks with Outlook references, Cowork, and Planner Agent task discovery show Microsoft moving quickly toward "project workspace + autonomous execution". Our differentiation should not depend on "summaries" or "drafts" alone, since those are becoming commodities.
- **The briefing opening.** Unwelcome proactive briefings that can't be switched off (Redmond) point to demand for user-controllable, auditable digests.

### Gaps
- I could not confirm the Workflows agent's GA status or price in Sep 2026. The June 2026 "What's New" page returned only its title.
- I could not verify Turkish-language quality or support for Copilot in Outlook features (the search budget ran out). Only "triage English-only" is sourced.
- The current state of Briefing email (permanently retired or still paused) and of the Viva Insights Outlook add-in is unconfirmed as of Sep 2026.
- I found no official Microsoft source on whether Copilot keeps cross-thread memory of commitments or decisions outside Notebooks.

---

## 2. Enterprise search and knowledge assistants (Glean, Dust, Moveworks, Rovo, Notion AI/Mail, Onyx, Claude for M365; Guru, Writer, Hebbia, Credal, Slack AI not covered): Outlook/SharePoint support and outputs

### Takeaway
Enterprise search vendors connect to Outlook and SharePoint and return citation-backed chat answers. Glean adds agent actions with optional human approval. They are cloud SaaS (Onyx excepted), priced per seat at enterprise levels (Glean is estimated at $45–75+ per user with roughly a 100-seat minimum), and built for retrieval rather than structured project, decision and risk tracking. The self-hosted, air-gapped option is Onyx (open source). Frontier labs are now entering the space directly: Claude for M365 gained email and calendar write tools in July 2026. Notion Mail shut down on 22 Sep 2026.

### Cited Findings

**Glean**
- Pricing:
  - Glean publishes no pricing. Buyers report about $45–65+ or $50–75+ per user per month and a minimum of about 100 seats (about $60k ACV).
  - Advanced AI and agents moved to consumption "FlexCredits".
  - Sources (secondary): [Workativ](https://workativ.com/ai-agent/blog/glean-pricing); [GoSearch](https://www.gosearch.ai/faqs/glean-enterprise-search-pricing-explained-costs-tiers-hidden-fees-gosearch-comparison/)
- The median buyer pays $98,890/year across 174 transactions, and large deployments reach $350k–480k/year (secondary). — [Vendr](https://www.vendr.com/marketplace/glean)
- It has an Outlook connector and M365 actions; agents can create Outlook email drafts. — [Glean Outlook connector](https://www.glean.com/connectors/outlook); [Glean docs: M365 actions](https://docs.glean.com/agents/actions/datasource/microsoft-365)
- Glean has integrated with Microsoft Agent 365, bringing its context into Word, Outlook and Teams. — [Glean blog](https://www.glean.com/blog/glean-microsoft-integration-2025)
- Human-in-the-loop controls can require approval before an action runs; action-level controls went GA in about Dec 2025 (secondary). Answers carry citations via RAG. — [gend.co](https://www.gend.co/blog/glean-autonomous-agents)
- It has 100+ connectors, or 275+ native and MCP-based ones. — [Glean connectors](https://www.glean.com/connectors)
- Reviews and complaints:
  - G2 rating 4.7/5 (162 reviews, Aug 2026). — [G2](https://www.g2.com/products/glean-ai/reviews)
  - Complaints cover high price and hidden costs, complex setup that needs admin access to each source, hallucinations and an unintuitive UI (secondary summary of G2 and reviews). — [Cybernews](https://cybernews.com/ai-tools/glean-ai-review/); [Workativ review](https://workativ.com/ai-agent/blog/glean-review)

**Dust**
- Pricing: Pro $24/seat/month on annual billing (also shown as €29), Max $120/seat/month. Enterprise starts at 100 seats with SSO/SCIM and US or EU data residency. It offers a choice of models (Claude, GPT, Gemini, Mistral) and a no-code agent builder. It has a Microsoft connection. (Secondary summaries.) — [Dust pricing](https://dust.tt/home/pricing); [Dust Microsoft docs](https://docs.dust.tt/docs/microsoft-connection); [Knowlee](https://www.knowlee.ai/blog/dust-tt-alternatives-2026)

**Moveworks**
- ServiceNow completed its acquisition of Moveworks on 15 Dec 2025, for about $2.85B. The product is positioned as a front-end AI assistant and enterprise search for IT/HR workflows and integrates with M365, SharePoint and Teams. — [ServiceNow newsroom](https://newsroom.servicenow.com/press-releases/details/2025/ServiceNow-completes-acquisition-of-Moveworks/default.aspx); [CX Today](https://www.cxtoday.com/crm/servicenow-moveworks-acquisition/)

**Atlassian Rovo**
- Rovo is included with paid Atlassian Cloud plans. Its connectors cover Google Drive, SharePoint, Teams and Slack; an Outlook email connector is not confirmed. Extra-usage billing for Rovo credits starts on 3 Dec 2026 (secondary). — [Atlassian Rovo SharePoint connector](https://www.atlassian.com/software/rovo/connectors/sharepoint); [Atlassian: Rovo credits](https://support.atlassian.com/rovo/docs/rovo-usage-limits/); [eesel](https://www.eesel.ai/blog/atlassian-intelligence-and-rovo-pricing-explained)

**Notion AI and Notion Mail**
- Notion Mail shut down on 22 Sep 2026. It was Gmail-only and never supported Outlook. Notion said more than half of its email users never opened the inbox and delegated it to agents instead. — [TechCrunch, 25 Jun 2026](https://techcrunch.com/2026/06/25/notion-mail-shuts-down-amid-agent-takeover/); [Android Authority](https://www.androidauthority.com/notion-mail-is-shutting-down-3681674/); [Notion Help](https://www.notion.com/en-gb/help/notion-mail-inbox-is-going-away-what-to-do-next)
- The Notion AI Outlook connector ([Notion Help](https://www.notion.com/help/microsoft-outlook-ai-connector)):
  - Reads and searches Outlook email but "can't read email attachments".
  - Requires a Business or Enterprise plan and an M365 admin.
  - Connects one workspace to one tenant.
  - Respects Outlook permissions.
  - Does not store raw email, though "auxiliary statistics" may be logged.
- The Business plan is $20/member/month (secondary). Enterprise Search connectors include SharePoint, OneDrive and Teams (beta for some). — [eesel](https://www.eesel.ai/blog/notion-ai-review); [Notion Help: SharePoint connector](https://www.notion.com/help/notion-ai-connector-for-microsoft-sharepoint-and-onedrive)

**Onyx (open source, self-hostable Glean alternative: the key local-first comparator for enterprise search)**
- Onyx is MIT-licensed and self-hosted on Docker or Kubernetes. It has 40+ connectors, including Outlook and SharePoint, with ACL/permission sync. It supports air-gapped deployment with local LLMs via Ollama or vLLM. UC San Diego reportedly runs it air-gapped for 37,000+ users. The community edition is free; Business is $20/user/month on annual billing. (Vendor or reseller claims.) — [Onyx](https://onyx.app/); [GitHub onyx-dot-app/onyx](https://github.com/onyx-dot-app/onyx); [Onyx enterprise search guide](https://onyx.app/insights/enterprise-search-tools-2026)

**Claude for Microsoft 365 (frontier-lab entrant)**
- The M365 connector searches SharePoint, OneDrive, Outlook and Teams. — [Claude Help Center](https://support.claude.com/en/articles/12542951-set-up-the-microsoft-365-connector)
- Write tools arrived on 7 Jul 2026: draft and send email, manage the calendar, set out-of-office, and create or update OneDrive and SharePoint files. They are off by default and need admin consent in Entra. — [usecarly](https://www.usecarly.com/blog/claude-for-microsoft-365/); [Okto Solutions](https://www.oktosolutions.ca/en/claude-microsoft-365-write-access/)
- The Claude for Outlook add-in is in beta and can only create drafts, not send. — [usecarly](https://www.usecarly.com/blog/claude-for-outlook/); [Claude docs: Outlook](https://claude.com/docs/office-agents/outlook)
- Grok and OpenAI also ship Office add-ins or plugins (secondary). — [empowering.cloud](https://empowering.cloud/microsoft-365-ai-workplace-update-august-2026/)

### Inferences
- **What the category optimizes for.** Enterprise search answers questions with citations but does not keep a standing model of projects, decisions and risks. Its outputs are answers and agent runs, not registers or dashboards.
- **Pricing.** Glean's price point and 100-seat minimum exclude mid-market companies, which is room for a flat-license local product.
- **The self-hosted comparator.** Onyx proves demand for air-gapped RAG over Outlook and SharePoint. It is a generic chat and search tool, however, with no mail agent, no project or phase model and no approval-gated drafts. A buyer might pair Onyx with Copilot instead of buying us, so our pitch must cover what Onyx does not.

### Gaps
- Guru, Writer, Hebbia, Credal and Slack AI were not researched: search budget exhausted. No findings on their Outlook support, pricing or deployment.
- Glean's official price list and Onyx's exact Outlook connector capabilities (mail body vs attachments, shared mailboxes) are unverified.

---

## 3. AI email clients and assistants (Superhuman, Shortwave, Fyxer, Spark, Canary, eM Client, local-LLM tools): triage, follow-ups, commitments, drafts, ask-your-inbox

### Takeaway
AI email clients are per-user cloud tools, typically $10–50/user/month, that do triage and labels (including "waiting on"), auto-drafts, follow-up reminders and ask-your-inbox. Outlook support is often second-class: Shortwave is Gmail-only, Fyxer lags on Outlook, and Notion Mail was Gmail-only and is now dead. On-device processing is rare. Canary does local triage but uses cloud AI for drafts and summaries. Fully local options are hobby or open-source projects (outlook-bob, Ollama Mail). None offers project, decision or risk registers or org-level BI.

### Cited Findings

**Superhuman (the Grammarly-owned suite)**
- Pricing and plans:
  - Starter is $30/month ($25 on annual billing); Business is $40/month ($33 annual); Enterprise is custom.
  - Auto Drafts (proactively writes replies "in your voice") and Ask AI (natural-language inbox queries "with source email links") are Business-only.
  - Summarize "condenses … into key decisions, action items, and open questions".
  - Auto Labels include "response needed, waiting on, meetings…".
  - It supports Gmail and Outlook.
  - Criticisms: no free tier, and Auto Drafts sometimes misreads context.
  - Source: [Fast.io review](https://fast.io/resources/superhuman-ai-review-2026/)
- Grammarly rebranded as Superhuman and launched the Superhuman Suite (Mail, Grammarly, Coda, Go). Starter includes follow-up reminders. — [usecarly](https://www.usecarly.com/blog/superhuman-pricing/); [Morgen](https://www.morgen.so/blog-posts/superhuman-pricing)
- Superhuman has a help article on "Auto Reminders & Auto Drafts"; the page returned 403 when fetched. — [Superhuman Help](https://help.superhuman.com/hc/en-us/articles/46005658551053-Auto-Reminders-Auto-Drafts)

**Shortwave**
- It supports Gmail and Google Workspace only, with no Outlook or M365. The assistant runs multi-step tasks and queues drafts in Drafts "for approval". Pricing is Business $24, Premier $36 and Max $100 per month on annual billing. (Secondary.) — [alfred_](https://get-alfred.ai/blog/shortwave-pricing); [Shortwave pricing](https://www.shortwave.com/pricing/)

**Fyxer AI**
- Pricing and support: Starter is $30/month ($22.50 annual) and Professional $50 ($37.50 annual). It supports Gmail and Outlook, but "multiple Outlook users say the experience is well behind the Gmail version".
- Complaints:
  - Drafts often need rewriting.
  - Mislabeling buries important mail.
  - Overage charges apply above the plan's email volume.
  - Trustpilot reviewers report billing after trial cancellation.
- Source (secondary): [eesel](https://www.eesel.ai/blog/fyxer-ai-reviews); [tl;dv](https://tldv.io/blog/fyxer-ai-review/)

**Spark (Readdle)**
- Plus is $10/month and Pro $20/month. "Spark + AI" composes, rephrases and translates. It runs on iOS, Android, macOS and Windows. (Secondary.) — [Clean Email](https://clean.email/blog/ai-for-work/spark-mail-ai-review); [AI:PRODUCTIVITY](https://aiproductivity.ai/pricing/spark-mail/)

**Canary Mail (hybrid local plus cloud)**
- Triage, prioritization and categorization run on the device and work offline. Drafts, summaries and natural-language answers use "privacy-controlled cloud AI", and content is not used for training. It supports Gmail, Outlook, iCloud, IMAP and Exchange on macOS, iOS, Windows and Android. AI can be switched off. (Vendor claims.) — [Canary Mail AI](https://canarymail.io/features/ai); [Canary blog: local vs cloud AI](https://canarymail.io/blog/local-ai-vs-cloud-ai-in-email)

**eM Client (Windows desktop)**
- Pro added generative AI in April 2026 (drafts, thread summaries, tone, translation) "powered by ChatGPT specifically". There is no local model option and no bring-your-own-key. (Secondary, from a competitor's site.) — [Saymail](https://saymail.eu/en/em-client-alternative/)

**Local-LLM and on-device email assistants**
- outlook-bob (open source) drafts email answers with local LLMs through Ollama and sends "no data … to the internet". — [GitHub haesleinhuepf/outlook-bob](https://github.com/haesleinhuepf/outlook-bob)
- Ollama Mail is a Chrome extension that processes email locally through Ollama. — [ollamamail.com](https://ollamamail.com/)
- DIY stacks (IMAP + Python + Ollama, Thunderbird + Ollama, self-hosted n8n + Ollama) are documented in guides. — [PromptQuorum](https://www.promptquorum.com/power-local-llm/local-llm-email-and-calendar-automation); [LocalAIMaster](https://localaimaster.com/blog/local-ai-email-triage)

**Outlook native task capture**
- New Outlook can turn an email into a Microsoft To Do task ("Create task"). — [Microsoft Support](https://support.microsoft.com/en-us/todo/adding-an-email-as-a-task-in-outlook-on-windows); [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/5706471/create-task-in-new-outlook)

### Inferences
- **Commodity features.** Auto-triage, "waiting on" labels, reminders, drafts and ask-your-inbox are now baseline. Superhuman even summarizes into "decisions / action items / open questions", but only per thread and only for one user.
- **The Windows local niche.** No mainstream vendor offers a Windows-native, fully local, Outlook/Exchange-first assistant with org-level project intelligence. Local options are open-source or hobby tools with no project, BI or approval layer.
- **Approval by default.** Shortwave queuing drafts "for approval" and Claude's drafts-only add-in show the market converging on draft-then-approve. Our approval-gated action drafts should add audit trails, multi-step approvers and evidence links to stand out.

### Gaps
- Missive and other Windows desktop clients (e.g., Mailbird, Thunderbird's own AI plans) were not researched.
- I found no vendor that offers cross-thread commitment tracking with evidence citations as a structured register; this negative finding rests on the sources reviewed only.

---

## 4. Meeting and communication intelligence (Read AI, Otter, Fireflies, Fathom): action items and project reports

### Takeaway
Meeting tools produce action items, summaries and follow-up email drafts at about $15–40/user/month. Read AI extends furthest into email and messages ("Ask Read", daily email digests), but its email features are Gmail-first; Outlook email summaries were announced as "coming soon". All are cloud SaaS, and enterprise tiers add HIPAA, SSO and custom retention.

### Cited Findings
- **Read AI**
  - Plans: Free (5 meetings/month); Pro $15/month on annual billing ($19.75 monthly); Enterprise $22.50 ($29.75); Enterprise+ $29.75 ($39.75), which adds HIPAA, SAML/SCIM, custom data retention and a 5-license minimum. — [Read AI pricing](https://www.read.ai/pricing)
  - "Ask Read" searches across meetings, emails and messages, and action items appear in meeting reports. — [Read AI pricing](https://www.read.ai/pricing)
  - Read AI for Gmail is a Chrome extension that summarizes threads and drafts replies from connected meeting and message content. "Email summaries are coming soon for Outlook"; the post announced the $50M Series B and gives no date. — [Read AI blog](https://www.read.ai/post/read-ai-announces-50-million-series-b-launch-of-read-ai-for-gmail)
  - Read AI also offers daily email digests. — [Read AI blog](https://www.read.ai/post/daily-email-digests-give-you-a-head-start-on-your-day)
- **Otter, Fireflies and Fathom** (secondary)
  - Otter: Free (800 min/month), Pro from $10, Business from $19.
  - Fireflies: Team from $19; logs action items to CRMs.
  - Fathom: Team $19; requires a Google or Outlook calendar; paid tiers add AI action items and follow-up email drafts.
  - Sources: [usecarly](https://www.usecarly.com/blog/otter-vs-fireflies-vs-fathom/); [Zapier](https://zapier.com/blog/fathom-vs-fireflies/)
- **Microsoft Facilitator** (GA) takes notes and "helps manage actions from the meeting". Teams meeting content can be attached to Copilot Notebooks projects (25 Aug 2026). — [Microsoft 365 Blog](https://www.microsoft.com/en-us/microsoft-365/blog/2025/11/18/microsoft-ignite-2025-copilot-and-agents-built-to-power-the-frontier-firm/); [release notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)

### Inferences
- **Where meeting AI stops.** Action items are extracted per meeting, and cross-source project roll-ups are weak outside Read AI and Copilot Notebooks. A local product that merges email, documents and (optionally) meeting transcripts into one project timeline and evidence graph is differentiated.
- **Importing transcripts.** Teams and Facilitator are becoming the default in M365 shops. Importing Teams transcripts or Facilitator notes, rather than competing with note-takers, is probably the pragmatic route.

### Gaps
- Read AI's current Outlook/Exchange email support status in Sep 2026 is unverified.
- I did not verify primary pricing pages for Otter, Fireflies or Fathom.

---

## 5. Project/work intelligence and process mining (Asana AI Studio, monday.com AI, ClickUp Brain, Jira/Rovo, Celonis, Power Automate Process Mining)

### Takeaway
Work-management AI acts on tasks already inside each tool: AI Studio, monday credits, ClickUp Brain 2 Super Agents. It does not infer projects, phases or commitments from raw email. Process mining (Celonis, Power Automate Process Mining) mines system event logs; Celonis is starting to ingest semi-structured email. The gap is "process/phase inference from communications" for mid-sized M365 organizations without ERP-grade logs.

### Cited Findings
- **Asana:** Starter is $10.99 and Advanced $24.99 per user per month on annual billing. AI Studio Basic is included with 50k credits (Starter) or 75k (Advanced). AI Teammates require the AI Studio Pro add-on, sold through sales. (Secondary.) — [Agiled](https://agiled.app/blog/asana-pricing); [Tracy Jackson](https://www.bytracyjackson.com/blog/asana-pricing)
- **monday.com:** AI credits cost $0.01 each on yearly plans and $0.0125 on monthly plans, with monthly minimums by plan. — [monday support: pricing model for monday AI](https://support.monday.com/hc/en-us/articles/35277848309394-The-pricing-model-for-monday-AI-portfolio); [monday blog](https://monday.com/blog/ai-agents/monday-ai-credits/)
- **ClickUp Brain:** a $9/user/month add-on on every paid seat. It relaunched as "Brain 2" in June 2026 with workspace knowledge retrieval, automated standups and Super Agents. (Secondary.) — [ClickUp Brain pricing](https://clickup.com/brain/pricing); [Agiled](https://agiled.app/blog/clickup-pricing)
- **Jira/Rovo:** see section 2. Rovo Search spans SharePoint and more to find "team players, projects and information needed to make decisions". — [eesel: Rovo connectors](https://www.eesel.ai/blog/rovo-ai-connectors)
- **Power Automate Process Mining** comes with the $15/user/month Power Automate Premium license. Microsoft rebuilt it after acquiring Minit in 2022. (Secondary.) — [KYP.ai comparison](https://kyp.ai/process-mining-software-comparison/); [Bardeen](https://www.bardeen.ai/best/process-mining-tools)
- **Celonis** was a Leader in Gartner's 2025 Process Mining MQ. It adds AI task discovery and the ability to integrate unstructured PDFs and "semi-structured data like emails". Gartner now calls the category "process intelligence". (Secondary.) — [Process Excellence Network](https://www.processexcellencenetwork.com/process-mining/news/celonis-announces-new-platform-innovations-to-power-ai-driven-composable-enterprises); [KYP.ai](https://kyp.ai/process-mining-software-comparison/)

### Inferences
- **Event flows from email.** Deriving event flows and process maps from email and document metadata is thinly served; Celonis is ERP-log-centric and expensive. A local "communication process mining" view (who waits on whom, cycle times per phase, bottlenecks) is a credible differentiator for BI dashboards.
- **Integration, not competition.** Our product should export tasks to Planner, To Do, Jira or Asana rather than compete as a task manager.

### Gaps
- Celonis pricing and its email-ingestion maturity are unverified.
- Whether Power Automate Process Mining can mine Outlook or Exchange events directly is unverified.

---

## 6. Product-by-product comparison (features, M365 depth, deployment, pricing, citations, approval gating, limitations)

### Takeaway
Every major competitor is cloud-hosted. Citation-backed answers are common (Copilot summaries, Researcher, Glean, Superhuman Ask AI), but persistent evidence-linked registers are absent. Approval gating is emerging only as "drafts, not sends" or admin-level switches, not as a business approval workflow with an audit trail.

### Cited Findings
Each row draws on the sources cited in sections 1–5.

| Product | Category | M365/Outlook depth | Deployment | Price (Sep 2026) | Citations | Approval-gated actions | Notable limits |
|---|---|---|---|---|---|---|---|
| M365 Copilot in Outlook ([MS](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing), [JM](https://justinmckelvey.com/blog/copilot-for-outlook)) | Native assistant | Deepest (Graph) | MS cloud only | $30 ent. / $21 Business ($18 promo) | Yes (numbered in summaries) | Confirmation only for bulk triage ([notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)) | English-only triage; primary mailbox; encrypted mail excluded; proactive briefs can't be disabled ([O365ITPros](https://office365itpros.com/2026/09/15/new-outlook-copilot/)) |
| Copilot Notebooks ([notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)) | Project context | Emails and meetings as references | Cloud | In Copilot license | Grounded | n/a | Curated manually; no structured registers (inferred) |
| Copilot Cowork ([MS](https://www.microsoft.com/en-us/microsoft-365/blog/2026/06/16/copilot-cowork-is-now-generally-available/), [CW](https://www.computerworld.com/article/4186190/microsoft-launches-copilot-cowork-with-usage-based-pricing.html)) | Agentic execution | Work IQ across tenant | Cloud (Anthropic models) | License + $0.01/credit | Not stated | Not stated in GA post; admin spend limits | Variable cost; no in-country at-rest for Turkey ([Learn](https://learn.microsoft.com/en-us/microsoft-365/enterprise/m365-dr-service-copilot?view=o365-worldwide)) |
| Copilot Studio email-trigger agents ([Learn](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-triggers-about)) | Custom autonomous agents | Outlook triggers | Cloud / Power Platform | $200 per 25k credits, or PAYG | Depends on build | Author-built | Maker-credential risk; billed per trigger; reliability drops past ~15 chained actions |
| Planner Agent ([notes](https://learn.microsoft.com/en-us/microsoft-365/copilot/release-notes)) | Task/project | Planner | Cloud | In Copilot license | n/a | n/a | Manages tasks already in Planner; no mining from mail found |
| Glean ([Vendr](https://www.vendr.com/marketplace/glean), [gend](https://www.gend.co/blog/glean-autonomous-agents)) | Enterprise search + agents | Outlook/SharePoint connectors; Outlook drafts | Cloud SaaS | About $50–75/user, ~100-seat minimum | Yes | Optional human-in-the-loop | Cost, setup complexity, hallucinations |
| Dust ([pricing](https://dust.tt/home/pricing)) | Agent platform | Microsoft connection | Cloud (US/EU residency) | $24 / $120 per seat | Yes (RAG) | Builder-defined | 100-seat minimum for enterprise |
| Onyx ([onyx.app](https://onyx.app/)) | OSS enterprise search | Outlook/SharePoint connectors with ACL sync | Self-hosted / air-gapped, local LLMs | Free OSS / $20 per user | Yes | n/a (chat) | No mail agent, project model or approvals (inferred) |
| Claude for M365 ([usecarly](https://www.usecarly.com/blog/claude-for-microsoft-365/)) | Frontier-lab assistant | Search + write (mail, calendar, files) | Cloud | Claude plans | Yes | Admin-gated write; Outlook add-in drafts only | Outlook add-in in beta |
| Notion AI ([Notion](https://www.notion.com/help/microsoft-outlook-ai-connector)) | Workspace AI | Outlook search (no attachments) | Cloud | $20/member (Business) | n/a | n/a | Notion Mail dead as of 22 Sep 2026 |
| Superhuman ([Fast.io](https://fast.io/resources/superhuman-ai-review-2026/)) | AI email client | Gmail + Outlook accounts | Cloud | $25–40/user | Ask AI links sources | Drafts (user sends) | Per-user; no org layer |
| Shortwave ([alfred_](https://get-alfred.ai/blog/shortwave-pricing)) | AI email client | None (Gmail only) | Cloud | $24–100 | Yes | Drafts queued for approval | No Outlook |
| Fyxer ([eesel](https://www.eesel.ai/blog/fyxer-ai-reviews)) | Inbox assistant | Outlook, weaker than Gmail | Cloud | $22.50–50 | n/a | Drafts | Mislabeling, billing complaints |
| Canary ([Canary](https://canarymail.io/features/ai)) | Email client | Outlook/Exchange | Hybrid: local triage, cloud generation | n/a | n/a | n/a | Generative AI is cloud |
| eM Client ([Saymail](https://saymail.eu/en/em-client-alternative/)) | Windows client | Exchange/Outlook | Desktop + ChatGPT cloud | Pro subscription | n/a | n/a | No local model |
| Read AI ([pricing](https://www.read.ai/pricing)) | Meetings + email/messages | Gmail-first; Outlook email "coming soon" | Cloud | $15–39.75 | Reports | n/a | Email mostly Gmail |
| Asana / monday / ClickUp | Work management AI | Via integrations | Cloud | Credits / $9 add-on | n/a | Agent-defined | Needs tasks already in the tool |
| Celonis / PA Process Mining ([KYP](https://kyp.ai/process-mining-software-comparison/)) | Process mining | Power Platform | Cloud | $15/user (PA Premium) | n/a | n/a | Log-centric |

### Inferences
- **The unoccupied quadrant.** No product in the table combines all of: local or on-prem deployment, Outlook/Exchange-first processing, persistent registers linked to email and document evidence, approval workflows with an audit trail, and org-level BI. This is the product's white space.

### Gaps
- Several cells ("n/a") reflect missing data, not confirmed absence.
- No first-hand verification of citation or approval behavior for Dust, Read AI or the work-management tools.

---

## 7. Synthesis: market gaps and differentiating ideas for a locally installed Windows "enterprise operations intelligence" app with a mail agent

### Takeaway
Positioning: "the operational memory of the company, run on your own machine." Persistent, evidence-linked registers of decisions, risks, open questions, commitments and project phases, with approval-gated actions and full audit, plus BI. It runs locally, is KVKK-friendly, is Turkish-first, and has no per-seat cloud AI bill. Microsoft and Glean-class tools answer questions and run tasks in the cloud; none maintains a governed, citable operational record.

### Cited Findings

**Market signals that support the positioning**
- Copilot adds $21–30/user/month on top of M365. Cowork, agents and Agent 365 add usage and per-user fees ($0.01/credit; $15/user; E7 at $99). — [Microsoft pricing](https://www.microsoft.com/en-us/microsoft-365-copilot/pricing); [Computerworld](https://www.computerworld.com/article/4186190/microsoft-launches-copilot-cowork-with-usage-based-pricing.html); [MS Security Blog](https://www.microsoft.com/en-us/security/blog/2026/05/01/microsoft-agent-365-now-generally-available-expands-capabilities-and-integrations/)
- Executives have long questioned whether $30/user/month is justified ("I wouldn't say we're ready to spend $30 per user for every user"). — [Yahoo Tech / WSJ report](https://tech.yahoo.com/ai/articles/microsofts-pricey-ai-assistant-copilot-213938908.html)
- Turkey has no in-country Copilot processing or Cowork at-rest commitment, and KVKK Article 9 transfer justification falls on the data controller. — [Computerworld](https://www.computerworld.com/article/4085303/m365-copilot-data-processing-goes-local-to-meet-sovereignty-demands.html); [Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-365/enterprise/m365-dr-service-copilot?view=o365-worldwide); [microsoftkurumsal.com](https://www.microsoftkurumsal.com/blog/microsoft-365-copilot-kvkk-uyumu-eu-data-boundary-rehberi/)
- Copilot triage is English-only. — [justinmckelvey.com](https://justinmckelvey.com/blog/copilot-for-outlook)
- Users push back on proactive AI they can't control ("no way to disable … at a user level or for the tenant"). — [Office365ITPros](https://office365itpros.com/2026/09/15/new-outlook-copilot/)
- Copilot Studio email agents run under the maker's credentials, which creates a data-exposure risk. — [Microsoft Learn](https://learn.microsoft.com/en-us/microsoft-copilot-studio/authoring-triggers-about)
- Viva's commitment-tracking Briefing email was paused and never visibly replaced with a structured commitments register. — [Office365ITPros](https://office365itpros.com/2022/12/23/viva-briefing-pause/)
- Windows now has a first-party local AI runtime (Foundry on Windows / Foundry Local), which lowers the cost of shipping local LLM features. — [Windows Developer Blog](https://blogs.windows.com/windowsdeveloper/2026/06/02/build-2026-furthering-windows-as-the-trusted-platform-for-development/)
- Demand for air-gapped RAG over Outlook and SharePoint exists (Onyx, including a 37k-user air-gapped deployment at UCSD). — [Onyx](https://onyx.app/insights/enterprise-search-tools-2026)
- Agent 365 now inventories and governs local agents (Claude Code and others) via Defender and Intune. A local agent should expect enterprise IT to discover it and plan to integrate with that governance. — [MS Security Blog](https://www.microsoft.com/en-us/security/blog/2026/05/01/microsoft-agent-365-now-generally-available-expands-capabilities-and-integrations/)
- Email UX is shifting from inbox apps to agents: Notion Mail shut down because users "handed the whole thing to AI agents". — [Android Authority](https://www.androidauthority.com/notion-mail-is-shutting-down-3681674/)

### Inferences

**Differentiators to build, highest leverage first:**

1. **Evidence-linked registers as first-class objects.** Decisions, risks, open questions and commitments ("I will…", "can you…"), each with:
   - quoted evidence spans plus message and document IDs;
   - owner, due date, confidence and status history;
   - supersession links ("decision D-12 replaced D-7 on 3 Sep, per email X").
   - Copilot summaries cite, but they don't persist or track lifecycle.
2. **Project and phase inference across threads.** Cluster threads and documents into projects automatically, infer the phase (initiation, planning, procurement, execution, acceptance, closure) from linguistic and attachment cues, and flag drift ("no activity for 14 days", "phase regressed"). Planner Agent and Notebooks need manual curation.
3. **"Waiting on others" and "my tasks" with follow-up drafts.** Bidirectional commitment tracking (what I owe, what is owed to me), with nudge drafts that need approval before sending. This revives what Viva Briefing promised, persistently and locally. Superhuman's "waiting on" is a per-user label, not an org register.
4. **Approval-gated action drafts with an immutable audit trail.** Every outbound action (reply, forward, task creation, calendar change) is a draft object, routed to one or more approvers, signed off, and logged with before and after state, the evidence used, and the model/prompt version. This contrasts with Copilot's bulk-only confirmations and Studio's maker-credential agents.
5. **Local-first, KVKK-friendly deployment.**
   - Windows install; mail accessed through the user's own Graph or EWS token or local OST/PST.
   - Local embeddings and LLM (Foundry Local / ONNX / llama.cpp), with optional BYO cloud model under explicit policy.
   - No data leaves the premises by default.
   - Built-in data retention, redaction, KVKK data-subject-request handling and a processing inventory (VERBİS-oriented).
6. **Turkish-first language quality.** Turkish extraction models and prompts, mixed TR/EN threads, Turkish date and commitment phrasing ("Cuma'ya kadar iletirim"), and formal register in drafts. Copilot triage is English-only.
7. **Event flows and communication process mining.** Who waits on whom, cycle times per phase, approval bottlenecks, SLA breaches. Local BI dashboards replace the Celonis-class log mining that mid-sized firms can't afford.
8. **Cost model.** A per-server or perpetual license, with no per-seat AI credit metering. Contrast this with Copilot ($21–30 per seat plus credits) and Glean (about $50–75 per seat, 100-seat minimum).
9. **Coexist rather than replace.**
   - Export to Planner, To Do, Jira and Asana.
   - Import Teams and Facilitator transcripts.
   - Expose an MCP or API so Copilot, Claude or Glean can query the registers, making us the system of record they read.
   - Register with Agent 365 or Intune for IT acceptance.
10. **User control.** Every proactive digest can be configured and switched off at user and tenant level. This answers the Redmond critique directly.

**Risks to the positioning:**
- Microsoft is moving fast: Notebooks with email references, Cowork, and Planner Agent task discovery. A future "Copilot project memory" could narrow the gap, so we need depth in registers, audit and local deployment, which Microsoft is structurally unlikely to offer on-prem.
- Local LLM quality and hardware limits for Turkish long-context extraction need benchmarking.

### Gaps
- I found no primary source quantifying demand for on-prem or local email AI in Turkey, and no KVKK Board guidance on LLM processing of employee email; both need separate research.
- The details of the 2024 KVKK Article 9 amendment (standard contracts for cross-border transfer) and how it applies to Copilot were not verified in this research.
- The absence of competitors with structured decision and risk registers is inferred from the products reviewed. Niche vendors (e.g., PMO-focused AI or RAID-log tools) were not searched because the search budget ran out.
