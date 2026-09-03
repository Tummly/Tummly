# Shop operator cancel does not initiate Revolut refund

Operator cancel on a **Shop order** while `paymentStatus = paid` and
`fulfilmentStatus = processing` sets fulfilment to `cancelled` only.
`paymentStatus` stays `paid`. Tummly does not call the Revolut refund API
and does not mint a TCN on cancel.

Ops refunds the customer manually in the Revolut Merchant account using the
order’s `revolutOrderId`. Admin then reconciles through the existing admin
payment-refund flow against that Revolut order UUID → webhook →
`MintCreditNoteForRefundAsync` → `paymentStatus = refunded`.

We rejected a synchronous Tummly→Revolut refund on cancel: ops must control
refund timing and partials in the Merchant account, and Shop should not grow
a second refund path beside admin reconciliation. We rejected flipping
payment to `refunded` on cancel alone: that would lie about money until
Revolut confirms. We rejected minting TCN on cancel: credit notes follow
confirmed Revolut refunds.

Product lock: `.scratch/tummly-shop-backend/issues/07-operator-cancel-and-reorder-rules.md`.
PRD: `.scratch/tummly-shop-backend/PRD.md`.
