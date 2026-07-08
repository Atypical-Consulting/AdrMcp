using System.ComponentModel;
using System.Text.RegularExpressions;
using AdrMcp.Interfaces;
using AdrMcp.Models;
using AdrMcp.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AdrMcp.Tools;

/// <summary>Higher-order analysis over the ADR corpus: validation, conflicts, staleness, coverage.</summary>
[McpServerToolType]
public sealed class IntelligenceTools
{
    private readonly IAdrRepository _repo;
    private readonly IAdrValidator _validator;
    private readonly ISearchService _search;
    private readonly ICodeLinkProvider _codeLinks;
    private readonly IAdrTemplateService _templates;
    private readonly AdrOptions _options;

    public IntelligenceTools(
        IAdrRepository repo,
        IAdrValidator validator,
        ISearchService search,
        ICodeLinkProvider codeLinks,
        IAdrTemplateService templates,
        AdrOptions options)
    {
        _repo = repo;
        _validator = validator;
        _search = search;
        _codeLinks = codeLinks;
        _templates = templates;
        _options = options;
    }

    [McpServerTool(Name = "validate_adr", ReadOnly = true, Idempotent = true)]
    [Description("Validate one ADR (by id/slug) or the whole corpus: required sections, valid status, dangling links, duplicate ids.")]
    public IReadOnlyList<ValidationResult> ValidateAdr(
        [Description("ADR id or slug. Omit to validate every ADR.")] string? idOrSlug = null)
    {
        var all = _repo.LoadAll();
        if (!string.IsNullOrWhiteSpace(idOrSlug))
        {
            var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");
            return new[] { _validator.Validate(adr, all) };
        }
        return all.Select(a => _validator.Validate(a, all)).ToList();
    }

    [McpServerTool(Name = "detect_conflicts", ReadOnly = true, Idempotent = true)]
    [Description("Find ADR pairs that conflict: explicit conflicts-with links plus highly similar accepted decisions.")]
    public IReadOnlyList<ConflictReport> DetectConflicts()
        => _search.DetectConflicts(_repo.LoadAll());

    [McpServerTool(Name = "find_stale_adrs", ReadOnly = true, Idempotent = true)]
    [Description("Find non-rejected ADRs whose code_refs no longer resolve against the repository (stale decisions).")]
    public IReadOnlyList<StaleAdr> FindStaleAdrs()
    {
        var stale = new List<StaleAdr>();
        foreach (var adr in _repo.LoadAll())
        {
            if (adr.Status == AdrStatus.Rejected || adr.CodeRefs.Count == 0) continue;
            var broken = adr.CodeRefs.Where(r => !_codeLinks.Resolves(r, _options.RepoRoot)).ToList();
            if (broken.Count > 0) stale.Add(new StaleAdr(adr.Id, adr.Title, broken));
        }
        return stale;
    }

    [McpServerTool(Name = "coverage_report", ReadOnly = true, Idempotent = true)]
    [Description("Report ADR coverage per architectural area (mapped to tags). Areas with zero ADRs are decision gaps. Provide 'areas' to check a specific set, otherwise all tags are reported.")]
    public IReadOnlyList<CoverageArea> CoverageReport(
        [Description("Areas (tags) to check. Omit to report every tag present in the corpus.")] string[]? areas = null)
    {
        var all = _repo.LoadAll();
        var areaList = areas is { Length: > 0 }
            ? areas.ToList()
            : all.SelectMany(a => a.Tags).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(t => t).ToList();

        return areaList
            .Select(area =>
            {
                var matches = all.Where(a => a.Tags.Contains(area, StringComparer.OrdinalIgnoreCase)).ToList();
                return new CoverageArea(area, matches.Count, matches.Select(a => a.Id).ToList());
            })
            .OrderBy(c => c.AdrCount).ThenBy(c => c.Area)
            .ToList();
    }

    [McpServerTool(Name = "suggest_adr_from_change", ReadOnly = true)]
    [Description("Given a unified diff, draft an ADR proposal: a suggested title, code_refs for the changed files, and a ready-to-edit MADR body. Does not write anything.")]
    public AdrSuggestion SuggestAdrFromChange(
        [Description("A unified diff (e.g. output of `git diff`).")] string diff)
    {
        var files = ChangedFiles(diff);
        var primary = files.FirstOrDefault();
        var title = primary is null
            ? "Record an architectural decision"
            : $"Record decision behind changes to {primary}";

        var codeRefs = files.Select(f => new CodeRef(f)).ToList();
        var context = files.Count == 0
            ? "Describe the change and why a decision is needed."
            : $"The following files changed and may embody an architectural decision:\n\n" +
              string.Join("\n", files.Select(f => $"- `{f}`"));

        var body = _templates.Render(TemplateKind.Madr, new AdrDraft(title, context));
        return new AdrSuggestion(title, codeRefs, body);
    }

    private static readonly Regex GitDiff = new(@"^diff --git a/.+ b/(?<f>.+)$", RegexOptions.Multiline);
    private static readonly Regex PlusPlus = new(@"^\+\+\+ b/(?<f>.+)$", RegexOptions.Multiline);

    private static List<string> ChangedFiles(string diff)
    {
        var files = new List<string>();
        foreach (Match m in GitDiff.Matches(diff)) files.Add(m.Groups["f"].Value.Trim());
        if (files.Count == 0)
            foreach (Match m in PlusPlus.Matches(diff))
            {
                var f = m.Groups["f"].Value.Trim();
                if (f != "/dev/null") files.Add(f);
            }
        return files.Distinct().ToList();
    }
}
