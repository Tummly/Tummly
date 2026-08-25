# Coding standards

Read this during **review**, not during implementation. Repo UI reuse rules stay in `AGENTS.md`.

## Operator settings

1. **Named tests.** Every acceptance line that says `HTTP tests cover` or `Page-module tests cover` has a matching test in the diff. A missing named case is a Spec fail.
2. **Locked copy.** Persist-only rules, owner helpers, and error strings stay as the ticket wrote them. Do not replace them with copy from another surface (Guests list, Feedback export, and similar).
3. **Actor identity.** Submitter name and email come from the signed-in `User` row, not from JWT claims alone.
4. **Figma chrome.** Button variant, helper copy, and menu stacking match the linked Figma frame on the ticket.
5. **Placeholder art.** Do not leave product-sample marks (brand PNG leftovers) on Operator or guest chrome. Use the shared Brand logo fallback.

## Leave-dirty

Settings children that hold a form draft reuse one leave-dirty path. See [ADR 0033](docs/adr/0033-operator-leave-dirty-is-one-guard.md).
