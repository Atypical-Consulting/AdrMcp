---
id: 0
title: Record architecture decisions
status: accepted
date: 2026-07-08
tags:
- process
---

# Record architecture decisions

## Context and Problem Statement

We need to record the architectural decisions made on this project so that the
reasoning behind them is preserved for current and future contributors.

## Considered Options

- Keep decisions in ad-hoc wiki pages
- Do not record decisions at all
- Use Architectural Decision Records (ADRs) as markdown files in the repository

## Decision Outcome

Chosen option: **Architectural Decision Records** stored as MADR-style markdown
files under `docs/adr/`, managed via the AdrMcp server.

## Consequences

- Decisions live next to the code, are versioned in git, and are diff-able.
- Contributors can navigate, author, and validate decisions through any MCP client.
