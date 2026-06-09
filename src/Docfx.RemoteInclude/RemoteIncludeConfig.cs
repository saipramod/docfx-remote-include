namespace Docfx.RemoteInclude;

/// <summary>
/// Authentication mode for the content service or Azure OpenAI endpoint.
/// </summary>
public enum AuthMode
{
    /// <summary><c>DefaultAzureCredential</c> — cascades through MI, env, IDE, CLI, browser.</summary>
    Default,

    /// <summary><c>ManagedIdentityCredential</c>. <c>Value</c> optionally selects a user-assigned identity by client ID.</summary>
    ManagedIdentity,

    /// <summary>Literal bearer token sent in the <c>Authorization</c> header. <c>Value</c> is the token.</summary>
    Jwt,

    /// <summary>API key sent in <c>api-key</c> (AI) or <c>X-API-Key</c> (content) header. <c>Value</c> is the key.</summary>
    Key,

    /// <summary>No authentication — requests go out anonymously. Use for public or localhost services. Not valid for AI auth.</summary>
    None,
}

/// <summary>
/// Authentication settings for an endpoint. <see cref="Value"/> may use <c>$VAR</c> or <c>${VAR}</c>
/// to read from an environment variable instead of embedding the literal secret in source control.
/// </summary>
public sealed class AuthSettings
{
    public AuthMode Mode { get; init; } = AuthMode.Default;
    public string? Value { get; init; }

    /// <summary>
    /// OAuth scope / audience to request when using <see cref="AuthMode.Default"/> or
    /// <see cref="AuthMode.ManagedIdentity"/>. If omitted the CLI derives
    /// <c>{baseUrl.Authority}/.default</c> automatically. Set this when the API's
    /// audience differs from its hostname (e.g. <c>api://my-app-id/.default</c>).
    /// </summary>
    public string? Scope { get; init; }
}

/// <summary>
/// Configuration for the page transform service.
/// </summary>
public sealed class TransformSettings
{
    /// <summary>The endpoint URL of the transform service (e.g. "https://localhost:8080/transform").</summary>
    public string? Endpoint { get; init; }

    /// <summary>Authentication settings for the transform service.</summary>
    public AuthSettings? Auth { get; init; }
}

/// <summary>
/// Root model for <c>remoteinclude.json</c>. All fields are optional; absent values fall back
/// to CLI flags, environment variables, then built-in defaults.
/// </summary>
public sealed class RemoteIncludeConfig
{
    public string? BaseUrl { get; init; }
    public bool? AllowMissing { get; init; }
    public AuthSettings? Auth { get; init; }
    public TransformSettings? Transform { get; init; }

    /// <summary>
    /// URL template that controls how the directive's <c>source</c> value is mapped to an
    /// HTTP request URI. The placeholder <c>{source}</c> is replaced with the (URL-encoded)
    /// source path.
    /// <para>
    /// Examples:
    /// <list type="bullet">
    /// <item><c>null</c> / omitted — source is appended as a relative path (default).</item>
    /// <item><c>"api/navigation/GetContent?route={source}"</c> — query-string style API.</item>
    /// <item><c>"v2/docs/{source}?format=raw"</c> — RESTful path + fixed query params.</item>
    /// </list>
    /// </para>
    /// </summary>
    public string? UrlTemplate { get; init; }
}
