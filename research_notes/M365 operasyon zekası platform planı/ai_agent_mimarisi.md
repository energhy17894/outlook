# AI / LLM / Agent Architecture for a Local Windows "Enterprise Operations Intelligence" Mail Agent (state: September 2026)

Scope note: these notes cover the AI layer only: model hosting, the extraction pipeline, grounding and citations, retrieval, project and phase detection, orchestration, human-in-the-loop (HITL), cost, security and evaluation. The target is a local Windows install: background services, a local HTTPS web UI on port 6500, Microsoft 365 email and documents, Turkish and English content. Preview and unstable items are flagged inline. Where only a secondary source was available, that is said.

---

## 1. Model hosting: fully local vs cloud vs hybrid, hardware, throughput, Turkish ability, licensing

### Takeaway
A **hybrid** design is the practical default for September 2026:
- **Local components (always on):** parsing, language ID, triage/classification, PII tagging and embeddings.
- **Cloud frontier model:** deep, cited extraction and weekly/project roll-ups, run through Batch APIs.
- **"Strict-local" mode:** an optional mode where a local 8–30B open-weight model (Qwen3/3.5 family, Gemma 4, gpt-oss) does the extraction instead.

Foundry Local is now GA, but at GA it is an **in-process native library**, not the separate Windows service described in the preview. Phi Silica is being replaced by Aion in Nov 2026. Neither the Windows AI APIs nor Aion should be a core dependency yet.

### Cited Findings

