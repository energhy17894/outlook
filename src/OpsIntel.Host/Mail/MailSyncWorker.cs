using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OpsIntel.Connectors.Graph;
using OpsIntel.Connectors.Graph.Auth;
using OpsIntel.Connectors.Graph.Mail;
using OpsIntel.Contracts;
using OpsIntel.Normalization;
using OpsIntel.Persistence.Sqlite;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Host.Mail;

/// <summary>
/// Host side of the Graph → extraction chain: polls each configured mail folder's delta feed
/// (ADR-0009, polling only), groups the round's changed messages by <c>conversationId</c>, and
/// enqueues one <see cref="ExtractionJobTypes.WorkItems"/> job per conversation on the shared
/// job queue (ADR-0012) for OpsIntel.Intelligence to lease. The folder's delta link lives in
/// <c>sync_state</c> and only advances after every job of the round is enqueued, so a crash
/// mid-round just replays it — and the idempotency key makes that replay a no-op.
/// </summary>
/// <remarks>
/// Idles (one Information log) while nobody is signed in or Graph isn't configured. Graph/DB
/// errors never escape <see cref="ExecuteAsync"/> (Host runs with
/// <c>BackgroundServiceExceptionBehavior.StopHost</c>): they bump <c>error_count</c> and back
/// the loop off exponentially. Mail content (subjects, bodies) is never logged — ids/counts only.
/// </remarks>
public sealed class MailSyncWorker : BackgroundService
{
    /// <summary><c>sync_state.source_kind</c> for mail folders.</summary>
    public const string MailFolderSourceKind = "mail_folder";

    private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

    private readonly IGraphAuthService _authService;
    private readonly GraphMailChangeFeed _changeFeed;
    private readonly IJobQueue _jobQueue;
    private readonly OpsIntelDbContext _dbContext;
    private readonly GraphConnectorOptions _options;
    private readonly ILogger<MailSyncWorker> _logger;

