---
transform:
  audience: engineer
  intent: onboarding
  overrides:
    prerequisites: "target macOS users"
---

# Team Onboarding

This page demonstrates the full knowledge-service flow:

1. Shared snippets are pulled via `[!remoteinclude]`
2. The assembled page is sent to the knowledge-service's `/transform` endpoint
3. The service applies rules based on the `transform:` metadata above
4. The `overrides` hint tells the service to adapt the prerequisites section

## Setup

[!remoteinclude[Prerequisites](snippets/prerequisites.md)]

[!remoteinclude[Installation](snippets/install.md)]

## Architecture Overview

[!remoteinclude[Architecture](guides/architecture.md)]

## Troubleshooting

[!remoteinclude[Service Connectivity](tsgs/service-connectivity.md)]

## What's different?

Because this page has `transform.audience: engineer` and `transform.intent: onboarding`,
the knowledge-service applies onboarding-specific rules:

- Prerequisites are adapted per the override hint
- Tone is welcoming and step-by-step
- Troubleshooting content is simplified for new engineers

All of this is controlled by the service — not by prompt engineering in frontmatter.
