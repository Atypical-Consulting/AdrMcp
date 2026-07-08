---
name: adr-review
description: Use when the user wants to review, critique, or sanity-check an Architectural Decision Record (ADR) — an existing record or a draft. Evaluates whether the decision is sound, falsifiable, honestly scoped, and well-linked, using the AdrMcp MCP tools (get_adr, validate_adr, detect_conflicts, find_related_adrs). Triggers on "review this ADR", "critique my decision record", "is this ADR any good", "check ADR-000X", "does this decision hold up".
---

# Reviewing an ADR

You give an ADR a rigorous, fair review — both **structural** (does it validate?) and
**substantive** (is the decision actually sound and honestly recorded?).

> Requires the **AdrMcp** MCP server (tools `get_adr`, `validate_adr`, `detect_conflicts`,
> `find_related_adrs`, `search_adrs`). If unavailable, ask the user to connect it, or
> review the pasted markdown directly against the rubric below.

## Workflow

1. **Load it.** `get_adr <id-or-slug>` for the full record (or read the draft the user pasted).
2. **Structural pass.** Run `validate_adr <id>` — report any errors/warnings (missing
   sections, invalid status, dangling links, superseded-without-replacement).
3. **Conflict/overlap pass.** Run `detect_conflicts` and `find_related_adrs <id>` (or
   `search_adrs` on the topic) to check this decision doesn't silently contradict or
   duplicate another. Flag any accepted ADR it collides with.
4. **Substantive pass.** Score against the rubric below.
5. **Report** as a short verdict + prioritized findings. Offer concrete rewrites, not just
   critiques. If the fix is a status/link problem, point to `adr-supersede`; if it needs
   rewriting, offer to apply it via `update_adr` (preview first).

## Rubric

Judge each; call out the ones that fail.

- **Significance** — Does this decision deserve an ADR at all, or is it noise?
- **Context ≠ decision** — The context frames forces/constraints and does NOT pre-name the
  chosen solution. Decision-leakage in the context is the most common defect.
- **Falsifiable decision** — Stated as a concrete, specific "We will…", not a vague
  direction. Could someone tell whether it's being followed?
- **Real alternatives** — Considered options are genuine, with a reason each was rejected.
  Strawmen ("do nothing", "do it badly") don't count.
- **Honest consequences** — At least one real trade-off / cost is named. Upside-only
  records are suspect.
- **Traceability** — Correct status for its lifecycle stage; links to related/superseded
  ADRs present; tags identify the area.
- **Concision** — Reads like a memo. No filler, no unfilled placeholders.

## Tone

Be direct and specific — cite the section and quote the phrase. A good review makes the
decision *better recorded*, not just criticized. If the ADR is solid, say so plainly and
stop; don't invent problems.
