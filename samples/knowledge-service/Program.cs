using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.AI.OpenAI;
using Azure.Identity;
using OpenAI.Chat;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
var app = builder.Build();

var jsonOptions = new JsonSerializerOptions
{
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
};

// Configuration via appsettings.json
var aiEndpoint = config["Transform:AiEndpoint"];
var aiDeployment = config["Transform:AiDeployment"] ?? "gpt-4o-mini";
var defaultTone = config["Transform:DefaultTone"] ?? "professional technical documentation";
var contentRoot = config["Content:RootPath"] ?? Path.Combine(Directory.GetCurrentDirectory(), "content");

ChatClient? chatClient = null;
if (!string.IsNullOrWhiteSpace(aiEndpoint))
{
    var client = new AzureOpenAIClient(new Uri(aiEndpoint), new DefaultAzureCredential());
    chatClient = client.GetChatClient(aiDeployment);
}

// --- Content endpoint: serves markdown files for remote include ---

app.MapGet("/content/{**path}", (string path) =>
{
    var filePath = Path.GetFullPath(Path.Combine(contentRoot, path));

    // Prevent directory traversal
    if (!filePath.StartsWith(Path.GetFullPath(contentRoot), StringComparison.OrdinalIgnoreCase))
    {
        return Results.StatusCode(403);
    }

    // Try exact path first, then with .md extension
    if (!File.Exists(filePath) && !Path.HasExtension(filePath))
    {
        filePath = filePath + ".md";
    }

    if (!File.Exists(filePath))
    {
        return Results.NotFound(new { error = $"Source '{path}' not found" });
    }

    var content = File.ReadAllText(filePath);
    return Results.Text(content, "text/markdown");
});

// --- Transform endpoint: processes assembled pages ---

app.MapPost("/transform", async (HttpContext ctx) =>
{
    var request = await ctx.Request.ReadFromJsonAsync<TransformRequest>(jsonOptions);
    if (request is null || string.IsNullOrWhiteSpace(request.Content))
    {
        ctx.Response.StatusCode = 400;
        await ctx.Response.WriteAsJsonAsync(new { error = "content is required" });
        return;
    }

    var diagnostics = new List<string>();
    var content = request.Content;

    if (chatClient is not null)
    {
        // Load central guidance as reference for the LLM
        var guidancePath = Path.Combine(contentRoot, "guidance");
        var centralGuidance = "";
        if (Directory.Exists(guidancePath))
        {
            var files = Directory.GetFiles(guidancePath, "*.md");
            centralGuidance = string.Join("\n\n---\n\n", files.Select(File.ReadAllText));
        }

        content = await ApplyLlmTransform(chatClient, content, centralGuidance, request.Metadata, defaultTone, diagnostics);
    }

    await ctx.Response.WriteAsJsonAsync(new TransformResponse
    {
        Content = content,
        Diagnostics = diagnostics.Count > 0 ? diagnostics : null,
    }, jsonOptions);
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }));

app.Run();

// --- LLM-based transform ---

static async Task<string> ApplyLlmTransform(
    ChatClient chat,
    string content,
    string centralGuidance,
    TransformMetadata? metadata,
    string defaultTone,
    List<string> diagnostics)
{
    var audience = metadata?.Audience ?? "engineers";
    var intent = metadata?.Intent ?? "reference";
    var hasGuidance = !string.IsNullOrWhiteSpace(centralGuidance);

    // With no user-defined guidance, the service is a pure tone/structure passthrough.
    // With guidance, it additionally compares the page against the source of truth and
    // annotates deviations.
    var systemPrompt = hasGuidance
        ? $"""
            You are a documentation quality service. You receive a team's document and the central 
            guidance (the source of truth for what this type of document should contain).

            Your job:
            1. COMPARE the team's document against central guidance. Identify where the team's 
               content DEVIATES from what central guidance recommends (different values, missing 
               sections, custom procedures that override the standard).
            2. For each deviation, insert a DocFX note callout DIRECTLY ABOVE the deviating content:
               > [!NOTE]
               > **Team Override** — <brief explanation of how this deviates from central guidance>
            3. Fix the heading hierarchy: exactly ONE H1 (the page title), everything else H2+.
               Remove any duplicate/fragment title headings. Remove YAML frontmatter blocks.
            4. Ensure consistent {defaultTone} tone for audience: {audience}, intent: {intent}.
            5. Preserve ALL technical content, code blocks, tables, links exactly. Do not invent 
               or remove information.
            6. Return ONLY the final markdown. No commentary.
            """
        : $"""
            You are a documentation quality service. You receive an assembled document.

            Your job:
            1. Fix the heading hierarchy: exactly ONE H1 (the page title), everything else H2+.
               Remove any duplicate/fragment title headings. Remove YAML frontmatter blocks.
            2. Ensure consistent {defaultTone} tone for audience: {audience}, intent: {intent}.
            3. Preserve ALL technical content, code blocks, tables, links exactly. Do not invent 
               or remove information.
            4. Return ONLY the final markdown. No commentary.
            """;

    var userPrompt = hasGuidance
        ? $"""
            ## Central Guidance (source of truth):

            {centralGuidance}

            ---

            ## Team Document (to transform):

            {content}
            """
        : $"""
            ## Document (to transform):

            {content}
            """;

    var options = new ChatCompletionOptions { Temperature = 0f };
    var completion = await chat.CompleteChatAsync(
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt),
        ],
        options);

    var result = completion.Value.Content.Count > 0 ? completion.Value.Content[0].Text : null;
    if (!string.IsNullOrWhiteSpace(result))
    {
        diagnostics.Add(hasGuidance
            ? "LLM transform applied: compared against central guidance, annotated overrides"
            : "LLM transform applied: tone and structure harmonization (no guidance configured)");
        return result.Trim();
    }

    return content;
}

// --- Models ---

record TransformRequest
{
    public string Content { get; init; } = "";
    public string? Source { get; init; }
    public TransformMetadata? Metadata { get; init; }
}

record TransformMetadata
{
    public string? Audience { get; init; }
    public string? Intent { get; init; }
    public Dictionary<string, string>? Overrides { get; init; }
    public Dictionary<string, string>? Extra { get; init; }
}

record TransformResponse
{
    public string? Content { get; init; }
    public List<string>? Diagnostics { get; init; }
}
