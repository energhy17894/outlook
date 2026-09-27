namespace OpsIntel.Host.Security;

/// <summary>
/// Shared configuration for the localhost-only defenses (ADR-0003): the Host-header allowlist
/// middleware and the Origin check middleware both key off the same set of allowed host names,
/// since a DNS-rebinding attack targets both checks identically.
/// </summary>
public sealed class HostAllowlistOptions
{
    /// <summary>
    /// Host names (without port) a request's <c>Host</c> header — and, for state-changing
    /// requests, its <c>Origin</c> header — must match. Defaults cover loopback only; add
    /// entries here (not by disabling the check) for any additional configured name.
    /// </summary>
    public string[] AllowedHostNames { get; set; } = ["localhost", "127.0.0.1", "[::1]"];
}
