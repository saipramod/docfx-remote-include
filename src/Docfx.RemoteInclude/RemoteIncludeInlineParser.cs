using Markdig.Helpers;
using Markdig.Parsers;

namespace Docfx.RemoteInclude;

/// <summary>
/// Parses an inline directive of the form <c>[!remoteinclude[title](source)]</c> appearing
/// inside paragraph text.
/// </summary>
public sealed class RemoteIncludeInlineParser : InlineParser
{
    public RemoteIncludeInlineParser()
    {
        OpeningCharacters = ['['];
    }

    public override bool Match(InlineProcessor processor, ref StringSlice slice)
    {
        if (!RemoteIncludeDirective.TryParseBracket(slice.Text, slice.Start, slice.End, out var consumedEnd, out var source, out var title))
        {
            return false;
        }

        slice.Start = consumedEnd;

        processor.Inline = new RemoteIncludeInline
        {
            Source = source,
            Title = title,
            OwningBlock = processor.Block,
        };
        return true;
    }
}
