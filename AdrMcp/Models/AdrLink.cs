namespace AdrMcp.Models;

/// <summary>A directed, typed link from one ADR to another (by target id).</summary>
public sealed record AdrLink(AdrLinkType Type, int TargetId);
