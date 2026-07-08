namespace AdrMcp.Models;

public enum TemplateKind { Madr, Nygard }

/// <summary>Inputs used to render a new ADR body from a template.</summary>
public sealed record AdrDraft(
    string Title,
    string? Context = null,
    string? Decision = null,
    string? Consequences = null,
    IReadOnlyList<string>? Options = null);
