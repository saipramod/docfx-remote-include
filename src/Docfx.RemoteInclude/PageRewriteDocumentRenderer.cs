using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

namespace Docfx.RemoteInclude;

/// <summary>
/// A document-level renderer that sends the fully-assembled page through an
/// <see cref="IPageTransformService"/> for centralized content transformation.
/// The service owns all transformation rules (tone, structure, quality).
/// Pages may pass metadata hints via frontmatter, but the service decides what to do.
/// </summary>
internal sealed class PageRewriteDocumentRenderer : HtmlObjectRenderer<MarkdownDocument>
{
    private readonly IPageTransformService _transformService;
    private readonly RemoteIncludeOptions _options;

    public PageRewriteDocumentRenderer(IPageTransformService transformService, RemoteIncludeOptions options)
    {
        _transformService = transformService;
        _options = options;
    }

    protected override void Write(HtmlRenderer renderer, MarkdownDocument document)
    {
        var metadata = PageMetadataExtractor.Extract(document);

        // Render the full page to a buffer first
        using var buffer = new StringWriter();
        var bufferRenderer = new HtmlRenderer(buffer);

        // Copy object renderers from the main renderer (except this one to avoid recursion)
        foreach (var objRenderer in renderer.ObjectRenderers)
        {
            if (objRenderer is not PageRewriteDocumentRenderer)
            {
                bufferRenderer.ObjectRenderers.Add(objRenderer);
            }
        }

        bufferRenderer.WriteChildren(document);
        buffer.Flush();
        var renderedContent = buffer.ToString();

        // Send to transform service
        try
        {
            var request = new PageTransformRequest(
                Content: renderedContent,
                Source: RemoteIncludeContext.CurrentSource,
                Metadata: metadata);

            var result = _transformService.TransformAsync(request).GetAwaiter().GetResult();

            // Log any diagnostics from the service
            if (result.Diagnostics is { Count: > 0 } diagnostics)
            {
                var log = _options.LogWarning ?? (m => Console.Error.WriteLine(m));
                foreach (var diag in diagnostics)
                {
                    log($"[transform] {diag}");
                }
            }

            renderer.Write(result.Content);
        }
        catch (Exception ex)
        {
            var log = _options.LogWarning ?? (m => Console.Error.WriteLine(m));
            log($"[transform] Page transform failed: {ex.Message}");

            if (!_options.AllowMissing)
            {
                throw new InvalidOperationException($"Page transform failed: {ex.Message}", ex);
            }

            // Fall back to original rendered content
            renderer.Write(renderedContent);
        }
    }
}
