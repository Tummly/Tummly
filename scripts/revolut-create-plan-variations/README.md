# Revolut plan variations create (ticket 13 / lock 06)

Repo-owned runbook. Creates the **eight** recurring Revolut subscription plans
(one plan per cadence) from pack lookup keys. Amounts are Tummly **gross**
minor units (net pack pence + 20% UK VAT, half-up). Each plan `name` is a
friendly checkout label (e.g. `Paid Starter Plan Monthly`). Each variation
`name` (label) is the pack `lookup_key` (same string as the env map key).
**Never PATCH** a live variation amount — new pricebook → new lookup keys →
new variations.

Top-ups have **no** Revolut catalog object.

## Prerequisites

- Pack JSON: `docs/product/billing-pack-v3.0/tummly_uk_billing_config_v3.0.json`
- Sandbox or Production Merchant secret (separate accounts; UUIDs do not transfer)
- `REVOLUT_API_BASE_URL` — sandbox default
  `https://sandbox-merchant.revolut.com` or live
  `https://merchant.revolut.com`
- `REVOLUT_API_VERSION` — default `2026-04-20`
- `REVOLUT_SECRET_KEY` — Bearer secret for that environment

## Dry-run (no HTTP)

```bash
./scripts/revolut-create-plan-variations/create-plan-variations.sh
```

Prints net → gross rows and the eight `POST /api/subscription-plans` bodies
(one plan per cadence: e.g. `Paid Starter Plan Monthly`,
`Paid Starter Plan Annual`, …). Hosted Checkout uses the plan `name`.
Variation `name` (label) stays the pack `lookup_key` (same string as the env
map key). **Never PATCH** a live variation amount — new pricebook → new
lookup keys → new variations.

## Apply (sandbox)

```bash
export REVOLUT_SECRET_KEY=sk_…          # sandbox
export REVOLUT_API_BASE_URL=https://sandbox-merchant.revolut.com
./scripts/revolut-create-plan-variations/create-plan-variations.sh --apply \
  --out backend/TummlyBackend/.env.revolut.sandbox.local
```

Mount the printed `Revolut__PlanVariations__*` lines into the deploy env
(or the gitignored local file). Live Production UUIDs stay out of git
(ticket 27 / 10). Full ACA/Key Vault HITL checklist:
[`infra/qa/REVOLUT-GO-LIVE.md`](../../infra/qa/REVOLUT-GO-LIVE.md);
**QA Sandbox / test cards first:**
[`infra/qa/REVOLUT-QA-SANDBOX.md`](../../infra/qa/REVOLUT-QA-SANDBOX.md);
empty ACA placeholders:
[`infra/qa/secrets.revolut.env.example`](../../infra/qa/secrets.revolut.env.example).

## Mount in the app

`RevolutSettings.PlanVariations` binds
`Revolut__PlanVariations__{lookup_key}`. Empty placeholders live in
`backend/TummlyBackend/.env.example`. Sandbox may use
`backend/TummlyBackend/.env.revolut.sandbox.local` (gitignored).

Missing a **current** recurring key → fail closed
(`plan_variation_missing`) on subscription create / change-plan onto that
SKU. Grandfathered subscriptions keep their existing variation ids.
