using System.Net.Http.Headers;
using Azure.Core;
using Azure.Identity;
using Docfx.RemoteInclude;

namespace Docfx.RemoteInclude.Cli;

/// <summary>
/// Builds an <see cref="HttpAuthHandler"/> for the content service based on an <see cref="AuthSettings"/> block.
/// </summary>
internal static class ContentAuthHandlerFactory
{
    public static HttpAuthHandler Build(Uri baseUri, AuthSettings auth)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(auth);

        var scope = auth.Scope ?? DefaultScope(baseUri);

        return auth.Mode switch
        {
            AuthMode.None            => NoOp(),
            AuthMode.Jwt             => BearerLiteral(auth.Value
                                            ?? throw new InvalidOperationException("auth.value (the bearer token) is required when auth.mode = 'jwt'.")),
            AuthMode.Key             => ApiKey(auth.Value
                                            ?? throw new InvalidOperationException("auth.value (the API key) is required when auth.mode = 'key'.")),
            AuthMode.ManagedIdentity => BearerFromCredential(
                                            string.IsNullOrWhiteSpace(auth.Value)
                                                ? new ManagedIdentityCredential()
                                                : new ManagedIdentityCredential(auth.Value),
                                            scope),
            AuthMode.Default         => BearerFromCredential(new DefaultAzureCredential(), scope),
            _                        => throw new InvalidOperationException($"Unsupported auth.mode '{auth.Mode}'."),
        };
    }

    private static HttpAuthHandler NoOp()
        => (_, _) => ValueTask.CompletedTask;

    private static string DefaultScope(Uri baseUri)
        => $"{baseUri.GetLeftPart(UriPartial.Authority)}/.default";

    private static HttpAuthHandler BearerLiteral(string token)
        => (request, _) =>
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return ValueTask.CompletedTask;
        };

    private static HttpAuthHandler ApiKey(string key)
        => (request, _) =>
        {
            request.Headers.TryAddWithoutValidation("X-API-Key", key);
            return ValueTask.CompletedTask;
        };

    private static HttpAuthHandler BearerFromCredential(TokenCredential credential, string scope)
        => async (request, ct) =>
        {
            var token = await credential.GetTokenAsync(new TokenRequestContext([scope]), ct).ConfigureAwait(false);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        };
}
