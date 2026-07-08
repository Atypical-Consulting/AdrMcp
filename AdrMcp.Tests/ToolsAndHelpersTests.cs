using AdrMcp.Models;
using AdrMcp.Services;

namespace AdrMcp.Tests;

public class ToolsAndHelpersTests
{
    [Fact]
    public void Get_adr_returns_only_requested_sections()
    {
        using var env = new TestEnv();
        env.Create("Sectioned", context: "CTX-MARKER", decision: "DEC-MARKER", consequences: "CON-MARKER");

        var detail = env.Navigation.GetAdr("1", new[] { "Consequences" });

        Assert.Contains("## Consequences", detail.Content);
        Assert.Contains("CON-MARKER", detail.Content);
        Assert.DoesNotContain("CTX-MARKER", detail.Content);
    }

    [Fact]
    public void Get_adr_index_lists_every_adr()
    {
        using var env = new TestEnv();
        env.Create("One");
        env.Create("Two");

        var index = env.Navigation.GetAdrIndex();
        Assert.Equal(2, index.Count);
        Assert.Contains(index, e => e.Title == "One");
    }

    [Fact]
    public void Update_adr_replaces_existing_and_appends_missing_section()
    {
        using var env = new TestEnv();
        env.Create("Updatable");

        env.Authoring.UpdateAdr("1", "Consequences", "REPLACED-BODY", previewOnly: false);
        env.Authoring.UpdateAdr("1", "Follow-up Actions", "A brand new section.", previewOnly: false);

        var body = env.Repo.Find("1")!.Body;
        Assert.Contains("REPLACED-BODY", body);
        Assert.Contains("## Follow-up Actions", body);
        Assert.Contains("A brand new section.", body);
    }

    [Fact]
    public void Update_adr_preview_returns_diff_without_writing()
    {
        using var env = new TestEnv();
        env.Create("Preview me");

        var result = env.Authoring.UpdateAdr("1", "Consequences", "NOT-ON-DISK", previewOnly: true);

        Assert.False(result.Committed);
        Assert.Contains("NOT-ON-DISK", result.Changes[0].Diff);
        Assert.DoesNotContain("NOT-ON-DISK", env.Repo.ReadRaw(env.Repo.Find("1")!));
    }

    [Fact]
    public void Diff_adr_compares_two_and_reports_formatting_drift()
    {
        using var env = new TestEnv();
        env.Create("Alpha", context: "alpha-context");
        env.Create("Beta", context: "beta-context");

        var pair = env.Utility.DiffAdr("1", "2");
        Assert.Contains("alpha-context", pair);
        Assert.Contains("beta-context", pair);

        var drift = env.Utility.DiffAdr("1");
        Assert.Contains("0001-alpha.md", drift);
    }

    [Fact]
    public void Detect_conflicts_flags_explicit_conflicts_with_link()
    {
        using var env = new TestEnv();
        env.Create("Decision A");
        env.Create("Decision B");
        env.Authoring.LinkAdrs("1", "2", "conflicts-with", bidirectional: true, previewOnly: false);

        var conflicts = env.Intelligence.DetectConflicts();
        Assert.Contains(conflicts, c => c.AdrId == 1 && c.OtherId == 2);
    }

    [Fact]
    public void Link_supersedes_adds_inverse_superseded_by_on_target()
    {
        using var env = new TestEnv();
        env.Create("New way");
        env.Create("Old way");

        env.Authoring.LinkAdrs("1", "2", "supersedes", bidirectional: true, previewOnly: false);

        Assert.Contains(env.Repo.Find("1")!.Links, l => l.Type == AdrLinkType.Supersedes && l.TargetId == 2);
        Assert.Contains(env.Repo.Find("2")!.Links, l => l.Type == AdrLinkType.SupersededBy && l.TargetId == 1);
    }

    [Fact]
    public void Render_index_preview_does_not_write_readme()
    {
        using var env = new TestEnv();
        env.Create("Indexed");

        var result = env.Utility.RenderIndex(previewOnly: true);

        Assert.False(result.Committed);
        Assert.False(File.Exists(Path.Combine(env.Root, "README.md")));
    }

    [Fact]
    public void Suggest_adr_from_change_handles_plain_plusplus_diff()
    {
        using var env = new TestEnv();
        const string diff = """
        --- a/config/settings.json
        +++ b/config/settings.json
        @@ -1 +1 @@
        -{"old":true}
        +{"new":true}
        """;

        var suggestion = env.Intelligence.SuggestAdrFromChange(diff);
        Assert.Contains(suggestion.CodeRefs, c => c.Path == "config/settings.json");
    }

    [Theory]
    [InlineData(TemplateKind.Madr, "Decision Outcome")]
    [InlineData(TemplateKind.Nygard, "Decision")]
    public void Template_required_sections_are_declared(TemplateKind kind, string expected)
    {
        var svc = new AdrTemplateService();
        Assert.Contains(expected, svc.RequiredSections(kind));
    }

    [Fact]
    public void AdrOptions_resolve_reads_arg_forms_and_default()
    {
        var spaced = AdrOptions.Resolve(new[] { "--adr-root", "custom-adrs", "--repo-root", "custom-repo" });
        Assert.EndsWith("custom-adrs", spaced.Root);
        Assert.EndsWith("custom-repo", spaced.RepoRoot);

        var equals = AdrOptions.Resolve(new[] { "--adr-root=eq-adrs" });
        Assert.EndsWith("eq-adrs", equals.Root);

        var fallback = AdrOptions.Resolve(Array.Empty<string>());
        Assert.EndsWith(Path.Combine("docs", "adr"), fallback.Root);
    }
}
