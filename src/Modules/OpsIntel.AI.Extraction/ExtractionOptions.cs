namespace OpsIntel.AI.Extraction;

/// <summary>
/// Which chat client backend the extraction pipeline should resolve
/// (<see cref="Hosting.ExtractionChatClientFactory"/>). Kept as a plain enum (rather than a
/// compile-time feature) so it can be flipped per-machine at runtime once the model-hosting
/// spike's hardware/OS findings are known (see SPIKE-FOUNDRY-LOCAL.md).
/// </summary>
public enum ModelHostingMode
{
    /// <summary>In-process Foundry Local via the <c>Microsoft.AI.Foundry.Local</c> C# SDK.</summary>
    FoundryLocalInProcess,

    /// <summary>An OpenAI-compatible HTTP endpoint (Foundry Local's own optional REST endpoint, or Ollama).</summary>
    OpenAiCompatibleEndpoint,
}

/// <summary>Configuration for the extraction pipeline: prompt file locations and model hosting.</summary>
public sealed class ExtractionOptions
{
    public const string SectionName = "OpsIntel:AI:Extraction";

    /// <summary>
    /// Root directory containing the versioned prompt set (<c>schemas/</c>, <c>templates/</c>,
    /// <c>_shared/spotlighting.md</c>) — defaults to the repo-relative
    /// <c>prompts/extraction/v1</c> used at build/dev time; production deploys point this at
    /// wherever the prompt set is installed.
    /// </summary>
    public string PromptsRoot { get; set; } = "prompts/extraction/v1";

    /// <summary>Recorded on every extraction run for idempotency-key / audit purposes (ADR-0015).</summary>
    public string PromptVersion { get; set; } = "extraction/v1";

    public ModelHostingMode ModelHosting { get; set; } = ModelHostingMode.OpenAiCompatibleEndpoint;

    /// <summary>Base URL of an OpenAI-compatible endpoint (Foundry Local's own REST endpoint, or Ollama's <c>http://localhost:11434/v1</c>).</summary>
    public string OpenAiCompatibleBaseUrl { get; set; } = "http://localhost:11434/v1";

    /// <summary>API key for the OpenAI-compatible endpoint. Local servers usually accept any non-empty placeholder value.</summary>
    public string OpenAiCompatibleApiKey { get; set; } = "local";

    /// <summary>Model id/alias to request from the OpenAI-compatible endpoint.</summary>
    public string OpenAiCompatibleModelId { get; set; } = "qwen3.5:9b";

    /// <summary>Foundry Local model alias to download/load (in-process mode).</summary>
    public string FoundryLocalModelAlias { get; set; } = "qwen2.5-7b-instruct-generic-cpu";

    /// <summary>Application name passed to <c>Foundry Local</c>'s <c>Configuration.AppName</c> (its cache/log directory naming).</summary>
    public string FoundryLocalAppName { get; set; } = "OpsIntel";
}
