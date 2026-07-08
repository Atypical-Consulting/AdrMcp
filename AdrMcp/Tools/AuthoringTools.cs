using System.ComponentModel;
using AdrMcp.Interfaces;
using AdrMcp.Models;
using AdrMcp.Services;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace AdrMcp.Tools;

/// <summary>Create and evolve ADRs. All mutations are preview-by-default (preview_only=true).</summary>
[McpServerToolType]
public sealed class AuthoringTools
{
    private readonly IAdrRepository _repo;
    private readonly IAdrTemplateService _templates;

    public AuthoringTools(IAdrRepository repo, IAdrTemplateService templates)
    {
        _repo = repo;
        _templates = templates;
    }

    [McpServerTool(Name = "create_adr", Destructive = false)]
    [Description("Create a new ADR from a template (madr or nygard). Previews the file by default; set preview_only=false to write it.")]
    public MutationResult CreateAdr(
        [Description("The decision title, e.g. 'Use PostgreSQL for persistence'.")] string title,
        [Description("Context and problem statement.")] string? context = null,
        [Description("The decision / chosen option.")] string? decision = null,
        [Description("Resulting consequences (good and bad).")] string? consequences = null,
        [Description("Template: 'madr' (default) or 'nygard'.")] string template = "madr",
        [Description("Considered options (for the MADR template).")] string[]? options = null,
        [Description("Tags / architectural areas.")] string[]? tags = null,
        [Description("Deciders (people/roles).")] string[]? deciders = null,
        [Description("Preview only (default true). Set false to write to disk.")] bool previewOnly = true)
    {
        var kind = ParseTemplate(template);
        var draft = new AdrDraft(title, context, decision, consequences, options);
        var adr = new Adr
        {
            Id = _repo.NextId(),
            Slug = Slug.From(title),
            Title = title,
            Status = AdrStatus.Proposed,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Tags = tags?.ToList() ?? new(),
            Deciders = deciders?.ToList() ?? new(),
            Body = _templates.Render(kind, draft)
        };

        var change = Authoring.Stage(_repo, adr, !previewOnly);
        return Authoring.Result(!previewOnly, $"create ADR {adr.Id:D4} '{title}'", change);
    }

    [McpServerTool(Name = "update_adr", Destructive = true, Idempotent = true)]
    [Description("Replace (or append) the content of a single ## section of an ADR. Preview-by-default.")]
    public MutationResult UpdateAdr(
        [Description("ADR id or slug.")] string idOrSlug,
        [Description("The ## section heading to set, e.g. 'Consequences'.")] string section,
        [Description("The new section content (markdown).")] string content,
        [Description("Preview only (default true). Set false to write to disk.")] bool previewOnly = true)
    {
        var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");
        adr.Body = MarkdownSections.SetSection(adr.Body, section, content);

        var change = Authoring.Stage(_repo, adr, !previewOnly);
        return Authoring.Result(!previewOnly, $"update section '{section}' of ADR {adr.Id:D4}", change);
    }

    [McpServerTool(Name = "set_status", Destructive = true, Idempotent = true)]
    [Description("Transition an ADR's status. Enforces the lifecycle (e.g. proposed->accepted). Preview-by-default.")]
    public MutationResult SetStatus(
        [Description("ADR id or slug.")] string idOrSlug,
        [Description("Target status: accepted, rejected, deprecated, superseded.")] string status,
        [Description("Preview only (default true). Set false to write to disk.")] bool previewOnly = true)
    {
        var adr = _repo.Find(idOrSlug) ?? throw new McpException($"ADR '{idOrSlug}' not found.");
        var target = EnumMap.ParseStatus(status);

        if (!AdrLifecycle.CanTransition(adr.Status, target))
        {
            var next = AdrLifecycle.NextStates(adr.Status).Select(EnumMap.ToWire).ToList();
            var allowed = next.Count > 0 ? string.Join(", ", next) : "(none)";
            throw new McpException(
                $"Illegal transition {EnumMap.ToWire(adr.Status)} -> {EnumMap.ToWire(target)}. " +
                $"Allowed from {EnumMap.ToWire(adr.Status)}: {allowed}.");
        }

        adr.Status = target;
        var change = Authoring.Stage(_repo, adr, !previewOnly);
        return Authoring.Result(!previewOnly, $"set ADR {adr.Id:D4} status to {EnumMap.ToWire(target)}", change);
    }

