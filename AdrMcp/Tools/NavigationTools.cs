using System.ComponentModel;
using AdrMcp.Interfaces;
using AdrMcp.Models;
using AdrMcp.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AdrMcp.Tools;

/// <summary>Read-only navigation and search over the ADR corpus.</summary>
[McpServerToolType]
public sealed class NavigationTools
{
    private readonly IAdrRepository _repo;
    private readonly IAdrGraphService _graph;
    private readonly ISearchService _search;

    public NavigationTools(IAdrRepository repo, IAdrGraphService graph, ISearchService search)
    {
        _repo = repo;
        _graph = graph;
        _search = search;
    }

    [McpServerTool(Name = "list_adrs", ReadOnly = true, Idempotent = true)]
    [Description("List ADRs, optionally filtered by status, tag, or date range. Returns compact summaries.")]
    public IReadOnlyList<AdrSummary> ListAdrs(
        [Description("Filter by status: proposed, accepted, rejected, deprecated, superseded.")] string? status = null,
        [Description("Filter to ADRs carrying this tag.")] string? tag = null,
        [Description("Only ADRs dated on/after this ISO date (yyyy-MM-dd).")] string? since = null,
        [Description("Only ADRs dated on/before this ISO date (yyyy-MM-dd).")] string? until = null)
    {
        IEnumerable<Adr> adrs = _repo.LoadAll();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = EnumMap.ParseStatus(status);
            adrs = adrs.Where(a => a.Status == s);
        }
        if (!string.IsNullOrWhiteSpace(tag))
            adrs = adrs.Where(a => a.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase));
        if (DateOnly.TryParse(since, out var from))
            adrs = adrs.Where(a => a.Date != default && a.Date >= from);
        if (DateOnly.TryParse(until, out var to))
            adrs = adrs.Where(a => a.Date != default && a.Date <= to);

        return adrs.Select(Map.Summary).ToList();
    }

    [McpServerTool(Name = "get_adr", ReadOnly = true, Idempotent = true)]
    [Description("Get a single ADR by id or slug. Pass 'sections' to return only specific ## sections (token-efficient).")]
    public AdrDetail GetAdr(
        [Description("ADR id (e.g. 7) or slug (e.g. use-postgres).")] string idOrSlug,
        [Description("Optional list of section headings to return instead of the full body.")] string[]? sections = null)
    {
        var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");

        string content;
        if (sections is { Length: > 0 })
        {
            var parts = sections.Select(h =>
            {
                var body = MarkdownSections.GetSection(adr.Body, h);
                return body is null ? $"## {h}\n\n_(section not found)_" : $"## {h}\n\n{body}";
            });
            content = string.Join("\n\n", parts);
        }
        else content = adr.Body;

        return Map.Detail(adr, content);
    }

    [McpServerTool(Name = "search_adrs", ReadOnly = true, Idempotent = true)]
    [Description("Search ADRs. Lexical by default; set semantic=true for term-vector similarity ranking.")]
    public IReadOnlyList<SearchHit> SearchAdrs(
        [Description("The search query.")] string query,
        [Description("Use semantic (term-vector) ranking instead of lexical term matching.")] bool semantic = false,
        [Description("Maximum number of hits to return.")] int topK = 10)
        => _search.Search(query, semantic, topK, _repo.LoadAll());

    [McpServerTool(Name = "get_adr_index", ReadOnly = true, Idempotent = true)]
    [Description("Return the decision log: every ADR as a chronological/id-ordered timeline entry.")]
    public IReadOnlyList<IndexEntry> GetAdrIndex()
        => _repo.LoadAll()
            .OrderBy(a => a.Date == default ? DateOnly.MaxValue : a.Date).ThenBy(a => a.Id)
            .Select(a => new IndexEntry(a.Id, a.Title, EnumMap.ToWire(a.Status), a.Date == default ? null : a.Date.ToString("yyyy-MM-dd")))
            .ToList();

    [McpServerTool(Name = "find_related_adrs", ReadOnly = true, Idempotent = true)]
    [Description("Find ADRs related to a given ADR: incoming/outgoing links plus its supersession chain.")]
    public RelatedAdrs FindRelatedAdrs([Description("ADR id or slug.")] string idOrSlug)
    {
        var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");
        return _graph.Related(adr.Id, _repo.LoadAll());
    }

    [McpServerTool(Name = "get_adr_graph", ReadOnly = true, Idempotent = true)]
    [Description("Return the full ADR relationship graph (nodes + typed edges).")]
    public AdrGraph GetAdrGraph() => _graph.Build(_repo.LoadAll());
}
