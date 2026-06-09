namespace Docfx.RemoteInclude;

internal static class RemoteIncludeDirective
{
    public const string BracketPrefix = "[!remoteinclude";

    /// <summary>
    /// Parses <c>[!remoteinclude[title](source)]</c> starting at <paramref name="start"/>
    /// in <paramref name="text"/>. Returns the exclusive end index on success.
    /// </summary>
    public static bool TryParseBracket(string text, int start, int end, out int consumedEnd, out string source, out string title)
    {
        consumedEnd = start;
        source = string.Empty;
        title = string.Empty;

        var p = start;
        for (var i = 0; i < BracketPrefix.Length; i++)
        {
            if (p > end) return false;
            if (char.ToLowerInvariant(text[p]) != BracketPrefix[i]) return false;
            p++;
        }

        if (p > end || text[p] != '[') return false;
        p++;
        var titleStart = p;
        while (p <= end && text[p] != ']' && text[p] != '\n') p++;
        if (p > end || text[p] != ']') return false;
        var t = text.Substring(titleStart, p - titleStart);
        p++;

        if (p > end || text[p] != '(') return false;
        p++;

        // Source: characters up to whitespace or ')'.
        var srcStart = p;
        while (p <= end && text[p] != ')' && text[p] != '\n' && !char.IsWhiteSpace(text[p])) p++;
        if (p > end) return false;
        var s = text.Substring(srcStart, p - srcStart);

        // Tolerate optional trailing whitespace before the closing paren.
        while (p <= end && (text[p] == ' ' || text[p] == '\t')) p++;

        if (p > end || text[p] != ')') return false;
        p++;

        if (p > end || text[p] != ']') return false;
        if (s.Length == 0) return false;

        source = s;
        title = t;
        consumedEnd = p + 1;
        return true;
    }
}