    [McpServerTool(Name = "supersede_adr", Destructive = true)]
    [Description("Create a new ADR that supersedes an existing one: writes the new ADR (accepted), marks the old superseded, and links both — in one call. Preview-by-default.")]
    public MutationResult SupersedeAdr(
        [Description("Id or slug of the ADR being superseded.")] string oldIdOrSlug,
        [Description("Title of the new (replacement) ADR.")] string newTitle,
        [Description("Context of the new decision.")] string? context = null,
        [Description("The new decision.")] string? decision = null,
        [Description("Consequences of the new decision.")] string? consequences = null,
        [Description("Template: 'madr' (default) or 'nygard'.")] string template = "madr",
        [Description("Tags for the new ADR.")] string[]? tags = null,
        [Description("Preview only (default true). Set false to write to disk.")] bool previewOnly = true)
    {
        var old = _repo.Find(oldIdOrSlug) ?? throw new McpException($"ADR '{oldIdOrSlug}' not found.");
        var kind = ParseTemplate(template);

        var neu = new Adr
        {
            Id = _repo.NextId(),
            Slug = Slug.From(newTitle),
            Title = newTitle,
            Status = AdrStatus.Accepted,
            Date = DateOnly.FromDateTime(DateTime.Today),
            Tags = tags?.ToList() ?? new(old.Tags),
            Body = _templates.Render(kind, new AdrDraft(newTitle, context, decision, consequences))
        };
        neu.Links.Add(new AdrLink(AdrLinkType.Supersedes, old.Id));

        old.Status = AdrStatus.Superseded;
        if (!old.Links.Any(l => l.Type == AdrLinkType.SupersededBy && l.TargetId == neu.Id))
            old.Links.Add(new AdrLink(AdrLinkType.SupersededBy, neu.Id));

        var commit = !previewOnly;
        var newChange = Authoring.Stage(_repo, neu, commit);   // write new first so its id exists
        var oldChange = Authoring.Stage(_repo, old, commit);
        return Authoring.Result(commit, $"ADR {neu.Id:D4} supersedes ADR {old.Id:D4}", newChange, oldChange);
    }

    [McpServerTool(Name = "link_adrs", Destructive = true, Idempotent = true)]
    [Description("Create a typed link between two ADRs (supersedes, superseded-by, relates-to, conflicts-with). Adds the inverse on the target unless bidirectional=false. Preview-by-default.")]
    public MutationResult LinkAdrs(
        [Description("Source ADR id or slug.")] string fromIdOrSlug,
        [Description("Target ADR id or slug.")] string toIdOrSlug,
        [Description("Link type: relates-to (default), supersedes, superseded-by, conflicts-with.")] string type = "relates-to",
        [Description("Also add the inverse link on the target ADR (default true).")] bool bidirectional = true,
        [Description("Preview only (default true). Set false to write to disk.")] bool previewOnly = true)
    {
        var from = _repo.Find(fromIdOrSlug) ?? throw new McpException($"ADR '{fromIdOrSlug}' not found.");
        var to = _repo.Find(toIdOrSlug) ?? throw new McpException($"ADR '{toIdOrSlug}' not found.");
        if (from.Id == to.Id) throw new McpException("Cannot link an ADR to itself.");

        var linkType = EnumMap.ParseLinkType(type);
        AddLinkIfMissing(from, linkType, to.Id);

        var commit = !previewOnly;
        var changes = new List<FileChange> { Authoring.Stage(_repo, from, commit) };

        if (bidirectional)
        {
            AddLinkIfMissing(to, EnumMap.Inverse(linkType), from.Id);
            changes.Add(Authoring.Stage(_repo, to, commit));
        }

        return Authoring.Result(commit,
            $"link ADR {from.Id:D4} {EnumMap.ToWire(linkType)} ADR {to.Id:D4}", changes.ToArray());
    }

    private static void AddLinkIfMissing(Adr adr, AdrLinkType type, int targetId)
    {
        if (!adr.Links.Any(l => l.Type == type && l.TargetId == targetId))
            adr.Links.Add(new AdrLink(type, targetId));
    }

    private static TemplateKind ParseTemplate(string template) =>
        template.Trim().ToLowerInvariant() is "nygard" ? TemplateKind.Nygard : TemplateKind.Madr;
}
