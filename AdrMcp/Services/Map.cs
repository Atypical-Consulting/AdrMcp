using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Maps domain <see cref="Adr"/> objects to wire response DTOs.</summary>
internal static class Map
{
    public static AdrSummary Summary(Adr a) =>
        new(a.Id, a.Title, EnumMap.ToWire(a.Status), a.Date == default ? null : a.Date.ToString("yyyy-MM-dd"), a.Tags);

    public static AdrDetail Detail(Adr a, string content) =>
        new(a.Id,
            a.Title,
            EnumMap.ToWire(a.Status),
            a.Date == default ? null : a.Date.ToString("yyyy-MM-dd"),
            a.Deciders,
            a.Tags,
            a.Links.Select(l => $"{EnumMap.ToWire(l.Type)} -> {l.TargetId}").ToList(),
            a.CodeRefs,
            content);
}
