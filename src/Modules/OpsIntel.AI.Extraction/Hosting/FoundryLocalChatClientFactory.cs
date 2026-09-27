using System.ClientModel;
using Microsoft.AI.Foundry.Local;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace OpsIntel.AI.Extraction.Hosting;

/// <summary>
/// Builds an in-process <see cref="IChatClient"/> backed by Foundry Local via the GA
/// <c>Microsoft.AI.Foundry.Local</c> C# SDK (see SPIKE-FOUNDRY-LOCAL.md for the full spike
/// write-up). The SDK's own chat client type
/// (<see cref="Microsoft.AI.Foundry.Local.OpenAIChatClient"/>) speaks the Betalgo Ranul OpenAI
/// object model, not <c>Microsoft.Extensions.AI.IChatClient</c> directly, so this factory
/// takes the path the SDK itself documents for exactly this situation: start its optional
/// in-process OpenAI-compatible REST endpoint (<c>FoundryLocalManager.StartWebServiceAsync</c>)
/// and point the official <c>Microsoft.Extensions.AI.OpenAI</c> client at it — the same
/// bridge <see cref="OpenAiCompatibleChatClientFactory"/> uses for a standalone Ollama/Foundry
/// Local service, so both hosting modes end up producing an ordinary <see cref="IChatClient"/>
/// with no code-path difference for the extractor.
/// </summary>
public sealed class FoundryLocalChatClientFactory
{
    private readonly ExtractionOptions _options;
    private readonly ILogger<FoundryLocalChatClientFactory>? _logger;

    public FoundryLocalChatClientFactory(IOptions<ExtractionOptions> options, ILogger<FoundryLocalChatClientFactory>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
        _logger = logger;
    }

    /// <summary>
    /// Initializes the Foundry Local manager (idempotent per process — <c>FoundryLocalManager</c>
    /// is an async singleton), ensures the configured model alias is downloaded and loaded,
    /// starts the in-process web service, and returns an <see cref="IChatClient"/> pointed at
    /// it. Callers own disposing the returned client's underlying HTTP resources as usual;
    /// <see cref="FoundryLocalManager.Instance"/> itself should be disposed once at host
    /// shutdown (<see cref="FoundryLocalManager.Dispose"/>), not per call.
    /// </summary>
    public async Task<IChatClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        if (!FoundryLocalManager.IsInitialized)
        {
            await FoundryLocalManager.CreateAsync(
                new Configuration
                {
                    AppName = _options.FoundryLocalAppName,
                    Web = new Configuration.WebService { Urls = "127.0.0.1:0" },
                },
                NullLogger.Instance,
                cancellationToken).ConfigureAwait(false);
        }

        var manager = FoundryLocalManager.Instance;

        var catalog = await manager.GetCatalogAsync(cancellationToken).ConfigureAwait(false);
        var model = await catalog.GetModelAsync(_options.FoundryLocalModelAlias, cancellationToken).ConfigureAwait(false)
            ?? throw new InvalidOperationException(
                $"Foundry Local catalog has no model matching alias '{_options.FoundryLocalModelAlias}'.");

        if (!await model.IsCachedAsync(cancellationToken).ConfigureAwait(false))
        {
            _logger?.LogInformation("Downloading Foundry Local model '{Alias}'.", _options.FoundryLocalModelAlias);
            await model.DownloadAsync(progress => _logger?.LogDebug("Model download {Percent:F1}%", progress), cancellationToken).ConfigureAwait(false);
        }

        if (!await model.IsLoadedAsync(cancellationToken).ConfigureAwait(false))
        {
            await model.LoadAsync(cancellationToken).ConfigureAwait(false);
        }

        await manager.StartWebServiceAsync(cancellationToken).ConfigureAwait(false);

        var baseUrl = manager.Urls?.FirstOrDefault()
            ?? throw new InvalidOperationException("Foundry Local web service did not report a listening URL.");

        var clientOptions = new OpenAIClientOptions { Endpoint = new Uri(baseUrl) };
        var chatClient = new ChatClient(model.Id, new ApiKeyCredential("local"), clientOptions);
        return chatClient.AsIChatClient();
    }
}
