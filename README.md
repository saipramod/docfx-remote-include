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

### Optional page transform

After all remote includes are resolved and a page is fully assembled, the build can
send the page to a **page transform service** for centralized governance — tone
harmonization, structure enforcement, or quality checks. The service owns the rules;
pages only declare intent via YAML frontmatter:

```yaml
---
transform:
  audience: engineer
  intent: onboarding
  overrides:
    prerequisites: "target macOS users"
---
```

The extension extracts this `transform:` block, sends the assembled page plus these
hints to your service's endpoint, and uses the response. Governance happens once, at
the page level — there are no per-include AI hints. Omit the `transform` config to
disable it.

## Install

Two packages on [NuGet](https://www.nuget.org/packages/Documentation.DocfxRemoteInclude):

[![NuGet Downloads](https://img.shields.io/nuget/dt/Documentation.DocfxRemoteInclude?label=Library%20downloads)](https://www.nuget.org/packages/Documentation.DocfxRemoteInclude)
[![NuGet Downloads](https://img.shields.io/nuget/dt/Documentation.DocfxRemoteInclude.Cli?label=CLI%20downloads)](https://www.nuget.org/packages/Documentation.DocfxRemoteInclude.Cli)

| Package                                  | Purpose                                                  |
| ---------------------------------------- | -------------------------------------------------------- |
| `Documentation.DocfxRemoteInclude`       | Library — for hosts that call `Docset.Build(...)`.        |
| `Documentation.DocfxRemoteInclude.Cli`   | `dotnet tool` — drop-in wrapper around `docfx build`.     |

### As a CLI

```powershell
dotnet tool install -g Documentation.DocfxRemoteInclude.Cli
```

Configure via `remoteinclude.json` next to `docfx.json`:

```json
{
  "baseUrl": "https://internal.example.com/",
  "allowMissing": false,
  "urlTemplate": "api/content/GetFile?path={source}",
  "auth": { "mode": "managedIdentity", "scope": "api://my-app-id/.default" },
  "transform": {
    "endpoint": "https://internal.example.com/transform",
    "auth": { "mode": "managedIdentity", "scope": "api://my-app-id/.default" }
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
(e.g. `api://my-app-id/.default`). Omit the `transform` section entirely to disable
page transformation. Legacy env vars `DOCFX_RI_BASE_URL` and `DOCFX_RI_TOKEN` (bearer JWT) still work when
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

```powershell
dotnet add package Documentation.DocfxRemoteInclude
```

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
        PageTransformService = myTransformService, // optional IPageTransformService
    }),
});
```

Provide your own `IRemoteContentClient` for non-HTTP sources, custom auth schemes
(mTLS, signed URLs), or on-disk caching. Implement `IPageTransformService` to plug in
any page-level governance — tone, structure, or quality rules — backed by an LLM or
deterministic rules. The library itself has no Azure dependency.

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

Stable. `1.0.0` — the public API is considered stable; breaking changes will follow
semantic versioning. Issues and PRs welcome.

## License

MIT.
