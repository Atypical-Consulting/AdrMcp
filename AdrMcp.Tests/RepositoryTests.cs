using AdrMcp.Models;
using AdrMcp.Services;

namespace AdrMcp.Tests;

public class RepositoryTests
{
    [Fact]
    public void Create_then_load_round_trips_frontmatter_and_body()
    {
        using var env = new TestEnv();
        var id = env.Create("Use PostgreSQL for persistence", tags: new[] { "data", "storage" });

        var adr = env.Repo.Find(id.ToString());
        Assert.NotNull(adr);
        Assert.Equal("Use PostgreSQL for persistence", adr!.Title);
        Assert.Equal(AdrStatus.Proposed, adr.Status);
        Assert.Equal(new[] { "data", "storage" }, adr.Tags);
        Assert.Contains("## Context and Problem Statement", adr.Body);
    }

    [Fact]
    public void Ids_are_sequential_and_zero_padded_in_filename()
    {
        using var env = new TestEnv();
        var a = env.Create("First decision");
        var b = env.Create("Second decision");

        Assert.Equal(1, a);
        Assert.Equal(2, b);

        var second = env.Repo.Find("2")!;
        Assert.Equal("0002-second-decision.md", second.FileName);
        Assert.Equal("second-decision", second.Slug);
    }

    [Fact]
    public void Find_resolves_by_id_and_by_slug()
    {
        using var env = new TestEnv();
        env.Create("Adopt hexagonal architecture");

        Assert.NotNull(env.Repo.Find("1"));
        Assert.NotNull(env.Repo.Find("adopt-hexagonal-architecture"));
        Assert.Null(env.Repo.Find("does-not-exist"));
    }

    [Fact]
    public void Links_and_code_refs_survive_a_render_parse_cycle()
    {
        using var env = new TestEnv();
        var one = env.Create("Decision one");
        var two = env.Create("Decision two");

        env.Authoring.LinkAdrs(one.ToString(), two.ToString(), "relates-to", bidirectional: true, previewOnly: false);

        var a = env.Repo.Find(one.ToString())!;
        var b = env.Repo.Find(two.ToString())!;
        Assert.Contains(a.Links, l => l.Type == AdrLinkType.RelatesTo && l.TargetId == two);
        Assert.Contains(b.Links, l => l.Type == AdrLinkType.RelatesTo && l.TargetId == one);
    }
}
