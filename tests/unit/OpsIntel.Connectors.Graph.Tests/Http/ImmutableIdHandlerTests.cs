using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Http;

public class ImmutableIdHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsPreferImmutableIdHeader()
    {
        var fake = new FakeHttpMessageHandler();
        using var handler = new ImmutableIdHandler { InnerHandler = fake };
        using var client = new HttpClient(handler);

        using var response = await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");

        var sent = Assert.Single(fake.Requests);
        Assert.True(sent.Headers.TryGetValues("Prefer", out var values));
        Assert.Contains("IdType=\"ImmutableId\"", values);
    }

    [Fact]
    public async Task SendAsync_DoesNotDuplicateHeader_WhenAlreadyPresent()
    {
        var fake = new FakeHttpMessageHandler();
        using var handler = new ImmutableIdHandler { InnerHandler = fake };
        using var client = new HttpClient(handler);

        using var request = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages");
        request.Headers.TryAddWithoutValidation("Prefer", "IdType=\"ImmutableId\"");

        await client.SendAsync(request);

        var sent = Assert.Single(fake.Requests);
        Assert.True(sent.Headers.TryGetValues("Prefer", out var values));
        Assert.Single(values);
    }
}
