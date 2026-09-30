# Revolut plan variations create (ticket 13 / lock 06)

Repo-owned runbook. Creates the **eight** recurring Revolut subscription plans
(one plan per cadence) from pack lookup keys.

**Amount modes** (`--amount`, default `net`):

| Mode | Revolut phase amount | Env map keys | VAT mode |
|------|----------------------|--------------|----------|
| `net` | Pack net pence (e.g. Growth monthly **9900**) | `Revolut__PlanVariations__*` | `TUMMLY_VAT_MODE_ACTIVE=false` |
| `gross` | Net + 20% UK VAT, half-up (e.g. Growth **11880**) | `Revolut__PlanVariationsGross__*` | `TUMMLY_VAT_MODE_ACTIVE=true` |

Plan `name` includes `(excl. VAT)` or `(incl. VAT)` so Sandbox catalogs stay
distinct. Each variation `name` (label) is the pack `lookup_key` (same string
as the env map key). **Never PATCH** a live variation amount — new pricebook →
new lookup keys → new variations.

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
# Net map (launch / VAT off) — default
./scripts/revolut-create-plan-variations/create-plan-variations.sh --amount net

# Gross map (VAT on)
./scripts/revolut-create-plan-variations/create-plan-variations.sh --amount gross
```

Prints net / gross / charge rows and the eight `POST /api/subscription-plans`
bodies. Hosted Checkout uses the plan `name`.

## Apply (sandbox)

```bash
export REVOLUT_SECRET_KEY=sk_…          # sandbox
export REVOLUT_API_BASE_URL=https://sandbox-merchant.revolut.com

# Net map → Revolut__PlanVariations__*
./scripts/revolut-create-plan-variations/create-plan-variations.sh --apply \
  --amount net \
  --out /tmp/revolut-sandbox-plan-variations-net.env

# Gross map → Revolut__PlanVariationsGross__* (when rehearsing VAT on)
./scripts/revolut-create-plan-variations/create-plan-variations.sh --apply \
  --amount gross \
  --out /tmp/revolut-sandbox-plan-variations-gross.env
```

Mount the printed lines into the deploy env (or the gitignored local file).
Live Production UUIDs stay out of git (ticket 27 / 10). Full ACA/Key Vault
HITL checklist: [`infra/qa/REVOLUT-GO-LIVE.md`](../../infra/qa/REVOLUT-GO-LIVE.md);
**QA Sandbox / test cards first:**
[`infra/qa/REVOLUT-QA-SANDBOX.md`](../../infra/qa/REVOLUT-QA-SANDBOX.md);
empty ACA placeholders:
[`infra/qa/secrets.revolut.env.example`](../../infra/qa/secrets.revolut.env.example).

## Mount in the app

- VAT off: `RevolutSettings.PlanVariations` ← `Revolut__PlanVariations__{lookup_key}`
- VAT on: `RevolutSettings.PlanVariationsGross` ← `Revolut__PlanVariationsGross__{lookup_key}`

Empty placeholders live in `backend/TummlyBackend/.env.example`. Sandbox may
use `backend/TummlyBackend/.env.revolut.sandbox.local` (gitignored).

Missing a **current** recurring key for the active mode → fail closed
(`plan_variation_missing`) on subscription create / change-plan onto that
SKU. Grandfathered subscriptions keep their existing variation ids.
