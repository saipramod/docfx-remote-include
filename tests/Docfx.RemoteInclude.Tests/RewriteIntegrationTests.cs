using System.Collections.Concurrent;
using Markdig;
using Xunit;

namespace Docfx.RemoteInclude.Tests;

public class RewriteIntegrationTests
{
    private sealed class FakeClient : IRemoteContentClient
    {
        public ConcurrentDictionary<string, string?> Map { get; } = new();
        public Task<string?> GetMarkdownAsync(string source, CancellationToken cancellationToken = default)
        {
            Map.TryGetValue(source, out var value);
            return Task.FromResult(value);
        }
    }

    private sealed class FakeRewriter : IRewriteService
    {
        public List<RewriteRequest> Calls { get; } = new();
        public Func<RewriteRequest, string> Respond { get; init; } = r => $"[rewritten:{r.Hint}] {r.RemoteContent}";

        public Task<string> RewriteAsync(RewriteRequest request, CancellationToken cancellationToken = default)
        {
            Calls.Add(request);
            return Task.FromResult(Respond(request));
        }
    }

    private static MarkdownPipeline BuildPipeline(IRemoteContentClient client, IRewriteService? rewriter, ContextStrategy ctx = ContextStrategy.Section)
        => new MarkdownPipelineBuilder()
            .UseRemoteInclude(client, new RemoteIncludeOptions { RewriteService = rewriter, ContextStrategy = ctx })
            .Build();

    [Fact]
    public void Block_directive_without_hint_does_not_call_rewriter()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "**hello**";
        var rewriter = new FakeRewriter();

        var pipeline = BuildPipeline(client, rewriter);
        var html = Markdown.ToHtml("[!remoteinclude[s](snippet.md)]", pipeline);

        Assert.Empty(rewriter.Calls);
        Assert.Contains("<strong>hello</strong>", html);
    }

    [Fact]
    public void Block_directive_with_hint_calls_rewriter_and_uses_result()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "original text";
        var rewriter = new FakeRewriter
        {
            Respond = _ => "**replaced** content",
        };

        var pipeline = BuildPipeline(client, rewriter);
        var html = Markdown.ToHtml("[!remoteinclude[s](snippet.md \"formal tone\")]", pipeline);

        Assert.Single(rewriter.Calls);
        Assert.Equal("formal tone", rewriter.Calls[0].Hint);
        Assert.Equal("original text", rewriter.Calls[0].RemoteContent);
        Assert.Equal("snippet.md", rewriter.Calls[0].Source);
        Assert.Contains("<strong>replaced</strong>", html);
        Assert.DoesNotContain("original text", html);
    }

    [Fact]
    public void Inline_bracket_directive_with_hint_calls_rewriter()
    {
        var client = new FakeClient();
        client.Map["status.md"] = "green";
        var rewriter = new FakeRewriter { Respond = _ => "**RED**" };

        var pipeline = BuildPipeline(client, rewriter);
        var html = Markdown.ToHtml("Status is [!remoteinclude[s](status.md \"shouty\")] today.", pipeline);

        Assert.Single(rewriter.Calls);
        Assert.Equal("shouty", rewriter.Calls[0].Hint);
        Assert.Contains("<strong>RED</strong>", html);
    }

    [Fact]
    public void Bracket_block_directive_with_hint_calls_rewriter()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "x";
        var rewriter = new FakeRewriter { Respond = _ => "y" };

        var pipeline = BuildPipeline(client, rewriter);
        var html = Markdown.ToHtml("[!remoteinclude[t](snippet.md \"concise\")]", pipeline);

        Assert.Single(rewriter.Calls);
        Assert.Equal("concise", rewriter.Calls[0].Hint);
        Assert.Contains("<p>y</p>", html);
    }

    [Fact]
    public void Context_strategy_section_includes_nearest_heading()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "body";
        var rewriter = new FakeRewriter { Respond = r => r.RemoteContent };

        var pipeline = BuildPipeline(client, rewriter, ContextStrategy.Section);
        var page = """
            # Page Title

            ## First Section

            Some paragraph here.

            [!remoteinclude[s](snippet.md "match")]

            ## Second Section

            Other text.
            """;
        Markdown.ToHtml(page, pipeline);

        Assert.Single(rewriter.Calls);
        var ctx = rewriter.Calls[0].Context;
        Assert.Contains("First Section", ctx);
        Assert.Contains("Some paragraph here", ctx);
        Assert.DoesNotContain("Second Section", ctx);
        Assert.DoesNotContain("Page Title", ctx);
    }

    [Fact]
    public void Context_strategy_none_passes_empty_context()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "body";
        var rewriter = new FakeRewriter { Respond = r => r.RemoteContent };

        var pipeline = BuildPipeline(client, rewriter, ContextStrategy.None);
        Markdown.ToHtml("# H\n\n[!remoteinclude[s](snippet.md \"x\")]", pipeline);

        Assert.Equal(string.Empty, rewriter.Calls[0].Context);
    }

    [Fact]
    public void Rewrite_failure_propagates_when_AllowMissing_false()
    {
        var client = new FakeClient();
        client.Map["snippet.md"] = "x";
        var rewriter = new FakeRewriter
        {
            Respond = _ => throw new InvalidOperationException("model down"),
        };

        var pipeline = new MarkdownPipelineBuilder()
            .UseRemoteInclude(client, new RemoteIncludeOptions { RewriteService = rewriter, LogWarning = _ => { } })
            .Build();

        Assert.Throws<InvalidOperationException>(() =>
            Markdown.ToHtml("[!remoteinclude[s](snippet.md \"x\")]", pipeline));
    }
}
