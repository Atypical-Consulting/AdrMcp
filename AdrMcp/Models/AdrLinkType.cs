namespace AdrMcp.Models;

/// <summary>Typed relationship between two ADRs.</summary>
public enum AdrLinkType
{
    Supersedes,
    SupersededBy,
    RelatesTo,
    ConflictsWith
}
