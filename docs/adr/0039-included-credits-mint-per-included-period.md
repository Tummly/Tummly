# Included credits mint per Included period

Paid included credits mint once per **Included period** (one month), not as a year of credits on Annual day one. Monthly cadence mints after a completed Revolut `ORDER_COMPLETED`. Annual cadence mints month 1 on the year payment and months 2–12 from a Tummly job while **Billing status** is **Active**, because a `P1Y` plan has no monthly payment event. We rejected a Stripe-style 29th–31st billing anchor: Revolut docs do not publish that rule, so Monthly uses cycle dates and Annual slices use SQL `DATEADD` month. We rejected pausing mint on **Pause workspace**: this path reads **Billing status** only.

Product lock: `.scratch/credit-ledger-backend/issues/06-included-credit-release-for-pilot-monthly-and-annual.md`.
