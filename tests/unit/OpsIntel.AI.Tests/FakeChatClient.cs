using Microsoft.Extensions.AI;

namespace OpsIntel.AI.Tests;

/// <summary>A canned <see cref="IChatClient"/> for extractor tests: always returns the JSON text it was constructed with, and records the last request for assertions (e.g. that no tools were passed — ADR-0013/ADR-0019 quarantine).</summary>
public sealed class FakeChatClient : IChatClient
{
    private readonly string _responseJson;

    public FakeChatClient(string responseJson)
    {
        _responseJson = responseJson;
    }

    public IReadOnlyList<ChatMessage>? LastMessages { get; private set; }

    public ChatOptions? LastOptions { get; private set; }

    public Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        LastMessages = messages.ToList();
        LastOptions = options;

        var response = new ChatResponse(new ChatMessage(ChatRole.Assistant, _responseJson));
        return Task.FromResult(response);
    }

    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
    {
        throw new NotSupportedException("Streaming is not used by the extraction pipeline.");
    }

    public object? GetService(Type serviceType, object? serviceKey = null) => null;

    public void Dispose()
    {
    }
}
