namespace AdrMcp.Models;

/// <summary>Compact ADR listing row.</summary>
public sealed record AdrSummary(int Id, string Title, string Status, string? Date, IReadOnlyList<string> Tags);

/// <summary>Full ADR view (or selected sections when requested).</summary>
public sealed record AdrDetail(
    int Id,
    string Title,
    string Status,
    string? Date,
    IReadOnlyList<string> Deciders,
    IReadOnlyList<string> Tags,
    IReadOnlyList<string> Links,
    IReadOnlyList<CodeRef> CodeRefs,
    string Content);

/// <summary>One decision-log / timeline entry.</summary>
public sealed record IndexEntry(int Id, string Title, string Status, string? Date);

/// <summary>A single proposed or applied change to a file on disk.</summary>
public sealed record FileChange(string Path, string Action, string Diff);

/// <summary>Result of a mutating tool call (preview or committed).</summary>
public sealed record MutationResult(bool Committed, string Message, IReadOnlyList<FileChange> Changes);

/// <summary>An ADR whose code references no longer resolve.</summary>
public sealed record StaleAdr(int Id, string Title, IReadOnlyList<CodeRef> BrokenRefs);

/// <summary>Coverage of an architectural area (mapped to an ADR tag).</summary>
public sealed record CoverageArea(string Area, int AdrCount, IReadOnlyList<int> AdrIds);

/// <summary>A drafted ADR proposal derived from a change/diff.</summary>
public sealed record AdrSuggestion(string SuggestedTitle, IReadOnlyList<CodeRef> CodeRefs, string DraftBody);
