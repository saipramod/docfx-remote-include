using System.ClientModel;
using Azure.AI.OpenAI;
using Azure.Core;
using Azure.Identity;
using Docfx.RemoteInclude;
using OpenAI.Chat;

namespace Docfx.RemoteInclude.Cli;

/// <summary>
/// <see cref="IRewriteService"/> backed by Azure OpenAI chat completions.
/// </summary>
internal sealed class AzureOpenAIRewriteService : IRewriteService
{
    private const string SystemPrompt =
        "You rewrite documentation snippets to match the voice and style of the surrounding page. " +
        "Do not add or remove information. Preserve all technical facts, code blocks, links, image " +
        "references, and markdown structure exactly. Only adjust word choice, sentence rhythm, and tone. " +
        "Return only the rewritten markdown, with no commentary, code fence, or wrapping prose.";

    private readonly ChatClient _chat;

    public AzureOpenAIRewriteService(AiSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("ai.endpoint is required when ai is configured.");
        if (string.IsNullOrWhiteSpace(settings.Deployment))
            throw new InvalidOperationException("ai.deployment is required when ai is configured.");

        var endpoint = new Uri(settings.Endpoint);
        var auth = settings.Auth ?? new AuthSettings();
        var client = auth.Mode switch
        {
            AuthMode.Key => CreateWithApiKey(endpoint, auth),
            _            => new AzureOpenAIClient(endpoint, CreateTokenCredential(auth)),
        };

        _chat = client.GetChatClient(settings.Deployment);
    }

    public async Task<string> RewriteAsync(RewriteRequest request, CancellationToken cancellationToken = default)
    {
        var user = BuildUserMessage(request);
        var options = new ChatCompletionOptions { Temperature = 0f };

        var completion = await _chat.CompleteChatAsync(
            [
                new SystemChatMessage(SystemPrompt),
                new UserChatMessage(user),
            ],
            options,
            cancellationToken).ConfigureAwait(false);

        var text = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : string.Empty;
        return string.IsNullOrWhiteSpace(text) ? request.RemoteContent : text.Trim();
    }

    private static string BuildUserMessage(RewriteRequest request)
    {
        var hint = string.IsNullOrWhiteSpace(request.Hint) ? "match the voice of the surrounding page" : request.Hint.Trim();
        var ctx = string.IsNullOrWhiteSpace(request.Context) ? "(no surrounding context provided)" : request.Context.Trim();

        return $$"""
            Rewrite hint: {{hint}}

            Surrounding page context (do not echo this; use it only for voice/style):
            ---
            {{ctx}}
            ---

            Snippet to rewrite (source: {{request.Source}}):
            ---
            {{request.RemoteContent}}
            ---
            """;
    }

    private static AzureOpenAIClient CreateWithApiKey(Uri endpoint, AuthSettings auth)
    {
        if (string.IsNullOrWhiteSpace(auth.Value))
            throw new InvalidOperationException("ai.auth.value (the API key) is required when ai.auth.mode = 'key'.");
        return new AzureOpenAIClient(endpoint, new ApiKeyCredential(auth.Value));
    }

    private static TokenCredential CreateTokenCredential(AuthSettings auth) => auth.Mode switch
    {
        AuthMode.Default         => new DefaultAzureCredential(),
        AuthMode.ManagedIdentity => string.IsNullOrWhiteSpace(auth.Value)
                                        ? new ManagedIdentityCredential()
                                        : new ManagedIdentityCredential(auth.Value),
        AuthMode.Jwt             => string.IsNullOrWhiteSpace(auth.Value)
                                        ? throw new InvalidOperationException("ai.auth.value (the bearer token) is required when ai.auth.mode = 'jwt'.")
                                        : new StaticTokenCredential(auth.Value!),
        AuthMode.None            => throw new InvalidOperationException("ai.auth.mode = 'none' is not supported — Azure OpenAI requires authentication."),
        _                        => throw new InvalidOperationException($"Unsupported ai.auth.mode '{auth.Mode}'."),
    };
}

internal sealed class StaticTokenCredential : TokenCredential
{
    private readonly AccessToken _token;
    public StaticTokenCredential(string token) => _token = new AccessToken(token, DateTimeOffset.UtcNow.AddHours(1));
    public override AccessToken GetToken(TokenRequestContext context, CancellationToken cancellationToken) => _token;
    public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext context, CancellationToken cancellationToken) => new(_token);
}
