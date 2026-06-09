# Remote Include — Basic Sample

This is a minimal DocFX site powered by the **knowledge-service**. Content is
served and transformed by a single service that handles both remote includes
and page-level quality control.

## What this sample shows

- **Shared snippets** — reusable content (prerequisites, install steps) pulled
  from the knowledge-service. See [Getting started](guide/getting-started.md).
- **Troubleshooting guides** — TSGs served from the knowledge-service and
  inlined into pages. See [Operations](guide/operations.md).
- **Page transforms** — assembled pages sent through the knowledge-service for
  tone harmonization and structure enforcement.
  See [Onboarding](guide/onboarding.md).
- **Inline directives** — fetched content spliced mid-paragraph.
  See [Status board](reference/status.md).

## How to run

1. Start the knowledge-service:

```bash
cd samples/knowledge-service
dotnet run
```

2. Build the DocFX site:

```bash
docfx-ri build samples/basic/docfx.json
```

The `remoteinclude.json` points both `baseUrl` and `transform.endpoint` at the
local knowledge-service. Content is fetched from `/content/{path}` and assembled
pages are transformed via `POST /transform`.
