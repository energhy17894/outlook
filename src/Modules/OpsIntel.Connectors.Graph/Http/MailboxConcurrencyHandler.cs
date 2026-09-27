namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Gates outgoing requests through <see cref="MailboxConcurrencyLimiter"/> so no more than 4 run
/// concurrently for a given mailbox (ADR-0009). The mailbox ID for a request is read from
/// <see cref="HttpRequestOptionsKeys.MailboxId"/>, defaulting to
/// <see cref="MailboxConcurrencyLimiter.DefaultMailboxId"/> when not set.
/// </summary>
public sealed class MailboxConcurrencyHandler : DelegatingHandler
{
    private readonly MailboxConcurrencyLimiter _limiter;

    public MailboxConcurrencyHandler(MailboxConcurrencyLimiter limiter)
    {
        _limiter = limiter;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var mailboxId = MailboxId(request);
        var semaphore = _limiter.GetSemaphore(mailboxId);

        await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static string MailboxId(HttpRequestMessage request) =>
        request.Options.TryGetValue(HttpRequestOptionsKeys.MailboxId, out var mailboxId) && !string.IsNullOrEmpty(mailboxId)
            ? mailboxId
            : MailboxConcurrencyLimiter.DefaultMailboxId;
}

/// <summary><see cref="HttpRequestOptionsKey{TValue}"/> constants shared by this connector's handlers.</summary>
public static class HttpRequestOptionsKeys
{
    /// <summary>The mailbox a request targets; read by <see cref="MailboxConcurrencyHandler"/>.</summary>
    public static readonly HttpRequestOptionsKey<string> MailboxId = new("OpsIntel.Graph.MailboxId");
}
