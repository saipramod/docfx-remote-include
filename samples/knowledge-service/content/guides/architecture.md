The platform is composed of three loosely coupled parts:

- **Authoring sites** — DocFX projects that own their own pages and pull shared
  content at build time via `[!remoteinclude]` directives.
- **Knowledge-service** — a single service that acts as both the *content source*
  (`GET /content/{path}`) and the *page transform* (`POST /transform`). It is the
  single source of truth for shared snippets and central guidance.
- **Build pipeline** — runs `docfx-ri build`, which resolves remote includes,
  assembles each page, and sends the result through the transform endpoint.

Because shared content lives in one place, an update to a snippet propagates to
every site on its next build — no copy-paste, no drift.
