using AdrMcp.Models;
using AdrMcp.Services;
using ModelContextProtocol;

namespace AdrMcp.Tests;

public class LifecycleAndGraphTests
{
    [Fact]
    public void Set_status_allows_valid_transition()
    {
        using var env = new TestEnv();
        var id = env.Create("Decision");

        env.Authoring.SetStatus(id.ToString(), "accepted", previewOnly: false);
        Assert.Equal(AdrStatus.Accepted, env.Repo.Find(id.ToString())!.Status);
    }

    [Fact]
    public void Set_status_rejects_illegal_transition()
    {
        using var env = new TestEnv();
        var id = env.Create("Decision");

        // proposed -> deprecated is not allowed
        var ex = Assert.Throws<McpException>(() =>
            env.Authoring.SetStatus(id.ToString(), "deprecated", previewOnly: false));
        Assert.Contains("Illegal transition", ex.Message);
    }

    [Fact]
    public void Preview_does_not_write_to_disk()
    {
        using var env = new TestEnv();
        var id = env.Create("Decision");

        var result = env.Authoring.SetStatus(id.ToString(), "accepted", previewOnly: true);
        Assert.False(result.Committed);
        Assert.Equal(AdrStatus.Proposed, env.Repo.Find(id.ToString())!.Status); // unchanged on disk
    }

    [Fact]
    public void Supersede_marks_old_superseded_and_links_both_ways()
    {
        using var env = new TestEnv();
        var oldId = env.Create("Use MySQL");
        env.Authoring.SetStatus(oldId.ToString(), "accepted", previewOnly: false);

        env.Authoring.SupersedeAdr(oldId.ToString(), "Use PostgreSQL", previewOnly: false);

        var all = env.Repo.LoadAll();
        var old = all.Single(a => a.Id == oldId);
        var neu = all.Single(a => a.Title == "Use PostgreSQL");

        Assert.Equal(AdrStatus.Superseded, old.Status);
        Assert.Equal(AdrStatus.Accepted, neu.Status);
        Assert.Contains(old.Links, l => l.Type == AdrLinkType.SupersededBy && l.TargetId == neu.Id);
        Assert.Contains(neu.Links, l => l.Type == AdrLinkType.Supersedes && l.TargetId == old.Id);
    }

    [Fact]
    public void Related_reports_supersession_chain()
    {
        using var env = new TestEnv();
        var v1 = env.Create("V1");
        env.Authoring.SetStatus(v1.ToString(), "accepted", previewOnly: false);
        env.Authoring.SupersedeAdr(v1.ToString(), "V2", previewOnly: false);
        var v2 = env.Repo.LoadAll().Single(a => a.Title == "V2");
        env.Authoring.SupersedeAdr(v2.Id.ToString(), "V3", previewOnly: false);

        var related = env.Navigation.FindRelatedAdrs(v1.ToString());
        var v3 = env.Repo.LoadAll().Single(a => a.Title == "V3");

        Assert.Equal(new[] { v2.Id, v3.Id }, related.SupersessionChain);
    }

    [Fact]
    public void Graph_contains_nodes_and_edges()
    {
        using var env = new TestEnv();
        var a = env.Create("A");
        var b = env.Create("B");
        env.Authoring.LinkAdrs(a.ToString(), b.ToString(), "relates-to", bidirectional: false, previewOnly: false);

        var graph = env.Navigation.GetAdrGraph();
        Assert.Equal(2, graph.Nodes.Count);
        Assert.Contains(graph.Edges, e => e.From == a && e.To == b && e.Type == "relates-to");
    }
}
