using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>The allowed ADR status transitions.</summary>
public static class AdrLifecycle
{
    private static readonly Dictionary<AdrStatus, AdrStatus[]> Allowed = new()
    {
        [AdrStatus.Proposed] = new[] { AdrStatus.Accepted, AdrStatus.Rejected },
        [AdrStatus.Accepted] = new[] { AdrStatus.Deprecated, AdrStatus.Superseded },
        [AdrStatus.Rejected] = Array.Empty<AdrStatus>(),
        [AdrStatus.Deprecated] = new[] { AdrStatus.Superseded },
        [AdrStatus.Superseded] = Array.Empty<AdrStatus>()
    };

    public static bool CanTransition(AdrStatus from, AdrStatus to) =>
        from != to && Allowed.TryGetValue(from, out var targets) && targets.Contains(to);

    public static IReadOnlyList<AdrStatus> NextStates(AdrStatus from) =>
        Allowed.TryGetValue(from, out var targets) ? targets : Array.Empty<AdrStatus>();
}
