using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Identity.Client;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Connectors.Graph.Auth;

public static class GraphAuthServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Entra public-client + PKCE BFF auth pipeline (ADR-0007): a singleton
    /// <see cref="IPublicClientApplication"/> whose token cache is DPAPI-backed via
    /// <see cref="ISecretStore"/>, and <see cref="IGraphAuthService"/> for Host's
    /// <c>/auth/*</c> endpoints. Requires an <see cref="ISecretStore"/> to already be
    /// registered (Host wires <c>DpapiSecretStore</c>).
    /// </summary>
    public static IServiceCollection AddOpsIntelGraphAuth(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<GraphAuthOptions>(configuration.GetSection(GraphAuthOptions.ConfigurationSection));

        services.AddSingleton<IPublicClientApplication>(sp =>
        {
            var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<GraphAuthOptions>>().Value;
            var secretStore = sp.GetRequiredService<ISecretStore>();

            var app = PublicClientApplicationBuilder.Create(options.ClientId)
                .WithAuthority(options.Authority)
                .WithRedirectUri(options.RedirectUri)
                // ADR-0007: CAE-aware, so a mid-life revocation/claims-challenge surfaces as a
                // clean re-auth prompt instead of an opaque 401 from Graph.
                .WithClientCapabilities(["cp1"])
                .Build();

            var tokenCache = new SecretStoreTokenCache(secretStore, options.TokenCacheSecretKey);
            tokenCache.Attach(app.UserTokenCache);

            return app;
        });
        services.AddSingleton<IGraphAuthService, GraphAuthService>();

        return services;
    }
}
