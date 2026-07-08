using AdrMcp.Interfaces;
using AdrMcp.Services;
using AdrMcp.Tools;

namespace AdrMcp.Tests;

/// <summary>
/// A disposable sandbox: a fresh temp ADR root wired to real services and tools,
/// so tests exercise the same code paths the MCP server uses.
/// </summary>
internal sealed class TestEnv : IDisposable
{
    public string Root { get; }
    public string RepoRoot { get; }
    public AdrOptions Options { get; }
    public IAdrRepository Repo { get; }
    public IAdrTemplateService Templates { get; }
    public IAdrValidator Validator { get; }
    public IAdrGraphService Graph { get; }
    public IEmbeddingProvider Embeddings { get; }
    public ISearchService Search { get; }
    public ICodeLinkProvider CodeLinks { get; }

    public NavigationTools Navigation { get; }
    public AuthoringTools Authoring { get; }
    public IntelligenceTools Intelligence { get; }
    public UtilityTools Utility { get; }

    public TestEnv()
    {
        RepoRoot = Path.Combine(Path.GetTempPath(), "adrmcp-tests", Guid.NewGuid().ToString("N"));
        Root = Path.Combine(RepoRoot, "docs", "adr");
        Directory.CreateDirectory(Root);

        Options = new AdrOptions(Root, RepoRoot);
        Repo = new AdrRepositoryService(Options);
        Templates = new AdrTemplateService();
        Validator = new AdrValidationService();
        Graph = new AdrGraphService();
        Embeddings = new LexicalEmbeddingProvider();
        Search = new SearchService(Embeddings);
        CodeLinks = new FileSystemCodeLinkProvider();

        Navigation = new NavigationTools(Repo, Graph, Search);
        Authoring = new AuthoringTools(Repo, Templates);
        Intelligence = new IntelligenceTools(Repo, Validator, Search, CodeLinks, Templates, Options);
        Utility = new UtilityTools(Repo);
    }

    /// <summary>Creates and commits an ADR, returning its id.</summary>
    public int Create(string title, string? context = "Some context.", string? decision = "The decision.",
        string? consequences = "The consequences.", string template = "madr", string[]? tags = null)
    {
        var result = Authoring.CreateAdr(title, context, decision, consequences, template,
            options: null, tags: tags, deciders: null, previewOnly: false);
        // id is embedded in the message "create ADR NNNN ..."
        return Repo.LoadAll().Single(a => a.Title == title).Id;
    }

    public void Dispose()
    {
        try { if (Directory.Exists(RepoRoot)) Directory.Delete(RepoRoot, recursive: true); }
        catch { /* best effort */ }
    }
}
