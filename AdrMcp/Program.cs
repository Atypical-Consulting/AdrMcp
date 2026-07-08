using AdrMcp.Interfaces;
using AdrMcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// stdout is reserved for the MCP protocol; all logs must go to stderr.
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Configuration (ADR root + repo root) resolved from args / env / defaults.
builder.Services.AddSingleton(AdrOptions.Resolve(args));

// Domain services.
builder.Services.AddSingleton<IAdrRepository, AdrRepositoryService>();
builder.Services.AddSingleton<IAdrTemplateService, AdrTemplateService>();
builder.Services.AddSingleton<IAdrValidator, AdrValidationService>();
builder.Services.AddSingleton<IAdrGraphService, AdrGraphService>();
builder.Services.AddSingleton<IEmbeddingProvider, LexicalEmbeddingProvider>();
builder.Services.AddSingleton<ISearchService, SearchService>();
builder.Services.AddSingleton<ICodeLinkProvider, FileSystemCodeLinkProvider>();

// MCP server over stdio; tools discovered via [McpServerToolType] in this assembly.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithToolsFromAssembly();

await builder.Build().RunAsync();
