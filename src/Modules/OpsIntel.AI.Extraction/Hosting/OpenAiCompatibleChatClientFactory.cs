using System.ClientModel;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;

namespace OpsIntel.AI.Extraction.Hosting;

/// <summary>
/// Builds an <see cref="IChatClient"/> against any OpenAI-compatible chat-completions
/// endpoint reachable at a configurable base URL — Foundry Local's own optional in-process
/// REST endpoint, or Ollama's native OpenAI-compatible API (AI notes §1: "Ollama ... exposes
/// OpenAI-compatible APIs"). This is the cross-platform default
/// (<see cref="ModelHostingMode.OpenAiCompatibleEndpoint"/>): it has no native/binary
/// dependency at all, just an HTTP client, which is why it is the safe fallback documented in
/// SPIKE-FOUNDRY-LOCAL.md for any machine where the in-process Foundry Local SDK isn't viable.
/// </summary>
public sealed class OpenAiCompatibleChatClientFactory
{
    private readonly ExtractionOptions _options;

    public OpenAiCompatibleChatClientFactory(IOptions<ExtractionOptions> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _options = options.Value;
    }

    public IChatClient Create()
    {
        var clientOptions = new OpenAIClientOptions
        {
            Endpoint = new Uri(_options.OpenAiCompatibleBaseUrl),
        };

        var credential = new ApiKeyCredential(string.IsNullOrEmpty(_options.OpenAiCompatibleApiKey) ? "local" : _options.OpenAiCompatibleApiKey);
        var chatClient = new ChatClient(_options.OpenAiCompatibleModelId, credential, clientOptions);
        return chatClient.AsIChatClient();
    }
}
