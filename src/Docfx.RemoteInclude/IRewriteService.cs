namespace Docfx.RemoteInclude;

/// <summary>
/// Inputs passed to <see cref="IRewriteService.RewriteAsync"/>.
/// </summary>
/// <param name="RemoteContent">The markdown body fetched from the content service.</param>
/// <param name="Hint">The verbatim value of the directive's <c>rewrite="..."</c> attribute.</param>
/// <param name="Context">Surrounding markdown from the host page, per the configured <see cref="ContextStrategy"/>. May be empty.</param>
/// <param name="Source">The directive's <c>source</c> value, for diagnostics.</param>
public readonly record struct RewriteRequest(string RemoteContent, string Hint, string Context, string Source);

/// <summary>
/// Rewrites fetched remote content using an LLM to match the surrounding page's voice/style.
/// Implementations are responsible for prompt construction, auth, and transport.
/// </summary>
public interface IRewriteService
{
    /// <summary>
    /// Rewrite <paramref name="request"/>.<see cref="RewriteRequest.RemoteContent"/> per the hint and context.
    /// Returns markdown that will replace the original content for rendering.
    /// </summary>
    Task<string> RewriteAsync(RewriteRequest request, CancellationToken cancellationToken = default);
}
