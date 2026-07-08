# AdrMcp

An [MCP](https://modelcontextprotocol.io) server for **Architectural Decision Records (ADRs)**.
It turns a folder of markdown ADRs into first-class, queryable, writable capabilities for any
MCP client (Claude Code, Cursor, Copilot, …): navigate, author, validate, and analyze decisions.

Built as a .NET 10 / C# stdio MCP server, architecturally modeled on
[RoselineMCP](https://github.com/Atypical-Consulting/RoselineMCP) — layered `Tools → Services`,
`[McpServerTool]` attributes, preview-by-default writes, shipped as a `dotnet tool` + Docker image.

## Storage & format

ADRs are markdown files under an ADR root (default `docs/adr/`), one file per decision named
`NNNN-kebab-title.md`, using **MADR 4.0** frontmatter + sections:

```markdown
---
id: 1
title: Use PostgreSQL for persistence
status: accepted          # proposed | accepted | rejected | deprecated | superseded
date: 2026-07-08
tags: [data]
links:
  - { type: superseded-by, target: 5 }
code_refs:
  - { path: src/Data/Repository.cs, symbol: PostgresRepository }
---

# Use PostgreSQL for persistence

## Context and Problem Statement
...
## Decision Outcome
...
## Consequences
...
```

Everything is git-native, human-readable, and diff-able — no database.

## Tools

| Tool | Kind | Description |
| --- | --- | --- |
| `list_adrs` | read | List/filter ADRs (status, tag, date range) |
| `get_adr` | read | Get one ADR; optionally only selected `##` sections |
| `search_adrs` | read | Lexical (default) or `semantic` term-vector search |
| `get_adr_index` | read | The decision log / timeline |
| `find_related_adrs` | read | Incoming/outgoing links + supersession chain |
| `get_adr_graph` | read | Full relationship graph (nodes + typed edges) |
| `create_adr` | write | Create an ADR from a template (`madr` or `nygard`) |
| `update_adr` | write | Replace/append a single `##` section |
| `set_status` | write | Transition status (enforces the lifecycle) |
| `supersede_adr` | write | Create replacement + mark old superseded + link, in one call |
| `link_adrs` | write | Typed link between two ADRs (adds inverse) |
| `validate_adr` | read | MADR compliance: sections, status, dangling links, duplicate ids |
| `detect_conflicts` | read | Explicit `conflicts-with` + highly similar accepted ADRs |
| `find_stale_adrs` | read | ADRs whose `code_refs` no longer resolve |
| `coverage_report` | read | ADR coverage per architectural area (tag); surfaces gaps |
| `suggest_adr_from_change` | read | Draft an ADR proposal from a unified diff |
| `render_index` | write | Regenerate the browsable `README.md` decision index |
| `diff_adr` | read | Diff two ADRs, or a file vs its canonical rendering |

**Writes are preview-by-default.** Every mutating tool takes `previewOnly` (default `true`) and
returns a unified diff of the intended change; pass `previewOnly=false` to write to disk.

## Configuration

Resolved in order: CLI arg → env var → default.

| Setting | CLI | Env | Default |
| --- | --- | --- | --- |
| ADR root | `--adr-root <path>` | `ADR_ROOT` | `<repo>/docs/adr` |
| Repo root (for `code_refs`) | `--repo-root <path>` | `ADR_REPO_ROOT` | current directory |

### Code-linking is provider-based

`find_stale_adrs` resolves `code_refs` through an `ICodeLinkProvider`. The default
`FileSystemCodeLinkProvider` is language-agnostic (a ref resolves if the file exists and, when a
symbol is given, that symbol's text is present). A Roslyn (.NET) or codebase-memory provider can be
plugged in behind the same interface — deep symbol resolution stays the client's job.

## Run

```bash
# from source
dotnet run --project AdrMcp -- --adr-root ./docs/adr

# as a global tool
dotnet tool install -g AdrMcp
adr-mcp --adr-root ./docs/adr
```

### MCP client config

```json
{
  "mcpServers": {
    "adr": {
      "command": "adr-mcp",
      "args": ["--adr-root", "/path/to/repo/docs/adr", "--repo-root", "/path/to/repo"]
    }
  }
}
```

### Docker

```bash
docker build -t adr-mcp .
docker run --rm -i -v "$PWD:/workspace" adr-mcp --adr-root /workspace/docs/adr
```

## Claude skills

Three project skills under `.claude/skills/` add the judgment/workflow layer on top of the
MCP tools (the server provides the mechanics; the skills provide the craft). They activate
automatically in Claude Code when the connected AdrMcp server is available:

| Skill | Triggers on | What it does |
| --- | --- | --- |
| `adr-author` | "write an ADR", "record this decision" | Decide if a decision warrants an ADR, frame a sharp Context/Decision/Consequences, draft via `create_adr` + `validate_adr` |
| `adr-review` | "review this ADR", "does this decision hold up" | Structural + substantive critique against a rubric, using `validate_adr` / `detect_conflicts` |
| `adr-supersede` | "we changed our mind", "retire this decision" | Pick the right lifecycle move and drive `supersede_adr` / `set_status` with correct linking |

## Develop

```bash
dotnet build AdrMcp.slnx
dotnet test AdrMcp.slnx        # unit tests + MCP-over-stdio integration tests
dotnet pack AdrMcp/AdrMcp.csproj -c Release -o ./artifacts
```

## Continuous integration

CI/CD runs on GitHub Actions, modeled on [RoselineMCP](https://github.com/Atypical-Consulting/RoselineMCP):

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | push / PR to `main` | Build + test matrix (ubuntu/windows/macos); 80% line-coverage gate on ubuntu |
| `codeql.yml` | push / PR / weekly | CodeQL security analysis (C#) |
| `pages.yml` | push to `site/**` | Publish the landing page to GitHub Pages |
| `docker-publish.yml` | `v*` tag | Build + push a multi-arch image to GHCR |
| `publish-nuget.yml` | `v*` tag | Pack the tool, push to NuGet.org, create a GitHub Release |

Cut a release by pushing a tag: `git tag v0.1.0 && git push origin v0.1.0`.

## Commit attribution

Every commit in this repository must be authored by **phmatray@gmail.com**. This is enforced
by a version-controlled `pre-commit` hook. Git does not run tracked hooks automatically, so
enable it once per clone:

```bash
git config core.hooksPath .githooks
git config user.email phmatray@gmail.com
```

See [`.githooks/`](.githooks/) for details.

## Project layout

```
AdrMcp/
├── Interfaces/   service contracts (IAdrRepository, ICodeLinkProvider, …)
├── Services/     repository, templates, validation, graph, search, embeddings, code-links
├── Tools/        Navigation / Authoring / Intelligence / Utility MCP tools
├── Models/       Adr, AdrStatus, AdrLink, CodeRef, response DTOs
└── Program.cs    DI wiring + stdio MCP host
```
