using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Maps enums to/from their stable on-disk string form.</summary>
internal static class EnumMap
{
    public static string ToWire(AdrStatus status) => status.ToString().ToLowerInvariant();

    public static AdrStatus ParseStatus(string? s) =>
        Enum.TryParse<AdrStatus>(s, ignoreCase: true, out var v) ? v : AdrStatus.Proposed;

    public static string ToWire(AdrLinkType type) => type switch
    {
        AdrLinkType.Supersedes => "supersedes",
        AdrLinkType.SupersededBy => "superseded-by",
        AdrLinkType.RelatesTo => "relates-to",
        AdrLinkType.ConflictsWith => "conflicts-with",
        _ => "relates-to"
    };

    public static AdrLinkType ParseLinkType(string? s) => (s ?? "").Trim().ToLowerInvariant() switch
    {
        "supersedes" => AdrLinkType.Supersedes,
        "superseded-by" or "superseded_by" => AdrLinkType.SupersededBy,
        "conflicts-with" or "conflicts_with" => AdrLinkType.ConflictsWith,
        _ => AdrLinkType.RelatesTo
    };

    public static AdrLinkType Inverse(AdrLinkType type) => type switch
    {
        AdrLinkType.Supersedes => AdrLinkType.SupersededBy,
        AdrLinkType.SupersededBy => AdrLinkType.Supersedes,
        _ => type // relates-to and conflicts-with are symmetric
    };
}
