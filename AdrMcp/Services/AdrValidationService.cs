using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Checks ADRs for structural compliance, valid status, and link integrity.</summary>
public sealed class AdrValidationService : IAdrValidator
{
    // A valid ADR must carry a context-style, a decision-style, and a consequences section.
    private static readonly string[] ContextHeadings = { "Context and Problem Statement", "Context" };
    private static readonly string[] DecisionHeadings = { "Decision Outcome", "Decision" };
    private static readonly string[] ConsequencesHeadings = { "Consequences" };

    public ValidationResult Validate(Adr adr, IReadOnlyList<Adr> all)
    {
        var issues = new List<ValidationIssue>();

        if (string.IsNullOrWhiteSpace(adr.Title))
            issues.Add(new(ValidationSeverity.Error, "ADR has no title (missing H1 / frontmatter title)."));

        if (adr.Date == default)
            issues.Add(new(ValidationSeverity.Warning, "ADR has no date."));

        RequireOneOf(adr, ContextHeadings, issues);
        RequireOneOf(adr, DecisionHeadings, issues);
        RequireOneOf(adr, ConsequencesHeadings, issues);

        if (all.Count(a => a.Id == adr.Id) > 1)
            issues.Add(new(ValidationSeverity.Error, $"Duplicate ADR id {adr.Id}."));

        var ids = all.Select(a => a.Id).ToHashSet();
        foreach (var link in adr.Links)
            if (!ids.Contains(link.TargetId))
                issues.Add(new(ValidationSeverity.Error,
                    $"Dangling link: {EnumMap.ToWire(link.Type)} -> ADR {link.TargetId} does not exist."));

        if (adr.Status == AdrStatus.Superseded &&
            !adr.Links.Any(l => l.Type == AdrLinkType.SupersededBy))
            issues.Add(new(ValidationSeverity.Warning,
                "Status is 'superseded' but no 'superseded-by' link records the replacement."));

        return new ValidationResult(adr.Id, adr.Title, issues);
    }

    private static void RequireOneOf(Adr adr, string[] headings, List<ValidationIssue> issues)
    {
        if (!headings.Any(h => MarkdownSections.HasSection(adr.Body, h)))
            issues.Add(new(ValidationSeverity.Error,
                $"Missing required section (one of: {string.Join(", ", headings)})."));
    }
}
