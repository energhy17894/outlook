using System.Net;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpsIntel.Connectors.Graph;
using OpsIntel.Host.Api;
using OpsIntel.Host.Auth;
using OpsIntel.Host.Security;
using OpsIntel.Observability;
using OpsIntel.Platform.Abstractions;
using OpsIntel.Platform.Windows;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseWindowsService();
builder.Host.AddOpsIntelObservability("OpsIntel.Host");

// ADR-0022 / crash-restart contract: an unhandled BackgroundService exception stops the whole
// host instead of leaving a silently-dead worker, and the catch below exits non-zero so the
// Windows Service Control Manager's recovery actions (restart) actually fire.
builder.Services.Configure<HostOptions>(options =>
{
    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.StopHost;
});

var kestrelSection = builder.Configuration.GetSection("OpsIntel:Kestrel");
var port = kestrelSection.GetValue("Port", 6500);
var allowedHostNames = kestrelSection.GetSection("AllowedHostNames").Get<string[]>()
    ?? ["localhost", "127.0.0.1", "[::1]"];

builder.Services.Configure<HostAllowlistOptions>(o => o.AllowedHostNames = allowedHostNames);

builder.WebHost.ConfigureKestrel((context, options) =>
{
    // ADR-0003: bind loopback only (127.0.0.1 and ::1), never 0.0.0.0/[::]. HTTP.sys is not used.
    X509Certificate2? certificate = null;

    if (!context.HostingEnvironment.IsDevelopment())
    {
        var thumbprint = kestrelSection.GetValue<string>("CertificateThumbprint");
        var subject = kestrelSection.GetValue<string>("CertificateSubject");
        var provider = new WindowsCertificateProvider(Options.Create(new WindowsCertificateProviderOptions
        {
            StoreLocation = StoreLocation.LocalMachine,
            StoreName = StoreName.My,
            Thumbprint = thumbprint,
            Subject = subject,
        }));
        certificate = provider.GetServerCertificateAsync().GetAwaiter().GetResult();
    }

    options.Listen(IPAddress.Loopback, port, listenOptions => ConfigureHttps(listenOptions, certificate));
    options.Listen(IPAddress.IPv6Loopback, port, listenOptions => ConfigureHttps(listenOptions, certificate));
});

builder.Services.AddHealthChecks();

// ADR-0007/0008/0009: Microsoft Graph connector (delegated auth BFF + PKCE, delta polling,
// reply-draft-only writes). The DPAPI secret store also backs the MSAL token cache.
builder.Services.Configure<DpapiSecretStoreOptions>(
    builder.Configuration.GetSection("OpsIntel:Secrets"));
builder.Services.AddSingleton<ISecretStore, DpapiSecretStore>();
builder.Services.AddOpsIntelGraphConnector(builder.Configuration);

var app = builder.Build();

app.UseOpsIntelSecurityHeaders();
app.UseOpsIntelHostAllowlist();
app.UseOpsIntelOriginCheck();

app.UseDefaultFiles();
app.UseStaticFiles();

app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

app.MapGet("/api/v1/status", () => Results.Ok(new { status = "ok", service = "OpsIntel.Host" }));

app.MapGet("/api/v1/events", EventsEndpoint.HandleAsync);

app.MapOpsIntelAuthEndpoints();

app.MapFallbackToFile("index.html");

try
{
    app.Run();
    return 0;
}
catch (Exception ex)
{
    Log.Fatal(ex, "OpsIntel.Host terminated unexpectedly.");
    return 1;
}

static void ConfigureHttps(Microsoft.AspNetCore.Server.Kestrel.Core.ListenOptions listenOptions, X509Certificate2? certificate)
{
    if (certificate is not null)
    {
        listenOptions.UseHttps(certificate);
    }
    else
    {
        // Development-only fallback: the ASP.NET Core HTTPS developer certificate.
        listenOptions.UseHttps();
    }
}

/// <summary>Marker type so integration/architecture tests can reference this assembly's entry point.</summary>
public partial class Program
{
}
