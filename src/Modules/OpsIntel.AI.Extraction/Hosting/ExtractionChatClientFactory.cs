using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace OpsIntel.AI.Extraction.Hosting;

/// <summary>
/// Resolves the <see cref="IChatClient"/> the extraction pipeline uses, per
/// <see cref="ExtractionOptions.ModelHosting"/> — the runtime feature flag that lets a
/// deployment pick in-process Foundry Local vs. any OpenAI-compatible endpoint without a
/// recompile (SPIKE-FOUNDRY-LOCAL.md).
/// </summary>
public sealed class ExtractionChatClientFactory
{
    private readonly ExtractionOptions _options;
    private readonly FoundryLocalChatClientFactory _foundryLocalFactory;
    private readonly OpenAiCompatibleChatClientFactory _openAiCompatibleFactory;

    public ExtractionChatClientFactory(
        IOptions<ExtractionOptions> options,
        FoundryLocalChatClientFactory foundryLocalFactory,
        OpenAiCompatibleChatClientFactory openAiCompatibleFactory)
    {
        _options = options.Value;
        _foundryLocalFactory = foundryLocalFactory;
        _openAiCompatibleFactory = openAiCompatibleFactory;
    }

    public Task<IChatClient> CreateAsync(CancellationToken cancellationToken = default)
    {
        return _options.ModelHosting switch
        {
            ModelHostingMode.FoundryLocalInProcess => _foundryLocalFactory.CreateAsync(cancellationToken),
            ModelHostingMode.OpenAiCompatibleEndpoint => Task.FromResult(_openAiCompatibleFactory.Create()),
            _ => throw new NotSupportedException($"Unsupported model hosting mode: {_options.ModelHosting}"),
        };
    }
}
