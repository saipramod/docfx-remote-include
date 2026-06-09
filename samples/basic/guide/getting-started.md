# Getting Started

Welcome! This page pulls shared content from the knowledge-service so it stays
in sync across all documentation sites.

## Prerequisites

The following prerequisites are served from the knowledge-service and shared
across all guides:

[!remoteinclude[Prerequisites](snippets/prerequisites.md)]

## Installation

[!remoteinclude[Installation](snippets/install.md)]

## What just happened?

At build time the extension issued `GET http://localhost:5000/content/snippets/prerequisites.md`
and `GET http://localhost:5000/content/snippets/install.md`, parsed the responses
as markdown, and inlined them above. This means:

- The knowledge-service is the single source of truth for these snippets
- When prerequisites change, every site that includes them picks up the
  update on next build — no copy-paste, no drift
- The same service that provides content also transforms the final page
  for consistent tone
