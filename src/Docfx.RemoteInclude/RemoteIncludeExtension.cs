using Markdig;
using Markdig.Renderers;

namespace Docfx.RemoteInclude;

/// <summary>
/// Markdig extension that recognizes <c>[!remoteinclude[title](source "hint")]</c> directives
/// (block and inline) and inlines markdown fetched from an <see cref="IRemoteContentClient"/>.
/// </summary>
public sealed class RemoteIncludeExtension : IMarkdownExtension
{
    private readonly IRemoteContentClient _client;
    private readonly RemoteIncludeOptions _options;

    public RemoteIncludeExtension(IRemoteContentClient client, RemoteIncludeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(client);
        _client = client;
        _options = options ?? new RemoteIncludeOptions();
    }

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.BlockParsers.Contains<RemoteIncludeBracketBlockParser>())
        {
            pipeline.BlockParsers.Insert(0, new RemoteIncludeBracketBlockParser());
        }
        if (!pipeline.InlineParsers.Contains<RemoteIncludeInlineParser>())
        {
            // Insert before LinkInlineParser so '[!remoteinclude...' isn't consumed as a link.
            pipeline.InlineParsers.Insert(0, new RemoteIncludeInlineParser());
        }
    }

    public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
    {
        if (renderer is not HtmlRenderer html)
        {
            return;
        }
        if (!html.ObjectRenderers.Contains<HtmlRemoteIncludeBlockRenderer>())
        {
            html.ObjectRenderers.Add(new HtmlRemoteIncludeBlockRenderer(_client, pipeline, _options));
        }
        if (!html.ObjectRenderers.Contains<HtmlRemoteIncludeInlineRenderer>())
        {
            html.ObjectRenderers.Add(new HtmlRemoteIncludeInlineRenderer(_client, pipeline, _options));
        }
    }
}

/// <summary>
/// Convenience extension method for fluent registration.
/// </summary>
public static class RemoteIncludePipelineExtensions
{
    public static MarkdownPipelineBuilder UseRemoteInclude(
        this MarkdownPipelineBuilder pipeline,
        IRemoteContentClient client,
        RemoteIncludeOptions? options = null)
    {
        pipeline.Extensions.AddIfNotAlready(new RemoteIncludeExtension(client, options));
        return pipeline;
    }
}
