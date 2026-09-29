using System.Net;
using System.Text.Json;
using OpsIntel.Connectors.Graph.Http;
using OpsIntel.Platform.Abstractions;

namespace OpsIntel.Connectors.Graph.Mail;

/// <summary>
/// <see cref="IChangeFeed"/> over Outlook's per-folder message delta query
/// (ADR-0009: <c>/me/mailFolders/{id}/messages/delta</c>). One <see cref="PollAsync"/> call is
/// one full "delta round": it pages through <c>@odata.nextLink</c> until an
/// <c>@odata.deltaLink</c> is returned, then hands that opaque link back to the caller to store
/// and replay verbatim on the next round (ADR-0009's "thin raw HTTP path" — deltaLinks are
/// followed exactly as given, with no re-added query parameters, since they already encode the
/// original request). <c>sourceId</c> is the mail folder's (immutable) ID.
/// </summary>
public sealed class GraphMailChangeFeed : IChangeFeed
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    // Minimal field set per the Faz 0 spec: conversationId, internetMessageId and uniqueBody are
    // required by downstream normalization/threading; the rest keep payloads small.
    private static readonly string Select = string.Join(',',
        "id", "conversationId", "internetMessageId", "uniqueBody", "subject",
        "receivedDateTime", "from", "toRecipients", "isRead", "parentFolderId", "changeKey");

    private readonly HttpClient _httpClient;
    private readonly string _mailboxId;

    public GraphMailChangeFeed(HttpClient httpClient, string mailboxId = MailboxConcurrencyLimiter.DefaultMailboxId)
    {
        _httpClient = httpClient;
        _mailboxId = mailboxId;
    }

    public async Task<ChangeFeedPage> PollAsync(string sourceId, string? deltaLink, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceId);

        var url = deltaLink ?? BuildInitialUrl(sourceId);
        var items = new List<ChangeFeedItem>();

        while (true)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Options.Set(HttpRequestOptionsKeys.MailboxId, _mailboxId);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (await RequiresResyncAsync(response, cancellationToken).ConfigureAwait(false))
            {
                return new ChangeFeedPage([], null, ResyncRequired: true);
            }

            response.EnsureSuccessStatusCode();

            var page = await ParsePageAsync(response, cancellationToken).ConfigureAwait(false);
            items.AddRange(page.Items);

            if (page.DeltaLink is not null)
            {
                return new ChangeFeedPage(items, page.DeltaLink, ResyncRequired: false);
            }

            if (page.NextLink is null)
            {
                // Malformed/empty response with neither link: nothing more to page through.
                return new ChangeFeedPage(items, null, ResyncRequired: false);
            }

            url = page.NextLink;
        }
    }

    private static string BuildInitialUrl(string folderId) =>
        $"{GraphBaseUrl}/me/mailFolders/{Uri.EscapeDataString(folderId)}/messages/delta?$select={Select}";

    private static async Task<bool> RequiresResyncAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode == HttpStatusCode.Gone)
        {
            return true;
        }

        if (response.IsSuccessStatusCode)
        {
            return false;
        }

        // syncStateNotFound is sometimes surfaced as a 400-range error code rather than 410
        // (ADR-0009). Peek the body for it without disturbing the rest of the error handling.
        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        return body.Contains("syncStateNotFound", StringComparison.OrdinalIgnoreCase);
    }

    private static async Task<ParsedPage> ParsePageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        await using (stream.ConfigureAwait(false))
        {
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);
            var root = document.RootElement;

            var items = new List<ChangeFeedItem>();
            if (root.TryGetProperty("value", out var values))
            {
                foreach (var element in values.EnumerateArray())
                {
                    var id = element.GetProperty("id").GetString()
                        ?? throw new InvalidOperationException("Graph delta item is missing 'id'.");
                    var isRemoved = element.TryGetProperty("@removed", out _);
                    items.Add(new ChangeFeedItem(id, isRemoved, isRemoved ? null : element.GetRawText()));
                }
            }

            var deltaLink = root.TryGetProperty("@odata.deltaLink", out var deltaLinkElement)
                ? deltaLinkElement.GetString()
                : null;
            var nextLink = root.TryGetProperty("@odata.nextLink", out var nextLinkElement)
                ? nextLinkElement.GetString()
                : null;

            return new ParsedPage(items, deltaLink, nextLink);
        }
    }

    private sealed record ParsedPage(IReadOnlyList<ChangeFeedItem> Items, string? DeltaLink, string? NextLink);
}
