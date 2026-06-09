using Xunit;

namespace Docfx.RemoteInclude.Tests;

public class PageMetadataExtractorTests
{
    [Fact]
    public void Extracts_transform_block_from_frontmatter_string()
    {
        var md = """
            ---
            transform:
              audience: engineer
              intent: onboarding
              overrides:
                prerequisites: "target macOS users"
              extra:
                team: platform
            ---

            # Page

            Body.
            """;

        var meta = PageMetadataExtractor.Extract(md);

        Assert.Equal("engineer", meta.Audience);
        Assert.Equal("onboarding", meta.Intent);
        Assert.NotNull(meta.Overrides);
        Assert.Equal("target macOS users", meta.Overrides!["prerequisites"]);
        Assert.NotNull(meta.Extra);
        Assert.Equal("platform", meta.Extra!["team"]);
    }

    [Fact]
    public void Returns_empty_metadata_when_no_frontmatter()
    {
        var meta = PageMetadataExtractor.Extract("# Just a heading\n\nNo frontmatter here.");

        Assert.Null(meta.Audience);
        Assert.Null(meta.Intent);
        Assert.Null(meta.Overrides);
        Assert.Null(meta.Extra);
    }

    [Fact]
    public void Returns_empty_metadata_when_frontmatter_has_no_transform_block()
    {
        var md = """
            ---
            title: Something
            author: someone
            ---

            Body.
            """;

        var meta = PageMetadataExtractor.Extract(md);

        Assert.Null(meta.Audience);
        Assert.Null(meta.Intent);
    }

    [Fact]
    public void Handles_crlf_line_endings()
    {
        var md = "---\r\ntransform:\r\n  audience: pm\r\n  intent: reference\r\n---\r\n\r\n# Title\r\n";

        var meta = PageMetadataExtractor.Extract(md);

        Assert.Equal("pm", meta.Audience);
        Assert.Equal("reference", meta.Intent);
    }

    [Fact]
    public void Extract_null_or_empty_is_safe()
    {
        Assert.Null(PageMetadataExtractor.Extract("").Audience);
        Assert.Null(PageMetadataExtractor.Extract((string)null!).Audience);
    }
}
