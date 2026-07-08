namespace AdrMcp.Services;

/// <summary>Lightweight line-based helpers for reading an ADR markdown body.</summary>
internal static class MarkdownSections
{
    /// <summary>Returns the H1 title (first <c># </c> line), or empty string.</summary>
    public static string ExtractTitle(string body)
    {
        foreach (var line in SplitLines(body))
        {
            var t = line.TrimStart();
            if (t.StartsWith("# ") && !t.StartsWith("## "))
                return t[2..].Trim();
        }
        return "";
    }

    /// <summary>All level-2 (<c>## </c>) heading titles, in order.</summary>
    public static IReadOnlyList<string> SectionHeadings(string body)
    {
        var result = new List<string>();
        foreach (var line in SplitLines(body))
        {
            var t = line.TrimStart();
            if (t.StartsWith("## ") && !t.StartsWith("### "))
                result.Add(t[3..].Trim());
        }
        return result;
    }

    /// <summary>Returns the content under a level-2 section (heading match is case-insensitive), or null.</summary>
    public static string? GetSection(string body, string heading)
    {
        var lines = SplitLines(body).ToList();
        int start = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("## ") && string.Equals(t[3..].Trim(), heading.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                start = i + 1;
                break;
            }
        }
        if (start < 0) return null;

        var sb = new System.Text.StringBuilder();
        for (int i = start; i < lines.Count; i++)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("## ") && !t.StartsWith("### ")) break;
            sb.AppendLine(lines[i]);
        }
        return sb.ToString().Trim();
    }

    public static bool HasSection(string body, string heading) =>
        SectionHeadings(body).Any(h => string.Equals(h, heading, StringComparison.OrdinalIgnoreCase));

    /// <summary>Replaces the content of a section, or appends the section if it does not exist.</summary>
    public static string SetSection(string body, string heading, string content)
    {
        var lines = SplitLines(body).ToList();
        int start = -1, end = lines.Count;
        for (int i = 0; i < lines.Count; i++)
        {
            var t = lines[i].TrimStart();
            if (t.StartsWith("## ") && string.Equals(t[3..].Trim(), heading.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                start = i;
                for (int j = i + 1; j < lines.Count; j++)
                {
                    var tj = lines[j].TrimStart();
                    if (tj.StartsWith("## ") && !tj.StartsWith("### ")) { end = j; break; }
                }
                break;
            }
        }

        var block = new List<string> { $"## {heading}", "", content.Trim(), "" };
        if (start < 0)
        {
            var appended = new List<string>(lines);
            if (appended.Count > 0 && appended[^1].Trim().Length != 0) appended.Add("");
            appended.AddRange(block);
            return string.Join("\n", appended).TrimEnd() + "\n";
        }

        var rebuilt = new List<string>();
        rebuilt.AddRange(lines.Take(start));
        rebuilt.AddRange(block);
        rebuilt.AddRange(lines.Skip(end));
        return string.Join("\n", rebuilt).TrimEnd() + "\n";
    }

    private static IEnumerable<string> SplitLines(string text) =>
        text.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
}
