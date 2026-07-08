using AdrMcp.Models;

namespace AdrMcp.Tests;

public class SearchAndIntelligenceTests
{
    [Fact]
    public void Lexical_search_ranks_matching_adr_first()
    {
        using var env = new TestEnv();
        env.Create("Use PostgreSQL", context: "We need a relational database with strong SQL support.");
        env.Create("Adopt event sourcing", context: "Capture every state change as an immutable event.");

        var hits = env.Navigation.SearchAdrs("relational database SQL", semantic: false, topK: 5);

        Assert.NotEmpty(hits);
        Assert.Equal("Use PostgreSQL", hits[0].Title);
    }

    [Fact]
    public void Semantic_search_returns_ranked_hits()
    {
        using var env = new TestEnv();
        env.Create("Use PostgreSQL", context: "Relational database, SQL, ACID transactions.");
        env.Create("Adopt Kubernetes", context: "Container orchestration and scaling.");

        var hits = env.Navigation.SearchAdrs("database transactions", semantic: true, topK: 5);

        Assert.NotEmpty(hits);
        Assert.Equal("Use PostgreSQL", hits[0].Title);
    }

    [Fact]
    public void Find_stale_adrs_flags_broken_code_ref()
    {
        using var env = new TestEnv();
        var id = env.Create("Governs a module");
        var adr = env.Repo.Find(id.ToString())!;
        adr.CodeRefs.Add(new CodeRef("src/does-not-exist.cs", "MissingType"));
        env.Repo.Save(adr);

        var stale = env.Intelligence.FindStaleAdrs();
        Assert.Contains(stale, s => s.Id == id && s.BrokenRefs.Count == 1);
    }

    [Fact]
    public void Find_stale_adrs_ignores_resolvable_code_ref()
    {
        using var env = new TestEnv();
        var codeFile = Path.Combine(env.RepoRoot, "src", "Thing.cs");
        Directory.CreateDirectory(Path.GetDirectoryName(codeFile)!);
        File.WriteAllText(codeFile, "public class Thing { }");

        var id = env.Create("Governs Thing");
        var adr = env.Repo.Find(id.ToString())!;
        adr.CodeRefs.Add(new CodeRef("src/Thing.cs", "Thing"));
        env.Repo.Save(adr);

        var stale = env.Intelligence.FindStaleAdrs();
        Assert.DoesNotContain(stale, s => s.Id == id);
    }

    [Fact]
    public void Coverage_report_counts_adrs_per_tag()
    {
        using var env = new TestEnv();
        env.Create("Data decision", tags: new[] { "data" });
        env.Create("Another data decision", tags: new[] { "data" });

        var coverage = env.Intelligence.CoverageReport(new[] { "data", "security" });

        Assert.Equal(2, coverage.Single(c => c.Area == "data").AdrCount);
        Assert.Equal(0, coverage.Single(c => c.Area == "security").AdrCount); // gap
    }

    [Fact]
    public void Suggest_adr_from_change_extracts_changed_files()
    {
        using var env = new TestEnv();
        const string diff = """
        diff --git a/src/Payment.cs b/src/Payment.cs
        index 000..111 100644
        --- a/src/Payment.cs
        +++ b/src/Payment.cs
        @@ -1 +1 @@
        -old
        +new
        """;

        var suggestion = env.Intelligence.SuggestAdrFromChange(diff);

        Assert.Contains("src/Payment.cs", suggestion.SuggestedTitle);
        Assert.Contains(suggestion.CodeRefs, c => c.Path == "src/Payment.cs");
        Assert.Contains("## Context and Problem Statement", suggestion.DraftBody);
    }

    [Fact]
    public void Render_index_writes_table_with_all_adrs()
    {
        using var env = new TestEnv();
        env.Create("First");
        env.Create("Second");

        var result = env.Utility.RenderIndex(previewOnly: false);
        Assert.True(result.Committed);

        var readme = File.ReadAllText(Path.Combine(env.Root, "README.md"));
        Assert.Contains("| 0001 | [First]", readme);
        Assert.Contains("| 0002 | [Second]", readme);
    }
}
