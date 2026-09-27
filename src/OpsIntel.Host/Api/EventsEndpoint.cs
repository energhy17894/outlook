namespace OpsIntel.Host.Api;

/// <summary>
/// Server-Sent Events stub for <c>/api/v1/events</c> (ADR-0020: SSE for live UI updates, no
/// WebSockets/SignalR). Emits a periodic heartbeat comment so proxies/browsers keep the
/// connection alive; real event publishing (job/review-queue/audit updates) lands here once
/// <c>OpsIntel.Notifications</c> exists.
/// </summary>
public static class EventsEndpoint
{
    public static async Task HandleAsync(HttpContext context, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-cache";
        context.Response.Headers["X-Accel-Buffering"] = "no";
        context.Response.ContentType = "text/event-stream";

        var combined = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, context.RequestAborted);

        try
        {
            while (!combined.IsCancellationRequested)
            {
                await context.Response.WriteAsync(": heartbeat\n\n", combined.Token);
                await context.Response.Body.FlushAsync(combined.Token);
                await Task.Delay(TimeSpan.FromSeconds(15), combined.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Client disconnected or the host is shutting down; nothing further to do.
        }
    }
}