    public MailSyncWorker(
        IGraphAuthService authService,
        GraphMailChangeFeed changeFeed,
        IJobQueue jobQueue,
        OpsIntelDbContext dbContext,
        IOptions<GraphConnectorOptions> options,
        ILogger<MailSyncWorker> logger)
    {
        _authService = authService;
        _changeFeed = changeFeed;
        _jobQueue = jobQueue;
        _dbContext = dbContext;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        string[] folderIds = _options.MailSyncFolderIds is { Length: > 0 } configured ? configured : ["inbox"];
        var loggedNotSignedIn = false;
        var consecutiveFailures = 0;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (await _authService.GetCurrentAccountAsync(stoppingToken) is null)
                {
                    if (!loggedNotSignedIn)
                    {
                        _logger.LogInformation("Mail sync idle: no signed-in Graph account (or Graph sign-in not configured).");
                        loggedNotSignedIn = true;
                    }
                }
                else
                {
                    loggedNotSignedIn = false;
                    var allSucceeded = true;
                    foreach (var folderId in folderIds)
                    {
                        allSucceeded &= await SyncFolderAsync(folderId, stoppingToken);
                    }

                    consecutiveFailures = allSucceeded ? 0 : consecutiveFailures + 1;
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogWarning(ex, "Mail sync round failed before polling.");
                consecutiveFailures++;
            }

            await Task.Delay(Backoff(_options.MailSyncInterval, consecutiveFailures), stoppingToken);
        }
    }

    /// <summary>
    /// Runs one delta round for <paramref name="folderId"/>. Returns false (after recording the
    /// failure in <c>sync_state.error_count</c>) instead of throwing on Graph/DB errors.
    /// </summary>
    public async Task<bool> SyncFolderAsync(string folderId, CancellationToken cancellationToken)
    {
        var state = await _dbContext.SyncStates.FindAsync([MailFolderSourceKind, folderId], cancellationToken);
        if (state is null)
        {
            state = new SyncStateRow { SourceKind = MailFolderSourceKind, ContainerId = folderId };
            _dbContext.SyncStates.Add(state);
        }

        try
        {
            var page = await _changeFeed.PollAsync(folderId, state.DeltaLink, cancellationToken);

            if (page.ResyncRequired)
            {
                // ADR-0009: 410 Gone / syncStateNotFound — drop the cursor so the next round is a
                // full initial sync. Re-enqueued threads are deduplicated by idempotency key.
                _logger.LogWarning("Mail folder {FolderId} delta link expired; full resync next round.", folderId);
                state.DeltaLink = null;
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            var threads = BuildThreads(page.Items, DateOnly.FromDateTime(DateTime.Now));
            foreach (var (thread, idempotencyKey) in threads)
            {
                await _jobQueue.EnqueueAsync(
                    ExtractionJobTypes.WorkItems,
                    JsonSerializer.Serialize(thread),
                    idempotencyKey,
                    cancellationToken: cancellationToken);
            }

            state.DeltaLink = page.NextDeltaLink;
            state.LastSuccessUtc = DateTimeOffset.UtcNow;
            state.ErrorCount = 0;
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Mail folder {FolderId}: {ItemCount} change(s), {ThreadCount} thread job(s) enqueued.",
                folderId, page.Items.Count, threads.Count);
            return true;
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Exception only (no payload/body in the message): HttpRequestException /
            // GraphSignInRequiredException / SqliteException texts carry no mail content.
            _logger.LogWarning(ex, "Mail sync of folder {FolderId} failed.", folderId);
            state.ErrorCount++;
            await _dbContext.SaveChangesAsync(CancellationToken.None);
            return false;
        }
    }

    /// <summary>
    /// Groups a round's non-removed messages by <c>conversationId</c> into one
    /// <see cref="ExtractionThread"/> each (messages oldest first), with an idempotency key over
    /// conversationId + the sorted (id, changeKey) pairs: re-polling the same message versions
    /// yields the same key, while an edited message (new changeKey) yields a new job.
    /// </summary>
    /// <remarks>
    /// ponytail: no RawItem/Message tables yet, so a thread only holds the messages seen in this
    /// delta round — earlier replies of the conversation are missing context for the extractor.
    /// Upgrade path: persist RawItem/Message rows (data-model.md) and rebuild the full thread with
    /// ThreadRebuilder before enqueueing.
    /// </remarks>
    private static List<(ExtractionThread Thread, string IdempotencyKey)> BuildThreads(
        IEnumerable<ChangeFeedItem> items, DateOnly today)
    {
        var result = new List<(ExtractionThread, string)>();
        var parsed = items
            .Where(i => !i.IsRemoved && i.RawPayloadJson is not null)
            .Select(i => ParseMessage(i.ItemId, i.RawPayloadJson!));

        foreach (var conversation in parsed.GroupBy(m => m.ConversationId, StringComparer.Ordinal))
        {
            var messages = conversation.OrderBy(m => m.Message.Date).ThenBy(m => m.Message.Id, StringComparer.Ordinal).ToList();
            var thread = new ExtractionThread(
                ThreadId: conversation.Key,
                Subject: messages[0].Message.Subject,
                ProjectName: null,
                Today: today,
                Messages: messages.Select(m => m.Message).ToList());

            var versions = messages
                .Select(m => $"{m.Message.Id}|{m.ChangeKey}")
                .Order(StringComparer.Ordinal);
            var hash = SHA256.HashData(Encoding.UTF8.GetBytes(conversation.Key + "\n" + string.Join("\n", versions)));
            result.Add((thread, $"{ExtractionJobTypes.WorkItems}:{Convert.ToHexStringLower(hash)}"));
        }

        return result;
    }

    // Tolerant on purpose: a missing field must not make one message poison the whole folder
    // (the delta link would never advance).
    private static (string ConversationId, string ChangeKey, ExtractionMessage Message) ParseMessage(string id, string rawJson)
    {
        using var document = JsonDocument.Parse(rawJson);
        var root = document.RootElement;

        var body = root.TryGetProperty("uniqueBody", out var uniqueBody) ? uniqueBody : default;
        var isHtml = body.ValueKind == JsonValueKind.Object
            && string.Equals(GetString(body, "contentType"), "html", StringComparison.OrdinalIgnoreCase);
        var cleaned = EmailBodyCleaner.Clean(body.ValueKind == JsonValueKind.Object ? GetString(body, "content") : null, isHtml);

        var to = root.TryGetProperty("toRecipients", out var recipients) && recipients.ValueKind == JsonValueKind.Array
            ? recipients.EnumerateArray().Select(Address).Where(a => a.Length > 0).ToList()
            : [];

        var date = root.TryGetProperty("receivedDateTime", out var received) && received.TryGetDateTimeOffset(out var parsedDate)
            ? parsedDate
            : DateTimeOffset.MinValue;

        var message = new ExtractionMessage(
            Id: id,
            From: root.TryGetProperty("from", out var from) ? Address(from) : string.Empty,
            To: to,
            Date: date,
            Subject: GetString(root, "subject") ?? string.Empty,
            CleanedBody: cleaned.CleanedText);

        // A message without conversationId stands alone rather than merging with others.
        return (GetString(root, "conversationId") ?? id, GetString(root, "changeKey") ?? string.Empty, message);
    }

    // Graph recipient shape: { "emailAddress": { "name": "...", "address": "..." } }.
    private static string Address(JsonElement recipient) =>
        recipient.ValueKind == JsonValueKind.Object && recipient.TryGetProperty("emailAddress", out var email)
            ? GetString(email, "address") ?? string.Empty
            : string.Empty;

    private static string? GetString(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static TimeSpan Backoff(TimeSpan interval, int consecutiveFailures)
    {
        // Same shape as SqliteJobQueue.ComputeBackoff: capped exponent, capped result.
        var candidate = interval * Math.Pow(2, Math.Min(consecutiveFailures, 10));
        return candidate > MaxBackoff ? MaxBackoff : candidate;
    }
}