**Microsoft Foundry Local**
- Foundry Local went into public preview at Build (19 May 2025). It was declared generally available on 9 April 2026. Source is secondary (a wiki); the GitHub README still refers to a "CLI preview 0.10.0" — [AI Wiki](https://aiwiki.ai/wiki/foundry_local); [GitHub microsoft/Foundry-Local](https://github.com/microsoft/Foundry-Local)
- **Architecture changed (important for this project).**
  - The current Learn architecture page (ms.date 2026-05-06, updated 2026-08-04) says Foundry Local "ships as a single native library inside your application" and that there is "no separate installer or background service to manage".
  - The Core API is a `.dll` loaded in-process. It is thread-safe and session-based, and supports concurrent requests.
  - SDKs: C# (`Microsoft.AI.Foundry.Local` NuGet), JS, Python and Rust.
  - An **optional OpenAI-compatible REST endpoint** can be started inside your own process.
  - Source: [Microsoft Learn – Foundry Local architecture](https://learn.microsoft.com/en-us/azure/foundry-local/concepts/foundry-local-architecture)
- Earlier (preview-era) write-ups describe a Windows Service started with `foundry service start` that exposes an OpenAI-compatible endpoint. This conflicts with the GA architecture page above; treat it as preview-era behaviour — [4sysops](https://4sysops.com/archives/run-and-install-ai-models-on-your-windows-pc-with-microsoft-foundry-local/); [zenn.dev](https://zenn.dev/suusanex/articles/139ce768546ae9?locale=en)
- Foundry Local supports OpenAI request/response formats, including the **Responses API** format, and tool calling. The catalog covers chat models (GPT OSS, Qwen, DeepSeek, Mistral, Phi) and Whisper. The SDK is MIT-licensed; the CLI is under Microsoft Software License Terms; each model has its own licence — [GitHub microsoft/Foundry-Local](https://github.com/microsoft/Foundry-Local)
- Execution providers:
  - NVIDIA CUDA (GPU)
  - WebGPU via Dawn → Direct3D (GPU)
  - AMD Vitis (NPU)
  - Qualcomm (NPU)
  - Intel OpenVINO (GPU)
  - CPU fallback (always available)
  - On Windows, EP plugins are acquired and registered through WinML/Windows Update. Models download once from the Foundry Catalog, then run offline from the local cache.
  - Source: [Microsoft Learn – Foundry Local architecture](https://learn.microsoft.com/en-us/azure/foundry-local/concepts/foundry-local-architecture)
- System requirements (secondary source): minimum 8 GB RAM and 3 GB disk; recommended 16 GB RAM and 15 GB disk. Supported OS: Windows 10 x64, Windows 11 x64/ARM, Windows Server 2025. New NPUs need Windows 24H2 or later — [OpenTechTips / search summary](https://opentechtips.com/run-ai-locally-windows-ollama-lm-studio-foundry-local/); [Robert Smit – Foundry Local on Windows Server 2025](https://robertsmit.wordpress.com/2025/10/29/running-ai-foundry-local-on-windows-server-2025-a-fully-offline-ai-model-deployment-guide/)

**Microsoft Foundry on Windows (umbrella)**
- The umbrella has three parts:
  - Windows AI APIs: Phi Silica plus imaging, OCR and semantic search, mostly on Copilot+ PCs.
  - Foundry Local: "20+ OSS LLM models", Windows 10 and later.
  - Windows ML: ONNX Runtime with dynamically installed hardware EPs, for your own models on Windows 10 and later.
  - Microsoft's own decision tree: try the Windows AI APIs first, then Foundry Local, then Windows ML for custom models.
  - Source: [Microsoft Learn – Use local AI with Microsoft Foundry on Windows](https://learn.microsoft.com/en-us/windows/ai/overview)

**Windows AI APIs, Phi Silica and Aion**
- Phi Silica hardware support:
  - On Copilot+ PCs it runs on the NPU.
  - GPU support covers NVIDIA RTX 30 series and newer, and AMD RX 9060 and newer, each with 6+ GB VRAM. GPU mode requires Developer Mode.
  - There is no CPU support.
  - The Phi Silica stable release is a **Limited Access Feature** (Windows App SDK 1.8). GPU support is only in the 2.2.2-experimental (June 2026) channel.
  - Source: [Microsoft Learn – What are Windows AI APIs?](https://learn.microsoft.com/en-us/windows/ai/apis/)
- **Phi Silica is being replaced by Aion Instruct.** Rollout to Windows Insider devices starts in October 2026; retail follows in November 2026, "at which point Phi Silica will be removed" — [Microsoft Learn – What are Windows AI APIs?](https://learn.microsoft.com/en-us/windows/ai/apis/)
- Secondary sources add more detail. Treat it as unverified:
  - Aion 1.0 Instruct is a lightweight SLM that runs on CPU, GPU or NPU.
  - Aion 1.0 Plan is 14B parameters with 32K context.
  - Retail GA is 24 Nov 2026.
  - LAF tokens are no longer required.
  - Sources: [WindowsForum](https://windowsforum.com/windows-news.4/microsoft-will-replace-phi-silica-with-aion-instruct-in-windows-11-this-fall.440821/); [windowsnews.ai](https://windowsnews.ai/article/windows-11s-on-device-ai-gets-a-faster-upgrade-aion-instruct-arrives-november-24.440821)

**Ollama for Windows**
- Requirements: Windows 10 22H2 or newer; NVIDIA drivers 551.61 or newer; AMD via ROCm v7/HIP or Vulkan.
- It runs in the background on `http://localhost:11434`. Model storage can be moved with `OLLAMA_MODELS`.
- A standalone `ollama-windows-amd64.zip` exists for embedding Ollama in your own app or running it as a system service.
- Source: [Ollama docs – Windows](https://docs.ollama.com/windows)
- Ollama supports **structured outputs constrained by a JSON schema** — [Ollama blog – Structured outputs](https://ollama.com/blog/structured-outputs). Practitioner guidance for Qwen3: temperature 0, Pydantic for schema and validation, keep schemas shallow — [Rost Glukhov](https://www.glukhov.org/post/2025/09/llm-structured-output-with-ollama-in-python-and-go/). There are known structured-output problems with gpt-oss on Ollama — [Rost Glukhov – gpt-oss issues](https://www.glukhov.org/post/2025/10/ollama-gpt-oss-structured-output-issues/)
- `qwen3-embedding` (0.6B/4B/8B) is in the Ollama library — [Ollama – qwen3-embedding](https://ollama.com/library/qwen3-embedding)

**Throughput reference points**
- RTX 4070 with Llama 3.1 8B Instruct Q4_K_M: about **3,192 tok/s prompt processing** and **76.3 tok/s generation** (LocalScore, via search summary). Other summaries cite about 52–68 tok/s generation for 8B models on an RTX 4070 — [LocalScore – RTX 4070](https://www.localscore.ai/accelerator/147); [LocalLLM.in](https://localllm.in/blog/llamacpp-vram-requirements-for-local-llms)

**Open-weight models and Turkish**
- **TurkBench** (Jan 2026) evaluated 27 open models. No proprietary models are in the main table. Top averages:

  | Model | TurkBench avg |
  |---|---|
  | gpt-oss-120b | 78.6 |
  | GLM-4.6 | 76.9 |
  | DeepSeek-V3.1 | 75.2 |
  | Qwen3-Next-80B-Instruct | 75.0 |
  | Qwen3-30B-A3B-Instruct | 73.4 |
  | TR-Gemma-9b (Turkish-specific) | 65.3 |

  - Larger models (Qwen-32B, Gemma-27B) consistently beat smaller ones.
  - Culturally grounded reasoning is still weak.
  - Source: [TurkBench arXiv 2601.07020](https://arxiv.org/html/2601.07020v1)
- **Cetvel** (EACL 2026):
  - 23 tasks and 33 open-weight models up to 70B.
  - Llama-3.3-70B-Instruct scored best overall.
  - Turkish-centric instruction-tuned models generally underperform multilingual general models.
  - Sources: [ACL Anthology](https://aclanthology.org/2026.eacl-long.46/); [arXiv 2508.16431](https://arxiv.org/abs/2508.16431)
- **Qwen3.5** (Feb 2026):
  - Apache 2.0 licence; 201 languages and dialects.
  - The 9B model has a hybrid Gated DeltaNet + gated-attention architecture and 262K native context.
  - Thinking mode is on by default and can be disabled (`enable_thinking: False`).
  - 9B scores: MMMLU 81.2, MMLU-ProX 76.3.
  - The model card does not name Turkish explicitly.
  - Source: [HF Qwen/Qwen3.5-9B](https://huggingface.co/Qwen/Qwen3.5-9B)
- **Gemma 4** (Apr 2026):
  - Sizes: E2B, E4B, 12B, 26B-A4B (MoE) and 31B dense.
  - **Apache 2.0**, which replaces the earlier Gemma Terms of Use.
  - Over 140 languages; up to 256K context; multimodal.
  - Sources: [Gemma 4 model card](https://ai.google.dev/gemma/docs/core/model_card_4); secondary summary [Codersera](https://codersera.com/blog/gemma-4-complete-guide-2026/)

**Cloud options and data residency**
- **Anthropic first-party API data residency:**
  - `inference_geo` supports only `"global"` and `"us"`.
  - The only workspace geo is `"us"`.
  - There is **no EU inference geo** on the first-party API.
  - US-only inference costs 1.1× (Claude 4.6+).
  - Claude in Microsoft Foundry offers a US Data Zone Standard option.
  - Bedrock and Google Cloud regional/multi-region endpoints carry a 10% premium.
  - Sources: [Claude docs – Data residency](https://platform.claude.com/docs/en/manage-claude/data-residency); [Claude docs – Pricing](https://platform.claude.com/docs/en/about-claude/pricing)
- **Azure OpenAI Data Zones:**
  - Data Zone Standard keeps processing inside the Microsoft-defined zone, EU or US.
  - **Data Zone Batch** exists as a batch deployment type restricted to the data zone.
  - Sources: [Azure blog – Data Zones](https://azure.microsoft.com/en-us/blog/announcing-the-availability-of-azure-openai-data-zones-and-latest-updates-from-azure-ai/); [Tech Community – Data Zones for Batch](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/announcing-data-zones-for-azure-openai-service-batch/4366754); [Learn – deployment types](https://learn.microsoft.com/en-us/azure/foundry/foundry-models/concepts/deployment-types)
  - Model-by-model EU availability varies. Q&A threads discuss GPT-5.5 as Data Zone Standard (EUR) — [Microsoft Q&A](https://learn.microsoft.com/en-us/answers/questions/5870925/gpt-5-5-data-zone-standard-eu)
- **OpenAI direct:** regional-processing (data residency) endpoints carry a 10% uplift for models released on or after 5 March 2026 — [OpenAI pricing](https://developers.openai.com/api/docs/pricing)

### Inferences
- **Recommended tiering:**
  - **Tier 0 – deterministic, local:** MIME and Graph parsing, quote and signature stripping, language ID, rules.
  - **Tier 1 – local model:**
    - Triage: newsletter/notification/FYI vs actionable.
    - Coarse project routing and PII tagging.
    - Embeddings.
    - Model size: a 4–9B model (e.g., Qwen3.5-4B/9B or Gemma 4 E4B/12B) through Foundry Local (in-process, C#) or Ollama.
  - **Tier 2 – cloud frontier model:** cited extraction and roll-ups via Batch (Claude Sonnet 5 / Haiku 4.5, GPT-5.x/6 family, or Azure OpenAI Data Zone EU where EU residency is mandatory).
  - **Strict-local mode:** replaces Tier 2 with Qwen3-30B-A3B-class or gpt-oss-class models on capable workstations.
- **Wiring for a .NET Windows service:** embed Foundry Local through the NuGet SDK so there is no second daemon to manage. Enable its in-process OpenAI-compatible REST endpoint only if Python tooling needs it. Ollama is the pragmatic alternative when you want GGUF models, faster model availability, or `qwen3-embedding`/`bge-m3`. Run it from the standalone zip as a service you control, not the tray app.
- **EU/Turkish data-residency needs:** first-party Anthropic cannot currently pin inference to the EU. The routes are Azure OpenAI Data Zone EU, or Claude via Bedrock or Vertex regional EU endpoints (+10%). Check model availability per region before committing.
- **Throughput at 200 emails/day (my arithmetic):**
  - Assumptions: about 6.5k input tokens and 0.6k output tokens per email for a full extraction.
  - Load: about 1.3M prefill tokens and 120k generated tokens per day.
  - On an RTX 4070-class GPU with an 8B Q4 model: about 7 minutes of prefill plus about 33 minutes of generation, i.e. **about 40 GPU-minutes per day**.
  - That is comfortably feasible as background processing.
  - CPU-only or NPU-only laptops will likely be too slow for a 30B-class model. They are fine for triage and embeddings with small models.
- **Turkish quality:** evidence favours big multilingual models over Turkish-specific finetunes. Among PC-runnable models, Qwen3-30B-A3B (MoE, about 3B active) is the best-evidenced Turkish performer in TurkBench.

### Gaps
- No Turkish benchmark results found for Qwen3.5, Gemma 4, Phi-4 or Aion; TurkBench and Cetvel predate or omit them. Run your own Turkish eval.
- Not confirmed whether Foundry Local's catalog or REST endpoint exposes **embeddings** or **JSON-schema-constrained output**.
- Not confirmed whether Aion or Phi Silica handles **Turkish**, or whether they can be called from a non-packaged Windows service (app identity / LAF constraints).
- Throughput numbers for CPU-only, Snapdragon/Intel NPUs, and 30B MoE models with partial GPU offload were not found.
- Licence terms for Llama 3.x and gpt-oss were not re-verified in this pass.

---

## 2. Extraction pipeline: thread reconstruction, quote/signature/disclaimer stripping, attachments, TR/EN, chunking

### Takeaway
Rebuild threads deterministically before any LLM call:
- Use Graph `conversationId` plus RFC 5322 headers.
- Use Graph `uniqueBody`, with a fallback to your own quote/signature stripper, so each message contributes only its new text.
- Parse attachments with Docling for structure-heavy files and MarkItDown for speed.

Chunk emails per message (or per paragraph within a message) and documents by structure, so citations can point to a stable `(message_id, char span)`.

### Cited Findings
- Graph `message.uniqueBody` is "the part of the body of the message that is unique to the current message". It is not returned by default and must be requested with `$select=uniqueBody` — [Microsoft Learn – message resource](https://learn.microsoft.com/en-us/graph/api/resources/message?view=graph-rest-1.0)
- A GitHub issue reports that `uniqueBody` sometimes still contains the whole conversation — [msgraph-sdk-php issue #1576](https://github.com/microsoftgraph/msgraph-sdk-php/issues/1576)
- Microsoft Q&A reports that `conversationId` can change when a user replies. Community advice is to link by RFC 5322 `Message-ID` / `In-Reply-To` / `References` and MAPI `PidTagConversationIndex` rather than rely only on `conversationId` — [Microsoft Q&A](https://learn.microsoft.com/en-sg/answers/questions/5629315/sometimes-the-conversation-id-changes-when-a-user)
- **Docling:**
  - Reconstructs table cell structure and reading order.
  - A secondary benchmark gives Docling 88% F1 vs MarkItDown 82%.
  - MarkItDown is 50–100× faster on clean PDFs.
  - Sources: [danilchenko.dev](https://www.danilchenko.dev/posts/markitdown-vs-docling-vs-marker/); [aicoolies](https://aicoolies.com/comparisons/docling-vs-markitdown) (secondary)
- **MarkItDown** converts `.eml` to body text only, with no indication of attachments — [markitdown issue #1662](https://github.com/microsoft/markitdown/issues/1662)
- Docling issues discuss email (`.msg`/`.eml`) parsing and routing attachment bytes back through `DocumentConverter`. These appear to be proposals and discussions, not confirmed shipped features — [docling #3909](https://github.com/docling-project/docling/issues/3909); [docling #3712](https://github.com/docling-project/docling/issues/3712)
- **Turkish token cost:** the same 2,009 sentences cost ×1.40 to ×2.21 as many tokens in Turkish as in English, depending on the tokenizer. A 100k window on GPT-4o holds about 30% less Turkish text — [token-toll (GitHub)](https://github.com/mtalhasahin/token-toll)
- Anthropic notes that Claude 4.7+ models use a new tokenizer that produces about 30% more tokens for the same text — [Claude pricing](https://platform.claude.com/docs/en/about-claude/pricing)
- Lampert et al. found that only some regions of an email matter for detecting requests and commitments; other regions add noise. This supports stripping quotes, signatures and disclaimers before extraction — summarized in [Azarbonyad et al., MSR](https://www.microsoft.com/en-us/research/wp-content/uploads/2019/01/AzarbonyadWSDM2019.pdf)
- The Claude Citations API chunks plain text and PDFs into sentences. **Custom content documents** use your own blocks as-is, and citations then return block index ranges — [Claude docs – Citations](https://platform.claude.com/docs/en/build-with-claude/citations)

### Inferences
- **Pipeline sketch:**
  1. **Ingest:** Graph delta sync → store raw MIME/HTML immutably, with `internetMessageId`, `conversationId`, `conversationIndex`, `In-Reply-To` and `References`.
  2. **Thread rebuild:** a union-find over header links plus `conversationId` plus normalized-subject fallback.
  3. **Clean:** use `uniqueBody`, then a rule-based stripper for:
     - Turkish/English reply headers: "From:/Gönderen:", "On … wrote:", "… tarihinde … yazdı:".
     - Signature and disclaimer blocks.
     - Legal footers ("Bu e-posta ve ekleri gizlidir…").
     - Store both raw and cleaned text, plus an offset map between them.
  4. **Attachments:** Docling for PDF/DOCX/PPTX/XLSX with tables; MarkItDown as a fast path; OCR only when needed (Azure Document Intelligence or Windows OCR API).
  5. **Language ID per paragraph**, since mixed TR/EN is common.
  6. **Chunking:**
     - Emails: one unit per message, split to paragraphs if long.
     - Documents: heading/section-aware chunks of about 500–1,000 tokens.
     - Every chunk carries a stable ID and char offsets, so citations are cheap to verify.
- **Store citations against the cleaned text plus the offset map, never against rendered HTML.** Keep the map back to raw text so the UI can highlight the span in the original.
- **Turkish text normalization:** dotted/dotless i (İ/ı) casing and diacritics need locale-aware lowercasing (tr-TR) for keyword matching and exact-quote verification. Use culture-aware normalization in .NET or Python (`casefold` is not Turkish-aware).

### Gaps
- No authoritative benchmark found for Turkish-language quote/signature stripping. Existing libraries (e.g., talon, email-reply-parser) are English-centric; not verified in this pass.
- No verified 2026 information on Azure AI Document Intelligence pricing or Turkish OCR quality.

---

## 3. Structured extraction with per-claim citations, grounding verification, confidence and abstention

### Takeaway
Use JSON-schema-constrained outputs where every extracted item carries an `evidence[]` array of `{message_id, quote}`. Then verify deterministically: the normalized exact quote must exist in that message's cleaned text, and char offsets are computed locally.

Optionally add an NLI/fact-check pass (MiniCheck-class). Anthropic's Citations API gives guaranteed-valid pointers, but it **cannot be combined with structured outputs**. Use it either in a two-pass design or for Q&A answers, not for the JSON extraction call.

### Cited Findings
- **Claude structured outputs** are **GA** on the Claude API, Bedrock, Google Cloud and Microsoft Foundry.
  - Two modes: `output_config.format` for JSON answers and `strict: true` for tool inputs.
  - Unsupported: recursive schemas, numeric/string length constraints, external `$ref`.
  - The grammar compiles on first use and is cached for 24 hours.
  - Changing `output_config.format` invalidates the prompt cache.
  - Source: [Claude docs – Structured outputs](https://platform.claude.com/docs/en/build-with-claude/structured-outputs)
- **Citations + structured outputs = HTTP 400.** "Citations cannot be used together with structured outputs… citations require interleaving citation blocks with text output" — [Claude docs – Citations](https://platform.claude.com/docs/en/build-with-claude/citations)
- Citations API properties:
  - Citations are "guaranteed to contain valid pointers to the provided documents".
  - `cited_text` does not count toward output tokens.
  - Formats: `char_location` (text), `page_location` (PDF), `content_block_location` (custom content).
  - GA on all platforms, including Microsoft Foundry.
  - Source: [Claude docs – Citations](https://platform.claude.com/docs/en/build-with-claude/citations)
- **ExtractBench** (Feb 2026):
  - 35 PDFs, 12,867 fields; field-level metrics (exact match for IDs, tolerance for quantities, semantic equivalence for names) that separate omissions from hallucinations.
  - Frontier models (GPT-5/5.2, Gemini 3, Claude 4.5) "remain unreliable on realistic schemas".
  - They produced **0% valid output on a 369-field schema**.
  - Source: [arXiv 2602.12247](https://arxiv.org/abs/2602.12247)
- **MiniCheck** (EMNLP 2024):
  - Small fact-checkers, Flan-T5-Large (770M) and Bespoke-MiniCheck-7B.
  - GPT-4-level grounding checks at about 400× lower cost; 74.7% balanced accuracy on LLM-AggreFact vs GPT-4 at 75.3%.
  - Sources: [arXiv 2404.10774](https://arxiv.org/abs/2404.10774); [GitHub Liyan06/MiniCheck](https://github.com/Liyan06/MiniCheck)
- The ALCE benchmark formalized **citation recall** (is each statement fully supported by its citations?) and **citation precision** (is each citation relevant?), scored with an NLI model — [Gao et al., arXiv 2305.14627](https://arxiv.org/abs/2305.14627) (well-known paper, not re-fetched in this pass)
- Ollama and Qwen3 structured outputs work well at temperature 0 with shallow schemas — [Rost Glukhov](https://www.glukhov.org/post/2025/09/llm-structured-output-with-ollama-in-python-and-go/)

### Inferences
- **Recommended extraction contract** (per thread, per run):
  - Items: `decisions[]`, `risks[]`, `open_questions[]`, `commitments[]`, `action_items[]` and `project_signals[]`.
  - Each item carries:
    - `evidence: [{message_id, quote}]`, with 1–3 short verbatim quotes.
    - `owner_email`, `counterparty_email`, `due_date` (ISO 8601 or `null`) and `due_date_text`.
    - `status` (open/done/cancelled) and `confidence` (low/med/high).
    - `abstain_reason`, used when owner or date is unclear.
  - Keep schemas small and flat. ExtractBench shows validity collapsing on large schemas. Split into multiple calls (decisions/risks vs commitments/actions vs project/phase) rather than one mega-schema.
- **Verification ladder** (deterministic first):
  1. Schema validation (Pydantic / System.Text.Json).
  2. Exact or near-exact quote match against the cleaned message text after tr-TR normalization and whitespace/quote-character folding. Reject or downgrade the item if there is no match.
  3. Check that `message_id` belongs to the thread and that the quote's author matches the claimed owner where relevant.
  4. Optional NLI entailment check (claim vs quote) using a MiniCheck-style model locally. Its Turkish coverage is unverified; a multilingual LLM judge is the fallback.
  5. Items that fail verification go to "needs review" and are not shown as facts.
- **Confidence scoring:** combine verification results, the model's self-rating, agreement across two samples or two models on high-impact items (e.g., commitments with deadlines), and rule features (modal verbs, explicit dates). Allow `null` for owner and date, and never infer a deadline from vague text without flagging it.
- **Two ways to use Anthropic Citations:**
  - Pass A: Citations API with **custom content documents**, one block per message paragraph, to get prose findings with guaranteed pointers. Pass B: convert them to JSON with structured outputs.
  - Or use Citations only for user-facing Q&A ("why is this marked a risk?") and keep extraction on structured outputs plus local verification. The second option is provider-agnostic and also works with local models.

### Gaps
- No published benchmark found for citation-grounded extraction from **email threads specifically**, nor for NLI/grounding checkers on **Turkish** text.
- No calibration studies found on LLM self-reported confidence for commitment/action-item extraction.

---

## 4. Retrieval and knowledge store: hybrid search, local multilingual embeddings, GraphRAG / temporal KGs, entity resolution

### Takeaway
For a single-user local app, one **SQLite** database covers most needs: FTS5 (BM25, with a trigram tokenizer for Turkish agglutination) plus `sqlite-vec`, fused with RRF. It also holds relational tables for entities, edges and evidence with validity intervals.

For local multilingual embeddings, use **bge-m3** (strong Turkish retrieval evidence), **Qwen3-Embedding-0.6B/4B** or **EmbeddingGemma-308M**. Full Microsoft GraphRAG is overkill. A lightweight temporal "evidence graph" (Graphiti-style bi-temporal edges) in SQL gives person–project–decision linking.

### Cited Findings
- Hybrid search in a single SQLite file: FTS5 (BM25) plus `sqlite-vec`, fused with Reciprocal Rank Fusion (k ≈ 60). About 200 lines of Python — [Simon Willison](https://simonwillison.net/2024/Oct/4/hybrid-full-text-search-and-vector-search-with-sqlite/); [DEV – Hybrid RAG in 200 lines](https://dev.to/soytuber/building-a-hybrid-rag-in-200-lines-sqlite-fts5-sqlite-vec-rrf-38h1)
- A secondary teardown claims an FTS5 trigram BM25 + sqlite-vec + RRF setup reaches about 90–95% of managed hybrid-search quality — [DEV – Cortex vs SQLite](https://dev.to/soytuber/cortex-search-vs-hybrid-sqlite-rag-a-cost-and-latency-teardown-3b69)
- Academic local-first hybrid retrieval with adaptive fusion for agents — [vstash arXiv 2604.15484](https://arxiv.org/pdf/2604.15484)
- **Qwen3-Embedding:** 0.6B/4B/8B; 32k context; 100+ languages; flexible output dimensions. The 8B model ranked #1 on MTEB multilingual (70.58) as of 5 June 2025 — [HF Qwen3-Embedding-8B](https://huggingface.co/Qwen/Qwen3-Embedding-8B); [Qwen3 Embedding blog](https://qwen.ai/blog?id=qwen3-embedding)
- **BGE-M3:** dense, sparse and multi-vector retrieval in one model; 100+ languages; up to about 8,192 tokens — [HF BAAI/bge-m3 model card](https://huggingface.co/BAAI/bge-m3) (the facts come from a search summary; the model card was not re-fetched in this pass)
- **Turkish evidence (search summary of the Mecellem paper):** BGE-M3 has the highest absolute scores in Turkish clustering and retrieval. Turkish-specific Mursit models (155M–403M) come within 2–5 points — [Mecellem arXiv 2601.16018](https://arxiv.org/pdf/2601.16018)
- **EmbeddingGemma:**
  - 308M parameters; 100+ languages; best open multilingual model under 500M on MTEB (at launch).
  - Matryoshka dimensions 768/512/256/128.
  - Under 200 MB RAM when quantized.
  - Sources: [Google Developers Blog](https://developers.googleblog.com/en/introducing-embeddinggemma/); [arXiv 2509.20354](https://arxiv.org/pdf/2509.20354)
- **Graphiti / Zep:**
  - Bi-temporal knowledge graph with valid time and ingestion time (`t_valid`, `t_invalid`, `t_created`, `t_expired`).
  - Superseded facts are invalidated, not deleted.
  - Hybrid semantic, keyword and graph search is used to detect conflicts.
  - DMR benchmark: 94.8% vs MemGPT 93.4%.
  - Sources: [Zep paper arXiv 2501.13956](https://arxiv.org/abs/2501.13956); [GitHub getzep/graphiti](https://github.com/getzep/graphiti); [Neo4j blog](https://neo4j.com/blog/developer/graphiti-knowledge-graph-memory/)
- **LazyGraphRAG:** indexing cost identical to vector RAG and 0.1% of full GraphRAG; community summarization is deferred to query time — [Microsoft Research blog](https://www.microsoft.com/en-us/research/blog/lazygraphrag-setting-a-new-standard-for-quality-and-cost/)
- A secondary source puts full GraphRAG at about $20–40 per 1M tokens with gpt-4o vs about $0.50 for LightRAG. LightRAG updates incrementally — [CallSphere](https://callsphere.ai/blog/vw6g-microsoft-graphrag-knowledge-graph-2026) (secondary; treat numbers as rough)

### Inferences
- **Recommended schema (SQLite):**
  - Content tables: `messages`, `threads`, `attachments`, `chunks` (plus FTS5 and sqlite-vec virtual tables).
  - Entity tables: `persons` (canonical key = SMTP address / Entra objectId), `organizations` and `projects` (registry).
  - Knowledge tables: `facts`/`items` (decision/risk/commitment/action/question) and `edges` (subject, predicate, object, `valid_from`, `valid_to`, `observed_at`).
  - Evidence table: `evidence` (item_id → message_id, char_start, char_end, quote).
  - This gives Graphiti-style temporal semantics without running Neo4j or FalkorDB on the user's PC.
- **Entity resolution:**
  - People: use M365 directory data (SMTP, proxyAddresses, display names) as the ground truth. Use the LLM only to map free-text mentions ("Ahmet Bey", "satın alma") to known entities, with a confidence score and user-confirmable merges.
  - Projects: a user-editable registry with aliases, codes and customer names, plus embedding similarity for candidates.
- **Scaling:** if data grows beyond one user's mailbox (e.g., shared mailboxes, team rollout), PostgreSQL + pgvector or LanceDB/Qdrant local become relevant. For one user's mailbox (tens of thousands of messages), SQLite is sufficient, easiest to back up, and fits a Windows service.
- **Turkish FTS:** the FTS5 trigram tokenizer sidesteps the lack of a Turkish stemmer. Pair it with dense embeddings for morphology-heavy queries.

### Gaps
- No 2026 Turkish-specific MTEB leaderboard numbers found comparing Qwen3-Embedding, EmbeddingGemma, multilingual-e5 and bge-m3 head-to-head.
- Not verified which local rerankers (e.g., bge-reranker-v2-m3, Qwen3-Reranker) handle Turkish well; no source retrieved.
- Graphiti's local backend requirements (Neo4j/FalkorDB/Kuzu) and Windows support were not verified.

---

## 5. Project clustering, phase detection and process mining from email

### Takeaway
Treat project assignment as **classification against a user-curated project registry**, with retrieval-augmented candidates from embeddings plus participants, subject codes and attachment names. Use unsupervised clustering plus LLM labeling only to **propose new projects** for user confirmation.

Phase detection should use an explicit, configurable lifecycle model, for example Teklif/Proposal → Sözleşme/Contract → Planlama → Uygulama/Execution → Test/Kabul → Kapanış/Closure, with evidence-backed phase-transition events. Those events also produce a process-mining event log (case = project, activity = event type, timestamp = message time).

### Cited Findings
- **Seeth, Tavares & Schuster** (arXiv 2609.01320, 1 Sep 2026):
  - Fine-tuned Llama-3.1-8B, Ministral-8B and Qwen2.5-7B with LoRA (r = 16) to convert unstructured text into XES event logs.
  - Fine-tuning "substantially outperformed" zero- and few-shot prompting.
  - Llama produced 430 valid traces out of 434.
  - Discovered-model F1 came close to the original (0.90) with the Heuristics Miner.
  - The paper does not address email directly.
  - Source: [arXiv 2609.01320](https://arxiv.org/html/2609.01320)
- LLM-assisted event log extraction via generated SQL shows potential when domain knowledge is available — [Springer CoopIS 2024/25](https://link.springer.com/chapter/10.1007/978-3-031-81375-7_4)
- Earlier work built a framework for mining process models from email logs by labeling emails with activity names — [arXiv 1609.06127](https://arxiv.org/abs/1609.06127)
- A local-LLM pipeline fused textual error reports with event logs for process mining — [Springer](https://link.springer.com/chapter/10.1007/978-3-032-28274-3_2)
- The PM4Py.LLM module exists for combining process mining with LLMs — [arXiv 2404.06035](https://arxiv.org/pdf/2404.06035)
- Commitment detection models degrade under **domain shift**; models trained on Enron do not transfer cleanly to other domains. This argues for per-tenant adaptation and registry-based classification — [Azarbonyad et al., WSDM 2019](https://www.microsoft.com/en-us/research/publication/domain-adaptation-for-commitment-detection-in-email/)

### Inferences
- **Incremental algorithm per new thread:**
  1. Candidate projects from (a) participant overlap with project rosters, (b) subject tokens and codes (PO/contract numbers), (c) embedding kNN over project centroids and recent project threads.
  2. LLM classification over the top 3–5 candidates plus "none/new", with a cited reason.
  3. If "none", accumulate the thread in an "unassigned" pool. Periodically cluster the pool (e.g., HDBSCAN on embeddings plus participant graph features) and have the LLM propose a name and summary for **user approval**.
- **Phase:** keep a finite-state lifecycle per project type with allowed transitions. The LLM proposes `phase_transition` events with evidence, and deterministic rules reject illegal jumps. The current phase is the latest accepted transition.
- **Timeline / event flow:** every extracted decision, commitment or phase transition becomes an event row `(case_id=project, activity, timestamp, actor, evidence)`. It can be exported to XES/OCEL for PM4Py if process-mining views are needed later.
- For strict-local mode, the Seeth et al. result suggests that a **LoRA fine-tuned 7–8B model** trained on the company's own labeled threads could beat prompting a similar local model. That is a phase-2 option once a golden set exists.

### Gaps
- No published dataset or benchmark found for **project-phase classification from email**.
- No academic work found that evaluates LLM-based email-to-project clustering in enterprises.

---

## 6. Commitment, action-item, decision and risk extraction research

### Takeaway
Research on commitment and request detection in email is mature for the pre-LLM era: MSR, Enron and Avocado datasets, and the Smart To-Do work. It highlights three lessons that still apply:
1. Only parts of an email carry commitments.
2. Models degrade under domain shift.
3. Sender and receiver roles and intent are the hard part.

There is no widely adopted 2025–2026 LLM benchmark for email commitments. Plan to build an internal golden set.

### Cited Findings
- **Azarbonyad et al.** (MSR, WSDM 2019):
  - Detecting commitments ("I'll send the report by end of day") helps assistants remind users of their promises.
  - Performance is reliable in-domain but degrades across domains.
  - Experiments used Enron (about 200K emails from 158 mostly senior users) and HP IT-incident chat logs.
  - Sources: [MSR publication](https://www.microsoft.com/en-us/research/publication/domain-adaptation-for-commitment-detection-in-email/); [PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2019/01/AzarbonyadWSDM2019.pdf)
- **Lampert et al.** (as summarized there): request and commitment classifiers used modal verbs, question words and length. Key conclusion: only some email regions matter — [PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2019/01/AzarbonyadWSDM2019.pdf)
- **"Smart To-Do"** (ACL 2020): automatic generation of to-do items from emails — [ACL Anthology 2020.acl-main.767](https://aclanthology.org/2020.acl-main.767.pdf)
- Context-aware intent identification in email conversations (SIGIR 2019) — [ACM DL](https://dl.acm.org/doi/pdf/10.1145/3331184.3331260)
- Enterprise email reply-behavior prediction (MSR, SIGIR 2017) — [PDF](https://www.microsoft.com/en-us/research/wp-content/uploads/2017/04/sigir17a.pdf)
- **EmailSum** (ACL 2021):
  - 2,549 Avocado email threads (3–10 emails each) with short (<30 words) and long (<100 words) human summaries.
  - Hard parts: understanding sender intent and sender/receiver roles.
  - **ROUGE and BERTScore correlate weakly with human judgments** on this task.
  - Sources: [ACL Anthology](https://aclanthology.org/2021.acl-long.537/); [GitHub ZhangShiyue/EmailSum](https://github.com/ZhangShiyue/EmailSum)
- **MUG** meeting benchmark includes action-item detection among its tasks — [arXiv 2303.13939](https://arxiv.org/pdf/2303.13939)
- **MeetingBank** is a related meeting-summarization benchmark — [arXiv 2305.17529](https://arxiv.org/abs/2305.17529)
- Related work tags "author commitment" (committed belief) on Enron email to infer social power and context — [arXiv 1805.06016](https://arxiv.org/pdf/1805.06016)

### Inferences
- **Map the research to the schema:**
  - Commitment = speaker-owned future action ("I will…/…göndereceğim").
  - Request = action asked of the addressee ("can you…/…rica ederim").
  - Action item = a commitment or request, normalized to "who owes what to whom by when".
  - Decision = a resolved choice with a decider and date.
  - Risk = an uncertain negative event with impact/likelihood cues.
  - Open question = an unanswered request for information.
- **"My tasks" = items where the owner is the mailbox user, plus requests addressed to the user that are still unanswered.** This follows directly from reply-behavior and intent research.
- **Status tracking** needs cross-message reasoning ("done", "attached", "tamamlandı" in later replies). Re-run a lightweight "status update" extraction when new messages arrive in a thread, rather than re-extracting everything.
- **Evaluation:** because ROUGE-style metrics are weak here (EmailSum), use item-level P/R/F1 against human labels plus LLM-as-judge with rubric checks.

### Gaps
- No 2025–2026 LLM-era benchmark found for email commitment and action-item extraction; searches returned only meeting-oriented or vendor content.
- No Turkish email action-item dataset found.

---

## 7. Agent orchestration, durable execution, MCP/A2A and human-in-the-loop approvals

### Takeaway
Build the core as a **deterministic workflow** (ingest → clean → triage → extract → verify → store → propose), not an autonomous agent. Use agent loops only for interactive Q&A over the knowledge base, with read-only tools.

For a .NET Windows service, **Microsoft Agent Framework (MAF) 1.0** (GA 3 Apr 2026) is the natural choice: graph workflows, checkpointing, HITL approvals, MCP, and Anthropic/OpenAI/Ollama/Foundry connectors. For Python, **LangGraph** (interrupts + SQLite checkpointer) or **Pydantic AI** (typed, provider-agnostic outputs) are the leading options.

All side-effecting actions (send reply, create task, schedule meeting) must be proposals in an approval queue with diffs. Never auto-send.

### Cited Findings
- **MAF 1.0 GA** (3 Apr 2026) is stable for .NET and Python. It includes:
  - Connectors for Microsoft Foundry, Azure OpenAI, OpenAI, **Anthropic Claude**, Amazon Bedrock, Google Gemini and **Ollama**.
  - Middleware and memory/context providers.
  - A graph-based workflow engine with **checkpointing and hydration**.
  - Streaming, **human-in-the-loop approvals** and pause/resume.
  - Declarative YAML agents and workflows, and **MCP** support.
  - Migration assistants from Semantic Kernel and AutoGen.
  - **Preview at 1.0:** DevUI, A2A 1.0 ("coming soon"), Agent Harness, Skills, and Claude Code / GitHub Copilot SDK integrations.
  - Sources: [MAF 1.0 devblog](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/); [Visual Studio Magazine](https://visualstudiomagazine.com/articles/2026/04/06/microsoft-ships-production-ready-agent-framework-1-0-for-net-and-python.aspx)
- MAF unifies Semantic Kernel and AutoGen. With 1.0, both predecessors moved to maintenance mode — [Tech Community](https://techcommunity.microsoft.com/blog/azuredevcommunityblog/the-future-of-agentic-ai-inside-microsoft-agent-framework-1-0/4510698)
- The **MAF Harness and Foundry Hosted Agents went GA in August 2026.** The harness defaults include function invocation, history persistence, context compaction, plan/execute todo lists, a **tool approval workflow** and built-in **OpenTelemetry**. Shell, file access and background sub-agents are optional (with warnings). A Claude Agent SDK connector was added — [InfoQ, Aug 2026](https://www.infoq.com/news/2026/08/agent-framework-harness-ga/)
- **LangGraph:**
  - `interrupt()` pauses a node, checkpoints the state, and resumes with `Command(resume=…)` on the same thread.
  - Persistent checkpointers (SQLite, Postgres) support fault recovery, time travel and HITL.
  - Sources: [LangChain docs – Interrupts](https://docs.langchain.com/oss/python/langgraph/interrupts); [ZenML blog](https://www.zenml.io/blog/langgraph-durable-runtime)
- **Pydantic AI** is provider-agnostic with typed `output_type` validation. The **OpenAI Agents SDK** primitives are agents, handoffs, guardrails, sessions and MCP. **Google ADK** focuses on Gemini/GCP and A2A — [Pydantic docs](https://pydantic.dev/docs/ai/comparisons/overview/); [agenticwire](https://www.agenticwire.news/article/pydantic-ai-vs-openai-agents-sdk). Caution: some secondary comparison pages (e.g., [morphllm](https://www.morphllm.com/ai-agent-framework)) give launch dates for these SDKs that look inconsistent. Verify against vendor docs before quoting dates.
- **MCP spec 2026-07-28** (latest):
  - Stateless protocol core; the `Mcp-Session-Id` header is removed.
  - Multi-round-trip requests and "authorization hardening" aligned with OAuth/OIDC.
  - A formal extensions framework.
  - **Roots, Sampling and Logging are deprecated.**
  - Sources: [MCP blog – 2026-07-28 spec](https://blog.modelcontextprotocol.io/posts/2026-07-28/); [Specification](https://modelcontextprotocol.io/specification/2026-07-28)

### Inferences
- **Recommended runtime topology (Windows):**
  - **Service A** (.NET Worker Service): Graph sync (delta queries / change notifications), storage, scheduler, approval executor. It holds the Graph tokens.
  - **Service B** (AI worker): a MAF workflow or LangGraph graph, with Foundry Local in-process or Ollama for local models and cloud clients for Tier 2. It has **no Graph write scopes**.
  - **Web UI** on https://localhost:6500: approval queue, evidence viewer, "my tasks", timelines.
  - Durable state: MAF checkpoints or LangGraph's SQLite checkpointer, plus an outbox table for idempotent execution.
- **Approval pattern:**
  1. The extractor emits an `ActionProposal {type: reply|task|meeting, payload, rationale, evidence[], risk_flags, policy_decision}`.
  2. The UI shows a rendered diff: draft body, recipients, and for meetings the time and attendees.
  3. The user can approve, edit or reject.
  4. The executor re-validates policy at execution time (recipient allow-list, no new external recipients without explicit confirmation, attachment rules) and executes with an idempotency key.
  5. Everything is audit-logged.
- **Default drafting:** create Outlook **drafts** (Graph `createReply` → draft) instead of sending. Even after approval, the user sends from Outlook, or a "send" is a second explicit click.
- **MCP exposure:** optionally expose a **read-only** local MCP server (stdio transport for Claude Desktop, VS Code, Copilot) with tools like `search_threads`, `get_item_with_evidence` and `list_my_tasks`. Do not expose write tools over MCP. Returned content is untrusted email text, so it can carry injections into the consuming agent (see Section 9). Target the 2026-07-28 spec and avoid the deprecated Sampling/Roots.
- **A2A** is not needed for a single-user local app; A2A 1.0 was still "coming soon" at MAF 1.0.

### Gaps
- Verified details on **Claude Agent SDK** and **Google ADK** 2026 versions and features were not retrieved from primary sources in this pass.
- Could not confirm whether MAF .NET checkpointing has a built-in SQLite store, or requires a custom store, on Windows.

---

## 8. Cost math: about 200 emails/day/user, cloud (batch + caching) vs local

### Takeaway
With triage filtering about half of the mail and Batch pricing, frontier-quality cloud extraction costs roughly **$1–3 per user per day**. That is about **$15–60 per user per month** on Claude Haiku 4.5 or Sonnet 5 / gpt-6-sol class models, and under **$10 per month** on mini/nano-class models. Local inference has near-zero marginal cost but needs a GPU workstation (about 40 GPU-minutes per day on an RTX 4070-class card for an 8B model). Local quality on Turkish extraction must be validated.

### Cited Findings
- **Anthropic list prices (per MTok, input / output; cache read):**

  | Model | Input | Output | Cache read | Batch input | Batch output |
  |---|---|---|---|---|---|
  | Claude Opus 5.5 | $4 | $20 | $0.20 (0.05×) | $2 | $10 |
  | Claude Sonnet 5 | $2 | $10 | $0.20 | $1 | $5 |
  | Claude Haiku 4.5 | $1 | $5 | $0.10 | $0.50 | $2.50 |

  - Sonnet 5's $2/$10 introductory price became the standard price; the planned move to $3/$15 on 1 Sep 2026 was cancelled.
  - Cache writes cost 1.25× (5-minute) or 2× (1-hour).
  - Batch is −50% on input and output. Caching and batch discounts stack.
  - US-only inference costs 1.1×.
  - Claude 4.7+ tokenizer: about 30% more tokens for the same text.
  - Source: [Claude pricing](https://platform.claude.com/docs/en/about-claude/pricing)
- **OpenAI list prices (per MTok, input / cached / output):**

  | Model | Input | Cached | Output |
  |---|---|---|---|
  | gpt-6-astra | $10 | $1 | $50 |
  | gpt-6-sol | $2 | $0.20 | $10 |
  | gpt-6-luna | $0.10 | $0.01 | $0.50 |
  | gpt-5.5 | $5 | $0.50 | $30 |
  | gpt-5.4-mini | $0.75 | $0.075 | $4.50 |
  | gpt-5.4-nano | $0.20 | $0.02 | $1.25 |
  | gpt-5-mini | $0.25 | $0.025 | $2.00 |

  - Batch and Flex are −50%.
  - Regional-processing endpoints +10% for models released on or after 5 Mar 2026.
  - Source: [OpenAI pricing](https://developers.openai.com/api/docs/pricing)
- Azure OpenAI Global Batch is 50% cheaper than Standard Global with a 24-hour turnaround. Data Zone Batch exists for EU/US — [Tech Community – Data Zones for Batch](https://techcommunity.microsoft.com/blog/azure-ai-foundry-blog/announcing-data-zones-for-azure-openai-service-batch/4366754)
- Turkish costs ×1.40–×2.21 as many tokens as English — [token-toll](https://github.com/mtalhasahin/token-toll)
- RTX 4070, 8B Q4: about 3,192 tok/s prefill, about 76 tok/s generation — [LocalScore](https://www.localscore.ai/accelerator/147)

### Inferences
- **Assumptions (mine; adjust with real telemetry):**
  - Per deeply processed email:
    - System prompt + schema + few-shot: 3,000 tokens, cached.
    - Thread state/context: 1,500 tokens.
    - New message: 1,000 tokens (already inflated for Turkish).
    - Amortized attachment text: 1,000 tokens.
    - Output JSON: 600 tokens.
  - Daily roll-ups (projects, digest, phase review): 400k input and 40k output tokens.
  - 22 working days per month.
- **Results (list price, per user):**

  | Model | All 200 emails deep, standard | 50% after local triage, standard | 50% after triage + Batch (about) |
  |---|---|---|---|
  | Claude Opus 5.5 | $7.72/day (~$170/mo) | $5.06/day (~$111/mo) | ~$2.53/day (~$56/mo) |
  | Claude Sonnet 5 / gpt-6-sol | $3.92/day (~$86/mo) | $2.56/day (~$56/mo) | ~$1.28/day (~$28/mo) |
  | Claude Haiku 4.5 | $1.96/day (~$43/mo) | $1.28/day (~$28/mo) | ~$0.64/day (~$14/mo) |
  | gpt-5.4-mini | $1.59/day (~$35/mo) | $1.03/day (~$23/mo) | ~$0.52/day (~$11/mo) |
  | gpt-5-mini | $0.61/day (~$13/mo) | $0.40/day (~$9/mo) | ~$0.20/day (~$4/mo) |
  | gpt-5.4-nano | $0.43/day (~$10/mo) | $0.28/day (~$6/mo) | ~$0.14/day (~$3/mo) |

  - Batch prompt-cache hits are best-effort, so real batch costs may land slightly higher than these figures.
  - Add +10% for EU/US regional processing where applicable.
- **Pattern:** model routing (Haiku/mini for routine threads, Sonnet/Opus for high-stakes or long threads) plus **Batch for anything not needed within minutes** plus local triage gives most of the savings. Prompt caching matters most for the fixed system/schema prefix and for repeated project-context blocks.
- **Local:** about 1.3M prefill and 120k generated tokens per day means about 40 minutes of RTX 4070 time for an 8B model. The cost is hardware (a GPU workstation, roughly 16 GB+ VRAM for 14–30B-class models) and electricity, not tokens. A 30B-A3B MoE will be slower per token than 8B dense unless it fits in VRAM (unverified).

### Gaps
- No measured token counts for real Turkish/English corporate threads. The numbers above are assumptions; instrument the pilot.
- Azure OpenAI EU Data Zone prices and per-model Batch availability in EU zones were not verified.
- Bedrock and Vertex EU regional prices for Claude were not verified; only the "+10% regional premium" rule was.

---

## 9. Security of the AI layer: indirect prompt injection via email/docs, defenses and OWASP guidance

### Takeaway
Incoming email is attacker-controlled input, and EchoLeak proved zero-click exfiltration against M365 Copilot. The architecture must assume the extraction model will sometimes be manipulated. The controls:
- **Quarantine the extractor:** no tools, schema-only output.
- **Never let untrusted content drive side effects without human approval.**
- **Block exfiltration channels** in the UI: no auto-loaded external images or links.
- **Label data flows:** FIDES/CaMeL-style information-flow control.
- **Spotlighting** as defense-in-depth.

### Cited Findings
- **EchoLeak (CVE-2025-32711, CVSS 9.3)** in M365 Copilot:
  - One crafted email caused Copilot to access internal data and exfiltrate it with no user interaction.
  - The attack chain bypassed Microsoft's XPIA classifier, evaded link redaction with **reference-style Markdown**, used **auto-fetched images**, and abused a Teams proxy allowed by CSP.
  - Patched server-side; no known in-the-wild exploitation.
  - Sources: [arXiv 2509.10540](https://arxiv.org/abs/2509.10540); [Hack The Box](https://www.hackthebox.com/blog/cve-2025-32711-echoleak-copilot-vulnerability)
- **LLMail-Inject** (Microsoft et al.):
  - Adaptive prompt-injection challenge against a simulated LLM email client.
  - 208,095 unique attack submissions from 839 participants.
  - The data and code are public. The authors advise against training directly on it because the attack goals are narrow.
  - Sources: [arXiv 2506.09956](https://arxiv.org/html/2506.09956v1); [GitHub microsoft/llmail-inject-challenge](https://github.com/microsoft/llmail-inject-challenge)
- **Spotlighting** (Hines et al.):
  - Variants: delimiting, datamarking and encoding.
  - Attack success fell from over 50% to under 2% on GPT-family models, with minimal task impact.
  - Used in production in Azure Prompt Shields.
  - Sources: [arXiv 2403.14720](https://arxiv.org/abs/2403.14720); [MSRC blog](https://www.microsoft.com/en-us/msrc/blog/2025/07/how-microsoft-defends-against-indirect-prompt-injection-attacks)
- **CaMeL** (Debenedetti et al., Google DeepMind/ETH):
  - A privileged LLM plans from the trusted query only. A quarantined LLM parses untrusted data with no tools. Capability-based control- and data-flow tracking enforces policy.
  - Solves **77% of AgentDojo tasks with provable security vs 84% undefended** (v2, June 2025).
  - A secondary source cites "67%", apparently from v1 — [arXiv 2503.18813](https://arxiv.org/abs/2503.18813); [SSOJet (secondary)](https://ssojet.com/blog/camel-a-robust-defense-against-llm-prompt-injection-attacks)
- **FIDES in Microsoft Agent Framework:**
  - Experimental since May 2026 (agent-framework 1.3.0+); **Python only**.
  - Content carries integrity labels (trusted/untrusted) and confidentiality labels (public/private/user_identity).
  - Labels propagate through tool calls, taking the most restrictive combination.
  - `store_untrusted_content()` swaps raw text for variable references, and `quarantined_llm()` processes them with a tools-free client.
  - Tools can declare `accepts_untrusted: False`.
  - Microsoft docs claim policy checks stop all AgentDojo injection attacks.
  - Sources: [MAF devblog – FIDES](https://devblogs.microsoft.com/agent-framework/fides/); [Learn – Agent Security with FIDES](https://learn.microsoft.com/en-us/agent-framework/agents/security); [MSR – Securing AI Agents with Information-Flow Control](https://www.microsoft.com/en-us/research/publication/securing-ai-agents-with-information-flow-control/)
- **OWASP Top 10 for LLM Applications 2025:**
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
  - Sources: [OWASP GenAI](https://genai.owasp.org/resource/owasp-top-10-for-llm-applications-2025/); [OWASP LLM Top 10 archive](https://genai.owasp.org/llm-top-10/)
- **OWASP Top 10 for Agentic Applications 2026** (published 9 Dec 2025):
  - ASI01 Agent Goal Hijack
  - ASI02 Tool Misuse & Exploitation
  - ASI03 Agent Identity & Privilege Abuse
  - ASI04 Agentic Supply Chain
  - ASI05 Unexpected Code Execution
  - ASI06 Memory & Context Poisoning
  - ASI07 Insecure Inter-Agent Communication
  - ASI08 Cascading Failures
  - ASI09 Human-Agent Trust Exploitation
  - ASI10 Rogue Agents
  - Sources: [OWASP GenAI – Agentic Top 10](https://genai.owasp.org/resource/owasp-top-10-for-agentic-applications-for-2026/); [Palo Alto Networks](https://www.paloaltonetworks.com/blog/cloud-security/owasp-agentic-ai-security/)

### Inferences (concrete controls for this app)
1. **Quarantined extractor (CaMeL/FIDES pattern).**
   - LLM calls that read email or document content get **no tools** and must return schema-validated JSON.
   - Free-text fields are length-capped and rendered as plain text, never Markdown or HTML.
   - The "planner" side (the Q&A agent) sees only structured fields or variable handles, not raw bodies, where feasible.
2. **No autonomous side effects** (LLM06 / ASI02 / ASI09).
   - Every send, task creation or meeting request is a proposal needing approval.
   - Policy is re-checked at execution: recipients must already be thread participants or the directory, external domains are flagged, no attachments are added by the AI, and "send" is disabled by default in favour of Outlook drafts.
3. **Close exfiltration channels in the web UI** (EchoLeak lessons).
   - Render AI output as sanitized text.
   - Block external image loads and remote fonts.
   - Strict CSP (`default-src 'self'`; no third-party `img-src`).
   - Do not auto-linkify URLs from AI output; show the domain and require a click-through.
   - Strip reference-style Markdown.
4. **Spotlighting / datamarking** for every untrusted block (e.g., wrap in `<email id=… trust="untrusted">` with interleaved markers or base64 in prompts), plus a system rule that instructions inside data are content to summarize, not commands. Record "possible injection" as a risk item when detected.
5. **Least-privilege Graph scopes.**
   - The AI worker gets only read scopes such as Mail.Read.
   - A separate executor holds Mail.ReadWrite / Calendars.ReadWrite / Tasks.ReadWrite and runs only approved proposals.
   - Store tokens with DPAPI / Windows Credential Manager, not in the AI worker.
6. **Memory poisoning (ASI06).** Facts in the knowledge base carry provenance (sender, internal vs external domain). External-origin claims can never silently overwrite internal decisions, and conflicting facts are shown with both sources.
7. **Red-team with LLMail-Inject samples** plus Turkish-translated injections in CI; track attack success rate per release.
8. **Local MCP server exposure** must be read-only and should mark untrusted content (spotlighting). A connected desktop agent with its own tools (browser, email send) plus our untrusted content is a data-exfiltration combination.

### Gaps
- No public evaluation found of spotlighting or Prompt Shields effectiveness on **Turkish-language** injections.
- Not verified whether FIDES's .NET implementation has shipped; as of the May 2026 blog it was Python-only.

---

## 10. Evaluation and observability

### Takeaway
Build a **golden dataset from real, consented mail**: about 200–500 threads, TR/EN, with attachments, labeled for items, owners, dates, evidence spans, project and phase. Score:
- field-level extraction P/R/F1 (ExtractBench-style),
- citation precision/recall (ALCE-style, with exact-match plus NLI),
- abstention quality,
- injection attack success rate.

Use LLM-as-judge only as a complement, since ROUGE-style metrics are weak for email. For observability, emit **OpenTelemetry GenAI** spans. Note that these conventions are still "Development" status. Self-hosted Langfuse is heavy (Postgres + ClickHouse + Redis + S3), so a local Windows install needs something lighter or an opt-in central collector.

### Cited Findings
- ExtractBench uses field-level scoring per data type (exact, tolerance, semantic) and distinguishes omissions from hallucinations — [arXiv 2602.12247](https://arxiv.org/abs/2602.12247)
- EmailSum: ROUGE and BERTScore correlate weakly with human judgments for email thread summaries — [ACL Anthology](https://aclanthology.org/2021.acl-long.537/)
- ALCE defines citation recall and precision via NLI — [arXiv 2305.14627](https://arxiv.org/abs/2305.14627)
- MiniCheck gives cheap grounding checks (770M/7B) at about GPT-4 level on LLM-AggreFact — [arXiv 2404.10774](https://arxiv.org/abs/2404.10774)
- **OpenTelemetry GenAI semantic conventions:**
  - The agent span tree is `invoke_agent` → `chat` → `execute_tool`, and `gen_ai.operation.name` also covers `invoke_workflow`, `retrieval`, `plan` and memory operations.
  - As of mid-July 2026, **all `gen_ai.*` conventions are still "Development" (not Stable)**, per a secondary source.
  - Sources: [OTel semconv – gen-ai spans](https://github.com/open-telemetry/semantic-conventions/blob/main/docs/gen-ai/gen-ai-spans.md); [DEV – not stable yet](https://dev.to/azena-ai/opentelemetrys-genai-semantic-conventions-are-not-stable-yet-heres-what-actually-shipped-in-2026-3mke); [Greptime](https://greptime.com/blogs/2026-05-09-opentelemetry-genai-semantic-conventions)
- **Langfuse self-hosting:**
  - Requires Postgres, ClickHouse, Redis/Valkey and S3/Blob.
  - v4 needs ClickHouse ≥ 25.12 and PostgreSQL ≥ 15.
  - OTLP ingest at `/api/public/otel/v1/traces`.
  - Sources: [Langfuse self-hosting](https://langfuse.com/self-hosting); [Langfuse ClickHouse](https://langfuse.com/self-hosting/deployment/infrastructure/clickhouse); [Railway Langfuse v4](https://railway.com/deploy/langfuse-v4--langfuse-observability)
- MAF has built-in OpenTelemetry (the harness has OTel by default), and DevUI (preview) is a local debugger — [InfoQ](https://www.infoq.com/news/2026/08/agent-framework-harness-ga/); [MAF 1.0 devblog](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-version-1-0/)

### Inferences
- **Metrics to track per release:**
  - Item-level P/R/F1 by type (decision/risk/commitment/action/question), with matching by type + owner + normalized object + due date (± tolerance).
  - Owner accuracy and due-date exact/±1-day accuracy.
  - Citation precision: the share of evidence quotes that exist and support the claim. Citation recall: the share of claims fully supported.
  - Abstention precision: when the model says "unknown", was it truly unknown?
  - Project-assignment accuracy and phase accuracy.
  - "My tasks" precision (user-facing trust hinges on this).
  - Injection attack success rate.
  - Cost and latency per email.
- **Human feedback loop:** approve/edit/reject in the UI is free labeling. Log edits as diffs to grow the golden set, and to support future LoRA fine-tuning of a local model (see Section 5).
- **LLM-as-judge:** use a different model family than the extractor, with rubric-based pairwise or pointwise checks, and calibrate against a human-labeled subset (target κ ≥ 0.6, my suggestion) before trusting it.
- **Local observability:**
  - Emit OTel GenAI spans to a local file or SQLite exporter by default, with prompts and content redacted.
  - Allow an opt-in OTLP export to a central Langfuse or Phoenix run by IT for multi-user deployments.
  - Do not ship Langfuse's ClickHouse stack inside every desktop install.

### Gaps
- Could not verify in this pass (the search budget was exhausted) Arize Phoenix's current lightweight local mode (single process / SQLite), which would be a lighter alternative to Langfuse for per-PC use.
- No public Turkish email golden datasets exist, to my search; one must be built internally with KVKK/GDPR-compliant consent and redaction.
