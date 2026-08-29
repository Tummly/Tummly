# 21 — Cycle-end downgrade and cadence change-plan

**Kind:** build

**What to build:** Downgrade and cadence changes that wait until renewal use Tummly **Scheduled change** and/or Revolut `change-plan` with `at_cycle_end`. No mid-cycle native amount edit. Apply on renewal still runs inside included mint (sister ledger).

**Blocked by:** 14 — First paid conversion Hosted Payment Page session; 16 — Setup and cycle ORDER_COMPLETED activation

**Status:** ready-for-human

## Parent

[PRD: Revolut billing integration](../PRD.md)

Product locks: [03](./03-saved-payment-methods-versus-tummly-controlled-plan-changes.md). Sister credit-ledger **24** owns the schedule slot and renewal apply-before-mint. If this ticket and a numbered lock disagree, the numbered lock wins. Do not reopen those decisions.

## Out of this ticket

- Pay-now upgrade (20). Extra Location (22). Cancel (23).

- [x] Schedule-only Operator confirm does not open HPP. When Revolut must move variation at cycle end, call `change-plan` with `at_cycle_end` using the mapped variation id.
- [x] Missing map entry for target SKU fails closed. Do not PATCH live variation amounts.
- [x] HTTP tests cover: schedule response has no `redirectUrl` pay outcome when no money due now. Service tests cover: `at_cycle_end` call uses mapped id; missing map fails closed.

## Answer

Shipped on `feat/settings-billing`.

- `ICycleEndPlanChange` / `CycleEndPlanChangeService` — on schedule-only plan-change, when a Revolut subscription is correlated from pending pay sessions, call existing Merchant `ChangeSubscriptionPlanAsync` (`at_cycle_end` + mapped variation id). Missing map fails closed. No HPP `redirectUrl`.
- Local Scheduled change is set on the tracked Billing Account before the Revolut call; `SaveChanges` runs after both succeed so a Revolut fail leaves no persisted slot.
- Shared `RevolutSubscriptionCorrelation.ResolveLatestSubscriptionIdAsync` for tickets 20/21.
- Renewal apply-before-mint stays on sister ledger **24** / included mint.

**Commits:** `2945d6db` (implement, rebased onto ticket 20), `28150d0b` (review: schedule→change-plan order; shared sub id; cadence HTTP).

## Comments

### Review (post-merge)

- Reordered schedule path: set slot → Revolut `change-plan` → save (fail closed without persist when Revolut refuses).
- Deduped subscription-id resolve with ticket 20 via `RevolutSubscriptionCorrelation`.
- Added cadence-only schedule HTTP coverage (no `redirectUrl`).
