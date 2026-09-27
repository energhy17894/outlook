using System.Net;
using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Http;

public class RetryAfterHandlerTests
{
    [Fact]
    public async Task SendAsync_RetriesOn429_HonoringRetryAfterSeconds()
    {
        var fake = new FakeHttpMessageHandler();
        var attempt = 0;
        fake.Responder = (_, _) =>
        {
            attempt++;
            if (attempt == 1)
            {
                var throttled = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
                throttled.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(TimeSpan.FromMilliseconds(1));
                return Task.FromResult(throttled);
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));
        };

        var delays = new List<TimeSpan>();
        using var handler = new RetryAfterHandler(maxRetries: 3, delay: (d, _) => { delays.Add(d); return Task.CompletedTask; })
        {
            InnerHandler = fake,
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, fake.Requests.Count);
        Assert.Single(delays);
        Assert.Equal(TimeSpan.FromMilliseconds(1), delays[0]);
    }

    [Fact]
    public async Task SendAsync_GivesUp_AfterMaxRetries()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.TooManyRequests)),
        };

        var delayCount = 0;
        using var handler = new RetryAfterHandler(maxRetries: 2, delay: (_, _) => { delayCount++; return Task.CompletedTask; })
        {
            InnerHandler = fake,
        };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
        Assert.Equal(3, fake.Requests.Count); // initial attempt + 2 retries
        Assert.Equal(2, delayCount);
    }

    [Fact]
    public async Task SendAsync_BacksOffExponentially_WhenNoRetryAfterHeader()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)),
        };

        var delays = new List<TimeSpan>();
        using var handler = new RetryAfterHandler(maxRetries: 3, delay: (d, _) => { delays.Add(d); return Task.CompletedTask; })
        {
            InnerHandler = fake,
        };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        Assert.Equal([TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(4)], delays);
    }

    [Fact]
    public async Task SendAsync_DoesNotRetry_OnSuccess()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)),
        };

        using var handler = new RetryAfterHandler(delay: (_, _) => Task.CompletedTask) { InnerHandler = fake };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        Assert.Single(fake.Requests);
    }
}
