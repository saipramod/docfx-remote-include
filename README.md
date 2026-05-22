# docfx-remote-include

A standalone [Markdig](https://github.com/xoofx/markdig) extension and CLI tool for
[DocFX](https://github.com/dotnet/docfx) that inlines markdown content fetched from
an authenticated HTTP service at build time.

This is **not a fork of DocFX**. It plugs into DocFX's public `BuildOptions.ConfigureMarkdig`
seam, so it tracks upstream releases as a regular NuGet package.

## Directive

In any markdown file processed by DocFX:

```markdown
Some local content.

[!remoteinclude[Welcome](path/to/snippet.md)]

More local content.

Inline usage also works: today's status is [!remoteinclude[s](status/prod.md)].
```

At build time the extension performs `GET {baseUrl}/{source}`, parses the response as
markdown, and inlines the result. Nested directives, cycle detection, and missing-source
handling are all supported. The directive has two shapes that share the same syntax:

- **Block** — the directive is the *only* thing on its line. The fetched markdown is
  inlined as block content (headings, lists, paragraphs allowed).
- **Inline** — the directive appears mid-paragraph. The fetched markdown must reduce
  to a single paragraph; only its inline content is spliced in (no `<p>` wrapper).

### Optional AI rewrite

Add a quoted hint after the source to ask an `IRewriteService` (e.g. Azure OpenAI in
the CLI) to rewrite the fetched snippet to match the surrounding page voice:

```markdown
[!remoteinclude[Install](snippets/install.md "match this page's tone and tense")]
```

Without a hint, the snippet is inlined verbatim.

## Install

Two packages on [GitHub Packages](https://github.com/saipramod?tab=packages&repo_name=docfx-remote-include):

| Package                          | Purpose                                                  |
| -------------------------------- | -------------------------------------------------------- |
| `Docfx.RemoteInclude`            | Library — for hosts that call `Docset.Build(...)`.        |
| `Docfx.RemoteInclude.Cli`        | `dotnet tool` — drop-in wrapper around `docfx build`.     |

### Setup the NuGet source

Add the GitHub Packages feed (one-time):

```powershell
dotnet nuget add source "https://nuget.pkg.github.com/saipramod/index.json" \
  --name "saipramod" \
  --username YOUR_GITHUB_USERNAME \
  --password YOUR_GITHUB_PAT
```

> The PAT needs the `read:packages` scope. If the repo is public, no PAT is required.

### As a CLI

```powershell
dotnet tool install -g Docfx.RemoteInclude.Cli --source "saipramod"
```

Configure via `remoteinclude.json` next to `docfx.json`:

```json
{
  "baseUrl": "https://internal.example.com/",
  "allowMissing": false,
  "urlTemplate": "api/content/GetFile?path={source}",
  "auth": { "mode": "managedIdentity", "scope": "api://my-app-id/.default" },
  "ai": {
    "endpoint": "https://my-aoai.openai.azure.com/",
    "deployment": "gpt-4o-mini",
    "contextStrategy": "section",
    "auth": { "mode": "default" }
  }
}
```

Then build:

```powershell
docfx-ri build docs/docfx.json
```

Both `auth` blocks accept `{ "mode": "none" | "default" | "managedIdentity" | "jwt" | "key", "value": "...", "scope": "..." }`.
`value` accepts `$VAR` / `${VAR}` to indirect through environment variables. `mode: "none"` sends
no auth header (valid for the content service only). `scope` overrides the OAuth audience when using
`default` or `managedIdentity` mode — useful when the API's audience differs from its hostname
(e.g. `api://my-app-id/.default`). Omit the `ai` section entirely to disable
rewriting. Legacy env vars `DOCFX_RI_BASE_URL` and `DOCFX_RI_TOKEN` (bearer JWT) still work when
no `remoteinclude.json` is present.

#### URL template

By default, the directive source (e.g. `snippets/intro.md`) is appended as a relative path to
`baseUrl`. If your content service uses a query-parameter or non-trivial URL scheme, set
`urlTemplate` with a `{source}` placeholder:

```json
{
  "baseUrl": "https://api.example.com/",
  "urlTemplate": "content/GetFile?path={source}"
}
```

The `{source}` token is replaced with the URL-encoded source value from the directive.

### As a library

```csharp
using Docfx;
using Docfx.RemoteInclude;

using var client = new HttpRemoteContentClient(
    baseUri: new Uri("https://internal.example.com/"),
    authHandler: async (request, ct) =>
    {
        request.Headers.Authorization = new("Bearer", await GetJwtAsync(ct));
    });

await Docset.Build("docs/docfx.json", new BuildOptions
{
    ConfigureMarkdig = pipeline => pipeline.UseRemoteInclude(client, new RemoteIncludeOptions
    {
        RewriteService = myRewriter, // optional IRewriteService
    }),
});
```

Provide your own `IRemoteContentClient` for non-HTTP sources, custom auth schemes
(mTLS, signed URLs), or on-disk caching. Implement `IRewriteService` to plug in any
LLM — the library itself has no Azure dependency.

## Behavior

- **Hard fail by default** when a source returns 404 or errors. Pass
  `--allow-missing` (CLI) or `new RemoteIncludeOptions { AllowMissing = true }`
  (library) to render a visible `<div class="remote-include-error">` placeholder
  instead.
- **Cycle detection** via an `AsyncLocal` source stack; max depth defaults to 8.
- **In-process cache** in `HttpRemoteContentClient`: each source is fetched once
  per build.
- **Concurrency capped** at 8 in-flight requests by default.

## Security

- Credentials are read from environment variables / a host-supplied callback.
  They are never read from `docfx.json` and never written to disk.
- Remote markdown is parsed by the same pipeline that processes your own content.
  If your upstream service is untrusted, disable raw HTML in your DocFX pipeline.

## Status

Alpha. Public API may change before 1.0. Issues and PRs welcome.

## License

MIT.
