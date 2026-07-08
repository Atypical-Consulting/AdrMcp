# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-07-08

Initial release.

### Added

- **MCP server** for Architectural Decision Records over stdio transport
  (.NET 10, ModelContextProtocol SDK), exposing **18 tools** across four
  areas:
  - *Navigation*: `list_adrs`, `get_adr`, `search_adrs`, `get_adr_index`,
    `find_related_adrs`, `get_adr_graph`
  - *Authoring*: `create_adr`, `update_adr`, `set_status`, `supersede_adr`,
    `link_adrs`, `validate_adr`
  - *Intelligence*: `detect_conflicts`, `find_stale_adrs`, `coverage_report`,
    `suggest_adr_from_change`
  - *Utility*: `render_index`, `diff_adr`
- **Markdown storage** of decision records under `docs/adr/`, with support for
  both **MADR 4.0** and **Nygard** templates.
- **Preview-by-default writes**: every mutating tool reports the intended
  change and applies it only on explicit confirmation.
- **Lifecycle enforcement** for ADR status transitions (proposed, accepted,
  rejected, deprecated, superseded).
- **Supersession and linking**, with bidirectional links maintained between
  related and superseding/superseded records.
- **Search**: combined lexical search and semantic (embedding-based) search
  over decision records.
- **Provider-based intelligence**: code-link and staleness checks via an
  `ICodeLinkProvider` abstraction, powering conflict detection, stale-record
  discovery, coverage reporting, and change-driven ADR suggestions.
- **Three Claude skills** — `adr-author`, `adr-review`, and `adr-supersede` —
  for guided authoring, review, and supersession workflows.
- **GitHub Pages landing site** describing the project and its tools.
- **CI/CD workflows** for build, test, and packaging, gating merges at 80%
  line coverage.
- **Commit-attribution hook** (`.githooks/pre-commit`) enforcing that every
  commit is authored by `phmatray@gmail.com`.

[Unreleased]: https://github.com/Atypical-Consulting/AdrMcp/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/Atypical-Consulting/AdrMcp/releases/tag/v0.1.0
