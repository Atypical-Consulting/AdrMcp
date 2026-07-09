using System.Text;
using System.Text.RegularExpressions;
using AdrMcp.Interfaces;
using AdrMcp.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace AdrMcp.Services;

/// <summary>Filesystem-backed ADR store: parses/serializes MADR-style markdown + YAML frontmatter.</summary>
public sealed class AdrRepositoryService : IAdrRepository
{
    private static readonly Regex Frontmatter =
        new(@"^﻿?---\s*\r?\n(?<yaml>.*?)\r?\n---\s*\r?\n?(?<body>.*)$",
            RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex LegacyFileName =
        new(@"^(?<id>\d+)-(?<slug>.+)$", RegexOptions.Compiled);

    private static readonly Regex LegacyDateLine =
        new(@"(?m)^[ \t]*Date:[ \t]*(?<date>\d{4}-\d{2}-\d{2})[ \t]*$", RegexOptions.Compiled);

    private static readonly Regex LegacyTitleNumberPrefix =
        new(@"^(?<num>\d+)\.\s*(?<title>.+)$", RegexOptions.Compiled);

    private static readonly Regex LegacyHtmlComment =
        new(@"<!--.*?-->", RegexOptions.Singleline | RegexOptions.Compiled);

    // Matches a "Superseded by ..." mention anywhere in the Status section (adr-tools keeps
    // the original status word, e.g. "Accepted", as its own line and appends this note on a
    // later line), then pulls the target ADR number out of whatever the link text looks like:
    // "[5]", "[5. Title]", "[ADR-0005]", or "[0005-slug]".
    private static readonly Regex LegacySupersededByNote =
        new(@"superseded\s+by\D*?(?:\[(?:ADR-)?0*(?<num>\d+)|#?(?<num2>\d+))",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly ISerializer YamlSerializer = new SerializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull | DefaultValuesHandling.OmitEmptyCollections)
        .Build();

    private static readonly IDeserializer YamlDeserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .IgnoreUnmatchedProperties()
        .Build();

    private readonly AdrOptions _options;

    public AdrRepositoryService(AdrOptions options) => _options = options;

    public string AdrRoot => _options.Root;

    public IReadOnlyList<Adr> LoadAll()
    {
        if (!Directory.Exists(AdrRoot))
            return Array.Empty<Adr>();

        var adrs = new List<Adr>();
        foreach (var path in Directory.EnumerateFiles(AdrRoot, "*.md"))
        {
            var name = Path.GetFileName(path);
            if (name.Equals("README.md", StringComparison.OrdinalIgnoreCase) ||
                name.Equals("index.md", StringComparison.OrdinalIgnoreCase))
                continue;

            var adr = TryParse(path);
            if (adr is not null) adrs.Add(adr);
        }
        return adrs.OrderBy(a => a.Id).ToList();
    }

    public Adr? Find(string idOrSlug)
    {
        var all = LoadAll();
        var key = idOrSlug.Trim();

        if (int.TryParse(key, out var id))
        {
            var byId = all.FirstOrDefault(a => a.Id == id);
            if (byId is not null) return byId;
        }

        return all.FirstOrDefault(a => a.Slug.Equals(key, StringComparison.OrdinalIgnoreCase))
               ?? all.FirstOrDefault(a => a.FileName.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    public int NextId()
    {
        var all = LoadAll();
        return all.Count == 0 ? 1 : all.Max(a => a.Id) + 1;
    }

    public string PathFor(Adr adr) => Path.Combine(AdrRoot, adr.FileName);

    public string ReadRaw(Adr adr)
    {
        var path = adr.FilePath ?? PathFor(adr);
        return File.Exists(path) ? File.ReadAllText(path) : "";
    }

    public void Save(Adr adr)
    {
        Directory.CreateDirectory(AdrRoot);
        var path = PathFor(adr);
        File.WriteAllText(path, Render(adr), new UTF8Encoding(false));
        adr.FilePath = path;
    }

    public string Render(Adr adr)
    {
        var dto = new FrontmatterDto
        {
            Id = adr.Id,
            Title = adr.Title,
            Status = EnumMap.ToWire(adr.Status),
            Date = adr.Date == default ? null : adr.Date.ToString("yyyy-MM-dd"),
            Deciders = adr.Deciders.Count > 0 ? adr.Deciders : null,
            Tags = adr.Tags.Count > 0 ? adr.Tags : null,
            Links = adr.Links.Count > 0
                ? adr.Links.Select(l => new LinkDto { Type = EnumMap.ToWire(l.Type), Target = l.TargetId }).ToList()
                : null,
            CodeRefs = adr.CodeRefs.Count > 0
                ? adr.CodeRefs.Select(c => new CodeRefDto { Path = c.Path, Symbol = c.Symbol, Line = c.Line }).ToList()
                : null
        };

        var yaml = YamlSerializer.Serialize(dto).TrimEnd('\r', '\n');
        var body = adr.Body.Replace("\r\n", "\n").TrimEnd() + "\n";
        return $"---\n{yaml}\n---\n\n{body}";
    }

    private Adr? TryParse(string path)
    {
        var text = File.ReadAllText(path);
        var m = Frontmatter.Match(text);
        if (!m.Success) return TryParseLegacyNygard(path, text);

        FrontmatterDto dto;
        try { dto = YamlDeserializer.Deserialize<FrontmatterDto>(m.Groups["yaml"].Value) ?? new FrontmatterDto(); }
        catch { return null; }

        var body = m.Groups["body"].Value.Replace("\r\n", "\n").TrimEnd() + "\n";
        var fileName = Path.GetFileNameWithoutExtension(path);

        int id = dto.Id;
        string slug = fileName;
        var fnMatch = Regex.Match(fileName, @"^(?<id>\d+)-(?<slug>.+)$");
        if (fnMatch.Success)
        {
            if (id == 0) int.TryParse(fnMatch.Groups["id"].Value, out id);
            slug = fnMatch.Groups["slug"].Value;
        }

        return new Adr
        {
            Id = id,
            Slug = slug,
            Title = string.IsNullOrWhiteSpace(dto.Title) ? MarkdownSections.ExtractTitle(body) : dto.Title!,
            Status = EnumMap.ParseStatus(dto.Status),
            Date = DateOnly.TryParse(dto.Date, out var d) ? d : default,
            Deciders = dto.Deciders ?? new(),
            Tags = dto.Tags ?? new(),
            Links = (dto.Links ?? new()).Select(l => new AdrLink(EnumMap.ParseLinkType(l.Type), l.Target)).ToList(),
            CodeRefs = (dto.CodeRefs ?? new()).Select(c => new CodeRef(c.Path ?? "", c.Symbol, c.Line)).ToList(),
            Body = body,
            FilePath = path
        };
    }

    /// <summary>
    /// Fallback for classic Nygard-style ADRs (no YAML frontmatter): title/date/status are
    /// recovered from the body text. Only fires for files matching the conventional
    /// <c>NNNN-slug.md</c> naming — anything else (README.md, template.md, unrelated notes)
    /// still returns null, unchanged from prior behavior.
    /// </summary>
    private static Adr? TryParseLegacyNygard(string path, string text)
    {
        var fileName = Path.GetFileNameWithoutExtension(path);
        var fnMatch = LegacyFileName.Match(fileName);
        if (!fnMatch.Success) return null;
        if (!int.TryParse(fnMatch.Groups["id"].Value, out var id)) return null;

        var body = text.Replace("\r\n", "\n").TrimEnd() + "\n";
        var slug = fnMatch.Groups["slug"].Value;

        var rawTitle = MarkdownSections.ExtractTitle(body);
        var titleMatch = LegacyTitleNumberPrefix.Match(rawTitle);
        var title = titleMatch.Success ? titleMatch.Groups["title"].Value.Trim() : rawTitle;

        var preamble = body.Split("\n##", 2)[0];
        var dateMatch = LegacyDateLine.Match(preamble);
        var date = dateMatch.Success && DateOnly.TryParse(dateMatch.Groups["date"].Value, out var d)
            ? d : default;

        var (status, links) = ParseLegacyStatus(MarkdownSections.GetSection(body, "Status"));

        return new Adr
        {
            Id = id,
            Slug = slug,
            Title = string.IsNullOrWhiteSpace(title) ? slug : title,
            Status = status,
            Date = date,
            Links = links,
            Body = body,
            FilePath = path
        };
    }

    private static (AdrStatus Status, List<AdrLink> Links) ParseLegacyStatus(string? statusSection)
    {
        if (string.IsNullOrWhiteSpace(statusSection)) return (AdrStatus.Proposed, new());

        var cleaned = LegacyHtmlComment.Replace(statusSection, "").Trim();
        var lines = cleaned
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        var firstLine = lines.FirstOrDefault() ?? "";

        foreach (var line in lines)
        {
            var supersededMatch = LegacySupersededByNote.Match(line);
            if (!supersededMatch.Success) continue;

            var targetText = supersededMatch.Groups["num"].Success
                ? supersededMatch.Groups["num"].Value
                : supersededMatch.Groups["num2"].Value;
            var links = int.TryParse(targetText, out var target)
                ? new List<AdrLink> { new AdrLink(AdrLinkType.SupersededBy, target) }
                : new List<AdrLink>();
            return (AdrStatus.Superseded, links);
        }

        return (EnumMap.ParseStatus(firstLine), new());
    }

    // ---- YAML DTOs (UnderscoredNamingConvention maps CodeRefs -> code_refs, etc.) ----

    private sealed class FrontmatterDto
    {
        public int Id { get; set; }
        public string? Title { get; set; }
        public string? Status { get; set; }
        public string? Date { get; set; }
        public List<string>? Deciders { get; set; }
        public List<string>? Tags { get; set; }
        public List<LinkDto>? Links { get; set; }
        public List<CodeRefDto>? CodeRefs { get; set; }
    }

    private sealed class LinkDto
    {
        public string? Type { get; set; }
        public int Target { get; set; }
    }

    private sealed class CodeRefDto
    {
        public string? Path { get; set; }
        public string? Symbol { get; set; }
        public int? Line { get; set; }
    }
}
