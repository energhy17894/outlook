using System.Net.Http.Headers;
using OpsIntel.Connectors.Graph.Auth;
using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Http;

public class GraphBearerTokenHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerToken_OnlyWhenNoAuthorizationHeader()
    {
        var fake = new FakeHttpMessageHandler();
        using var handler = new GraphBearerTokenHandler(new FixedTokenAuthService()) { InnerHandler = fake };
        using var client = new HttpClient(handler);

        await client.GetAsync("https://graph.microsoft.com/v1.0/me/messages");
        using var preAuthorized = new HttpRequestMessage(HttpMethod.Get, "https://graph.microsoft.com/v1.0/me/messages");
        preAuthorized.Headers.Authorization = new AuthenticationHeaderValue("Bearer", "sdk-token");
        await client.SendAsync(preAuthorized);

        var sent = fake.Requests.ToArray();
        Assert.Equal("Bearer test-token", sent[0].Headers.Authorization!.ToString());
        Assert.Equal("Bearer sdk-token", sent[1].Headers.Authorization!.ToString());
    }

    private sealed class FixedTokenAuthService : IGraphAuthService
    {
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => Task.FromResult("test-token");
        public Task<Uri> BeginInteractiveLoginAsync(string state, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GraphSignInOutcome> CompleteInteractiveLoginAsync(string state, Uri callbackUri, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GraphAccountInfo?> GetCurrentAccountAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SignOutAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
