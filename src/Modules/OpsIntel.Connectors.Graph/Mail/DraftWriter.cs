using System.Net.Http.Json;
using OpsIntel.Connectors.Graph.Http;

namespace OpsIntel.Connectors.Graph.Mail;

/// <summary>One created reply/reply-all draft.</summary>
public sealed record DraftMessage(string Id, string RawJson);

/// <summary>
/// Creates reply drafts only (ADR-0008: <c>Mail.ReadWrite</c>, never <c>Mail.Send</c>). This
/// class deliberately has no send/forward-and-send capability of any kind — sending is always
/// done by the human, from Outlook, on the draft this class leaves behind. Both methods call
/// Graph's <c>createReply</c>/<c>createReplyAll</c> actions, which only ever create a draft;
/// Graph has no way to make those actions send.
/// </summary>
public sealed class DraftWriter
{
    private const string GraphBaseUrl = "https://graph.microsoft.com/v1.0";

    private readonly HttpClient _httpClient;
    private readonly string _mailboxId;

    public DraftWriter(HttpClient httpClient, string mailboxId = MailboxConcurrencyLimiter.DefaultMailboxId)
    {
        _httpClient = httpClient;
        _mailboxId = mailboxId;
    }

    /// <summary>Creates a reply-to-sender draft for <paramref name="messageId"/> via Graph's <c>createReply</c> action.</summary>
    public Task<DraftMessage> CreateReplyDraftAsync(string messageId, string? comment = null, CancellationToken cancellationToken = default) =>
        CreateDraftAsync(messageId, "createReply", comment, cancellationToken);

    /// <summary>Creates a reply-all draft for <paramref name="messageId"/> via Graph's <c>createReplyAll</c> action.</summary>
    public Task<DraftMessage> CreateReplyAllDraftAsync(string messageId, string? comment = null, CancellationToken cancellationToken = default) =>
        CreateDraftAsync(messageId, "createReplyAll", comment, cancellationToken);

    private async Task<DraftMessage> CreateDraftAsync(string messageId, string action, string? comment, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(messageId);

        var url = $"{GraphBaseUrl}/me/messages/{Uri.EscapeDataString(messageId)}/{action}";
        using var request = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(new { comment = comment ?? string.Empty }),
        };
        request.Options.Set(HttpRequestOptionsKeys.MailboxId, _mailboxId);

        using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        using var document = System.Text.Json.JsonDocument.Parse(body);
        var id = document.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Graph createReply/createReplyAll response is missing 'id'.");

        return new DraftMessage(id, body);
    }
}
