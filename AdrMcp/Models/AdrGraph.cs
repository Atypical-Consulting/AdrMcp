namespace AdrMcp.Models;

public sealed record AdrGraphNode(int Id, string Title, string Status);

public sealed record AdrGraphEdge(int From, int To, string Type);

/// <summary>The typed relationship graph across all ADRs.</summary>
public sealed record AdrGraph(IReadOnlyList<AdrGraphNode> Nodes, IReadOnlyList<AdrGraphEdge> Edges);

/// <summary>Incoming and outgoing relations for a single ADR.</summary>
public sealed record RelatedAdrs(
    int Id,
    string Title,
    IReadOnlyList<AdrGraphEdge> Outgoing,
    IReadOnlyList<AdrGraphEdge> Incoming,
    IReadOnlyList<int> SupersessionChain);
