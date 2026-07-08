using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>Full-text and semantic search over the ADR corpus.</summary>
public interface ISearchService
{
    IReadOnlyList<SearchHit> Search(string query, bool semantic, int topK, IReadOnlyList<Adr> all);

    /// <summary>Finds ADR pairs whose content is highly similar (conflict candidates).</summary>
    IReadOnlyList<ConflictReport> DetectConflicts(IReadOnlyList<Adr> all, double threshold = 0.72);
}
