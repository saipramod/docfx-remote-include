using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.RemoteInclude;

/// <summary>
/// Renderer for <see cref="RemoteIncludeInline"/>. Fetches the markdown body, requires the
/// payload to parse to a single paragraph, and writes only that paragraph's inline children
/// (no wrapping <c>&lt;p&gt;</c>). Rejects payloads containing block-level constructs.
/// </summary>
public sealed class HtmlRemoteIncludeInlineRenderer : HtmlObjectRenderer<RemoteIncludeInline>
{
    private readonly IRemoteContentClient _client;
    private readonly MarkdownPipeline _pipeline;
    private readonly RemoteIncludeOptions _options;

    public HtmlRemoteIncludeInlineRenderer(
        IRemoteContentClient client,
        MarkdownPipeline pipeline,
        RemoteIncludeOptions options)
    {
        _client = client;
        _pipeline = pipeline;
        _options = options;
    }

    protected override void Write(HtmlRenderer renderer, RemoteIncludeInline inline)
    {
        var stack = RemoteIncludeContext.Stack;
        if (stack.Count >= _options.MaxDepth)
        {
            HandleError(renderer, inline, $"Max remote-include depth ({_options.MaxDepth}) exceeded.");
            return;
        }
        if (stack.Contains(inline.Source, StringComparer.Ordinal))
        {
            HandleError(renderer, inline, $"Cycle detected for remote-include source '{inline.Source}'.");
            return;
        }

        string? content;
        try
        {
            content = _client.GetMarkdownAsync(inline.Source).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            HandleError(renderer, inline, $"Failed to fetch '{inline.Source}': {ex.Message}");
            return;
        }

        if (content is null)
        {
            HandleError(renderer, inline, $"Remote source '{inline.Source}' not found.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(inline.RewriteHint) && _options.RewriteService is { } rewriter)
        {
            try
            {
                var context = SectionContext.Extract(inline, _options.ContextStrategy);
                content = rewriter.RewriteAsync(
                    new RewriteRequest(content, inline.RewriteHint, context, inline.Source))
                    .GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                HandleError(renderer, inline, $"Rewrite failed for '{inline.Source}': {ex.Message}");
                return;
            }
        }

        stack.Push(inline.Source);
        try
        {
            var document = Markdown.Parse(content, _pipeline);

            if (document.Count == 0)
            {
                return;
            }

            if (document.Count != 1 || document[0] is not ParagraphBlock paragraph)
            {
                HandleError(
                    renderer,
                    inline,
                    $"Remote source '{inline.Source}' contains block-level content and cannot be used in an inline directive.");
                return;
            }

            if (paragraph.Inline != null)
            {
                renderer.WriteChildren(paragraph.Inline);
            }
        }
        finally
        {
            stack.Pop();
        }
    }

    private void HandleError(HtmlRenderer renderer, RemoteIncludeInline inline, string message)
    {
        var log = _options.LogWarning ?? (m => Console.Error.WriteLine(m));
        log($"[remote-include] {message}");

        if (!_options.AllowMissing)
        {
            throw new InvalidOperationException(message);
        }

        renderer.Write("<span class=\"remote-include-error\" data-source=\"");
        renderer.WriteEscape(inline.Source);
        renderer.Write("\">");
        renderer.WriteEscape(message);
        renderer.Write("</span>");
    }
}
