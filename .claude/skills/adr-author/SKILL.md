---
name: adr-author
description: Use when the user wants to write, draft, or create an Architectural Decision Record (ADR) — capturing an architecture or technical decision, recording why a choice was made, or documenting a decision for the team. Guides framing a sharp Context/Decision/Consequences and drives the AdrMcp MCP tools (create_adr, validate_adr) to produce a well-formed MADR record. Triggers on "write an ADR", "record this decision", "create a decision record", "document this architecture choice", "we decided to…".
---

# Authoring an ADR

You help the user turn a real architectural decision into a crisp, durable ADR. The MCP
server handles mechanics (numbering, files, frontmatter, validation) — your job is the
**judgment and prose**: is this decision worth recording, and is it framed honestly?

> Requires the **AdrMcp** MCP server (tools `create_adr`, `validate_adr`, `list_adrs`).
> If those tools aren't available, tell the user to connect the AdrMcp server first.

## Does this decision even warrant an ADR?

Record it when the decision is **architecturally significant** — it constrains future work
and would be expensive to reverse. Good signals:

- It affects structure, dependencies, interfaces, or cross-cutting concerns.
- A future contributor will ask "why is it done this way?" and deserve an answer.
- Reasonable engineers would have chosen differently; there were real alternatives.

Don't write an ADR for reversible, local, or obvious choices (variable names, a one-off
bug fix, formatting). If it doesn't constrain the future, skip it and say so.

## Workflow

1. **Check for overlap.** Call `list_adrs` (or `search_adrs`) so you don't duplicate or
   silently contradict an existing decision. If one exists, consider `adr-review` or
   `adr-supersede` instead of a new record.
2. **Interview for the four parts** (ask only what you can't infer from the codebase/context):
   - **Context / problem** — the forces at play, constraints, what makes this non-trivial.
   - **Considered options** — the real alternatives, not strawmen.
   - **Decision** — the chosen option, stated as a decision ("We will…"), not a discussion.
   - **Consequences** — what becomes easier AND what becomes harder / what we accept.
3. **Draft with `create_adr`** (`template=madr` default), `previewOnly=true` first so the
   user sees the diff. Pass `tags` for the architectural area, `deciders` if known.
4. **Validate** with `validate_adr` on the new id; fix any errors (missing sections,
   dangling links) before committing.
5. **Commit** by re-calling `create_adr` with `previewOnly=false` once the user approves.
6. New ADRs start as `proposed`. Use `set_status` to move to `accepted` only when the
   decision is actually adopted.

## Quality bar

- **Context states the problem, not the answer.** If the context already names the chosen
  tool, it's decision-leakage — rewrite it around the forces.
- **The decision is falsifiable and specific.** "Use PostgreSQL 16 for the primary store"
  beats "improve the database situation."
- **Consequences are honest.** Every real decision has costs. An ADR with only upsides is
  marketing, not a record — name at least one trade-off you're accepting.
- **Alternatives were genuinely considered.** List what you rejected and one reason why.
- Keep it tight: an ADR is a memo, not an essay. If a section is a placeholder, fill it or
  cut the ADR.
