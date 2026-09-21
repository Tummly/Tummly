# Launch Shop cutoff / refund

**Status:** Implemented (2026-09-21); plan at `docs/superpowers/plans/2026-09-21-launch-shop-cutoff-refund.md`  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive  
**Clears P0:** Production-start/cutoff event; before cutoff cancel + Revolut refund; after start block normal cancel/refund except audited exception  
**ADR impact:** Amends [ADR 0046](../../adr/0046-shop-operator-cancel-does-not-initiate-revolut-refund.md) for **pre-cutoff** paid operator cancel (Revolut full refund); post-cutoff stays blocked except Admin force cancel

## Problem

Operator cancel on a Shop order (`paid` + `processing`) sets fulfilment to `cancelled` only. `paymentStatus` stays `paid`. Tummly does not call Revolut refund (ADR 0046: ops refunds in Merchant; Admin reconciles).

Launch truth requires:

1. An actual **production-start / cutoff** event (not only `in_transit`).
2. **Before cutoff:** cancel **+** Revolut refund.
3. **After start:** block normal cancellation/refund except an **audited** exception.

There is no production-start stamp today. Cancel already blocks `in_transit` / `delivered`, but that is not the directive’s production-start event, and money never moves on cancel.

## Locked decisions

| Topic | Choice |
|-------|--------|
| ADR 0046 vs Launch | **Launch wins** for pre-cutoff: operator cancel triggers Tummly→Revolut **full** refund |
| Cutoff event | Explicit Admin **production started** stamp (`ProductionStartedAtUtc`), not auto on `in_transit` |
| Who stamps cutoff | **Admin only** (staff JWT) |
| Post-cutoff exception | Admin-only force cancel; default full Revolut refund (same fail-closed); `skipRefund` opt-out; append `AdminAuditEvent` |
| Revolut failure on cancel | **Fail closed** — do not cancel fulfilment; order stays `processing` / `paid` |
| Complimentary / £0 | Cancel fulfilment only; **no** Revolut call |
| Money truth | `paymentStatus` stays `paid` until existing refund webhook → `refunded` + TCN (unchanged) |
| Approach | Extend cancel path; call `IRevolutMerchantClient.RefundOrderAsync` directly (not via Admin payment-refund ownership checks) |
| Operator UI | Honour API `canCancel` / `cancelBlockReason`; no operator production-started control |
| Admin UI | Prefer API + audit for launch; light UI only if Admin Shop panel already supports fulfilment actions |

## 1. Cutoff model + cancel rules

### Columns on `ShopOrder`

- `ProductionStartedAtUtc` (`DateTime?`)
- `ProductionStartedByAdminUserId` (`int?`)

EF migration required. Stamp is independent of fulfilment status (order may remain `processing` until ops ships).

### Mark production started (Admin)

- Admin-only endpoint on existing Admin Shop routes (or adjacent), e.g. `POST …/shop/orders/{id}/production-started`.
- Sets stamp + actor once; **idempotent** if already set (return success / no-op).
- Appends audit action `shop.production_started` via `IAdminAuditService` in the same save.

### Operator cancel eligibility (`ShopOrderCancelRules`)

Keep:

- `paymentStatus` is paid (or complimentary paid-equivalent used today), and
- `fulfilmentStatus` is `processing`.

Add:

- `ProductionStartedAtUtc == null`.

If stamp is set: `canCancel = false`, `cancelBlockReason = production_started`. Cancel API still returns `shop_order_not_cancellable` (same as today). Existing `in_transit` / `delivered` block reasons stay.

### Pre-cutoff cancel + refund (`ShopOrderCancelReorderService`)

1. Validate eligibility (including null production stamp) and cancel reason slug (unchanged).
2. If complimentary / £0 / missing Revolut payment order id: set fulfilment `cancelled` + reason/actor/timestamps; save; return. No Merchant call.
3. Else call `RefundOrderAsync` with **full** amount (`AmountMinor` null per Merchant “full refund” convention) and a **stable** idempotency key derived from the Shop order id (e.g. `shop-cancel:{orderId}`).
4. If Revolut fails: **do not** mutate fulfilment; return error (e.g. `revolut_refund_failed`).
5. If Revolut succeeds: set fulfilment `cancelled` + reason/actor/timestamps; save. Leave `paymentStatus` as `paid` until webhook confirms refund.

Idempotent re-cancel of an already-cancelled order remains OK (current behaviour) without a second refund when fulfilment is already `cancelled`.

## 2. Admin exception + ADR + surfaces

### Force cancel (post-cutoff audited exception)

- Admin-only endpoint that bypasses the production-started gate (usable after stamp; also usable before stamp if needed).
- Same money rules as operator cancel: full Revolut refund when a payment order id exists; complimentary / no Revolut id → cancel only; Revolut fail → no fulfilment change.
- Request may include `skipRefund: true` for rare audited exceptions that cancel fulfilment without calling Merchant; default is `false`.
- Appends audit `shop.force_cancel` with detail JSON (refund attempted/skipped, reason, Shop order id).
- Operator cancel remains blocked after stamp.

### ADR 0046 amendment

Update the ADR to state:

- **Pre-cutoff** paid operator cancel **does** initiate Revolut full refund; payment/TCN still wait for Revolut confirmation.
- **Post-cutoff** normal operator cancel stays blocked; money exceptions go through Admin force cancel and/or existing Admin payment-refund / Merchant paths.
- Partials remain Admin / Merchant-controlled (not operator cancel).

### Operator UI

- Existing cancel CTA already uses `canCancel` / `cancelBlockReason`; add copy for `production_started`.
- No new operator control to mark production started.

### Out of scope

- Partial refunds from operator cancel
- Auto-setting production started when fulfilment moves to `in_transit`
- Changing webhook → `refunded` / TCN mint path
- Extending `AdminPaymentRefundService` ownership checks to Shop (Shop cancel uses Merchant client directly)
- Full Admin Shop redesign beyond optional light actions on the existing panel

## 3. Errors + tests

### Error / block codes

| Code | When |
|------|------|
| `cancelBlockReason = production_started` | Detail/list when stamp set; operator cancel still `shop_order_not_cancellable` |
| `revolut_refund_failed` | Merchant refund failed; fulfilment unchanged |

### Tests

- Cancel rules: stamp null → cancellable; stamp set → blocked; `in_transit` / `delivered` still blocked.
- Paid cancel: Revolut success → fulfilment cancelled; `paymentStatus` still `paid` in that step.
- Paid cancel: Revolut fail → still `processing` / `paid`.
- Complimentary: cancel with zero Merchant refund calls.
- Mark production started: sets columns + audit; second call idempotent.
- Force cancel after stamp: succeeds + audit; operator cancel still denied.
- Integration: new Admin endpoints require Admin role.

## Out of scope (other P0s)

- Retention purge / IR runbook
- UK data residency
- Cookies / a11y evidence pack
