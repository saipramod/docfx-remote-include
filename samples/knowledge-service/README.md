# Content & Transform Service

A reference implementation that serves as both the **content source** (for `[!remoteinclude]` directives) and the **page transform service** (for post-assembly processing) in a single service.

## Endpoints

| Endpoint | Method | Purpose |
| --- | --- | --- |
| `/content/{path}` | GET | Serves markdown files for remote include directives |
| `/transform` | POST | Transforms assembled pages (structure rules + LLM tone) |
| `/health` | GET | Health check |

## What it does

**Content serving (`GET /content/{path}`):**
- Serves markdown files from a configurable root directory
- Maps directly to the `[!remoteinclude[title](path)]` source paths
- Directory traversal protection built in

**Page transformation (`POST /transform`):**

After all remote includes are resolved and the page is fully assembled, the service
governs the page. Its behavior depends on whether you've defined guidance content:

- **With central guidance** (markdown under `content/guidance/`): the service treats
  that guidance as the source of truth, compares each assembled page against it, and
  inserts `> [!NOTE] **Team Override**` callouts above content that deviates. It also
  fixes heading hierarchy and harmonizes tone for the page's `audience`/`intent`.
- **Without any guidance content**: the service is a **pure passthrough to the LLM** —
  it just harmonizes tone and structure for the declared audience/intent. No comparison,
  no override annotations.
- **Without an `AiEndpoint` configured**: the page is returned unchanged (the service
  is a no-op transform). The `/content` endpoint still works.

## Configuration

Settings in `appsettings.json`:

```json
{
  "Content": {
    "RootPath": "./content"
  },
  "Transform": {
    "AiEndpoint": "https://your-aoai.openai.azure.com/",
    "AiDeployment": "gpt-4o-mini",
    "DefaultTone": "professional technical documentation"
  }
}
```

| Setting | Required | Description |
| --- | --- | --- |
| `Content:RootPath` | No | Directory containing markdown files to serve. Default: `./content` |
| `Transform:AiEndpoint` | No | Azure OpenAI endpoint. If empty, `/transform` returns the page unchanged. |
| `Transform:AiDeployment` | No | Model deployment name. Default: `gpt-4o-mini` |
| `Transform:DefaultTone` | No | Default tone for all documents. Default: `professional technical documentation` |

All settings can also be overridden via environment variables using the `__` separator (e.g. `Transform__AiEndpoint`).

## Run as a sidecar

The service is designed to run alongside your docs build as a sidecar. The container
binds `0.0.0.0:8080` (set in the Dockerfile via `ASPNETCORE_URLS`), reads all config
from environment variables, and has no required dependencies — if you provide no
`content/guidance` and no `AiEndpoint`, it still serves `/content` and returns pages
unchanged from `/transform`.

**Docker Compose** — the docs build talks to the sidecar over the compose network:

```yaml
services:
  knowledge-service:
    build: ./samples/knowledge-service
    environment:
      - Transform__AiEndpoint=${AI_ENDPOINT:-}
      - Transform__AiDeployment=gpt-4o-mini
    ports:
      - "8080:8080"

  docs-build:
    image: mcr.microsoft.com/dotnet/sdk:8.0
    depends_on: [knowledge-service]
    working_dir: /docs
    volumes:
      - ./samples/basic:/docs
    # remoteinclude.json baseUrl: http://knowledge-service:8080/content/
    command: sh -c "dotnet tool install -g Documentation.DocfxRemoteInclude.Cli &&
                    ~/.dotnet/tools/docfx-ri build docfx.json"
```

**Kubernetes** — run it as a sidecar container in the same Pod and probe `/health`:

```yaml
containers:
  - name: docs-build
    image: your-docs-builder:latest
    # config points baseUrl/transform at http://localhost:8080
  - name: knowledge-service
    image: ghcr.io/saipramod/knowledge-service:latest
    ports:
      - containerPort: 8080
    env:
      - name: Transform__AiEndpoint
        value: ""            # empty = passthrough/no-op transform
    readinessProbe:
      httpGet: { path: /health, port: 8080 }
      initialDelaySeconds: 3
    livenessProbe:
      httpGet: { path: /health, port: 8080 }
      initialDelaySeconds: 5
```

Because the sidecar shares `localhost` with the build container, point both
`baseUrl` and `transform.endpoint` in `remoteinclude.json` at `http://localhost:8080`.

## Run locally

```bash
cd samples/knowledge-service
dotnet run
```

The service starts at `http://localhost:5000` with sample content ready to serve.

## Sample content included

```
content/
├── snippets/
│   ├── prerequisites.md      # Reusable prerequisites snippet
│   └── install.md            # Installation instructions
├── tsgs/
│   └── service-connectivity.md  # Troubleshooting guide
├── guides/
│   └── architecture.md       # Architecture overview
├── guidance/
│   ├── documentation-standards.md  # Central guidance (source of truth)
│   └── runbook-standards.md        # Central guidance (source of truth)
└── onboarding/
    └── new-engineer.md       # Example page using remote includes + transform hints
```

The `guidance/` folder holds the central documentation standards. The `/transform`
endpoint loads these as the source of truth and annotates any page content that
deviates from them as a **Team Override**.

The `onboarding/new-engineer.md` file demonstrates the full flow:
- Pulls in shared snippets via `[!remoteinclude]`
- Passes transform metadata (`audience: engineer`, `intent: onboarding`)
- Overrides the prerequisites section for macOS users

## Run with Docker

```bash
docker build -t transform-service .
docker run -p 8080:8080 \
  -e Transform__AiEndpoint=https://your-aoai.openai.azure.com/ \
  -e Transform__AiDeployment=gpt-4o-mini \
  transform-service
```

## Configure in remoteinclude.json

Point both `baseUrl` and `transform` at the same service:

```json
{
  "baseUrl": "http://localhost:8080/content/",
  "transform": {
    "endpoint": "http://localhost:8080/transform",
    "auth": { "mode": "none" }
  }
}
```

## Page frontmatter hints

The service owns the rules. Pages just pass hints:

```yaml
---
transform:
  audience: pm
  intent: onboarding
  overrides:
    prerequisites: skip
    install: "target .NET 10"
  extra:
    team: azure-compute
---
```

The service decides what to do with these hints. You can extend the service to:
- Enforce required sections per `intent` type
- Apply different tones per `audience`
- Route to different LLM prompts based on `extra` metadata
- Reject content that doesn't meet quality thresholds

## API Contract

```
POST /transform
Content-Type: application/json

{
  "content": "<assembled markdown/HTML>",
  "source": "docs/onboarding/new-hire.md",
  "metadata": {
    "audience": "pm",
    "intent": "onboarding",
    "overrides": { "prerequisites": "skip" },
    "extra": { "team": "azure-compute" }
  }
}

→ 200 OK
{
  "content": "<transformed content>",
  "diagnostics": ["Removed section: prerequisites", "Applied tone harmonization for audience=pm"]
}
```

## Health check

```
GET /health → { "status": "healthy" }
```
