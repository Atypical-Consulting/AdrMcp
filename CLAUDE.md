# CLAUDE.md

Guidance for Claude Code (and other agents) working in this repository.

## What this project is

AdrMcp is an **MCP server for Architectural Decision Records (ADRs)**. It lets
an agent navigate, author, validate, link, and analyze decision records stored
as Markdown files on disk. It targets **.NET 10**, communicates over the
**stdio** transport using the **ModelContextProtocol** SDK, and exposes 18
tools. All mutating tools are **preview-by-default**.

## Architecture

The code is layered: **Tools → Services → Models / Interfaces**, wired together
with dependency injection in `AdrMcp/Program.cs`, which builds and runs the
stdio MCP host.

- **`AdrMcp/Tools/`** — MCP tool surface, grouped by concern:
  - `NavigationTools` — `list_adrs`, `get_adr`, `search_adrs`, `get_adr_index`,
    `find_related_adrs`, `get_adr_graph`
  - `AuthoringTools` — `create_adr`, `update_adr`, `set_status`,
    `supersede_adr`, `link_adrs`, `validate_adr`
  - `IntelligenceTools` — `detect_conflicts`, `find_stale_adrs`,
    `coverage_report`, `suggest_adr_from_change`
  - `UtilityTools` — `render_index`, `diff_adr`
  Tool methods are annotated with `[McpServerTool]`; every parameter carries a
  `[Description]`.
- **`AdrMcp/Services/`** — business logic: `AdrRepositoryService`,
  `AdrValidationService`, `AdrGraphService`, `AdrTemplateService`,
  `AdrLifecycle`, `SearchService`, `FileSystemCodeLinkProvider`,
  `LexicalEmbeddingProvider`, plus helpers (`DiffUtil`, `MarkdownSections`,
  `Slug`, `Map`, `EnumMap`, `Authoring`) and `AdrOptions` for configuration.
- **`AdrMcp/Interfaces/`** — abstractions the services implement and depend on:
  `IAdrRepository`, `IAdrValidator`, `IAdrGraphService`, `IAdrTemplateService`,
  `ISearchService`, `ICodeLinkProvider`, `IEmbeddingProvider`.
- **`AdrMcp.Tests/`** — xUnit test project mirroring the source layout.

## Storage format

ADRs are Markdown files under **`docs/adr/`**, one file per decision, numbered
and slugged (e.g. `0000-record-architecture-decisions.md`). The default
authoring format is **MADR 4.0**; a **Nygard** template is also supported. An
index can be regenerated with `render_index`.

## Conventions

- **Preview-by-default writes.** Any tool that creates or changes files
  (`create_adr`, `update_adr`, `set_status`, `supersede_adr`, `link_adrs`,
  `render_index`) returns a preview of the intended change and only applies it
  when the caller explicitly confirms. Preserve this behavior when adding or
  modifying tools.
- **Lifecycle.** Status transitions (proposed → accepted / rejected →
  deprecated / superseded) are enforced through `AdrLifecycle`. Supersession
  and linking maintain bidirectional links between records.
- **Provider-based code linking.** Associating ADRs with source code and
  detecting stale decisions goes through `ICodeLinkProvider` (default:
  `FileSystemCodeLinkProvider`). Intelligence tools (`find_stale_adrs`,
  `coverage_report`, `suggest_adr_from_change`, `detect_conflicts`) build on
  this abstraction — keep the provider seam intact rather than reaching into
  the filesystem directly from tools.
- **Search.** `SearchService` combines lexical matching with semantic
  (embedding-based) search via `IEmbeddingProvider`.

## Skills

Three Claude skills live in `.claude/skills/`:

- `adr-author` — guided authoring of a new decision record
- `adr-review` — reviewing/critiquing an existing ADR
- `adr-supersede` — driving the supersession lifecycle and bidirectional links

## Build and test

The solution uses the new XML solution format, `AdrMcp.slnx`.

```bash
dotnet build AdrMcp.slnx
dotnet test AdrMcp.slnx
```

CI gates merges at **80% line coverage**. Keep new code covered.
