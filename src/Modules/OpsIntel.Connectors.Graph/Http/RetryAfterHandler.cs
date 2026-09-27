using System.Net;

namespace OpsIntel.Connectors.Graph.Http;

/// <summary>
/// Retries <c>429</c>/<c>503</c> Graph responses honoring the <c>Retry-After</c> header
/// (ADR-0009: "Retry-After'a uyulur"). Falls back to capped exponential backoff when the header
/// is absent, per Graph's general throttling guidance. This handler is registered as the
/// innermost handler (closest to the network), so <see cref="MailboxConcurrencyHandler"/>'s
/// semaphore is held across all of a request's retries — a throttled request keeps its
/// concurrency slot rather than freeing it for another request to immediately hit the same
/// throttle.
/// </summary>
public sealed class RetryAfterHandler : DelegatingHandler
{
    private readonly int _maxRetries;
    private readonly TimeSpan _maxDelay;
    private readonly Func<TimeSpan, CancellationToken, Task> _delay;

    public RetryAfterHandler(int maxRetries = 3, TimeSpan? maxDelay = null, Func<TimeSpan, CancellationToken, Task>? delay = null)
    {
        if (maxRetries < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxRetries), maxRetries, "Must be non-negative.");
        }

        _maxRetries = maxRetries;
        _maxDelay = maxDelay ?? TimeSpan.FromSeconds(30);
        _delay = delay ?? ((d, ct) => Task.Delay(d, ct));
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            // Graph SDK v6 request bodies are typically re-creatable per attempt by the caller;
            // for the spike's fake-handler tests only GET/no-body calls exercise retry.
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            var isThrottledOrUnavailable = response.StatusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.ServiceUnavailable;
            if (!isThrottledOrUnavailable || attempt >= _maxRetries)
            {
                return response;
            }

            var delay = GetRetryDelay(response, attempt);
            response.Dispose();
            await _delay(delay, cancellationToken).ConfigureAwait(false);
        }
    }

    private TimeSpan GetRetryDelay(HttpResponseMessage response, int attempt)
    {
        if (response.Headers.RetryAfter is { } retryAfter)
        {
            if (retryAfter.Delta is { } delta)
            {
                return Clamp(delta);
            }

            if (retryAfter.Date is { } date)
            {
                var untilDate = date - DateTimeOffset.UtcNow;
                return Clamp(untilDate < TimeSpan.Zero ? TimeSpan.Zero : untilDate);
            }
        }

        // No Retry-After: capped exponential backoff (1s, 2s, 4s, ...).
        return Clamp(TimeSpan.FromSeconds(Math.Pow(2, attempt)));
    }

    private TimeSpan Clamp(TimeSpan delay) => delay > _maxDelay ? _maxDelay : delay;
}
