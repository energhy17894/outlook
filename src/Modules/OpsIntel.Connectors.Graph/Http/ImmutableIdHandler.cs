namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Adds <c>Prefer: IdType="ImmutableId"</c> to every outgoing Graph request (ADR-0009). Outlook
/// item IDs are otherwise unstable across folder moves; every request — deltas included — must
/// carry this header, and deltaLinks stay valid across the two ID formats so no resync is
/// triggered by adding it.
/// </summary>
public sealed class ImmutableIdHandler : DelegatingHandler
{
    public const string HeaderName = "Prefer";
    public const string HeaderValue = "IdType=\"ImmutableId\"";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(HeaderName))
        {
            request.Headers.TryAddWithoutValidation(HeaderName, HeaderValue);
        }

        return base.SendAsync(request, cancellationToken);
    }
}
