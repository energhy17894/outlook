using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace OpsIntel.Host.Security;

/// <summary>
/// For state-changing requests (anything other than GET/HEAD/OPTIONS/TRACE), verifies that a
/// present <c>Origin</c> header names an allowed host (ADR-0003). Combined with
/// <see cref="HostAllowlistMiddleware"/>, this closes the DNS-rebinding gap a Host-header check
/// alone would leave: a rebound page still presents a browser-set <c>Origin</c> that must also
/// resolve to an allowed local name.
/// </summary>
public sealed class OriginCheckMiddleware
{
    private static readonly HashSet<string> SafeMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Get, HttpMethods.Head, HttpMethods.Options, HttpMethods.Trace,
    };

    private readonly RequestDelegate _next;
    private readonly IOptionsMonitor<HostAllowlistOptions> _options;

    public OriginCheckMiddleware(RequestDelegate next, IOptionsMonitor<HostAllowlistOptions> options)
    {
        _next = next;
        _options = options;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!SafeMethods.Contains(context.Request.Method) &&
            context.Request.Headers.TryGetValue("Origin", out var originHeader) &&
            !IsAllowedOrigin(originHeader, _options.CurrentValue.AllowedHostNames))
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("Origin not allowed.");
            return;
        }

        await _next(context);
    }

    /// <summary>
    /// Returns whether <paramref name="originHeader"/> (e.g. <c>https://localhost:6500</c>)
    /// names a host in <paramref name="allowedHostNames"/>. An unparsable or missing origin is
    /// treated as not allowed by the caller's default; this method only judges present values.
    /// </summary>
    public static bool IsAllowedOrigin(StringValues originHeader, IReadOnlyCollection<string> allowedHostNames)
    {
        var value = originHeader.ToString();
        if (string.IsNullOrEmpty(value) || !Uri.TryCreate(value, UriKind.Absolute, out var origin))
        {
            return false;
        }

        return HostAllowlistMiddleware.IsAllowedHost(origin.Host, allowedHostNames);
    }
}

public static class OriginCheckMiddlewareExtensions
{
    public static IApplicationBuilder UseOpsIntelOriginCheck(this IApplicationBuilder app)
        => app.UseMiddleware<OriginCheckMiddleware>();
}
