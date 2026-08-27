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

## Page modules

Operator page modules wired through `useSyncExternalStore` must return a **stable** snapshot object until the next `emit` / `publish`. Rebuilding a new object on every `getSnapshot` call trips React's max update depth.

**Named test:** `expect(module.getSnapshot()).toBe(module.getSnapshot())` (same reference until a state change). Guests already caches this way; new modules must match.

## Operator chrome access

New Operator chrome flags (`*Access` on `/auth/me`, locations, or shell snapshots) must document **omit** behaviour.

- During rollout, **omit must not hide** Account-owner chrome. Only an explicit deny (for example `"none"`) hides.
- Front and back must land together, or the parser default must keep owner chrome visible until the API field is live.

## EF migrations (SQL Server)

1. **Generate, do not invent.** Create migrations with `dotnet ef migrations add`. Do not hand-author `*.Designer.cs` or invent snapshot fragments. Pairing CI (`check-migration-designer-pairs.sh`) is a gate, not a substitute for EF tooling. See [ADR 0015](docs/adr/0015-deploy-schema-safety.md).
2. **No multiple cascade paths.** New FKs from `Restaurants` (or any table that already cascades with `Users`) must not use `SetNull` or a second `Cascade` onto `Users` without proving SQL Server accepts the graph. Prefer `NoAction` or `Restrict`, with a short comment naming error **1785**. Guest-note FKs already show the pattern. Static CI: `backend/TummlyBackend/scripts/check-restaurant-user-setnull-fks.sh`.
