using AdrMcp.Models;
using AdrMcp.Services;

namespace AdrMcp.Tests;

public class LegacyNygardParsingTests
{
    private const string ClassicAdr = """
        # 0001. Record architecture decisions

        Date: 2026-06-30

        ## Status

        Accepted

        ## Context

        Some context about why this decision was needed.

        ## Decision

        We will do the thing.

        ## Consequences

        Things become easier and harder.
        """;

    [Fact]
    public void Parses_classic_nygard_file_with_no_frontmatter()
    {
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "0001-record-architecture-decisions.md"), ClassicAdr);

        var adr = env.Repo.Find("1");

        Assert.NotNull(adr);
        Assert.Equal(1, adr!.Id);
        Assert.Equal("record-architecture-decisions", adr.Slug);
        Assert.Equal("Record architecture decisions", adr.Title);
        Assert.Equal(new DateOnly(2026, 6, 30), adr.Date);
        Assert.Equal(AdrStatus.Accepted, adr.Status);
        Assert.Contains("## Decision", adr.Body);
        Assert.Contains("We will do the thing.", adr.Body);
    }

    [Fact]
    public void Superseded_by_status_line_sets_status_and_link()
    {
        using var env = new TestEnv();
        var superseded = ClassicAdr
            .Replace("Accepted", "Superseded by [2](0002-other.md)");
        File.WriteAllText(Path.Combine(env.Root, "0001-record-architecture-decisions.md"), superseded);

        var adr = env.Repo.Find("1");

        Assert.NotNull(adr);
        Assert.Equal(AdrStatus.Superseded, adr!.Status);
        Assert.Contains(adr.Links, l => l.Type == AdrLinkType.SupersededBy && l.TargetId == 2);
    }

    [Fact]
    public void Non_numbered_frontmatterless_file_is_still_ignored()
    {
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "notes.md"), "# Just some notes\n\nNot an ADR.\n");

        var all = env.Repo.LoadAll();

        Assert.Empty(all);
    }

    [Fact]
    public void Missing_date_and_status_degrade_to_defaults_without_throwing()
    {
        using var env = new TestEnv();
        const string minimal = """
            # 0002. Minimal decision

            ## Context

            Context.

            ## Decision

            Decision.

            ## Consequences

            Consequences.
            """;
        File.WriteAllText(Path.Combine(env.Root, "0002-minimal-decision.md"), minimal);

        var adr = env.Repo.Find("2");

        Assert.NotNull(adr);
        Assert.Equal(default, adr!.Date);
        Assert.Equal(AdrStatus.Proposed, adr.Status);
    }

    [Fact]
    public void Validate_adr_reports_no_missing_section_errors_for_a_legacy_file()
    {
        using var env = new TestEnv();
        File.WriteAllText(Path.Combine(env.Root, "0001-record-architecture-decisions.md"), ClassicAdr);
        var adr = env.Repo.Find("1")!;

        var result = env.Validator.Validate(adr, env.Repo.LoadAll());

        Assert.DoesNotContain(result.Issues, i => i.Message.Contains("Missing required section"));
    }

    [Fact]
    public void Set_status_upgrades_a_legacy_file_to_frontmatter_while_preserving_its_body()
    {
        using var env = new TestEnv();
        var path = Path.Combine(env.Root, "0001-record-architecture-decisions.md");
        File.WriteAllText(path, ClassicAdr);
        var adr = env.Repo.Find("1")!;
        Assert.Equal(AdrStatus.Accepted, adr.Status); // sanity: legacy parse worked

        // Accepted -> Deprecated is a legal transition (AdrLifecycle).
        env.Authoring.SetStatus("1", "deprecated", previewOnly: false);

        var raw = File.ReadAllText(path);
        Assert.StartsWith("---", raw);
        Assert.Contains("status: deprecated", raw);
        Assert.Contains("## Decision", raw);
        Assert.Contains("We will do the thing.", raw);
    }
}
