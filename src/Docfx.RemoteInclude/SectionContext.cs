using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Docfx.RemoteInclude;

/// <summary>
/// Extracts surrounding markdown context from the host document for a given directive,
/// per the configured <see cref="ContextStrategy"/>.
/// </summary>
internal static class SectionContext
{
    public static string Extract(Block target, ContextStrategy strategy)
    {
        if (strategy == ContextStrategy.None || target is null) return string.Empty;

        var topLevel = FindTopLevelAncestor(target, out var doc);
        if (doc is null || topLevel is null) return string.Empty;

        if (strategy == ContextStrategy.Page)
        {
            return RenderBlocks(doc, 0, doc.Count, skip: topLevel);
        }

        var idx = doc.IndexOf(topLevel);
        if (idx < 0) return string.Empty;

        var (start, end) = FindSectionRange(doc, idx);
        return RenderBlocks(doc, start, end, skip: topLevel);
    }

    public static string Extract(Inline target, ContextStrategy strategy)
    {
        if (strategy == ContextStrategy.None || target is null) return string.Empty;
        var block = (target as RemoteIncludeInline)?.OwningBlock;
        return block is null ? string.Empty : Extract(block, strategy);
    }

    private static Block? FindTopLevelAncestor(Block block, out MarkdownDocument? doc)
    {
        doc = null;
        Block current = block;
        while (current.Parent is ContainerBlock parent)
        {
            if (parent is MarkdownDocument document)
            {
                doc = document;
                return current;
            }
            current = parent;
        }
        return null;
    }

    private static (int start, int end) FindSectionRange(MarkdownDocument doc, int targetIndex)
    {
        // Find nearest preceding heading (inclusive).
        int headingIndex = -1;
        int headingLevel = 7;
        for (int i = targetIndex - 1; i >= 0; i--)
        {
            if (doc[i] is HeadingBlock h)
            {
                headingIndex = i;
                headingLevel = h.Level;
                break;
            }
        }

        int start = headingIndex < 0 ? 0 : headingIndex;
        int end = doc.Count;
        for (int i = Math.Max(targetIndex + 1, start + 1); i < doc.Count; i++)
        {
            if (doc[i] is HeadingBlock nh && nh.Level <= headingLevel)
            {
                end = i;
                break;
            }
        }
        return (start, end);
    }

    private static string RenderBlocks(MarkdownDocument doc, int start, int end, Block? skip)
    {
        using var sw = new StringWriter();
        var renderer = new NormalizeRenderer(sw);
        for (int i = start; i < end; i++)
        {
            var b = doc[i];
            if (ReferenceEquals(b, skip)) continue;
            renderer.Render(b);
        }
        return sw.ToString().Trim();
    }
}
