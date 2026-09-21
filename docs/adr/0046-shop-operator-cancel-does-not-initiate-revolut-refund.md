# Shop cancel and Revolut refund (amended 2026-09-21)

**Pre-cutoff** (no `ProductionStartedAtUtc`): operator cancel on
`paymentStatus = paid` and `fulfilmentStatus = processing` calls Revolut
full refund via `RefundOrderAsync`, then sets fulfilment to `cancelled`.
`paymentStatus` stays `paid` until the refund webhook confirms and Tummly
mints a TCN / marks `refunded`.

**Post-cutoff:** operator cancel is blocked. Admin may force-cancel
(audited), with default full Revolut refund unless `skipRefund` is set.

Partials and Merchant-only ops refunds remain outside operator cancel.

Design: `docs/superpowers/specs/2026-09-21-launch-shop-cutoff-refund-design.md`.
Plan: `docs/superpowers/plans/2026-09-21-launch-shop-cutoff-refund.md`.
Product lock: `.scratch/tummly-shop-backend/issues/07-operator-cancel-and-reorder-rules.md`.
