---
transform:
  audience: engineer
  intent: onboarding
  overrides:
    prerequisites: "target macOS users"
---

# New Engineer Onboarding

Welcome to the team! This page assembles everything you need for your first week
by pulling shared content from the knowledge-service and adapting it for new hires.

## Setup

[!remoteinclude[Prerequisites](snippets/prerequisites.md)]

[!remoteinclude[Installation](snippets/install.md)]

## How Things Fit Together

[!remoteinclude[Architecture](guides/architecture.md)]

## When Something Breaks

[!remoteinclude[Service Connectivity](tsgs/service-connectivity.md)]

## What's Happening Behind the Scenes

Because this page declares `transform.audience: engineer` and
`transform.intent: onboarding`, the knowledge-service applies onboarding rules:
the prerequisites are adapted per the override hint, the tone is kept welcoming
and step-by-step, and any deviation from the central documentation standards is
flagged as a **Team Override**. None of that logic lives in this page — the
service owns it.
