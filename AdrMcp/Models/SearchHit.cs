namespace AdrMcp.Models;

/// <summary>A ranked search result over the ADR corpus.</summary>
public sealed record SearchHit(int Id, string Title, double Score, string Snippet);
