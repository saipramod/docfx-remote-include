using Xunit;

namespace Docfx.RemoteInclude.Tests;

public class HttpRemoteContentClientTests
{
    [Fact]
    public void BuildUrlBuilder_null_template_returns_source_as_is()
    {
        var builder = HttpRemoteContentClient.BuildUrlBuilder(null);
        Assert.Equal("snippets/intro.md", builder("snippets/intro.md"));
    }

    [Fact]
    public void BuildUrlBuilder_empty_template_returns_source_as_is()
    {
        var builder = HttpRemoteContentClient.BuildUrlBuilder("");
        Assert.Equal("snippets/intro.md", builder("snippets/intro.md"));
    }

    [Fact]
    public void BuildUrlBuilder_query_param_template_encodes_source()
    {
        var builder = HttpRemoteContentClient.BuildUrlBuilder(
            "api/navigation/RetrieveRawFileDataForRoute?route={source}");

        Assert.Equal(
            "api/navigation/RetrieveRawFileDataForRoute?route=help%2Fauthoring",
            builder("help/authoring"));
    }

    [Fact]
    public void BuildUrlBuilder_path_template_encodes_source()
    {
        var builder = HttpRemoteContentClient.BuildUrlBuilder("v2/docs/{source}?format=raw");

        Assert.Equal(
            "v2/docs/help%2Fsearch?format=raw",
            builder("help/search"));
    }

    [Fact]
    public void BuildUrlBuilder_case_insensitive_placeholder()
    {
        var builder = HttpRemoteContentClient.BuildUrlBuilder("content?path={SOURCE}");
        Assert.Equal("content?path=readme.md", builder("readme.md"));
    }
}
