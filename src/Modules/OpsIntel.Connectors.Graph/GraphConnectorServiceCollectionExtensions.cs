using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.Graph;
using Microsoft.Kiota.Abstractions.Authentication;
using OpsIntel.Connectors.Graph.Auth;
using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Connectors.Graph.Mail;

namespace OpsIntel.Connectors.Graph;

/// <summary>Wires the Graph connector module (auth, HTTP pipeline, mail feed, draft writer) into DI.</summary>
public static class GraphConnectorServiceCollectionExtensions
{
    /// <summary>Name of the <see cref="HttpClient"/> registered for all Graph calls in this module.</summary>
    public const string HttpClientName = "OpsIntel.Graph";

    /// <summary>
    /// Registers <see cref="AddOpsIntelGraphAuth"/> plus the Graph HTTP pipeline
    /// (<see cref="ImmutableIdHandler"/> -&gt; <see cref="MailboxConcurrencyHandler"/> -&gt;
    /// <see cref="RetryAfterHandler"/>), a <see cref="GraphServiceClient"/>,
    /// <see cref="GraphMailChangeFeed"/> and <see cref="DraftWriter"/>.
    /// </summary>
    public static IServiceCollection AddOpsIntelGraphConnector(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOpsIntelGraphAuth(configuration);
        services.Configure<GraphConnectorOptions>(configuration.GetSection(GraphConnectorOptions.ConfigurationSection));

        services.AddSingleton<MailboxConcurrencyLimiter>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;
            return new MailboxConcurrencyLimiter(options.MaxConcurrentRequestsPerMailbox);
        });

        services.AddTransient<ImmutableIdHandler>();
        services.AddTransient<MailboxConcurrencyHandler>();
        services.AddTransient(sp =>
        {
            var options = sp.GetRequiredService<IOptions<GraphConnectorOptions>>().Value;
            return new RetryAfterHandler(options.MaxThrottledRetries);
        });

        services.AddSingleton<IAccessTokenProvider, GraphAccessTokenProvider>();
        services.AddSingleton<IAuthenticationProvider>(sp =>
            new BaseBearerTokenAuthenticationProvider(sp.GetRequiredService<IAccessTokenProvider>()));

        services
            .AddHttpClient(HttpClientName)
            // Outermost first: the concurrency gate must be held across a request's retries, so
            // RetryAfterHandler sits innermost, closest to the network (see its own doc comment).
            .AddHttpMessageHandler<ImmutableIdHandler>()
            .AddHttpMessageHandler<MailboxConcurrencyHandler>()
            .AddHttpMessageHandler<RetryAfterHandler>();

        services.AddSingleton<GraphServiceClient>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            var authProvider = sp.GetRequiredService<IAuthenticationProvider>();
            return new GraphServiceClient(httpClient, authProvider);
        });

        services.AddSingleton<GraphMailChangeFeed>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new GraphMailChangeFeed(httpClient);
        });

        services.AddSingleton<DraftWriter>(sp =>
        {
            var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(HttpClientName);
            return new DraftWriter(httpClient);
        });

        return services;
    }
}
