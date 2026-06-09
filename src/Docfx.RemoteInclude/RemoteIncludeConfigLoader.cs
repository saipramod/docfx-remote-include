using System.Text.Json;
using System.Text.Json.Serialization;

namespace Docfx.RemoteInclude;

/// <summary>
/// Loads <c>remoteinclude.json</c> and resolves <c>$VAR</c> / <c>${VAR}</c> environment-variable
/// references in <see cref="AuthSettings.Value"/> fields.
/// </summary>
public static class RemoteIncludeConfigLoader
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, allowIntegerValues: false) },
    };

    /// <summary>
    /// Load the file at <paramref name="path"/>. Returns null if the file does not exist.
    /// Throws if the file exists but cannot be parsed.
    /// </summary>
    public static RemoteIncludeConfig? LoadOrNull(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        if (!File.Exists(path))
        {
            return null;
        }
        var json = File.ReadAllText(path);
        var config = JsonSerializer.Deserialize<RemoteIncludeConfig>(json, s_jsonOptions)
            ?? throw new InvalidOperationException($"Failed to parse '{path}'.");
        return Resolve(config);
    }

    /// <summary>
    /// Resolve env-var indirection in any <see cref="AuthSettings.Value"/> fields.
    /// Returns a new instance with resolved values.
    /// </summary>
    public static RemoteIncludeConfig Resolve(RemoteIncludeConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        return new RemoteIncludeConfig
        {
            BaseUrl = config.BaseUrl,
            AllowMissing = config.AllowMissing,
            UrlTemplate = config.UrlTemplate,
            Auth = ResolveAuth(config.Auth),
            Transform = config.Transform is null ? null : new TransformSettings
            {
                Endpoint = config.Transform.Endpoint,
                Auth = ResolveAuth(config.Transform.Auth),
            },
        };
    }

    /// <summary>
    /// Resolve a single <c>$VAR</c> or <c>${VAR}</c> token to its environment value.
    /// Returns the input unchanged if it is null, empty, or does not start with <c>$</c>.
    /// Returns null if the referenced variable is not set.
    /// </summary>
    public static string? ResolveEnvIndirection(string? value)
    {
        if (string.IsNullOrEmpty(value) || value[0] != '$')
        {
            return value;
        }

        var name = value.Length >= 3 && value[1] == '{' && value[^1] == '}'
            ? value.Substring(2, value.Length - 3)
            : value[1..];

        if (string.IsNullOrEmpty(name))
        {
            return value;
        }

        return Environment.GetEnvironmentVariable(name);
    }

    private static AuthSettings? ResolveAuth(AuthSettings? auth)
    {
        if (auth is null) return null;
        return new AuthSettings
        {
            Mode = auth.Mode,
            Value = ResolveEnvIndirection(auth.Value),
            Scope = ResolveEnvIndirection(auth.Scope),
        };
    }
}
