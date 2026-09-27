using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpsIntel.AI.Extraction.Hosting;
using OpsIntel.AI.Extraction.WorkItems;

namespace OpsIntel.AI.Extraction;

/// <summary>DI wiring for the AI extraction pipeline, to be called from <c>OpsIntel.Intelligence</c>'s host builder.</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ExtractionOptions"/> (bound from <c>OpsIntel:AI:Extraction</c>),
    /// the model-hosting factories, the resolved <see cref="IChatClient"/> singleton, and
    /// <see cref="IExtractor"/>. Registering this holds no Microsoft Graph tokens and grants
    /// the extractor no tools (ADR-0007/0008/0013/0019) — it only ever sees content the Host
    /// hands it via the job queue.
    /// </summary>
    public static IServiceCollection AddOpsIntelAiExtraction(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddOptions<ExtractionOptions>()
            .Bind(configuration.GetSection(ExtractionOptions.SectionName));

        services.TryAddSingleton<FoundryLocalChatClientFactory>();
        services.TryAddSingleton<OpenAiCompatibleChatClientFactory>();
        services.TryAddSingleton<ExtractionChatClientFactory>();

        services.TryAddSingleton<IChatClient>(sp =>
        {
            var factory = sp.GetRequiredService<ExtractionChatClientFactory>();
            // The DI container builds singletons synchronously; Foundry Local's own
            // CreateAsync/DownloadAsync/LoadAsync calls are genuinely async (model
            // download/load), so this blocks once at first resolution rather than
            // threading async-init through the whole host. Acceptable for a background
            // worker service with no request-per-thread pressure at startup.
            return factory.CreateAsync().GetAwaiter().GetResult();
        });

        services.TryAddSingleton<IExtractor, WorkItemExtractor>();

        return services;
    }
}
