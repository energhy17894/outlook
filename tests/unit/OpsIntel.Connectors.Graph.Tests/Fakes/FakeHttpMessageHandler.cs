using System.Collections.Concurrent;

namespace OpsIntel.Connectors.Graph.Tests.Fakes;

/// <summary>
/// A scriptable innermost <see cref="HttpMessageHandler"/> for exercising the connector's HTTP
/// pipeline with no network access. <see cref="Responder"/> is called once per request; tests
/// set it to a queue-popping or URL-inspecting delegate as needed. Every request that reaches
/// <see cref="SendAsync"/> is recorded in <see cref="Requests"/> for assertions.
/// </summary>
public sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    public ConcurrentQueue<HttpRequestMessage> Requests { get; } = new();

    /// <summary>The number of requests concurrently inside <see cref="SendAsync"/> right now.</summary>
    public int CurrentConcurrency => _current;

    /// <summary>The highest value <see cref="CurrentConcurrency"/> ever reached.</summary>
    public int MaxObservedConcurrency => _maxObserved;

    private int _current;
    private int _maxObserved;

    public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> Responder { get; set; }
        = (_, _) => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Enqueue(request);

        var concurrent = Interlocked.Increment(ref _current);
        InterlockedMax(ref _maxObserved, concurrent);
        try
        {
            return await Responder(request, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            Interlocked.Decrement(ref _current);
        }
    }

    private static void InterlockedMax(ref int target, int candidate)
    {
        int initial, computed;
        do
        {
            initial = target;
            computed = Math.Max(initial, candidate);
        }
        while (Interlocked.CompareExchange(ref target, computed, initial) != initial);
    }
}
