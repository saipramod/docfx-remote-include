using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Core;
using Azure.Identity;
using Docfx.RemoteInclude;

namespace Docfx.RemoteInclude.Cli;

/// <summary>
/// <see cref="IPageTransformService"/> that calls an external HTTP service for page transformation.
/// The service owns all rules — the client just passes assembled content and metadata.
/// </summary>
internal sealed class HttpPageTransformService : IPageTransformService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly Uri _endpoint;

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public HttpPageTransformService(TransformSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        if (string.IsNullOrWhiteSpace(settings.Endpoint))
            throw new InvalidOperationException("transform.endpoint is required.");

        _endpoint = new Uri(settings.Endpoint);
        _httpClient = new HttpClient();

        if (settings.Auth is { } auth && auth.Mode != AuthMode.None)
        {
            ConfigureAuth(auth);
        }
    }

    public async Task<PageTransformResponse> TransformAsync(PageTransformRequest request, CancellationToken cancellationToken = default)
    {
        var payload = new TransformRequestPayload
        {
            Content = request.Content,
            Source = request.Source,
            Metadata = request.Metadata,
        };

        var response = await _httpClient.PostAsJsonAsync(
            _endpoint, payload, s_jsonOptions, cancellationToken).ConfigureAwait(false);

        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<TransformResponsePayload>(
            s_jsonOptions, cancellationToken).ConfigureAwait(false);

        return new PageTransformResponse(
            Content: result?.Content ?? request.Content,
            Diagnostics: result?.Diagnostics);
    }

    public void Dispose() => _httpClient.Dispose();

    private void ConfigureAuth(AuthSettings auth)
    {
        // Pre-configure auth header for simple modes; token-based modes will need refresh logic
        if (auth.Mode == AuthMode.Key && !string.IsNullOrWhiteSpace(auth.Value))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", auth.Value);
        }
    }

    private sealed class TransformRequestPayload
    {
        public string Content { get; init; } = "";
        public string? Source { get; init; }
        public PageTransformMetadata? Metadata { get; init; }
    }

    private sealed class TransformResponsePayload
    {
        public string? Content { get; init; }
        public List<string>? Diagnostics { get; init; }
    }
}
