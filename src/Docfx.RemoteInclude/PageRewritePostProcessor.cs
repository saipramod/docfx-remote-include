namespace Docfx.RemoteInclude;

/// <summary>
/// Request sent to the transform service after the page is fully assembled.
/// </summary>
/// <param name="Content">The fully-rendered page content (all remote includes resolved).</param>
/// <param name="Source">The source path of the page being built.</param>
/// <param name="Metadata">Metadata hints extracted from the page's YAML frontmatter <c>transform:</c> block.</param>
public readonly record struct PageTransformRequest(
    string Content,
    string? Source,
    PageTransformMetadata Metadata);

/// <summary>
/// Response from the transform service.
/// </summary>
/// <param name="Content">The transformed page content.</param>
/// <param name="Diagnostics">Optional diagnostics/messages from the service (logged as warnings).</param>
public readonly record struct PageTransformResponse(
    string Content,
    IReadOnlyList<string>? Diagnostics);

/// <summary>
/// Metadata hints extracted from YAML frontmatter. The service decides how to interpret these.
/// </summary>
public sealed class PageTransformMetadata
{
    /// <summary>Target audience (e.g. "engineer", "pm", "executive").</summary>
    public string? Audience { get; init; }

    /// <summary>Page intent (e.g. "onboarding", "troubleshooting", "reference").</summary>
    public string? Intent { get; init; }

    /// <summary>Sections to override or skip. Keys are section names, values are instructions.</summary>
    public IReadOnlyDictionary<string, string>? Overrides { get; init; }

    /// <summary>Any additional key-value hints the page author wants to pass to the service.</summary>
    public IReadOnlyDictionary<string, string>? Extra { get; init; }
}

/// <summary>
/// Centralized page transformation service. Owns all rules for tone, structure, and quality.
/// Pages pass metadata hints, but the service decides what to do.
/// <para>
/// Implementations may call an external HTTP service, run local rules, or delegate to an LLM
/// — the library is agnostic. A reference Docker-based implementation is provided.
/// </para>
/// </summary>
public interface IPageTransformService
{
    /// <summary>
    /// Transform the assembled page content according to the service's rules and the page's metadata hints.
    /// </summary>
    Task<PageTransformResponse> TransformAsync(PageTransformRequest request, CancellationToken cancellationToken = default);
}

