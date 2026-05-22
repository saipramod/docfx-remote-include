using Markdig.Parsers;

namespace Docfx.RemoteInclude;

/// <summary>
/// Parses a whole-line directive of the form <c>[!remoteinclude[title](source)]</c>.
/// If the directive appears mid-line (mixed with other text), the inline parser handles it instead.
/// </summary>
public sealed class RemoteIncludeBracketBlockParser : BlockParser
{
    public RemoteIncludeBracketBlockParser()
    {
        OpeningCharacters = ['['];
    }

    public override BlockState TryOpen(BlockProcessor processor)
    {
        if (processor.IsCodeIndent)
        {
            return BlockState.None;
        }

        var line = processor.Line.ToString();
        var trimmed = line.TrimEnd();

        if (!RemoteIncludeDirective.TryParseBracket(trimmed, 0, trimmed.Length - 1, out var consumed, out var source, out var title, out var hint))
        {
            return BlockState.None;
        }

        if (consumed != trimmed.Length)
        {
            return BlockState.None;
        }

        Dictionary<string, string>? attrs = null;
        if (!string.IsNullOrEmpty(title))
        {
            attrs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["title"] = title };
        }

        var block = new RemoteIncludeBlock(this)
        {
            Source = source,
            RewriteHint = hint,
            Attributes = attrs,
            Column = processor.Column,
            Span = { Start = processor.Start, End = processor.Line.End },
        };

        processor.NewBlocks.Push(block);
        return BlockState.BreakDiscard;
    }
}
