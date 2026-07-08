using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Builds the typed link graph and traverses supersession chains.</summary>
public sealed class AdrGraphService : IAdrGraphService
{
    public AdrGraph Build(IReadOnlyList<Adr> all)
    {
        var nodes = all
            .Select(a => new AdrGraphNode(a.Id, a.Title, EnumMap.ToWire(a.Status)))
            .ToList();

        var edges = all
            .SelectMany(a => a.Links.Select(l => new AdrGraphEdge(a.Id, l.TargetId, EnumMap.ToWire(l.Type))))
            .ToList();

        return new AdrGraph(nodes, edges);
    }

    public RelatedAdrs Related(int id, IReadOnlyList<Adr> all)
    {
        var self = all.FirstOrDefault(a => a.Id == id);
        var title = self?.Title ?? $"ADR {id}";

        var outgoing = (self?.Links ?? new())
            .Select(l => new AdrGraphEdge(id, l.TargetId, EnumMap.ToWire(l.Type)))
            .ToList();

        var incoming = all
            .SelectMany(a => a.Links
                .Where(l => l.TargetId == id)
                .Select(l => new AdrGraphEdge(a.Id, id, EnumMap.ToWire(l.Type))))
            .ToList();

        return new RelatedAdrs(id, title, outgoing, incoming, SupersessionChain(id, all));
    }

    /// <summary>Follows 'superseded-by' links forward, so the caller can find the current decision.</summary>
    private static IReadOnlyList<int> SupersessionChain(int id, IReadOnlyList<Adr> all)
    {
        var byId = all.ToDictionary(a => a.Id);
        var chain = new List<int>();
        var seen = new HashSet<int> { id };
        var current = id;

        while (byId.TryGetValue(current, out var adr))
        {
            var next = adr.Links.FirstOrDefault(l => l.Type == AdrLinkType.SupersededBy)?.TargetId;
            if (next is null || !seen.Add(next.Value)) break;
            chain.Add(next.Value);
            current = next.Value;
        }
        return chain;
    }
}
