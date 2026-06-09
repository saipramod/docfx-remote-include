namespace Docfx.RemoteInclude;

/// <summary>
/// Behavior knobs for <see cref="RemoteIncludeExtension"/>.
/// </summary>
public sealed class RemoteIncludeOptions
{
    /// <summary>
    /// If true, fetch failures emit a visible HTML placeholder and a warning instead of throwing.
    /// Default: false (hard fail).
    /// </summary>
    public bool AllowMissing { get; init; }

    /// <summary>
    /// Maximum recursion depth for nested remote includes. Default 8.
    /// </summary>
    public int MaxDepth { get; init; } = 8;

    /// <summary>
    /// Optional sink for diagnostic messages. Defaults to <see cref="Console.Error"/>.
    /// </summary>
    public Action<string>? LogWarning { get; init; }

    /// <summary>
    /// Optional page transform service. When set, the fully-assembled page is sent through
    /// this service after all remote includes are resolved. The service owns transformation
    /// rules; pages pass metadata hints via frontmatter.
    /// </summary>
    public IPageTransformService? PageTransformService { get; init; }
}
