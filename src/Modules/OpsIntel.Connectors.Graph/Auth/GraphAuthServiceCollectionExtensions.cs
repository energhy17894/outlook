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

        // RedirectUri left empty (the default) derives from Host's actual Kestrel port rather
        // than hardcoding it, and always as https:// — Host never listens on http (ADR-0003).
        services.PostConfigure<GraphAuthOptions>(options =>
        {
            var port = configuration.GetValue("OpsIntel:Kestrel:Port", 6500);
            options.RedirectUri = GraphAuthRedirectUriResolver.Resolve(options.RedirectUri, port);
        });

        // No ClientId configured yet (no tenant/app registration provisioned): register a
        // no-op auth service instead of a real MSAL pipeline, so Host still starts and serves
        // everything except sign-in (which reports 503, not a crash — see
        // NotConfiguredGraphAuthService). Building PublicClientApplicationBuilder.Create("")
        // throws, so this check must happen before that call, not inside it.
        var clientId = configuration[$"{GraphAuthOptions.ConfigurationSection}:ClientId"];
        if (string.IsNullOrWhiteSpace(clientId))
        {
            services.AddSingleton<IGraphAuthService, NotConfiguredGraphAuthService>();
            return services;
        }

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
