# Plan entitlements are derived caps on incrementing writes

Non-credit **Plan entitlement**s (locations, **Team member**s, Guest Forms, Active **QR code**s, Active Offers) are computed from **Contracted Pricebook** plus **Subscription plan** plus paid extra-location count. They are not stored as cap integers on `BillingAccounts`. The backend denies only writes that would raise counted usage above the cap, after `UPDLOCK` on that row. Operator Setup location inserts are not gated, so Multi Guest Loop may exceed **Pilot** location slots; those venues stay. Guest records have no hard cap in v1.

We rejected persisted cap snapshots: they can drift from the JSON. We rejected gating Setup: that would break Multi onboarding. We rejected counting Team members as Active memberships only: unlimited pending **Team invitation**s would bypass the pack. We rejected gating Offer create: create is **Draft**; the cap is stored **Active** at first live attach. We rejected a guest-record number: the pack has none.

Product lock: `.scratch/credit-ledger-backend/issues/08-plan-entitlements-besides-credits.md`.
