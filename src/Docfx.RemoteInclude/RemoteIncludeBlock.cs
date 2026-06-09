using Markdig.Syntax;

namespace Docfx.RemoteInclude;

/// <summary>
/// Markdig AST node produced by <see cref="RemoteIncludeBracketBlockParser"/>.
/// Resolution happens later in <see cref="HtmlRemoteIncludeBlockRenderer"/>.
/// </summary>
public sealed class RemoteIncludeBlock : LeafBlock
{
    public RemoteIncludeBlock(Markdig.Parsers.BlockParser? parser) : base(parser)
    {
    }

    /// <summary>Verbatim <c>source</c> attribute as authored.</summary>
    public string Source { get; init; } = string.Empty;

    /// <summary>Additional key/value attributes from the directive (reserved for future use).</summary>
    public IReadOnlyDictionary<string, string>? Attributes { get; init; }
}
