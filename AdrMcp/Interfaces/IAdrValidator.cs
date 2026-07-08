using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>Validates ADRs for format compliance and link integrity.</summary>
public interface IAdrValidator
{
    /// <summary>Validates one ADR against the full corpus (needed to resolve links / ids).</summary>
    ValidationResult Validate(Adr adr, IReadOnlyList<Adr> all);
}
