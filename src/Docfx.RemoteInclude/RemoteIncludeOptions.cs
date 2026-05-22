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
    /// Optional rewrite service. When set, directives with a non-empty <c>rewrite="..."</c> hint
    /// route fetched content through it before parsing.
    /// </summary>
    public IRewriteService? RewriteService { get; init; }

    /// <summary>
    /// How much of the host page to send as context for rewrites. Default <see cref="ContextStrategy.Section"/>.
    /// </summary>
    public ContextStrategy ContextStrategy { get; init; } = ContextStrategy.Section;
}
