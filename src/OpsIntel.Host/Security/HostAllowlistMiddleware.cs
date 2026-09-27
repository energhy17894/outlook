using Microsoft.Extensions.Options;

namespace OpsIntel.Host.Security;

/// <summary>
/// Rejects any request whose <c>Host</c> header does not match the configured allowlist
/// (ADR-0003). This is the primary DNS-rebinding defense: even though Kestrel only binds
/// loopback addresses, a malicious public DNS record pointed at 127.0.0.1 would otherwise let
/// a remote page's script talk to the local service under the browser's same-origin rules.
/// </summary>
public sealed class HostAllowlistMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<HostAllowlistOptions> _options;

    public HostAllowlistMiddleware(RequestDelegate next, IOptionsMonitor<HostAllowlistOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var host = context.Request.Host.Host;
        if (!IsAllowedHost(host, _options.CurrentValue.AllowedHostNames))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync("Host not allowed.");
            return;
        }

        await _next(context);
    }

    /// <summary>Case-insensitive membership check of <paramref name="host"/> against <paramref name="allowedHostNames"/>.</summary>
    public static bool IsAllowedHost(string host, IReadOnlyCollection<string> allowedHostNames)
    {
        foreach (var allowed in allowedHostNames)
        {
            if (string.Equals(host, allowed, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public static class HostAllowlistMiddlewareExtensions
{
    public static IApplicationBuilder UseOpsIntelHostAllowlist(this IApplicationBuilder app)
        => app.UseMiddleware<HostAllowlistMiddleware>();
}
