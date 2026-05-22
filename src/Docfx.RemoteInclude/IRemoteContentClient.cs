namespace Docfx.RemoteInclude;

/// <summary>
/// Abstraction over the upstream content service. Implementations are responsible
/// for authentication, caching, and concurrency control.
/// </summary>
public interface IRemoteContentClient
{
    /// <summary>
    /// Fetch the markdown body for <paramref name="source"/>.
    /// </summary>
    /// <param name="source">Source identifier as written in the directive (e.g. a path or full URL).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Markdown body, or <c>null</c> if not found.</returns>
    Task<string?> GetMarkdownAsync(string source, CancellationToken cancellationToken = default);
}
