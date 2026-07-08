using AdrMcp.Models;

namespace AdrMcp.Interfaces;

/// <summary>Renders new ADR bodies from a template.</summary>
public interface IAdrTemplateService
{
    /// <summary>Renders the markdown body (including the <c># Title</c> heading) for a draft.</summary>
    string Render(TemplateKind kind, AdrDraft draft);

    /// <summary>The section headings a valid ADR of this template must contain.</summary>
    IReadOnlyList<string> RequiredSections(TemplateKind kind);
}
