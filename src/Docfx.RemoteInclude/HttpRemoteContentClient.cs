using System.Net;
using System.Net.Http.Headers;

namespace Docfx.RemoteInclude;

/// <summary>
/// Delegate that mutates an outgoing <see cref="HttpRequestMessage"/> to apply authentication
/// (e.g. set an <c>Authorization</c> or API-key header).
/// </summary>
public delegate ValueTask HttpAuthHandler(HttpRequestMessage request, CancellationToken cancellationToken);

/// <summary>
/// Default <see cref="IRemoteContentClient"/> that fetches markdown over HTTP.
/// Caches successful responses in-process for the lifetime of the client (one per build).
/// </summary>
public sealed class HttpRemoteContentClient : IRemoteContentClient, IDisposable
{
    private readonly HttpClient _http;
    private readonly HttpAuthHandler _authHandler;
    private readonly Func<string, string> _urlBuilder;
    private readonly SemaphoreSlim _gate;
    private readonly Dictionary<string, Task<string?>> _cache = new(StringComparer.Ordinal);
    private readonly object _cacheLock = new();
    private readonly bool _ownsHttpClient;

    /// <summary>
    /// Bearer-token convenience constructor. Each request gets <c>Authorization: Bearer {token}</c>
    /// from <paramref name="tokenProvider"/>.
    /// </summary>
    public HttpRemoteContentClient(
        Uri baseUri,
        Func<CancellationToken, ValueTask<string>> tokenProvider,
        int maxParallelism = 8,
        HttpClient? httpClient = null,
        string? urlTemplate = null)
        : this(baseUri, BearerHandler(tokenProvider), maxParallelism, httpClient, urlTemplate)
    {
        ArgumentNullException.ThrowIfNull(tokenProvider);
    }

    /// <summary>
    /// Construct with an arbitrary auth handler that mutates each request before it is sent.
    /// </summary>
    public HttpRemoteContentClient(
        Uri baseUri,
        HttpAuthHandler authHandler,
        int maxParallelism = 8,
        HttpClient? httpClient = null,
        string? urlTemplate = null)
    {
        ArgumentNullException.ThrowIfNull(baseUri);
        ArgumentNullException.ThrowIfNull(authHandler);

        if (httpClient is null)
        {
            _http = new HttpClient { BaseAddress = baseUri, Timeout = TimeSpan.FromMinutes(2) };
            _ownsHttpClient = true;
        }
        else
        {
            _http = httpClient;
            _http.BaseAddress ??= baseUri;
            _ownsHttpClient = false;
        }

        _authHandler = authHandler;
        _urlBuilder = BuildUrlBuilder(urlTemplate);
        _gate = new SemaphoreSlim(Math.Max(1, maxParallelism));
    }

    public Task<string?> GetMarkdownAsync(string source, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        lock (_cacheLock)
        {
            if (_cache.TryGetValue(source, out var existing))
            {
                return existing;
            }

            var task = FetchAsync(source, cancellationToken);
            _cache[source] = task;
            return task;
        }
    }

    private async Task<string?> FetchAsync(string source, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var requestUri = _urlBuilder(source);
            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            await _authHandler(request, cancellationToken).ConfigureAwait(false);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/markdown"));
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

            using var response = await _http.SendAsync(request, cancellationToken).ConfigureAwait(false);
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    private static HttpAuthHandler BearerHandler(Func<CancellationToken, ValueTask<string>> tokenProvider)
        => async (request, ct) =>
        {
            var token = await tokenProvider(ct).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        };

    /// <summary>
    /// Builds a function that maps a directive source to a request URI.
    /// When <paramref name="urlTemplate"/> is null the source is used as-is (relative path).
    /// Otherwise <c>{source}</c> is replaced with the URL-encoded source value.
    /// </summary>
    internal static Func<string, string> BuildUrlBuilder(string? urlTemplate)
    {
        if (string.IsNullOrWhiteSpace(urlTemplate))
        {
            return source => source;
        }

        return source => urlTemplate.Replace("{source}", Uri.EscapeDataString(source), StringComparison.OrdinalIgnoreCase);
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _http.Dispose();
        }
        _gate.Dispose();
    }
}

