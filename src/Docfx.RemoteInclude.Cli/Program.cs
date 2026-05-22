using Docfx;
using Docfx.RemoteInclude;
using Docfx.RemoteInclude.Cli;

if (args.Length == 0 || args[0] is "-h" or "--help")
{
    Console.WriteLine("""
        docfx-ri — DocFX build with remote-include support.

        Usage:
          docfx-ri build <path-to-docfx.json> [options]

        Options:
          --config <path>        Path to remoteinclude.json (default: sibling of docfx.json).
          --allow-missing        Render error placeholders instead of failing on fetch errors.

        Config precedence (highest first):
          1. CLI flags
          2. remoteinclude.json
          3. Environment variables (DOCFX_RI_BASE_URL, DOCFX_RI_TOKEN)
          4. Built-in defaults

        Directive syntax in markdown:
          [!remoteinclude[title](source "hint")]
        """);
    return args.Length == 0 ? 1 : 0;
}

if (args[0] != "build")
{
    Console.Error.WriteLine($"Unknown command '{args[0]}'. Run 'docfx-ri --help'.");
    return 2;
}

var configPath = args.Length > 1 && !args[1].StartsWith('-') ? args[1] : "docfx.json";
var allowMissingFlag = args.Contains("--allow-missing");
var explicitConfigPath = ArgValue(args, "--config");

var resolvedConfigPath = explicitConfigPath
    ?? Path.Combine(Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? ".", "remoteinclude.json");

RemoteIncludeConfig? jsonConfig = null;
try
{
    jsonConfig = RemoteIncludeConfigLoader.LoadOrNull(resolvedConfigPath);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to load '{resolvedConfigPath}': {ex.Message}");
    return 3;
}

var baseUrl = jsonConfig?.BaseUrl
    ?? Environment.GetEnvironmentVariable("DOCFX_RI_BASE_URL")
    ?? throw new InvalidOperationException("baseUrl not configured (set it in remoteinclude.json or DOCFX_RI_BASE_URL).");

var allowMissing = allowMissingFlag || (jsonConfig?.AllowMissing ?? false);

var contentAuth = jsonConfig?.Auth ?? DefaultContentAuth();
var baseUri = new Uri(baseUrl);
var authHandler = ContentAuthHandlerFactory.Build(baseUri, contentAuth);

using var client = new HttpRemoteContentClient(baseUri, authHandler, urlTemplate: jsonConfig?.UrlTemplate);

IRewriteService? rewriteService = null;
if (jsonConfig?.Ai is { } aiSettings && !string.IsNullOrWhiteSpace(aiSettings.Endpoint))
{
    rewriteService = new AzureOpenAIRewriteService(aiSettings);
}

var options = new RemoteIncludeOptions
{
    AllowMissing = allowMissing,
    LogWarning = m => Console.Error.WriteLine(m),
    RewriteService = rewriteService,
    ContextStrategy = jsonConfig?.Ai?.ContextStrategy ?? ContextStrategy.Section,
};

await Docset.Build(configPath, new BuildOptions
{
    ConfigureMarkdig = pipeline => pipeline.UseRemoteInclude(client, options),
});

return 0;

static string? ArgValue(string[] args, string name)
{
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (args[i] == name) return args[i + 1];
    }
    return null;
}

static AuthSettings DefaultContentAuth()
{
    var token = Environment.GetEnvironmentVariable("DOCFX_RI_TOKEN");
    if (!string.IsNullOrEmpty(token))
    {
        return new AuthSettings { Mode = AuthMode.Jwt, Value = token };
    }
    return new AuthSettings { Mode = AuthMode.Default };
}

