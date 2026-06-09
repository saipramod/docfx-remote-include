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

IPageTransformService? transformService = null;
if (jsonConfig?.Transform is { } transformSettings && !string.IsNullOrWhiteSpace(transformSettings.Endpoint))
{
    transformService = new HttpPageTransformService(transformSettings);
}

var options = new RemoteIncludeOptions
{
    AllowMissing = allowMissing,
    LogWarning = m => Console.Error.WriteLine(m),
    RewriteService = rewriteService,
    PageTransformService = null, // Transform is handled in pre-build step below
    ContextStrategy = jsonConfig?.Ai?.ContextStrategy ?? ContextStrategy.Section,
};

// Pre-build: resolve remote includes and apply page transform on a temp copy
var docfxDir = Path.GetDirectoryName(Path.GetFullPath(configPath)) ?? ".";
if (transformService is not null)
{
    // Work on a temp copy so source files stay untouched
    var tempDir = Path.Combine(Path.GetTempPath(), $"docfx-ri-{Guid.NewGuid():N}");
    CopyDirectory(docfxDir, tempDir, excludes: ["_site", "obj", "bin"]);

    var tempConfigPath = Path.Combine(tempDir, Path.GetFileName(configPath));
    var mdFiles = Directory.GetFiles(tempDir, "*.md", SearchOption.AllDirectories)
        .Where(f => !f.Contains("_site") && !f.Contains("obj") && !f.Contains("bin"))
        .ToList();

    foreach (var mdFile in mdFiles)
    {
        var rawMarkdown = File.ReadAllText(mdFile);
        if (!rawMarkdown.Contains("[!remoteinclude"))
            continue;

        // Resolve remote includes into flat markdown
        var resolved = ResolveIncludes(rawMarkdown, client, allowMissing);

        // Send assembled markdown to transform service
        try
        {
            var request = new PageTransformRequest(
                Content: resolved,
                Source: Path.GetRelativePath(tempDir, mdFile),
                Metadata: new PageTransformMetadata());
            var result = transformService.TransformAsync(request).GetAwaiter().GetResult();

            if (result.Diagnostics is { Count: > 0 } diags)
            {
                foreach (var d in diags)
                    Console.Error.WriteLine($"[transform] {Path.GetFileName(mdFile)}: {d}");
            }

            File.WriteAllText(mdFile, result.Content);
            Console.Error.WriteLine($"[transform] Transformed: {Path.GetRelativePath(tempDir, mdFile)}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"[transform] Failed for {Path.GetFileName(mdFile)}: {ex.Message}");
        }
    }

    // Build from the temp copy, output to original _site
    await Docset.Build(tempConfigPath, new BuildOptions());

    // Copy _site back to original location
    var tempSite = Path.Combine(tempDir, "_site");
    var originalSite = Path.Combine(docfxDir, "_site");
    if (Directory.Exists(tempSite))
    {
        if (Directory.Exists(originalSite)) Directory.Delete(originalSite, true);
        CopyDirectory(tempSite, originalSite);
    }

    // Clean up temp
    try { Directory.Delete(tempDir, true); } catch { /* best effort */ }
}
else
{
    await Docset.Build(configPath, new BuildOptions
    {
        ConfigureMarkdig = pipeline => pipeline.UseRemoteInclude(client, options),
    });
}

return 0;

static void CopyDirectory(string source, string dest, string[]? excludes = null)
{
    Directory.CreateDirectory(dest);
    foreach (var dir in Directory.GetDirectories(source))
    {
        var name = Path.GetFileName(dir);
        if (excludes?.Contains(name) == true) continue;
        CopyDirectory(dir, Path.Combine(dest, name), excludes);
    }
    foreach (var file in Directory.GetFiles(source))
    {
        File.Copy(file, Path.Combine(dest, Path.GetFileName(file)), true);
    }
}

static string ResolveIncludes(string markdown, IRemoteContentClient client, bool allowMissing)
{
    // Simple regex-based include resolution for the pre-build step
    var pattern = @"\[!remoteinclude\[([^\]]*)\]\(([^)]+)\)\]";
    return System.Text.RegularExpressions.Regex.Replace(markdown, pattern, match =>
    {
        var source = match.Groups[2].Value;
        try
        {
            var content = client.GetMarkdownAsync(source).GetAwaiter().GetResult();
            return content ?? (allowMissing ? $"<!-- include not found: {source} -->" : throw new InvalidOperationException($"Source '{source}' not found"));
        }
        catch (Exception ex)
        {
            if (allowMissing) return $"<!-- include failed: {source}: {ex.Message} -->";
            throw;
        }
    });
}

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

