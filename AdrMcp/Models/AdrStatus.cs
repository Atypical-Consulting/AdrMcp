namespace AdrMcp.Models;

/// <summary>Lifecycle state of an ADR. Wire/serialized form is the lowercase name.</summary>
public enum AdrStatus
{
    Proposed,
    Accepted,
    Rejected,
    Deprecated,
    Superseded
}
