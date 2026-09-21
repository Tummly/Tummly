# Launch Starter Kit — per Active Location

**Status:** Design approved (chat 2026-09-21); awaiting spec review before plan/build  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive  
**Clears conflict:** Billing Account lifetime starter-kit entitlement vs one complimentary kit per newly activated production Location

## Problem

Code already creates a £0 complimentary Shop order per Location (`IsComplimentary`, unique index, idempotent `EnsureForLocationAsync`). That conflicts with launch truth in two ways:

1. **Too early:** `OwnedLocationInsertService` creates the kit when the Location is still **Draft**, not only when it is newly **Active**.
2. **Wrong scope in product surface:** `BillingAccount.StarterKitState` is written as BA-lifetime state, and Billing / Manage Plan FAQ still describe one kit per Billing Account lifetime.

Directive: one complimentary standard Starter Kit per newly activated, verified production Location, once under the qualifying operator/business relationship.

## Decision

**Approach A+B:** Move create call sites to first Active only, and gate inside Ensure so non-Active Locations never create.

| Event | Create kit? |
|-------|-------------|
| Signup provision (Location created as **Active**) | Yes — keep Ensure after save |
| Draft insert (`OwnedLocationInsertService`) | **No** — remove Ensure |
| Draft → Active (`LocationsLifecycleWriteService.ActivateDraftAsync`) | **Yes** — add Ensure + print-ready queue |
| Resume (Paused → Active) | **No** |
| Ensure called while Location is not Active | **No** — Active gate inside Ensure |

Idempotency stays per Location: existing complimentary order → return existing id, `Created: false`.

## Entitlement rule

- One complimentary standard Starter Kit = one £0 paid `ShopOrder` with `IsComplimentary = true`, three standard SKUs (`table-tents`, `window-stickers`, `offer-card`), unique per `LocationId` (keep `IX_ShopOrders_LocationId_Complimentary`).
- “Verified production” for this change = Location `LifecycleStatus == Active` on first enter to Active (signup Active or `ActivateDraft`). No extra verification flag.
- Resume never creates. Existing complimentary order is enough if the Location was Active before pause.

### Ensure Active gate

`ComplimentaryStarterShopOrderService.EnsureForLocationAsync`:

1. If a complimentary order already exists for `LocationId` → return that id (`Created: false`) regardless of lifecycle (idempotent).
2. Else load Location; if missing → throw as today.
3. Else if `LifecycleStatus != Active` → **do not create**. Return `ComplimentaryStarterShopOrderResult(ShopOrderId: Guid.Empty, Created: false)`. Callers must not queue print-ready when `ShopOrderId` is empty.
4. Else create £0 order as today.

Do **not** write `BillingAccount.StarterKitState` on create.

## BA state and operator copy

- Stop all writes to `BillingAccount.StarterKitState` from complimentary kit create.
- Leave the DB column and EF mapping; **no** drop-column migration in this change.
- Remove BA kit state from operator-facing Billing Credits surfaces that present it as entitlement:
  - Manage Plan FAQ: replace BA-lifetime wording with **one complimentary kit per Active Location**.
  - Plan overview / usage “QR packs” / starter row that currently uses `starterKitState`: replace with **static launch copy** matching the per-Location rule (do not invent a BA roll-up).
- API may keep returning `StarterKitState` for wire compatibility if removing the field is high-churn; UI must not treat it as the entitlement source of truth. Prefer stopping display over building a derived BA summary.

## Call sites

| Call site | Change |
|-----------|--------|
| `GuestLoopProvisioningService.CreateOperatorAccountAsync` | Keep Ensure after Active location save; keep print-ready queue |
| `OwnedLocationInsertService.AddLockedAsync` | Remove Ensure and print-ready request for complimentary kit |
| `LocationsLifecycleWriteService.ActivateDraftAsync` | After Draft → Active succeeds, call Ensure; if `Created` (or order id present), queue print-ready |
| `LocationLifecycleService.ResumeAsync` | No Ensure |

## Data already created

Draft Locations that already have a complimentary order: **leave as-is**. No cancel, no backfill job. Unique index + idempotent Ensure cover re-activate.

## Tests

- Ensure: Active → create; Draft → no create; existing order → idempotent.
- Owned location Draft add → no complimentary order.
- ActivateDraft → Active + one complimentary order + print-ready request (when created).
- Signup provision → complimentary order for Active locations (existing coverage; adjust if BA state assertions remain).
- Resume → no new Ensure / no second order.
- Manage Plan FAQ / presentation: per-Location copy; no BA-lifetime kit claim.

## Out of scope

- Drop `StarterKitState` column / EF migration
- Backfill or cancel kits already created on Drafts
- Shop cutoff / Revolut refund policy (separate P0)
- Billing-pack markdown docs beyond code-owned FAQ/presentation
- New “qualifying plan” gate beyond existing location create/activate auth (signup and ActivateDraft already require an entitled operator account)

## Success criteria

1. No complimentary starter order is created for Draft Locations after this change.
2. First Active (signup or ActivateDraft) creates exactly one £0 complimentary order per Location.
3. Resume does not create a kit.
4. Operator FAQ / plan overview no longer claim one kit per Billing Account lifetime.
5. `BillingAccount.StarterKitState` is not updated by kit create.
