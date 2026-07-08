using System.Text.RegularExpressions;
using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Lexical (default) and semantic search plus similarity-based conflict detection.</summary>
public sealed class SearchService : ISearchService
{
    private static readonly Regex Token = new(@"[a-zA-Z0-9]+", RegexOptions.Compiled);
    private readonly IEmbeddingProvider _embeddings;

    public SearchService(IEmbeddingProvider embeddings) => _embeddings = embeddings;

    public IReadOnlyList<SearchHit> Search(string query, bool semantic, int topK, IReadOnlyList<Adr> all)
    {
        if (topK <= 0) topK = 10;
        var scored = new List<SearchHit>();

        if (semantic)
        {
            var qv = _embeddings.Embed(query);
            foreach (var adr in all)
            {
                var score = _embeddings.Similarity(qv, _embeddings.Embed(DocText(adr)));
                if (score > 0) scored.Add(new SearchHit(adr.Id, adr.Title, Math.Round(score, 4), Snippet(adr, query)));
            }
        }
        else
        {
            var terms = Tokenize(query).ToHashSet();
            foreach (var adr in all)
            {
                var docTokens = Tokenize(DocText(adr)).ToList();
                var present = terms.Count(t => docTokens.Contains(t));
                if (present == 0) continue;
                var occurrences = docTokens.Count(terms.Contains);
                var score = present + occurrences / 1000.0; // primary: distinct terms; tiebreak: frequency
                scored.Add(new SearchHit(adr.Id, adr.Title, Math.Round(score, 4), Snippet(adr, query)));
            }
        }

        return scored.OrderByDescending(h => h.Score).ThenBy(h => h.Id).Take(topK).ToList();
    }

    public IReadOnlyList<ConflictReport> DetectConflicts(IReadOnlyList<Adr> all, double threshold = 0.72)
    {
        var reports = new List<ConflictReport>();

        // 1) Explicit conflicts-with links.
        foreach (var adr in all)
            foreach (var link in adr.Links.Where(l => l.Type == AdrLinkType.ConflictsWith))
                if (adr.Id < link.TargetId)
                    reports.Add(new ConflictReport(adr.Id, link.TargetId, "Explicit conflicts-with link.", 1.0));

        // 2) High textual similarity between active (accepted) decisions.
        var active = all.Where(a => a.Status == AdrStatus.Accepted).ToList();
        var vectors = active.ToDictionary(a => a.Id, a => _embeddings.Embed(DocText(a)));
        for (int i = 0; i < active.Count; i++)
            for (int j = i + 1; j < active.Count; j++)
            {
                var sim = _embeddings.Similarity(vectors[active[i].Id], vectors[active[j].Id]);
                if (sim >= threshold && !AlreadyLinked(active[i], active[j].Id))
                    reports.Add(new ConflictReport(active[i].Id, active[j].Id,
                        "High content similarity between two accepted ADRs (possible overlap/contradiction).",
                        Math.Round(sim, 4)));
            }

        return reports.OrderByDescending(r => r.Similarity).ToList();
    }

    private static bool AlreadyLinked(Adr adr, int otherId) =>
        adr.Links.Any(l => l.TargetId == otherId);

    private static string DocText(Adr adr) => $"{adr.Title}\n{string.Join(" ", adr.Tags)}\n{adr.Body}";

    private static IEnumerable<string> Tokenize(string text) =>
        Token.Matches(text.ToLowerInvariant()).Select(m => m.Value).Where(w => w.Length >= 2);

    private static string Snippet(Adr adr, string query)
    {
        var body = Regex.Replace(adr.Body, @"\s+", " ").Trim();
        var terms = Tokenize(query).ToList();
        int idx = -1;
        foreach (var t in terms)
        {
            idx = body.IndexOf(t, StringComparison.OrdinalIgnoreCase);
            if (idx >= 0) break;
        }
        int start = idx < 0 ? 0 : Math.Max(0, idx - 40);
        var slice = body.Substring(start, Math.Min(180, body.Length - start));
        return (start > 0 ? "…" : "") + slice + (start + slice.Length < body.Length ? "…" : "");
    }
}
