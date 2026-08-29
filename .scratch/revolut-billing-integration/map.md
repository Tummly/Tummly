# Revolut billing integration

Type: wayfinder:map
Status: open

## Destination

A locked product spec for **Revolut Merchant API** as payment source of truth: first paid conversion, Revolut subscriptions, verified idempotent webhooks, VAT invoices and credit notes, card and invoice management without a Stripe Customer Portal, and Tummly-controlled plan and Location changes. Spec is [PRD.md](./PRD.md). Build tickets come after `/to-tickets`.

## Notes

- Wayfinder sessions: `CONTEXT.md`, `/grilling`, `/domain-modeling`. Language: ASD-STE100 for agent reports.
- Sister maps: [Billing & credits operator frontend](../billing-credits-frontend/map.md), [Credit ledger backend](../credit-ledger-backend/map.md). This map owns money movement. It does not own credit consumption math.
- Commercial source: [pack v3.0](../../docs/product/billing-pack-v3.0/) for prices, allowances, VAT rate, invoice prefixes, and lifecycle rules. Pricebook JSON: [tummly_uk_billing_config_v3.0.json](../../docs/product/billing-pack-v3.0/tummly_uk_billing_config_v3.0.json). The pack named Stripe. **This effort overrides the payment provider to Revolut.** Do not reopen commercial numbers. Do not use `/home/salman/Downloads/`.
- Currency GBP. VAT 20% exclusive. Live paid conversion fails closed if HMRC VAT number and effective date are missing.
- Do not store card data. Store Revolut customer / order / subscription IDs. Do not edit a live Revolut plan amount in place; version the pricebook.
- Expected webhook families: `ORDER_*` (entitlements from `ORDER_COMPLETED`), `SUBSCRIPTION_*` (state, including `SUBSCRIPTION_OVERDUE`), `DISPUTE_*`. Verify `Revolut-Signature`. Do not activate on client `onSuccess`.
- Pack Stripe event names and Customer Portal are not the implementation contract.

## Decisions so far

