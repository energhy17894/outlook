using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpsIntel.Connectors.Graph;
using OpsIntel.Connectors.Graph.Auth;
using OpsIntel.Connectors.Graph.Mail;
using OpsIntel.Connectors.Graph.Tests.Fakes;
using OpsIntel.Contracts;
using OpsIntel.Host.Mail;
using OpsIntel.Persistence.Sqlite;
using Xunit;

namespace OpsIntel.UnitTests;

/// <summary>
/// <see cref="MailSyncWorker.SyncFolderAsync"/> against a scripted Graph delta feed and a real
/// temp-file SQLite job queue (the idempotency guarantee is the jobs table's UNIQUE constraint).
/// </summary>
public sealed class MailSyncWorkerTests : IDisposable
{
    private const string DeltaLink = "https://graph.microsoft.com/v1.0/me/mailFolders/inbox/messages/delta?$deltatoken=round1";

    // Two conversations; conv-a has two messages delivered newest first, plus one removal.
    private const string DeltaRound = """
        {
          "value": [
            { "id": "m2", "conversationId": "conv-a", "changeKey": "ck2", "subject": "RE: Kickoff",
              "receivedDateTime": "2026-08-03T14:47:00Z",
              "from": { "emailAddress": { "name": "Ali", "address": "ali@ornek.com.tr" } },
              "toRecipients": [ { "emailAddress": { "address": "mehmet@ornek.com.tr" } } ],
              "uniqueBody": { "contentType": "html", "content": "<p>Kullanıcı listesini gönderir misin?</p>" } },
            { "id": "m1", "conversationId": "conv-a", "changeKey": "ck1", "subject": "Kickoff",
              "receivedDateTime": "2026-08-03T09:12:00Z",
              "from": { "emailAddress": { "address": "mehmet@ornek.com.tr" } },
              "toRecipients": [ { "emailAddress": { "address": "ali@ornek.com.tr" } } ],
              "uniqueBody": { "contentType": "text", "content": "Test ortamını Pazartesiye kadar hazır edeceğim." } },
            { "id": "m3", "conversationId": "conv-b", "changeKey": "ck3", "subject": "Risk",
              "receivedDateTime": "2026-08-04T10:00:00Z",
              "from": { "emailAddress": { "address": "ayse@ornek.com.tr" } },
              "uniqueBody": { "contentType": "text", "content": "Tedarik gecikebilir." } },
            { "id": "m0", "@removed": { "reason": "deleted" } }
          ],
          "@odata.deltaLink": "%DELTA%"
        }
        """;

    private readonly string _dbPath;
    private readonly OpsIntelDbContext _db;
    private readonly FakeHttpMessageHandler _graph = new();
    private readonly HttpClient _httpClient;
    private readonly MailSyncWorker _worker;

    public MailSyncWorkerTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"opsintel-mailsync-tests-{Guid.NewGuid():N}.db");
        _db = OpsIntelDbContextFactory.Create($"Data Source={_dbPath};Pooling=False");
        _httpClient = new HttpClient(_graph);
        _worker = new MailSyncWorker(
            new UnusedAuthService(),
            new GraphMailChangeFeed(_httpClient),
            new SqliteJobQueue(_db),
            _db,
            Options.Create(new GraphConnectorOptions()),
            NullLogger<MailSyncWorker>.Instance);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
        _db.Dispose();
        foreach (var path in new[] { _dbPath, _dbPath + "-wal", _dbPath + "-shm" })
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task DeltaRound_WithTwoConversations_EnqueuesOneJobPerConversation_AndStoresDeltaLink()
    {
        RespondWith(DeltaRound.Replace("%DELTA%", DeltaLink));

        Assert.True(await _worker.SyncFolderAsync("inbox", CancellationToken.None));

        var jobs = _db.Jobs.OrderBy(j => j.Id).Select(j => new { j.Type, j.Payload }).ToList();
        Assert.Equal(2, jobs.Count);
        Assert.All(jobs, j => Assert.Equal(ExtractionJobTypes.WorkItems, j.Type));

        var threads = jobs.Select(j => JsonSerializer.Deserialize<ExtractionThread>(j.Payload)!).ToDictionary(t => t.ThreadId);
        var a = threads["conv-a"];
        Assert.Equal("Kickoff", a.Subject);
        Assert.Equal(["m1", "m2"], a.Messages.Select(m => m.Id));
        Assert.Equal("mehmet@ornek.com.tr", a.Messages[0].From);
        Assert.Equal(["ali@ornek.com.tr"], a.Messages[0].To);
        Assert.Equal("Test ortamını Pazartesiye kadar hazır edeceğim.", a.Messages[0].CleanedBody);
        Assert.Equal("Kullanıcı listesini gönderir misin?", a.Messages[1].CleanedBody); // HTML stripped
        Assert.Equal(new DateTimeOffset(2026, 8, 3, 14, 47, 0, TimeSpan.Zero), a.Messages[1].Date);
        Assert.Equal(["m3"], threads["conv-b"].Messages.Select(m => m.Id));

        var state = Assert.Single(_db.SyncStates.AsEnumerable());
        Assert.Equal(DeltaLink, state.DeltaLink);
        Assert.Equal(0, state.ErrorCount);
        Assert.NotNull(state.LastSuccessUtc);
    }

    [Fact]
    public async Task SameRoundPolledAgain_EnqueuesNothingNew()
    {
        RespondWith(DeltaRound.Replace("%DELTA%", DeltaLink));

        await _worker.SyncFolderAsync("inbox", CancellationToken.None);
        await _worker.SyncFolderAsync("inbox", CancellationToken.None);

        Assert.Equal(2, _db.Jobs.Count());
        // The second round replayed the stored delta link rather than starting over.
        Assert.Equal(DeltaLink, _graph.Requests.Last().RequestUri!.ToString());
    }

    [Fact]
    public async Task ResyncRequired_ClearsStoredDeltaLink()
    {
        RespondWith(DeltaRound.Replace("%DELTA%", DeltaLink));
        await _worker.SyncFolderAsync("inbox", CancellationToken.None);

        _graph.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Gone));
        Assert.True(await _worker.SyncFolderAsync("inbox", CancellationToken.None));

        Assert.Null(Assert.Single(_db.SyncStates.AsEnumerable()).DeltaLink);
    }

    [Fact]
    public async Task GraphError_IncrementsErrorCount_WithoutThrowingOrAdvancing()
    {
        _graph.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Assert.False(await _worker.SyncFolderAsync("inbox", CancellationToken.None));

        var state = Assert.Single(_db.SyncStates.AsEnumerable());
        Assert.Equal(1, state.ErrorCount);
        Assert.Null(state.DeltaLink);
        Assert.Equal(0, _db.Jobs.Count());
    }

    private void RespondWith(string json) =>
        _graph.Responder = (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, System.Text.Encoding.UTF8, "application/json"),
        });

    /// <summary>SyncFolderAsync never consults auth; only ExecuteAsync's signed-in gate does.</summary>
    private sealed class UnusedAuthService : IGraphAuthService
    {
        public Task<Uri> BeginInteractiveLoginAsync(string state, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GraphSignInOutcome> CompleteInteractiveLoginAsync(string state, Uri callbackUri, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<GraphAccountInfo?> GetCurrentAccountAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SignOutAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
