using OpsIntel.Connectors.Graph.Auth;

namespace OpsIntel.Host.Auth;

/// <summary>
/// Host-side BFF endpoints for the Entra public-client + PKCE sign-in (ADR-0007). All MSAL/PKCE
/// mechanics live in <c>OpsIntel.Connectors.Graph</c>'s <see cref="IGraphAuthService"/>; these
/// endpoints only translate HTTP &lt;-&gt; that service and hold the CSRF-style <c>state</c>
/// cookie a browser round-trip needs.
/// </summary>
public static class AuthEndpoints
{
    private const string StateCookieName = "opsintel-auth-state";

    public static IEndpointRouteBuilder MapOpsIntelAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/auth/login", HandleLoginAsync);
        endpoints.MapGet("/auth/callback", HandleCallbackAsync);
        endpoints.MapPost("/auth/logout", HandleLogoutAsync);
        endpoints.MapGet("/auth/me", HandleMeAsync);

        return endpoints;
    }

    private static async Task<IResult> HandleLoginAsync(
        HttpContext context,
        IGraphAuthService authService,
        CancellationToken cancellationToken)
    {
        var state = Guid.NewGuid().ToString("N");

        context.Response.Cookies.Append(StateCookieName, state, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            MaxAge = TimeSpan.FromMinutes(10),
        });

        try
        {
            var authorizationUri = await authService.BeginInteractiveLoginAsync(state, cancellationToken);
            return Results.Redirect(authorizationUri.ToString());
        }
        catch (OperationCanceledException)
        {
            return Results.Problem("Timed out building the Entra authorization request.", statusCode: StatusCodes.Status504GatewayTimeout);
        }
    }

    private static async Task<IResult> HandleCallbackAsync(
        HttpContext context,
        IGraphAuthService authService,
        CancellationToken cancellationToken)
    {
        if (!context.Request.Cookies.TryGetValue(StateCookieName, out var expectedState) || string.IsNullOrEmpty(expectedState))
        {
            return Results.BadRequest("Missing sign-in state cookie; start again at /auth/login.");
        }

        var state = context.Request.Query["state"].ToString();
        if (!string.Equals(state, expectedState, StringComparison.Ordinal))
        {
            return Results.BadRequest("state mismatch.");
        }

        context.Response.Cookies.Delete(StateCookieName);

        var callbackUri = new Uri($"{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
        var outcome = await authService.CompleteInteractiveLoginAsync(state, callbackUri, cancellationToken);

        return outcome.Success
            ? Results.Redirect("/")
            : Results.Problem($"Sign-in failed: {outcome.ErrorMessage}", statusCode: StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> HandleLogoutAsync(IGraphAuthService authService, CancellationToken cancellationToken)
    {
        await authService.SignOutAsync(cancellationToken);
        return Results.Ok();
    }

    private static async Task<IResult> HandleMeAsync(IGraphAuthService authService, CancellationToken cancellationToken)
    {
        var account = await authService.GetCurrentAccountAsync(cancellationToken);
        return account is null
            ? Results.Ok(new { signedIn = false })
            : Results.Ok(new { signedIn = true, account.Username, account.DisplayName });
    }
}
