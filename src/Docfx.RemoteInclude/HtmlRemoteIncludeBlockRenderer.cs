using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;

namespace Docfx.RemoteInclude;

/// <summary>
/// Renderer for <see cref="RemoteIncludeBlock"/>. Fetches the markdown body via
/// <see cref="IRemoteContentClient"/>, parses it with the host pipeline, and writes the result inline.
/// </summary>
public sealed class HtmlRemoteIncludeBlockRenderer : HtmlObjectRenderer<RemoteIncludeBlock>
{
    private readonly IRemoteContentClient _client;
    private readonly MarkdownPipeline _pipeline;
    private readonly RemoteIncludeOptions _options;

    public HtmlRemoteIncludeBlockRenderer(
        IRemoteContentClient client,
        MarkdownPipeline pipeline,
        RemoteIncludeOptions options)
    {
        _client = client;
        _pipeline = pipeline;
        _options = options;
    }

    protected override void Write(HtmlRenderer renderer, RemoteIncludeBlock block)
    {
        var stack = RemoteIncludeContext.Stack;
        if (stack.Count >= _options.MaxDepth)
        {
            HandleError(renderer, block, $"Max remote-include depth ({_options.MaxDepth}) exceeded.");
            return;
        }
        if (stack.Contains(block.Source, StringComparer.Ordinal))
        {
            HandleError(renderer, block, $"Cycle detected for remote-include source '{block.Source}'.");
            return;
        }

        string? content;
        try
        {
            content = _client.GetMarkdownAsync(block.Source).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            HandleError(renderer, block, $"Failed to fetch '{block.Source}': {ex.Message}");
            return;
        }

        if (content is null)
        {
            HandleError(renderer, block, $"Remote source '{block.Source}' not found.");
            return;
        }

        stack.Push(block.Source);
        try
        {
            var document = Markdown.Parse(content, _pipeline);
            renderer.Render(document);
        }
        finally
        {
            stack.Pop();
        }
    }

    private void HandleError(HtmlRenderer renderer, RemoteIncludeBlock block, string message)
    {
        var log = _options.LogWarning ?? (m => Console.Error.WriteLine(m));
        log($"[remote-include] {message}");

        if (!_options.AllowMissing)
        {
            throw new InvalidOperationException(message);
        }

        renderer.Write("<div class=\"remote-include-error\" data-source=\"");
        renderer.WriteEscape(block.Source);
        renderer.Write("\">");
        renderer.WriteEscape(message);
        renderer.Write("</div>");
    }
}