- Charting lock: destination is a locked spec, not a code implementation pass.
- Charting lock: this map covers Revolut money movement, VAT invoices, and payment lifecycle events.
- Charting lock: pack v3.0 is the commercial source of truth except the payment provider.
- Charting lock: plan and Location changes stay Tummly-controlled. Revolut must not offer unrestricted plan changes to the operator.
- Charting lock: payment provider is **Revolut Merchant API**, not Stripe.
- [Revolut Merchant API, subscriptions, tax, and webhooks](./issues/01-revolut-merchant-api-subscriptions-tax-and-webhooks.md): Tummly owns VAT invoices, fail-closed VAT, and plan-change UI. Activate on `ORDER_COMPLETED`. No Revolut customer portal. Native cancel is immediate; native `change-plan` is cycle-end only. Detail: [research/revolut-merchant-subscriptions-tax-webhooks.md](./research/revolut-merchant-subscriptions-tax-webhooks.md).
- [First paid conversion flow](./issues/02-first-paid-conversion-flow.md): Hosted Payment Page on subscription setup order. Customer at conversion only (BillingEmail else Owner). VAT fail-closed before any Revolut create. Session: customer → subscription → setup `checkout_url`. Success → Plan & subscription; no activate on land. Reuse or cancel pending on retry; Idempotency-Key binds target.
- [Saved payment methods versus Tummly-controlled plan changes](./issues/03-saved-payment-methods-versus-tummly-controlled-plan-changes.md): Cards/invoices/cancel are Tummly-hosted (HPP for card update only; `TM-`/`TCN-` PDFs; Manage plan cancel). Plan/Location mutations only via `/api/billing-credits`. Pay-now upgrade/Location = one-time order then subscription update; cycle-end changes use `at_cycle_end`. Cancel at period end in Tummly; call Revolut cancel at period end.
- [Webhooks, idempotency, and entitlement activation](./issues/04-webhooks-idempotency-and-entitlement-activation.md): `POST /api/webhooks/revolut`; verify signature; activate only on `ORDER_COMPLETED` + retrieve `completed`. Unique claim `(event, object_id)` in same txn as apply. Hybrid purpose (intent row / `billing_reason`). Mint only `setup_intent` / `cycle_billing`. Route to `MintOnOrderCompletedAsync` / `MintTopupAllocationAsync` / upgrade / Location apply. `SUBSCRIPTION_*` and `DISPUTE_*` sync or hand off — no mint.
- [VAT exclusive pricing and Tummly invoice numbers](./issues/05-vat-exclusive-pricing-and-tummly-invoice-numbers.md): Tummly owns VAT math and legal PDFs. Platform `TM`/`TCN` sequences; mint on pay/refund success; one row per Revolut order UUID; fail-closed on four pack VAT env keys; Revolut secrets → ticket 10.
- [Pricebook lookup keys to Revolut products](./issues/06-pricebook-lookup-keys-to-revolut-products.md): Repo script creates four plans × monthly/annual variations (gross amounts; label = lookup key). Per-env map lookup_key → plan_variation_id (not in pack JSON); fail closed if missing. Top-ups = order amounts + `external_id` = lookup key. Sandbox/live = separate accounts, maps, secrets; live UUIDs out of git.
- [Dunning mapping to past_due, Soft lock, and Dormant](./issues/08-dunning-mapping-to-past-due-soft-lock-and-dormant.md): Tummly `Tick` owns days 0/3/7/10/24. `SUBSCRIPTION_OVERDUE` → `StartDunningEpisode` + store cycle `order_id`. Adapter Pays on day-step 0/3 and after Update payment method. `ORDER_COMPLETED` → `RecoverDunning`. No Revolut auto-cancel on Soft lock/Dormant. Enforcement stays ledger **10**.
- [Top-up order](./issues/07-top-up-order.md): One-time order + HPP only; VAT as ticket 05; Pilot/lock/SMS-5000/VAT/customer gates before create; Idempotency-Key reuse pending; allocate on `ORDER_COMPLETED` → `MintTopupAllocationAsync` (12-month expiry on ledger).
- [Refunds, disputes, and credit reversal with Revolut](./issues/09-refunds-disputes-and-credit-reversal-with-revolut.md): `SourcePaymentRef` = original payment order UUID. Support/Admin refund only (`payment_refund`); `DISPUTE_*` separate (`dispute`). Open+drain on `ACTION_REQUIRED`; won → restore+clear; lost → leave drain + `TCN`, auto-clear only if unused. Plan disputes: overlay only. `TCN` on refund `completed` / `DISPUTE_LOST`. Balance math → ledger **11**/**34**.
- [Go-live VAT and Revolut credentials](./issues/10-go-live-vat-and-revolut-credentials.md): HITL checklist only — pack VAT four keys + `Revolut__SecretKey` / `WebhookSigningSecret` / `ApiBaseUrl` / `ApiVersion` + eight `PlanVariations__*` UUIDs in ACA/Key Vault (not git). Fail closed `vat_not_ready` / `revolut_not_ready` / missing variation / bad webhook signature. No public key for HPP. No real secrets in repo.
- [Assemble Revolut billing integration PRD](./issues/11-assemble-revolut-billing-integration-prd.md): Wrote [PRD.md](./PRD.md) (`ready-for-agent`). Folds 01–10: conversion, payment methods, webhooks, VAT, catalog IDs, top-ups, dunning, disputes, go-live. Build tickets in **Frontier**.

## Not yet specified

- Test / sandbox clocks for dunning rehearsal
- Daily Revolut-versus-ledger reconcile job shape

## Out of scope

- Credit ledger math (backend map)
- Operator Settings chrome except the redirect or embed contract the frontend map needs
- Campaign Email adapter
- Physical starter-kit fulfilment
- Inserting live HMRC / Revolut secrets during lock (ticket 10 records the checklist only)
- Shipping product code on this map
- Stripe as payment provider. Superseded research: [superseded-stripe-checkout-portal-tax-anchors.md](./research/superseded-stripe-checkout-portal-tax-anchors.md)

## Frontier

Build tickets (`Kind: build`). Locks 01–11 stay in **Decisions so far**.

| # | Ticket | Status |
|---|---|---|
| 12 | [Revolut Merchant client and fail-closed config](./issues/12-revolut-merchant-client-and-fail-closed-config.md) | ready-for-agent |
| 13 | [Pricebook lookup keys to Revolut plan variations script](./issues/13-pricebook-lookup-keys-to-revolut-plan-variations-script.md) | resolved |
| 14 | [First paid conversion Hosted Payment Page session](./issues/14-first-paid-conversion-hosted-payment-page-session.md) | ready-for-agent |
| 15 | [Revolut webhook verify, claim store, and retrieve gate](./issues/15-revolut-webhook-verify-claim-store-and-retrieve-gate.md) | resolved |
| 16 | [Setup and cycle ORDER_COMPLETED activation](./issues/16-setup-and-cycle-order-completed-activation.md) | ready-for-agent |
| 17 | [Tummly VAT invoice TM rows and PDF](./issues/17-tummly-vat-invoice-tm-rows-and-pdf.md) | ready-for-agent |
| 18 | [Credit-pack top-up order and allocate](./issues/18-credit-pack-top-up-order-and-allocate.md) | ready-for-agent |
| 19 | [Update payment method via Hosted Payment Page](./issues/19-update-payment-method-via-hosted-payment-page.md) | ready-for-agent |
| 20 | [Same-cadence upgrade pay-now Revolut order](./issues/20-same-cadence-upgrade-pay-now-revolut-order.md) | ready-for-agent |
| 21 | [Cycle-end downgrade and cadence change-plan](./issues/21-cycle-end-downgrade-and-cadence-change-plan.md) | ready-for-agent |
| 22 | [Extra Location add and remove Revolut money path](./issues/22-extra-location-add-and-remove-revolut-money-path.md) | ready-for-human |
| 23 | [Cancel-at-period-end Revolut adapter](./issues/23-cancel-at-period-end-revolut-adapter.md) | ready-for-agent |
| 24 | [Dunning overdue Past due Soft lock recover](./issues/24-dunning-overdue-past-due-soft-lock-recover.md) | ready-for-agent |
| 25 | [Admin payment refund drain and TCN](./issues/25-admin-payment-refund-drain-and-tcn.md) | ready-for-agent |
| 26 | [Dispute webhooks overlay drain restore and TCN](./issues/26-dispute-webhooks-overlay-drain-restore-and-tcn.md) | ready-for-agent |
| 27 | [Go-live VAT and Revolut credentials deploy checklist](./issues/27-go-live-vat-and-revolut-credentials-deploy-checklist.md) | ready-for-agent |
