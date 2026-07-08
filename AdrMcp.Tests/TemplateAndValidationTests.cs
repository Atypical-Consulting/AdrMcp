using AdrMcp.Models;
using AdrMcp.Services;

namespace AdrMcp.Tests;

public class TemplateAndValidationTests
{
    [Theory]
    [InlineData(TemplateKind.Madr, "Context and Problem Statement", "Decision Outcome", "Consequences")]
    [InlineData(TemplateKind.Nygard, "Context", "Decision", "Consequences")]
    public void Templates_render_required_sections(TemplateKind kind, string s1, string s2, string s3)
    {
        var svc = new AdrTemplateService();
        var body = svc.Render(kind, new AdrDraft("A title"));

        Assert.StartsWith("# A title", body);
        Assert.Contains($"## {s1}", body);
        Assert.Contains($"## {s2}", body);
        Assert.Contains($"## {s3}", body);
    }

    [Fact]
    public void Generated_adr_validates_clean()
    {
        using var env = new TestEnv();
        env.Create("A well-formed decision");

        var results = env.Intelligence.ValidateAdr();
        Assert.All(results, r => Assert.True(r.IsValid, string.Join("; ", r.Issues.Select(i => i.Message))));
    }

    [Fact]
    public void Validator_flags_missing_sections()
    {
        using var env = new TestEnv();
        var adr = new Adr { Id = 1, Slug = "bare", Title = "Bare", Body = "# Bare\n\nNo sections here.\n" };

        var result = env.Validator.Validate(adr, new[] { adr });
        Assert.False(result.IsValid);
        Assert.Contains(result.Issues, i => i.Message.Contains("Missing required section"));
    }

    [Fact]
    public void Validator_flags_dangling_links()
    {
        using var env = new TestEnv();
        var id = env.Create("Real decision");
        var adr = env.Repo.Find(id.ToString())!;
        adr.Links.Add(new AdrLink(AdrLinkType.RelatesTo, 999));

        var result = env.Validator.Validate(adr, env.Repo.LoadAll());
        Assert.Contains(result.Issues, i =>
            i.Severity == ValidationSeverity.Error && i.Message.Contains("Dangling link"));
    }
}
