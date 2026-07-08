namespace AdrMcp.Models;

/// <summary>A pair of ADRs that appear to conflict, with the reason and a similarity score.</summary>
public sealed record ConflictReport(int AdrId, int OtherId, string Reason, double Similarity);
