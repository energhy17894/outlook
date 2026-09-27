using System.Net;
using OpsIntel.Connectors.Graph.Mail;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Mail;

public class DraftWriterTests
{
    [Fact]
    public async Task CreateReplyDraftAsync_PostsToCreateReply_AndReturnsDraftId()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""{ "id": "draft-1", "isDraft": true }""")),
        };
        using var client = new HttpClient(fake);
        var writer = new DraftWriter(client);

        var draft = await writer.CreateReplyDraftAsync("msg-1", "thanks!");

        Assert.Equal("draft-1", draft.Id);
        var sent = Assert.Single(fake.Requests);
        Assert.Equal(HttpMethod.Post, sent.Method);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/messages/msg-1/createReply", sent.RequestUri!.ToString());
    }

    [Fact]
    public async Task CreateReplyAllDraftAsync_PostsToCreateReplyAll()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""{ "id": "draft-2" }""")),
        };
        using var client = new HttpClient(fake);
        var writer = new DraftWriter(client);

        var draft = await writer.CreateReplyAllDraftAsync("msg-1");

        Assert.Equal("draft-2", draft.Id);
        var sent = Assert.Single(fake.Requests);
        Assert.Equal("https://graph.microsoft.com/v1.0/me/messages/msg-1/createReplyAll", sent.RequestUri!.ToString());
    }

    [Fact]
    public async Task DraftWriter_NeverIssuesARequest_ToAnySendEndpoint()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""{ "id": "draft-3" }""")),
        };
        using var client = new HttpClient(fake);
        var writer = new DraftWriter(client);

        await writer.CreateReplyDraftAsync("msg-1");
        await writer.CreateReplyAllDraftAsync("msg-2");

        Assert.All(fake.Requests, request =>
            Assert.DoesNotContain("send", request.RequestUri!.ToString(), StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DraftWriter_ExposesNoSendMethod()
    {
        // Belt-and-braces architecture-style guard: DraftWriter's public surface must never grow
        // a send/forward-and-send capability (ADR-0008).
        var methodNames = typeof(DraftWriter).GetMethods(
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.DeclaredOnly)
            .Select(m => m.Name);

        Assert.All(methodNames, name => Assert.DoesNotContain("send", name, StringComparison.OrdinalIgnoreCase));
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.Created)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };
}
