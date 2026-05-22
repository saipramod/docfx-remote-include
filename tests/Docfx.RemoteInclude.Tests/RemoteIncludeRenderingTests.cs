using System.Collections.Concurrent;
using Markdig;
using Xunit;

namespace Docfx.RemoteInclude.Tests;

public class RemoteIncludeRenderingTests
{
    private sealed class FakeClient : IRemoteContentClient
    {
        public ConcurrentDictionary<string, string?> Map { get; } = new();
        public int CallCount;

        public Task<string?> GetMarkdownAsync(string source, CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref CallCount);
            Map.TryGetValue(source, out var value);
            return Task.FromResult(value);
        }
    }

    private static MarkdownPipeline BuildPipeline(IRemoteContentClient client, RemoteIncludeOptions? opts = null)
        => new MarkdownPipelineBuilder()
            .UseRemoteInclude(client, opts)
            .Build();

    [Fact]
    public void Block_form_inlines_fetched_markdown()
    {
        var client = new FakeClient();
        client.Map["intro.md"] = "# Hello\n\nWorld.";

        var pipeline = BuildPipeline(client);
        var html = Markdown.ToHtml("[!remoteinclude[Intro](intro.md)]", pipeline);

        Assert.Contains("<h1", html);
        Assert.Contains("Hello", html);
        Assert.Contains("<p>World.</p>", html);
        Assert.DoesNotContain("remoteinclude", html);
    }

    [Fact]
    public void Block_form_missing_source_throws_by_default()
    {
        var client = new FakeClient();
        var pipeline = BuildPipeline(client);

        Assert.Throws<InvalidOperationException>(() =>
            Markdown.ToHtml("[!remoteinclude[x](nope.md)]", pipeline));
    }

    [Fact]
    public void Block_form_missing_source_emits_placeholder_when_allowed()
    {
        var client = new FakeClient();
        var pipeline = BuildPipeline(client, new RemoteIncludeOptions { AllowMissing = true, LogWarning = _ => { } });

        var html = Markdown.ToHtml("[!remoteinclude[x](nope.md)]", pipeline);
        Assert.Contains("<div class=\"remote-include-error\"", html);
        Assert.Contains("nope.md", html);
    }

    [Fact]
    public void Block_form_detects_cycles()
    {
        var client = new FakeClient();
        client.Map["a.md"] = "[!remoteinclude[a](a.md)]";

        var pipeline = BuildPipeline(client);
        Assert.Throws<InvalidOperationException>(() =>
            Markdown.ToHtml("[!remoteinclude[a](a.md)]", pipeline));
    }

    [Fact]
    public void Block_form_supports_nested_includes()
    {
        var client = new FakeClient();
        client.Map["outer.md"] = "Before\n\n[!remoteinclude[inner](inner.md)]\n\nAfter";
        client.Map["inner.md"] = "**bold**";

        var pipeline = BuildPipeline(client);
        var html = Markdown.ToHtml("[!remoteinclude[outer](outer.md)]", pipeline);

        Assert.Contains("Before", html);
        Assert.Contains("<strong>bold</strong>", html);
        Assert.Contains("After", html);
    }

    [Fact]
    public void Inline_form_splices_inline_content()
    {
        var client = new FakeClient();
        client.Map["status.md"] = "**green**";

        var pipeline = BuildPipeline(client);
        var html = Markdown.ToHtml("Build is [!remoteinclude[status](status.md)] right now.", pipeline);

        Assert.Contains("<p>Build is <strong>green</strong> right now.</p>", html);
    }

    [Fact]
    public void Inline_form_rejects_block_content()
    {
        var client = new FakeClient();
        client.Map["docs.md"] = "# Heading\n\nParagraph.";

        var pipeline = BuildPipeline(client);
        Assert.Throws<InvalidOperationException>(() =>
            Markdown.ToHtml("See [!remoteinclude[docs](docs.md)] for details.", pipeline));
    }

    [Fact]
    public void Inline_form_rejects_block_content_with_span_placeholder()
    {
        var client = new FakeClient();
        client.Map["docs.md"] = "# Heading\n\nParagraph.";

        var pipeline = BuildPipeline(client, new RemoteIncludeOptions { AllowMissing = true, LogWarning = _ => { } });
        var html = Markdown.ToHtml("See [!remoteinclude[docs](docs.md)] for details.", pipeline);

        Assert.Contains("<span class=\"remote-include-error\"", html);
        Assert.Contains("docs.md", html);
        Assert.Contains("<p>See <span", html);
    }

    [Fact]
    public void Inline_form_detects_cycles()
    {
        var client = new FakeClient();
        client.Map["loop.md"] = "before [!remoteinclude[x](loop.md)] after";

        var pipeline = BuildPipeline(client);
        Assert.Throws<InvalidOperationException>(() =>
            Markdown.ToHtml("[!remoteinclude[loop](loop.md)]", pipeline));
    }

    [Fact]
    public void Inline_form_missing_source_emits_span_placeholder()
    {
        var client = new FakeClient();
        var pipeline = BuildPipeline(client, new RemoteIncludeOptions { AllowMissing = true, LogWarning = _ => { } });

        var html = Markdown.ToHtml("Hi [!remoteinclude[x](nope.md)] there.", pipeline);

        Assert.Contains("<span class=\"remote-include-error\"", html);
        Assert.Contains("nope.md", html);
    }

    [Fact]
    public void Regular_links_are_not_swallowed()
    {
        var client = new FakeClient();
        var pipeline = BuildPipeline(client);
        var html = Markdown.ToHtml("See [the docs](https://example.com).", pipeline);

        Assert.Contains("<a href=\"https://example.com\"", html);
    }
}
