# Remote Include — Basic Sample

This is a minimal DocFX site that exercises the `[!remoteinclude]` directive.
Everything you see in the navigation is plain markdown, except for a few spots
where content is pulled from a remote service at build time.

## What this sample shows

- **Block directive** — fetched markdown rendered as a top-level block.
  See [Getting started](guide/getting-started.md).
- **Inline directive** — fetched markdown spliced into a sentence.
  See [Status board](reference/status.md).
- **AI rewrite hint** — opt-in per directive via a quoted hint after the source.
  See [Authentication](guide/authentication.md).

## How to build it

```powershell
dotnet tool install -g Docfx.RemoteInclude.Cli
docfx-ri build samples/basic/docfx.json
```

The bundled `remoteinclude.json` is configured with `allowMissing: true` and
points at a stand-in host, so the build succeeds and every directive renders
as a visible error placeholder. Point `baseUrl` at a real service to see real
content.
