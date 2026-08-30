# Unused top-up reverse drains Bindable; consumed units stay

Payment-linked `refund` takes only Bindable leftover on `topup_allocation`s for that `SourcePaymentRef`. Consumed units are never clawed: that would need Remaining < 0. While unreverted payment-linked refunds exist, Bindable on those allocations stays 0 (drain again after Release or any Remaining-increasing write). Staff **Admin** writes `manual_adjustment` with reason; operator **Billing activity** still shows **Tummly Support**.

We rejected operator self-serve refund: pack credits are non-refundable after allocation. We rejected reverse-consumption-then-refund: a wash that pretends used units were unused. We rejected `expiry` plus `refund` on a draining expired hold: two Remaining-debits. We rejected folding this into **Billing status**: ticket **10** already owns the chargeback overlay.

Product lock: `.scratch/credit-ledger-backend/issues/11-manual-adjustment-refund-and-chargeback-on-the-ledger.md`.
