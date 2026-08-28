# Operator billing HTTP is BillingCreditsController

Operator **Billing & credits** HTTP stays on `/api/billing-credits`. Reads split (page, usage, activity, invoice PDF). Writes for plan-change, top-up, extra Location, **Cancel plan**, scheduled-change clear, payment-method update, and billing contacts stay on the same controller; Revolut map owns order-create internals of pay-now actions. Credit chrome elsewhere does not call this Area: shell / **Lock Alert** carry `billingStatus` and restoration CTA; Campaigns / recovery / Assistant remaining come from `ICreditBalanceSnapshot` on those GETs. **Lock Alert** must work for operators with **No access** to this Area.

We rejected a second Revolut controller for pay-now: the frontend already posts here. We rejected a `/chrome` GET: it would leak remaining to **No access** roles or break **Lock Alert**. We rejected one fat GET: **Billing activity** is a growing log.

Product lock: `.scratch/credit-ledger-backend/issues/12-operator-billing-api-contract.md`.
