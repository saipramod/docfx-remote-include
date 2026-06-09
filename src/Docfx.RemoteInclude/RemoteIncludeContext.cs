namespace Docfx.RemoteInclude;

internal static class RemoteIncludeContext
{
    private static readonly AsyncLocal<Stack<string>?> s_stack = new();
    private static readonly AsyncLocal<string?> s_currentSource = new();

    public static Stack<string> Stack => s_stack.Value ??= new Stack<string>();

    /// <summary>
    /// The source path of the page currently being rendered (for passing to transform service).
    /// </summary>
    public static string? CurrentSource
    {
        get => s_currentSource.Value;
        set => s_currentSource.Value = value;
    }
}
