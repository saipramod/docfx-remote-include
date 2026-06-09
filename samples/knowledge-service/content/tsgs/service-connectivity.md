If a build fails to fetch remote content, work through these steps in order.

### 1. Confirm the knowledge-service is running

```bash
curl http://localhost:5000/health
# -> { "status": "healthy" }
```

If this fails, start the service with `dotnet run` from `samples/knowledge-service`.

### 2. Verify the content path resolves

```bash
curl http://localhost:5000/content/snippets/prerequisites.md
```

A `404` means the `source` in your directive doesn't match a file under the
content root. Check for typos and the `.md` extension.

### 3. Check authentication

If the service requires auth, make sure `remoteinclude.json` sets the correct
`auth` mode and that any referenced environment variables are set. For the local
sample, `auth` is `{ "mode": "none" }`.

### 4. Inspect build output

Run the build with verbose logging to see the exact URL requested and the HTTP
status returned for each directive.
