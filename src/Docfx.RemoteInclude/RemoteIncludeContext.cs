namespace Docfx.RemoteInclude;

internal static class RemoteIncludeContext
{
    private static readonly AsyncLocal<Stack<string>?> s_stack = new();

    public static Stack<string> Stack => s_stack.Value ??= new Stack<string>();
}
