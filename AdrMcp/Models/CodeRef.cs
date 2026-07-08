namespace AdrMcp.Models;

/// <summary>
/// A reference from an ADR to the code it governs. Language-agnostic: a repo-relative
/// <paramref name="Path"/>, an optional <paramref name="Symbol"/> name expected to appear
/// in that file, and an optional <paramref name="Line"/> hint.
/// </summary>
public sealed record CodeRef(string Path, string? Symbol = null, int? Line = null);
