using System.Net;
using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Http;

public class MailboxConcurrencyHandlerTests
{
    [Fact]
    public async Task SendAsync_NeverExceedsFourConcurrentRequests_PerMailbox()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = async (_, ct) =>
            {
                // Hold the "in-flight" slot long enough for other requests to pile up behind it.
                await Task.Delay(50, ct);
                return new HttpResponseMessage(HttpStatusCode.OK);
            },
        };
        var limiter = new MailboxConcurrencyLimiter(maxConcurrentPerMailbox: 4);
        using var handler = new MailboxConcurrencyHandler(limiter) { InnerHandler = fake };
        using var client = new HttpClient(handler);

        var tasks = Enumerable.Range(0, 20).Select(_ => client.GetAsync("https://graph.microsoft.com/v1.0/me/messages"));
        var responses = await Task.WhenAll(tasks);

        foreach (var response in responses)
        {
            response.Dispose();
        }

        Assert.Equal(20, fake.Requests.Count);
        Assert.True(fake.MaxObservedConcurrency <= 4, $"observed {fake.MaxObservedConcurrency} concurrent requests, expected <= 4");
        Assert.True(fake.MaxObservedConcurrency >= 2, "test is not actually exercising concurrency");
    }

    [Fact]
    public async Task SendAsync_TracksMailboxesIndependently()
    {
        var release = new TaskCompletionSource();
        var insideMailboxA = new TaskCompletionSource();

        var fake = new FakeHttpMessageHandler
        {
            Responder = async (request, ct) =>
            {
                var mailboxId = request.Options.TryGetValue(HttpRequestOptionsKeys.MailboxId, out var id) ? id : null;
                if (mailboxId == "mailbox-a")
                {
                    insideMailboxA.TrySetResult();
                    await release.Task.WaitAsync(ct);
                }

                return new HttpResponseMessage(HttpStatusCode.OK);
            },
        };
        var limiter = new MailboxConcurrencyLimiter(maxConcurrentPerMailbox: 1);
        using var handler = new MailboxConcurrencyHandler(limiter) { InnerHandler = fake };
        using var client = new HttpClient(handler);

        var mailboxATask = SendForMailboxAsync(client, "mailbox-a");
        await insideMailboxA.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // mailbox-b's semaphore is independent of mailbox-a's, so this must complete even though
        // mailbox-a's single slot is still held.
        var mailboxBTask = SendForMailboxAsync(client, "mailbox-b");
        var completed = await Task.WhenAny(mailboxBTask, Task.Delay(TimeSpan.FromSeconds(5)));
        Assert.Same(mailboxBTask, completed);

        release.TrySetResult();
        await mailboxATask;
    }

    private static async Task SendForMailboxAsync(HttpClient client, string mailboxId)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages");
        request.Options.Set(HttpRequestOptionsKeys.MailboxId, mailboxId);
        using var response = await client.SendAsync(request);
    }
}
