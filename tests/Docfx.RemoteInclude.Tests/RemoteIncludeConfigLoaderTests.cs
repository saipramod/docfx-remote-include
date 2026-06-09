using System.Text.Json;
using Xunit;

namespace Docfx.RemoteInclude.Tests;

public class RemoteIncludeConfigLoaderTests
{
    [Fact]
    public void Loads_full_config_with_camelCase_enums()
    {
        var json = """
            {
              "baseUrl": "https://content.example.com/",
              "allowMissing": true,
              "urlTemplate": "api/content?path={source}",
              "auth": { "mode": "jwt", "value": "literal-token", "scope": "api://my-app/.default" },
              "transform": {
                "endpoint": "https://transform.example.com/transform",
                "auth": { "mode": "managedIdentity", "value": "client-id-guid" }
              }
            }
            """;
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, json);
            var cfg = RemoteIncludeConfigLoader.LoadOrNull(path);

            Assert.NotNull(cfg);
            Assert.Equal("https://content.example.com/", cfg!.BaseUrl);
            Assert.True(cfg.AllowMissing);
            Assert.Equal("api/content?path={source}", cfg.UrlTemplate);
            Assert.Equal(AuthMode.Jwt, cfg.Auth!.Mode);
            Assert.Equal("literal-token", cfg.Auth.Value);
            Assert.Equal("api://my-app/.default", cfg.Auth.Scope);
            Assert.Equal("https://transform.example.com/transform", cfg.Transform!.Endpoint);
            Assert.Equal(AuthMode.ManagedIdentity, cfg.Transform.Auth!.Mode);
            Assert.Equal("client-id-guid", cfg.Transform.Auth.Value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Returns_null_when_file_missing()
    {
        var path = Path.Combine(Path.GetTempPath(), $"definitely-not-there-{Guid.NewGuid():N}.json");
        Assert.Null(RemoteIncludeConfigLoader.LoadOrNull(path));
    }

    [Fact]
    public void Parses_auth_mode_none()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, """{ "baseUrl": "http://localhost:5000/", "auth": { "mode": "none" } }""");
            var cfg = RemoteIncludeConfigLoader.LoadOrNull(path);
            Assert.Equal(AuthMode.None, cfg!.Auth!.Mode);
            Assert.Null(cfg.Auth.Value);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Resolves_dollar_var_indirection()
    {
        var name = $"DOCFX_RI_TEST_{Guid.NewGuid():N}";
        Environment.SetEnvironmentVariable(name, "resolved-secret");
        try
        {
            Assert.Equal("resolved-secret", RemoteIncludeConfigLoader.ResolveEnvIndirection($"${name}"));
            Assert.Equal("resolved-secret", RemoteIncludeConfigLoader.ResolveEnvIndirection($"${{{name}}}"));
        }
        finally { Environment.SetEnvironmentVariable(name, null); }
    }

    [Fact]
    public void Resolves_dollar_var_returns_null_when_unset()
    {
        var name = $"DOCFX_RI_TEST_{Guid.NewGuid():N}";
        Assert.Null(RemoteIncludeConfigLoader.ResolveEnvIndirection($"${name}"));
    }

    [Fact]
    public void Resolves_returns_input_when_no_dollar()
    {
        Assert.Equal("literal", RemoteIncludeConfigLoader.ResolveEnvIndirection("literal"));
        Assert.Null(RemoteIncludeConfigLoader.ResolveEnvIndirection(null));
        Assert.Equal("", RemoteIncludeConfigLoader.ResolveEnvIndirection(""));
    }

    [Fact]
    public void Throws_on_invalid_json()
    {
        var path = Path.GetTempFileName();
        try
        {
            File.WriteAllText(path, "{ not valid json");
            Assert.Throws<JsonException>(() => RemoteIncludeConfigLoader.LoadOrNull(path));
        }
        finally { File.Delete(path); }
    }
}
