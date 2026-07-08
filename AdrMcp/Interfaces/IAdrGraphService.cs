using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>Builds the typed relationship graph across ADRs.</summary>
public interface IAdrGraphService
{
    /// <summary>The full node/edge graph for the corpus.</summary>
    AdrGraph Build(IReadOnlyList<Adr> all);

    /// <summary>Incoming/outgoing relations and supersession chain for one ADR.</summary>
    RelatedAdrs Related(int id, IReadOnlyList<Adr> all);
}
