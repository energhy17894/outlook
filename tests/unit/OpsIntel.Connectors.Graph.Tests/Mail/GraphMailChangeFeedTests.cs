using System.Net;
using OpsIntel.Connectors.Graph.Mail;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using Xunit;

namespace OpsIntel.Connectors.Graph.Tests.Mail;

public class GraphMailChangeFeedTests
{
    private const string NextLinkUrl = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$skiptoken=page2";
    private const string DeltaLinkUrl = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$deltatoken=abc123";

    [Fact]
    public async Task PollAsync_PagesThroughNextLink_AndCapturesDeltaLink()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (request, _) =>
            {
                var url = request.RequestUri!.ToString();
                HttpResponseMessage response;
                if (url.Contains("skiptoken"))
                {
                    response = JsonResponse("""
                        { "value": [ { "id": "msg-2", "subject": "second" } ], "@odata.deltaLink": "%DELTA%" }
                        """.Replace("%DELTA%", DeltaLinkUrl));
                }
                else
                {
                    response = JsonResponse("""
                        { "value": [ { "id": "msg-1", "subject": "first" } ], "@odata.nextLink": "%NEXT%" }
                        """.Replace("%NEXT%", NextLinkUrl));
                }

                return Task.FromResult(response);
            },
        };

        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client);

        var page = await feed.PollAsync("inbox", deltaLink: null);

        Assert.False(page.ResyncRequired);
        Assert.Equal(DeltaLinkUrl, page.NextDeltaLink);
        Assert.Equal(2, page.Items.Count);
        Assert.Equal("msg-1", page.Items[0].ItemId);
        Assert.Equal("msg-2", page.Items[1].ItemId);
        Assert.All(page.Items, item => Assert.False(item.IsRemoved));
        Assert.Contains("second", page.Items[1].RawPayloadJson);

        Assert.Equal(2, fake.Requests.Count);
        Assert.Contains("mailFolders/inbox/messages/delta", fake.Requests.First().RequestUri!.ToString());
        Assert.Contains("$select=", fake.Requests.First().RequestUri!.ToString());
    }

    [Fact]
    public async Task PollAsync_ReplaysDeltaLinkVerbatim_WithNoAddedQueryParameters()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""
                { "value": [], "@odata.deltaLink": "%DELTA%" }
                """.Replace("%DELTA%", DeltaLinkUrl))),
        };
        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client);

        await feed.PollAsync("inbox", deltaLink: DeltaLinkUrl);

        var sent = Assert.Single(fake.Requests);
        Assert.Equal(DeltaLinkUrl, sent.RequestUri!.ToString());
    }

    [Fact]
    public async Task PollAsync_ReportsRemovedItems()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""
                { "value": [ { "id": "msg-3", "@removed": { "reason": "deleted" } } ], "@odata.deltaLink": "%DELTA%" }
                """.Replace("%DELTA%", DeltaLinkUrl))),
        };
        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client);

        var page = await feed.PollAsync("inbox", deltaLink: null);

        var item = Assert.Single(page.Items);
        Assert.Equal("msg-3", item.ItemId);
        Assert.True(item.IsRemoved);
        Assert.Null(item.RawPayloadJson);
    }

    [Fact]
    public async Task PollAsync_On410Gone_SignalsResyncRequired()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone)),
        };
        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client);

        var page = await feed.PollAsync("inbox", deltaLink: "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$deltatoken=stale");

        Assert.True(page.ResyncRequired);
        Assert.Empty(page.Items);
        Assert.Null(page.NextDeltaLink);
    }

    [Fact]
    public async Task PollAsync_OnSyncStateNotFoundErrorBody_SignalsResyncRequired_EvenWithout410Status()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) =>
            {
                var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("""{ "error": { "code": "SyncStateNotFound", "message": "token expired" } }"""),
                };
                return Task.FromResult(response);
            },
        };
        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client);

        var page = await feed.PollAsync("inbox", deltaLink: "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$deltatoken=stale");

        Assert.True(page.ResyncRequired);
    }

    [Fact]
    public async Task PollAsync_SetsMailboxIdOnRequestOptions_ForConcurrencyLimiter()
    {
        var fake = new FakeHttpMessageHandler
        {
            Responder = (_, _) => Task.FromResult(JsonResponse("""{ "value": [], "@odata.deltaLink": "%DELTA%" }""".Replace("%DELTA%", DeltaLinkUrl))),
        };
        using var client = new HttpClient(fake);
        var feed = new GraphMailChangeFeed(client, mailboxId: "mailbox-42");

        await feed.PollAsync("inbox", deltaLink: null);

        var sent = Assert.Single(fake.Requests);
        Assert.True(sent.Options.TryGetValue(OpsIntel.Connectors.Graph.Http.HttpRequestOptionsKeys.MailboxId, out var mailboxId));
        Assert.Equal("mailbox-42", mailboxId);
    }

    private static HttpResponseMessage JsonResponse(string json) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
    };
}
