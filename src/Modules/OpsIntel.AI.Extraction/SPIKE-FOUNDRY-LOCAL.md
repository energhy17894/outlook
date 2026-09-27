# Spike: Foundry Local .NET SDK (Faz 0 spike C, ADR-0014)

Date: 2026-09-27. Environment probed: this repo's Linux dev container (`dotnet --version` =
10.0.401), against `nuget.org` as of today. Production target per ADR-0014 is Windows, so
the Windows-specific claims below (WinML EP acceleration, `NT SERVICE` account behaviour,
VC++ runtime) are carried over from `docs/research/notes/ai_agent_mimarisi.md` §1/§8 and
**not** independently re-verified in this Linux sandbox — flagged explicitly below.

## Package identity (as of 2026-09-27)

- **`Microsoft.AI.Foundry.Local`** (not `Microsoft.Extensions.AI.Foundry.Local` — no such
  package exists), current stable version **2.0.1**, published 2026-08-31, MIT-licensed,
  `verified: true` publisher, 360k+ total downloads. Targets `net8.0`, `net9.0` and
  `netstandard2.0` (no `net10.0`-specific asset yet; a `net10.0` app resolves the `net9.0`
  asset via the standard TFM fallback — confirmed by restoring/building this spike against
  `net10.0` with no warnings).
- It depends on **`Microsoft.AI.Foundry.Local.Runtime` 2.0.1** (the native binary package)
  plus `Betalgo.Ranul.OpenAI` 9.1.0 (an OpenAI-object-model library the SDK's own chat/audio
  clients are shaped around) and `Microsoft.Extensions.Logging`.

## Finding 1 — the package restores and its native runtime resolves on Linux

This directly updates the open question the AI notes and ADR-0014 both flag ("Foundry
Local'ın ... servis içinde çalışması ... doğrulanmadı"): **`Microsoft.AI.Foundry.Local.Runtime`
2.0.1 ships `runtimes/linux-x64/native/libfoundry_local.so` and
`runtimes/linux-arm64/native/libfoundry_local.so`** (verified by unzipping the `.nupkg` from
nuget.org directly), alongside `osx-arm64` and `win-x64`/`win-arm64`. The runtime package's
own description says so explicitly: *"Contains platform-specific foundry_local binaries for
Windows (x64, ARM64), Linux (x64, ARM64 CPU-only), and macOS (ARM64)."*

`dotnet restore -r linux-x64` and `dotnet build` against a `net10.0` project referencing
`Microsoft.AI.Foundry.Local` 2.0.1 both succeed in this container with no manual NuGet
source beyond the default `nuget.org` feed. This spike did **not** go on to actually load a
model and run inference on Linux (no GPU, and the CPU-only Linux path would need a real
model download, which this sandboxed environment cannot do against the Foundry catalog CDN
in the time available) — so "restores and links" is verified; "runs a model end-to-end on
Linux" is not.

**Conclusion: the package is not Windows-only.** Per the task's instruction ("implement
`FoundryLocalChatClientFactory` behind a feature flag if the package restores on Linux"),
it is wired in directly (see `Hosting/FoundryLocalChatClientFactory.cs`), not gated behind a
`#if WINDOWS` compile-time condition — the runtime choice is a `ModelHostingMode` value on
`ExtractionOptions`, switchable per deployment without a rebuild.

## Finding 2 — in-process, not a service (matches ADR-0014/AI notes)

The SDK's own `README.md` (shipped inside the `.nupkg`) confirms the AI notes' finding: it
is a singleton (`FoundryLocalManager.CreateAsync`/`.Instance`) loaded into your process —
"no cloud required", nothing to install separately. There is **no separate CLI/service
process this SDK shells out to**; `foundry_local` is a native library (`.so`/`.dll`)
P/Invoked directly by the C# wrapper (`Detail.Native.*` types in the assembly, confirmed by
reflecting the shipped DLL). This matches ADR-0014's "süreç içi Foundry Local" decision and
supersedes the preview-era "Windows Service started with `foundry service start`" behaviour
the AI notes flagged as outdated.

## Finding 3 — the SDK's chat client does not implement `Microsoft.Extensions.AI.IChatClient` directly

`Model.GetChatClientAsync()` returns `Microsoft.AI.Foundry.Local.OpenAIChatClient`, whose
`CompleteChatAsync`/`CompleteChatStreamingAsync` methods take/return **Betalgo Ranul OpenAI**
object-model types (`Betalgo.Ranul.OpenAI.ObjectModels.*`), not `Microsoft.Extensions.AI`
types. There is no `AsIChatClient()` extension for it in this SDK version. Writing a
hand-rolled `IChatClient` adapter over the Betalgo types was one option; this spike instead
uses the path the SDK's own README documents for exactly this situation:

> **Optional web service** — start an OpenAI-compatible REST endpoint
> (`/v1/chat_completions`, `/v1/models`)

