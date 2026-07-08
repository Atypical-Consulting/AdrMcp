using AdrMcp.Interfaces;
using AdrMcp.Models;

namespace AdrMcp.Services;

/// <summary>Renders ADR bodies for the supported templates (MADR 4.0 default, Nygard classic).</summary>
public sealed class AdrTemplateService : IAdrTemplateService
{
    private const string Placeholder = "_To be documented._";

    public string Render(TemplateKind kind, AdrDraft draft) => kind switch
    {
        TemplateKind.Nygard => RenderNygard(draft),
        _ => RenderMadr(draft)
    };

    public IReadOnlyList<string> RequiredSections(TemplateKind kind) => kind switch
    {
        TemplateKind.Nygard => new[] { "Context", "Decision", "Consequences" },
        _ => new[] { "Context and Problem Statement", "Decision Outcome", "Consequences" }
    };

    private static string RenderMadr(AdrDraft d)
    {
        var options = d.Options is { Count: > 0 }
            ? string.Join("\n", d.Options.Select(o => $"- {o}"))
            : "- _Option 1_\n- _Option 2_";

        return $"""
        # {d.Title}

        ## Context and Problem Statement

        {Or(d.Context)}

        ## Considered Options

        {options}

        ## Decision Outcome

        {Or(d.Decision)}

        ## Consequences

        {Or(d.Consequences)}
        """;
    }

    private static string RenderNygard(AdrDraft d) => $"""
        # {d.Title}

        ## Context

        {Or(d.Context)}

        ## Decision

        {Or(d.Decision)}

        ## Consequences

        {Or(d.Consequences)}
        """;

    private static string Or(string? s) => string.IsNullOrWhiteSpace(s) ? Placeholder : s.Trim();
}
