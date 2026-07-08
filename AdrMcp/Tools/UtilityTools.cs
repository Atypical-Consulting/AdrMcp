using System.ComponentModel;
using System.Text;
using AdrMcp.Interfaces;
using AdrMcp.Models;
using AdrMcp.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AdrMcp.Tools;

/// <summary>Export and diff utilities.</summary>
[McpServerToolType]
public sealed class UtilityTools
{
    private readonly IAdrRepository _repo;

    public UtilityTools(IAdrRepository repo) => _repo = repo;

    [McpServerTool(Name = "render_index", Destructive = true, Idempotent = true)]
    [Description("Regenerate the browsable decision index (README.md in the ADR folder). Preview-by-default.")]
    public MutationResult RenderIndex(
        [Description("Preview only (default true). Set false to write README.md.")] bool previewOnly = true)
    {
        var all = _repo.LoadAll();
        var sb = new StringBuilder();
        sb.AppendLine("# Architectural Decision Records");
        sb.AppendLine();
        sb.AppendLine("| ID | Title | Status | Date |");
        sb.AppendLine("| --- | --- | --- | --- |");
        foreach (var a in all.OrderBy(a => a.Id))
        {
            var date = a.Date == default ? "" : a.Date.ToString("yyyy-MM-dd");
            sb.AppendLine($"| {a.Id:D4} | [{a.Title}]({a.FileName}) | {EnumMap.ToWire(a.Status)} | {date} |");
        }
        var proposed = sb.ToString().Replace("\r\n", "\n").TrimEnd() + "\n";

        var path = Path.Combine(_repo.AdrRoot, "README.md");
        var current = File.Exists(path) ? File.ReadAllText(path) : "";
        var diff = DiffUtil.Unified(current, proposed, "README.md");

        var commit = !previewOnly;
        if (commit)
        {
            Directory.CreateDirectory(_repo.AdrRoot);
            File.WriteAllText(path, proposed, new UTF8Encoding(false));
        }

        var action = commit ? $"wrote {path}" : $"would write {path}";
        return Authoring.Result(commit, $"render decision index ({all.Count} ADRs)", new FileChange(path, action, diff));
    }

    [McpServerTool(Name = "diff_adr", ReadOnly = true, Idempotent = true)]
    [Description("Diff two ADRs' bodies, or (when 'other' is omitted) diff an ADR's on-disk file against its canonical rendering to reveal formatting drift.")]
    public string DiffAdr(
        [Description("ADR id or slug.")] string idOrSlug,
        [Description("Optional second ADR id or slug to diff against.")] string? other = null)
    {
        var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");

        if (!string.IsNullOrWhiteSpace(other))
        {
            var b = _repo.Find(other) ?? throw new McpException($"ADR '{other}' not found.");
            return DiffUtil.Unified(adr.Body, b.Body, $"ADR {adr.Id:D4} vs ADR {b.Id:D4}");
        }

        return DiffUtil.Unified(_repo.ReadRaw(adr), _repo.Render(adr), adr.FileName);
    }
}
