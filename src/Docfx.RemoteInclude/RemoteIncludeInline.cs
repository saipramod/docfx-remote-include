using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.RemoteInclude;

/// <summary>
/// Markdig inline AST node produced by <see cref="RemoteIncludeInlineParser"/>.
/// Resolution happens in <see cref="HtmlRemoteIncludeInlineRenderer"/>.
/// </summary>
public sealed class RemoteIncludeInline : Inline
{
    public string Source { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;

    /// <summary>Block that owns this inline. Set at parse time so renderers can find surrounding context.</summary>
    public LeafBlock? OwningBlock { get; init; }
}
