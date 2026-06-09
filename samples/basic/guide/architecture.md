# Architecture

This page inlines the platform architecture overview from the knowledge-service.
The content is maintained centrally and stays consistent across all sites.

[!remoteinclude[Architecture](guides/architecture.md)]

## How transforms work with architecture docs

When this page is built, the assembled content (local prose + remote architecture
doc) is sent to `POST /transform` on the knowledge-service. The service can:

- Enforce consistent heading levels
- Ensure diagrams have alt text
- Validate that required sections (Security Model, Data Flow) are present
- Adapt terminology for the target audience
