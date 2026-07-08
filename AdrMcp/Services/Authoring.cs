using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Shared preview-or-commit logic for mutating tools.</summary>
internal static class Authoring
{
    /// <summary>
    /// Computes the change for one ADR file. When <paramref name="commit"/> is true the file is
    /// written; otherwise only the unified diff is produced (preview-by-default).
    /// </summary>
    public static FileChange Stage(IAdrRepository repo, Adr adr, bool commit)
    {
        var path = repo.PathFor(adr);
        var current = repo.ReadRaw(adr);
        var proposed = repo.Render(adr);
        var verb = string.IsNullOrEmpty(current) ? "create" : "update";
        var diff = DiffUtil.Unified(current, proposed, adr.FileName);

        if (commit) repo.Save(adr);

        var action = commit ? $"{verb}d {path}" : $"would {verb} {path}";
        return new FileChange(path, action, diff);
    }

    public static MutationResult Result(bool commit, string message, params FileChange[] changes) =>
        new(commit, commit ? message : $"PREVIEW — {message} (set preview_only=false to write)", changes);
}
