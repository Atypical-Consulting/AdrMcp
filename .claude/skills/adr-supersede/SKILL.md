---
name: adr-supersede
description: Use when an existing architectural decision is being changed, replaced, retired, reversed, or deprecated — the team changed its mind and a prior ADR no longer reflects reality. Drives the correct lifecycle transition and bidirectional linking via the AdrMcp MCP tools (supersede_adr, set_status, link_adrs, find_related_adrs). Triggers on "supersede ADR-000X", "we changed our mind about", "replace/retire this decision", "this ADR is out of date", "mark as deprecated/superseded", "reverse decision".
---

# Superseding a decision

You retire or replace a prior decision **correctly** — preserving history rather than
editing the past. ADRs are immutable records: you don't rewrite an accepted decision, you
supersede it with a new one and link them.

> Requires the **AdrMcp** MCP server (tools `supersede_adr`, `set_status`, `link_adrs`,
> `find_related_adrs`, `get_adr`). If unavailable, ask the user to connect it first.

## First, pick the right move

- **Replaced by a new decision** → `supersede_adr` (create the replacement, mark the old
  one `superseded`, link both ways — in one call). This is the common case.
- **No longer relevant, nothing replaces it** → `set_status` to `deprecated`.
- **Was never adopted** → `set_status` to `rejected` (only valid from `proposed`).
- **Just needs a small correction** (typo, clarification) → this is NOT superseding; use
  `update_adr` on the original instead.

Remember the lifecycle the server enforces: `proposed → accepted → {deprecated, superseded}`,
and `proposed → rejected`. You can't supersede something that was never accepted.

## Workflow (replacement case)

1. **Inspect the old decision.** `get_adr <old-id>` and `find_related_adrs <old-id>` — know
   what already points at it so nothing is orphaned.
2. **Frame the new decision** with the same rigor as `adr-author`: what changed in the
   forces since the original? The new Context should explain *why the old choice no longer
   holds*, not just restate the new one.
3. **Preview** `supersede_adr(oldIdOrSlug, newTitle, context, decision, consequences, …)`
   with `previewOnly=true`. Confirm the diff shows: new ADR created as `accepted` with a
   `supersedes` link, and the old ADR flipped to `superseded` with a `superseded-by` link.
4. **Commit** by re-calling with `previewOnly=false` once approved.
5. **Re-point stragglers.** If other ADRs `relates-to` the old one and should now track the
   replacement, add links with `link_adrs`.

## Guardrails

- Never delete or silently rewrite the superseded ADR — its value is the historical trail.
- The new ADR must say what the old one got wrong or what changed; a supersession without a
  reason is just churn.
- After committing, the supersession chain should read cleanly end-to-end
  (`find_related_adrs` on the original should list the replacement in its chain).
