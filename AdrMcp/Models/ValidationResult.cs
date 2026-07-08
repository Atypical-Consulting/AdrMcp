namespace AdrMcp.Models;

public enum ValidationSeverity { Error, Warning }

public sealed record ValidationIssue(ValidationSeverity Severity, string Message);

/// <summary>Outcome of validating a single ADR.</summary>
public sealed record ValidationResult(int AdrId, string Title, IReadOnlyList<ValidationIssue> Issues)
{
    public bool IsValid => Issues.All(i => i.Severity != ValidationSeverity.Error);
}