i.e. `FoundryLocalManager.StartWebServiceAsync()` + `FoundryLocalManager.Instance.Urls`, then
point the **official** `Microsoft.Extensions.AI.OpenAI` package's
`OpenAI.Chat.ChatClient` (from the `OpenAI` NuGet package, via
`OpenAIClientOptions.Endpoint`) at that local URL, and call `.AsIChatClient()` on it (a real
extension method confirmed to exist on `Microsoft.Extensions.AI.OpenAIClientExtensions` in
`Microsoft.Extensions.AI.OpenAI` 10.10.1). This gives a bona fide `IChatClient` with zero
custom adapter code, and — importantly — **the exact same code path** then also serves the
"OpenAI-compatible endpoint" fallback mode (Ollama, or a standalone Foundry Local web
service) via `OpenAiCompatibleChatClientFactory`. `ExtractionChatClientFactory` is the single
switch point between the two.

## In-process vs. service

Confirmed (Finding 2): in-process by default, GA architecture. The optional web service is
just a REST *front door* onto the same in-process instance — starting it does not spawn a
second process. For this codebase, using that front door with the standard OpenAI client
library is what turns the SDK's Betalgo-shaped chat client into an ordinary
`Microsoft.Extensions.AI.IChatClient`, which is worth the (loopback-only) extra HTTP hop.

## Model download

- `catalog.GetModelAsync(alias)` looks up a model by alias in the Foundry catalog;
  `model.DownloadAsync(progressCallback)` fetches it into the local cache
  (`Configuration.ModelCacheDir`, default `~/.{AppName}/cache/models`); `model.LoadAsync()`
  loads a cached model into memory. `model.IsCachedAsync()`/`IsLoadedAsync()` let a caller
  skip redundant downloads/loads (used in `FoundryLocalChatClientFactory`).
- ADR-0014's existing decision to pin `models/model-manifest.json` (model ID + SHA-256) at
  the `OpsIntel.Intelligence` deployment level is unaffected by this SDK: the SDK manages its
  own cache directory and does not itself expose a manifest-pinning API in this version, so
  the manifest/pinning stays an `OpsIntel.Intelligence`-level concern layered on top (out of
  scope for this Faz 0 module — tracked as a TODO below).

## Hardware / execution providers

- Confirmed from the README: EP (execution provider) download/registration is explicit via
  `DiscoverEps()` / `DownloadAndRegisterEpsAsync(...)`, decoupled from catalog access (a
  change from earlier previews where catalog calls blocked on EP downloads).
- **Not verified in this Linux sandbox**: WinML-based GPU/NPU acceleration is a Windows-only
  path per the AI notes (§1); the Linux/macOS runtime builds are explicitly **CPU-only** per
  the runtime package's own description quoted in Finding 1. This matches the AI notes'
  existing guidance that CPU-only machines should stay on small (4-9B) models or Tier 1
  triage rather than a 30B-class strict-local model.

## VC++ runtime question (ADR-0014 open item)

**Not resolved by this spike.** The native Windows binaries
(`runtimes/win-x64/native/foundry_local.dll`, `Microsoft.Windows.AI.MachineLearning.dll`)
were inspected only by listing the `.nupkg` contents in this Linux container; whether they
require the Visual C++ Redistributable to be present on a bare Windows Server 2025 install
(relevant to ADR-0005's single-MSI goal) can only be answered by actually installing and
running the SDK on a real or virtualized Windows host, which this sandbox cannot do. This
remains an open TODO for whoever runs the actual Windows-hosted spike C.

## Recommendation

1. Keep `ModelHostingMode.OpenAiCompatibleEndpoint` (pointed at a standalone Ollama or a
   manually-started Foundry Local web service) as the **default** for Faz 0 dev/CI, since it
   has zero native/binary dependency and behaves identically on Linux and Windows.
2. Flip `ModelHostingMode.FoundryLocalInProcess` on for the real Windows pilot once someone
   can verify Finding 1's Windows counterpart (native DLL loads under the service account,
   VC++ question) and run an actual model through it — the C# code path itself does not need
   to change; only the config flag does.
3. Do **not** invest in a custom `IChatClient` adapter over the SDK's own
   `OpenAIChatClient`/Betalgo types — the web-service bridge is simpler, already exercises
   the SDK's documented public surface, and de-risks a future SDK version changing its
   internal object model out from under a hand-rolled adapter.

## TODOs (tracked, not blocking Faz 0)

- Run the actual Windows spike C (service-account load, VC++, model cache path under
  `NT SERVICE\OpsIntel.Intelligence`).
- Wire `models/model-manifest.json` (model id + SHA-256 pinning) into
  `FoundryLocalChatClientFactory` once that manifest format lands elsewhere in the repo.
- Re-run this spike once `Microsoft.AI.Foundry.Local` ships a `net10.0`-specific asset (it
  currently resolves via the `net9.0` TFM fallback, which is fine functionally but worth
  revisiting for trimming/AOT compatibility later).
