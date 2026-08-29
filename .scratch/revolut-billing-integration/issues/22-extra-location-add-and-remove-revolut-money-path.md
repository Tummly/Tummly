# 22 — Extra Location add and remove Revolut money path

**Kind:** build

**What to build:** Add Location pay-now creates a one-time Revolut order; on `ORDER_COMPLETED`, Tummly applies the add-on and updates the Revolut subscription quantity/plan. Remove Location is schedule / cycle-end only — no immediate Revolut charge.

**Blocked by:** 14 — First paid conversion Hosted Payment Page session; 16 — Setup and cycle ORDER_COMPLETED activation

**Status:** ready-for-human

## Parent

[PRD: Revolut billing integration](../PRD.md)

Product locks: [03](./03-saved-payment-methods-versus-tummly-controlled-plan-changes.md). Ledger: sister credit-ledger **26**. If this ticket and a numbered lock disagree, the numbered lock wins. Do not reopen those decisions.

## Out of this ticket

- Plan upgrade (20). Cancel (23). Entitlement **409** gates (ledger).

- [x] Intent `Purpose = extra_location` on add pay-now order. Completed webhook → `IExtraGroupLocationService.ApplyAddOnOrderCompletedAsync` + Revolut subscription update + `TM`.
- [x] Remove Location does not create a pay-now order; uses Tummly schedule and/or cycle-end Revolut update.
- [x] HTTP tests cover: add returns HPP redirect; remove is schedule-only. Service tests cover: intent routing; replay safe.

## Answer

Shipped on `feat/settings-billing`.

- Add Location: `ExtraGroupLocationService.SubmitAddAsync` creates a one-time Revolut order (VAT line items), writes `RevolutOrderIntent` (`Purpose = extra_location`, amounts, `TargetPaidExtraLocationCount`), returns HPP `redirectUrl`.
- `ORDER_COMPLETED`: webhook treats `extra_location` as a one-time intent → claim → `RevolutOrderCompletedApplier.ApplyExtraLocationAsync` → `ApplyAddOnOrderCompletedAsync` + `ChangeSubscriptionPlanAsync` (GroupLocation variation) + `MintForCompletedOrderAsync`. Closed intent skips re-apply.
- Remove Location: schedule-only on Billing Account; when target extras go to 0, `change-plan` to base Group at `at_cycle_end` on submit (not again on scheduled apply).
- Migration `AddRevolutOrderIntentTargetPaidExtraLocationCount` is EF-generated (Designer pair).

**Commits:** `93ad8712` (implement, rebased), `8fd9437b` (review: EF migration; replay; remove change-plan once).

## Comments

### Review (post-merge)

- Regenerated EF migration with Designer; removed unused purpose constants.
- Closed-intent / webhook replay coverage for extra_location; no double grant.
- Dropped duplicate `change-plan` from `ApplyScheduledRemoveAsync` (submit already schedules Revolut).
