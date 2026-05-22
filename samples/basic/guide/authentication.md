# Authentication

This page demonstrates the optional **AI rewrite** hook. The block below is
fetched from a remote source and then handed to the configured
`IRewriteService` with a rewrite hint. Without the AI section configured,
the content is inlined verbatim.

[!remoteinclude[Markdig README](xoofx/markdig/master/readme.md)]

## How the hint is delivered

The string after the source is passed verbatim to your `IRewriteService` along
with the fetched snippet and the surrounding section (the heading and prose
above this directive). The CLI's built-in Azure OpenAI implementation turns
this into a single chat completion with a fixed, opinionated system prompt;
your own implementation can do whatever you like.

If no hint is supplied (just `[!remoteinclude[t](source)]`), the rewriter is
never called and the snippet is inlined verbatim — keeping the AI path strictly
opt-in per directive.
