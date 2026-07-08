using DiffPlex;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace AdrMcp.Services;

/// <summary>Produces a readable unified-style diff between two text versions.</summary>
internal static class DiffUtil
{
    public static string Unified(string oldText, string newText, string label)
    {
        var diff = InlineDiffBuilder.Diff(oldText, newText);
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"--- {label} (current)");
        sb.AppendLine($"+++ {label} (proposed)");
        foreach (var line in diff.Lines)
        {
            var prefix = line.Type switch
            {
                ChangeType.Inserted => "+",
                ChangeType.Deleted => "-",
                _ => " "
            };
            sb.Append(prefix).AppendLine(line.Text);
        }
        return sb.ToString();
    }
}
