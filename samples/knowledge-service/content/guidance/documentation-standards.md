# Documentation Standards

These standards are the source of truth for how pages on this platform should be
written. The knowledge-service `/transform` endpoint compares each assembled page
against this guidance and annotates any deviations as **Team Override** callouts.

## Page Structure

Every page must contain the following, in order:

1. **Title** — exactly one H1 that names the page.
2. **Summary** — one or two sentences describing what the page covers.
3. **Body** — the content, organized under H2 and deeper headings.
4. **Next steps / references** — links to related pages where relevant.

## Heading Rules

- Exactly one H1 per page (the title). Everything else is H2 or deeper.
- No skipped levels (don't jump from H2 to H4).
- Do not include YAML frontmatter in the rendered body.

## Tone

- Write for engineers: clear, direct, and task-oriented.
- Prefer the active voice and present tense.
- Define an acronym the first time it appears.

## Code and Examples

- Every runnable example must specify its language for syntax highlighting.
- Keep commands copy-pasteable; avoid placeholders that look like real values.
- Show expected output where it helps the reader confirm success.
