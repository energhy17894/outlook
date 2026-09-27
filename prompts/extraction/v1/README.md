# Extraction prompts & schemas — v1

Faz 0 deliverable. Source: research report §1 ("Yapay zekâ katmanı"), §3 (evidence data
model), §4 (AI threats); AI architecture notes §2-3, §9; ADR-0015, ADR-0019.

## Layout

```text
v1/
├── schemas/
│   ├── work_items.schema.json          # task / commitment / request / follow_up / open_question
│   ├── decisions.schema.json
│   ├── risks.schema.json
│   ├── project_assignment.schema.json
│   └── phase_signal.schema.json
├── templates/
│   ├── work_items.md                   # system + user prompt template (Markdown)
│   ├── decisions.md
│   ├── risks.md
│   ├── project_assignment.md
│   └── phase_signal.md
└── _shared/
    └── spotlighting.md                 # delimiting + datamarking convention, read by every template
```

Each schema is its own small, flat call per ADR-0015 (ExtractBench: large/nested schemas
collapse to 0% valid output on realistic field counts). `work_items` folds
task/commitment/request/follow_up/open_question into one schema (a slightly wider split
than the ADR's 3-call grouping) because they share the same shape (owner, counterparty,
due date, evidence) — it is still flat, still has one `enum` discriminator, and stays well
under the field counts ExtractBench flags as unreliable. If the Faz 0 eval run shows this
hurts validity versus splitting further, split `work_items` again; nothing else changes.

## Non-negotiable contract (every schema)

- Every item requires `evidence: [{message_id, quote}]`, 1-3 verbatim quotes.
- Every item carries `confidence` (`low`/`med`/`high`) and `needs_review` (bool).
- Ambiguous fields (owner, due date, decider, phase) are `null` plus an `abstain_reason`
  enum value — the model is told explicitly never to guess a confident-looking answer over
  admitting it doesn't know (research report §3, ADR-0015 "doğrulanamayan öğe asla 'gerçek'
  olarak gösterilmez").
- `additionalProperties: false` everywhere, so a provider's strict/structured-output mode
  can enforce the contract, and so an injected instruction cannot smuggle extra fields.
- No `$ref`/recursive schemas, no numeric/string length constraints beyond `minLength=0`
  implicit and `maxLength`/`minItems`/`maxItems` — kept inside the documented Claude
  structured-outputs restriction list (AI notes §3) so the same schema works unmodified
  against Claude, and is a plain JSON Schema so it also works as an Ollama/`response_format`
  schema or a Pydantic model for a local Foundry Local model.

## Quote verification is local, not provider-side

Per ADR-0015, `evidence[].quote` is checked deterministically after the call returns:
Turkish-aware normalization (NFC, tr-TR casefold for İ/ı/I/i, whitespace/quote-mark
folding) then an exact substring match against the message's cleaned text, producing char
offsets for the `Evidence` record's `TextPositionSelector`. See
`tests/eval/opsintel_eval/verify_quotes.py` for the reference implementation and its unit
tests — the production .NET implementation must match its normalization behaviour
(same test vectors should be ported once `src/` exists).

## Prompt versioning

- The prompt version string is the path fragment after `extraction/`, e.g.
  `extraction/v1/work_items`. It is one of the two inputs to the pipeline's idempotency key
  (`content_sha256 + prompt_version`) and is recorded on every `ExtractionRun`.
- Changing a system prompt, a user template, or a schema is a **new version directory**
  (`v2/`, ...), never an in-place edit of `v1/` once it has been used to produce any stored
  `ExtractionRun` — old runs must remain reproducible/attributable.
- Any prompt change requires re-running `tests/eval/` (metrics + the injection corpus)
  before merge, per `prompts/README.md`.

## What's still open (tracked as TODO, resolved after Faz 0 spike E)

- Whether `work_items` should be split further once real Turkish/English throughput and
  validity numbers come back from the discovery-phase model comparison (Qwen3.5-9B /
  Gemma 4 12B / Qwen3-30B-A3B / cloud baseline).
- The exact runtime templating engine (the `{{...}}` placeholders above are illustrative;
  `src/` will decide between a plain string builder and a templating library — this is an
  `OpsIntel.Intelligence` implementation detail, out of scope here).
- Wiring these templates to Anthropic's Citations API for the read-only Q&A agent (not
  the extraction calls, which cannot mix Citations with structured outputs) is Faz 2+.
