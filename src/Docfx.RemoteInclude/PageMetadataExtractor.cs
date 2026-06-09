using Markdig.Extensions.Yaml;
using Markdig.Syntax;

namespace Docfx.RemoteInclude;

/// <summary>
/// Extracts <c>transform:</c> metadata from a document's YAML frontmatter block.
/// <para>
/// Example frontmatter:
/// <code>
/// ---
/// transform:
///   audience: pm
///   intent: onboarding
///   overrides:
///     prerequisites: skip
///     install: "target .NET 10"
///   extra:
///     team: azure-compute
/// ---
/// </code>
/// </para>
/// </summary>
internal static class PageMetadataExtractor
{
    public static PageTransformMetadata Extract(MarkdownDocument document)
    {
        var yamlBlock = document.Descendants<YamlFrontMatterBlock>().FirstOrDefault();
        if (yamlBlock is null) return new PageTransformMetadata();

        var yaml = yamlBlock.Lines.ToString();
        return ParseTransformBlock(yaml);
    }

    private static PageTransformMetadata ParseTransformBlock(string yaml)
    {
        string? audience = null;
        string? intent = null;
        Dictionary<string, string>? overrides = null;
        Dictionary<string, string>? extra = null;

        var lines = yaml.Split('\n');
        var inTransform = false;
        var currentSubBlock = (string?)null;
        var baseIndent = 0;

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.TrimEnd();

            if (!inTransform)
            {
                if (trimmed.TrimStart().StartsWith("transform:", StringComparison.OrdinalIgnoreCase))
                {
                    inTransform = true;
                    baseIndent = GetIndent(line) + 2;
                }
                continue;
            }

            // If we hit a line at the same or lower indentation as 'transform:', we've left the block
            var indent = GetIndent(line);
            if (!string.IsNullOrWhiteSpace(trimmed) && indent < baseIndent)
            {
                break;
            }

            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            // Check if this is a sub-block key (overrides:, extra:)
            if (indent == baseIndent && trimmed.TrimStart().EndsWith(':') &&
                !trimmed.TrimStart().Contains(": "))
            {
                currentSubBlock = trimmed.Trim().TrimEnd(':').ToLowerInvariant();
                continue;
            }

            // Parse key-value at base indent level
            if (indent == baseIndent && currentSubBlock is null)
            {
                var (key, value) = ParseKeyValue(trimmed);
                if (key is null) continue;

                switch (key.ToLowerInvariant())
                {
                    case "audience": audience = value; break;
                    case "intent": intent = value; break;
                    case "overrides": currentSubBlock = "overrides"; break;
                    case "extra": currentSubBlock = "extra"; break;
                }
            }
            // Parse sub-block entries
            else if (indent > baseIndent && currentSubBlock is not null)
            {
                var (key, value) = ParseKeyValue(trimmed);
                if (key is null) continue;

                switch (currentSubBlock)
                {
                    case "overrides":
                        overrides ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        overrides[key] = value ?? "true";
                        break;
                    case "extra":
                        extra ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                        extra[key] = value ?? "";
                        break;
                }
            }
            // Reset sub-block if we're back at base indent with a key-value
            else if (indent == baseIndent)
            {
                currentSubBlock = null;
                var (key, value) = ParseKeyValue(trimmed);
                if (key is null) continue;

                switch (key.ToLowerInvariant())
                {
                    case "audience": audience = value; break;
                    case "intent": intent = value; break;
                }
            }
        }

        return new PageTransformMetadata
        {
            Audience = audience,
            Intent = intent,
            Overrides = overrides,
            Extra = extra,
        };
    }

    private static int GetIndent(string line)
    {
        var count = 0;
        foreach (var c in line)
        {
            if (c == ' ') count++;
            else break;
        }
        return count;
    }

    private static (string? key, string? value) ParseKeyValue(string line)
    {
        var trimmed = line.Trim();
        var colonIdx = trimmed.IndexOf(':');
        if (colonIdx <= 0) return (null, null);

        var key = trimmed[..colonIdx].Trim();
        var value = trimmed[(colonIdx + 1)..].Trim();

        // Strip quotes
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') ||
             (value[0] == '\'' && value[^1] == '\'')))
        {
            value = value[1..^1];
        }

        return (key, string.IsNullOrWhiteSpace(value) ? null : value);
    }
}
