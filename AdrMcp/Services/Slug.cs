using System.Text.RegularExpressions;

namespace AdrMcp.Services;

internal static class Slug
{
    private static readonly Regex NonAlnum = new(@"[^a-z0-9]+", RegexOptions.Compiled);

    /// <summary>Turns a title into a kebab-case slug, e.g. "Use PostgreSQL!" -> "use-postgresql".</summary>
    public static string From(string title)
    {
        var s = NonAlnum.Replace(title.ToLowerInvariant(), "-").Trim('-');
        return string.IsNullOrEmpty(s) ? "adr" : s;
    }
}
