namespace OpsIntel.Connectors.Graph.Auth;

/// <summary>
/// The fixed set of delegated Microsoft Graph scopes this connector ever requests
/// (ADR-0007, ADR-0008). <c>Mail.Send</c> is never included, in any phase: sending is always
/// done by the human, from Outlook, so the "human executes" guarantee is enforced at the token
/// level. <see cref="Delegated"/> is asserted against in
/// <c>tests/unit/OpsIntel.Connectors.Graph.Tests/Auth/GraphScopesTests.cs</c> and again as an
/// architecture-style guard so a future edit cannot silently reintroduce it.
/// </summary>
public static class GraphScopes
{
    /// <summary>
    /// MVP delegated scopes. <c>Mail.ReadWrite</c> (not <c>Mail.Read</c>) is requested because
    /// <see cref="Mail.DraftWriter"/> needs it to create reply drafts; it explicitly "does not
    /// include permission to send mail" (ADR-0008).
    /// </summary>
    public static IReadOnlyList<string> Delegated { get; } =
    [
        "User.Read",
        "Mail.ReadWrite",
        "Files.Read.All",
        "Sites.Read.All",
        "Calendars.Read",
        "offline_access",
    ];

    /// <summary>The one scope this connector must never request.</summary>
    public const string ForbiddenMailSendScope = "Mail.Send";
}
