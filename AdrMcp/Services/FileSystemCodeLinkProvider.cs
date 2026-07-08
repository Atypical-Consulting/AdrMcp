using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>
/// Language-agnostic default: a code reference resolves if its file exists under the repo
/// root and (when a symbol is given) that symbol's text appears in the file.
/// </summary>
public sealed class FileSystemCodeLinkProvider : ICodeLinkProvider
{
    public string Name => "filesystem";

    public bool Resolves(CodeRef reference, string repoRoot)
    {
        if (string.IsNullOrWhiteSpace(reference.Path)) return false;

        var full = Path.IsPathRooted(reference.Path)
            ? reference.Path
            : Path.Combine(repoRoot, reference.Path);

        if (!File.Exists(full)) return false;
        if (string.IsNullOrWhiteSpace(reference.Symbol)) return true;

        try
        {
            return File.ReadAllText(full).Contains(reference.Symbol!, StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
