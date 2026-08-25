---
name: implement
description: "Implement one build ticket: isolated worktree, wait for merge, review, update the ticket."
disable-model-invocation: true
---

# Implement

One **build** ticket per session. `/implement-spec` is a different job (one PR for a whole spec). Do not use it for a single `.scratch` build ticket.

## Protocol

1. Read the ticket, its numbered locks, and `CONTEXT.md`. Do not grill.
2. Isolated git worktree on a feature branch. Do not merge until the human says yes.
3. TDD at the ticket's **named tests** (`HTTP tests cover` / `Page-module tests cover`). Those lines are the agreed seams.
4. UI tickets: one browser pass of each linked Figma frame (button variants, helpers, stacking). Email chrome: a rendered fixture, or keep it **Out of this ticket**.
5. Typecheck and the ticket's test files as you go. Full suite once at the end.
6. Stop. Wait for merge permission. Rebase onto the integration branch if needed. Resolve conflicts with `/resolving-merge-conflicts`. Then merge.
7. `/code-review` against the merge-base. Fix findings that belong to this ticket.
8. Update the ticket **Answer** (or Done notes), check the acceptance boxes, set status as the user asked.

Done when the named tests pass, the review findings for this ticket are fixed, and the ticket file records the commit.
