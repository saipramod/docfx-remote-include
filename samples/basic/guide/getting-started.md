# Getting started

Welcome! This page mixes local prose with a remote snippet that lives in a
public repository. Edit the snippet there and the next build of this site picks
it up — no changes needed in this repository.

## DocFX README

The README below is fetched from the `dotnet/docfx` GitHub repository and
rendered as a block:

[!remoteinclude[DocFX README](dotnet/docfx/main/README.md)]

## What just happened?

At build time the extension issued
`GET {baseUrl}/dotnet/docfx/main/README.md`, parsed the response as markdown,
and inlined it above this paragraph. If the source had returned a 404, one of
two things happens:

- With `allowMissing: false` the build fails loudly — desired in CI.
- With `allowMissing: true` (this sample) the directive renders as a small
  error box so the rest of the page still builds.
