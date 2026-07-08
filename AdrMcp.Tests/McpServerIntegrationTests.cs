using System.Text.Json;
using ModelContextProtocol.Client;

namespace AdrMcp.Tests;

/// <summary>
/// End-to-end verification over the real MCP stdio transport: launches the built server
/// as a child process via the SDK client and drives a full ADR workflow.
/// </summary>
public class McpServerIntegrationTests
{
    private static string ServerDll()
    {
        // The test build copies AdrMcp.dll next to the test assembly (project reference).
        var dir = AppContext.BaseDirectory;
        var dll = Path.Combine(dir, "AdrMcp.dll");
        if (!File.Exists(dll)) throw new FileNotFoundException($"Server dll not found at {dll}");
        return dll;
    }

    private static async Task<McpClient> ConnectAsync(string adrRoot)
    {
        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = "AdrMcp",
            Command = "dotnet",
            Arguments = new[] { ServerDll(), "--adr-root", adrRoot }
        });
        return await McpClient.CreateAsync(transport);
    }

    [Fact]
    public async Task Server_lists_all_tools_over_stdio()
    {
        var root = Path.Combine(Path.GetTempPath(), "adrmcp-it", Guid.NewGuid().ToString("N"));
        await using var client = await ConnectAsync(root);

        var tools = await client.ListToolsAsync();
        var names = tools.Select(t => t.Name).ToHashSet();

        Assert.Contains("create_adr", names);
        Assert.Contains("list_adrs", names);
        Assert.Contains("supersede_adr", names);
        Assert.Contains("validate_adr", names);
        Assert.True(names.Count >= 13, $"Expected all ADR tools, got {names.Count}: {string.Join(", ", names)}");
    }

    [Fact]
    public async Task Full_workflow_create_list_validate_over_stdio()
    {
        var root = Path.Combine(Path.GetTempPath(), "adrmcp-it", Guid.NewGuid().ToString("N"));
        await using var client = await ConnectAsync(root);

        // create_adr (commit)
        var create = await client.CallToolAsync("create_adr", new Dictionary<string, object?>
        {
            ["title"] = "Use PostgreSQL for persistence",
            ["context"] = "We need a relational store.",
            ["decision"] = "Adopt PostgreSQL.",
            ["consequences"] = "Ops must run Postgres.",
            ["previewOnly"] = false
        });
        Assert.True(create.IsError != true);

        // The file must now exist on disk.
        var file = Path.Combine(root, "0001-use-postgresql-for-persistence.md");
        Assert.True(File.Exists(file), $"Expected ADR file at {file}");

        // list_adrs returns the new ADR.
        var list = await client.CallToolAsync("list_adrs", new Dictionary<string, object?>());
        Assert.True(list.IsError != true);
        Assert.Contains("Use PostgreSQL for persistence", TextOf(list));

        // validate_adr reports no errors.
        var validate = await client.CallToolAsync("validate_adr", new Dictionary<string, object?>());
        Assert.True(validate.IsError != true);
        var validateText = TextOf(validate);
        // The generated ADR is well-formed, so no validation error severity should be present.
        Assert.DoesNotContain("Error", validateText, StringComparison.Ordinal);
    }

    private static string TextOf(ModelContextProtocol.Protocol.CallToolResult result) =>
        string.Concat(result.Content.OfType<ModelContextProtocol.Protocol.TextContentBlock>().Select(c => c.Text));
}
