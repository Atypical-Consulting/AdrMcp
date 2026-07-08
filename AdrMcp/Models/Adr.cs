namespace AdrMcp.Models;

/// <summary>
/// An Architectural Decision Record loaded from disk. <see cref="Body"/> is the markdown
/// after the YAML frontmatter (including the <c># Title</c> heading and all sections).
/// </summary>
public sealed class Adr
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Title { get; set; } = "";
    public AdrStatus Status { get; set; } = AdrStatus.Proposed;
    public DateOnly Date { get; set; }
    public List<string> Deciders { get; set; } = new();
    public List<string> Tags { get; set; } = new();
    public List<AdrLink> Links { get; set; } = new();
    public List<CodeRef> CodeRefs { get; set; } = new();

    /// <summary>Markdown body (everything after the frontmatter block).</summary>
    public string Body { get; set; } = "";

    /// <summary>Absolute path on disk, or null for an ADR not yet persisted.</summary>
    public string? FilePath { get; set; }

    /// <summary>Conventional file name for this ADR, e.g. <c>0007-use-postgres.md</c>.</summary>
    public string FileName => $"{Id:D4}-{Slug}.md";
}
