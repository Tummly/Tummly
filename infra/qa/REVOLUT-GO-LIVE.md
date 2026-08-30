# Revolut go-live — VAT, Merchant secrets, and catalog map

HITL ops checklist before the first **live** paid transaction on an
environment (Azure Container Apps / Key Vault). Product locks:
[`.scratch/revolut-billing-integration/issues/10-go-live-vat-and-revolut-credentials.md`](../../.scratch/revolut-billing-integration/issues/10-go-live-vat-and-revolut-credentials.md).

**Do not** paste real secrets, Production variation UUIDs, or webhook
signing secrets into git, tickets, pack JSON, or this file. Empty
placeholders only — see
[`backend/TummlyBackend/.env.example`](../../backend/TummlyBackend/.env.example)
and [`secrets.revolut.env.example`](./secrets.revolut.env.example).

Related: plan-variation create runbook
[`scripts/revolut-create-plan-variations/README.md`](../../scripts/revolut-create-plan-variations/README.md);
apply ACA env
[`apply-aca-secrets.ps1`](./apply-aca-secrets.ps1) (gitignored
`secrets.qa.env` + Prod twin).

---

## Where values live

| Store | Use |
| --- | --- |
| Azure Container Apps secrets / Key Vault (same path as `secrets.qa.env` + `apply-aca-secrets.ps1`; Prod twin) | Production and QA live secrets |
| Local gitignored `.env` / `.env.development.local` / `.env.revolut.sandbox.local` | Developer sandbox only |
| Revolut Business dashboard (**Sandbox** or **Production** account) | Create Merchant API secret, create webhook → copy `signing_secret`, create plan variations |
| Per-env config mount (ACA env / Key Vault / gitignored file) | `lookup_key` → `plan_variation_id` map |

Never commit `secrets*.env`, live UUIDs, or API secrets.

---

## Sandbox vs Production

Sandbox and Production are **separate** Revolut Business accounts, Merchant
secrets, webhook signing secrets, webhook registrations, and eight-key
variation maps. UUIDs and secrets **do not** transfer between accounts.

| | Sandbox | Production (live paid) |
| --- | --- | --- |
| Revolut account | Sandbox Business | Production Business |
| `Revolut__ApiBaseUrl` | `https://sandbox-merchant.revolut.com` | `https://merchant.revolut.com` |
| `Revolut__SecretKey` | Sandbox Merchant secret | Production Merchant secret |
| `Revolut__WebhookSigningSecret` | Sandbox webhook `signing_secret` | Production webhook `signing_secret` |
| `Revolut__PlanVariations__*` | Sandbox variation UUIDs | Production variation UUIDs |
| Webhook URL | Sandbox/QA API host + `/api/webhooks/revolut` | Live API host + `/api/webhooks/revolut` |

Pin `Revolut__ApiVersion` per deploy (research default `2026-04-20`).

HPP first conversion does **not** need a Merchant public / publishable key
on the frontend.

---

## Pre-flight checklist (HITL)

Tick every row for the target environment before enabling live paid
conversion.

### A. Seller VAT / legal (pack `vat.*_env`)

| Done | Env key | Notes |
| --- | --- | --- |
| [ ] | `TUMMLY_VAT_REGISTRATION_NUMBER` | HMRC VAT registration number (public on invoices; treat as controlled config) |
| [ ] | `TUMMLY_VAT_EFFECTIVE_DATE` | VAT registration effective date |
| [ ] | `TUMMLY_LEGAL_NAME` | Legal entity name on VAT PDFs |
| [ ] | `TUMMLY_REGISTERED_ADDRESS` | Registered address on VAT PDFs |

Pack: `fail_live_paid_checkout_if_missing: true`.

### B. Revolut Merchant (server — `.NET` double-underscore form)

| Done | Env key | Secret? | Notes |
| --- | --- | --- | --- |
| [ ] | `Revolut__SecretKey` | **Yes** | Merchant API Bearer secret for **this** account |
| [ ] | `Revolut__WebhookSigningSecret` | **Yes** | Webhook `signing_secret` from create/retrieve webhook |
| [ ] | `Revolut__ApiBaseUrl` | No | Host for this account (sandbox or live — see table above) |
| [ ] | `Revolut__ApiVersion` | No | `Revolut-Api-Version` header; pin per deploy |

### C. Catalog map (eight current recurring keys)

Create Production (or sandbox) plans/variations with
`scripts/revolut-create-plan-variations/` (`--apply` prints
`Revolut__PlanVariations__*` lines). Mount **all eight** for this env:

| Done | Env key |
| --- | --- |
| [ ] | `Revolut__PlanVariations__tummly_starter_monthly_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_starter_annual_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_growth_monthly_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_growth_annual_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_group_monthly_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_group_annual_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_group_location_monthly_gbp_v3` |
| [ ] | `Revolut__PlanVariations__tummly_group_location_annual_gbp_v3` |

Top-up packs need **no** Revolut catalog UUID (order amount + `external_id`
= lookup key).

### D. Ops identifiers (Revolut dashboard — not env secrets)

| Done | Item | Notes |
| --- | --- | --- |
| [ ] | Webhook URL | `POST {api-host}/api/webhooks/revolut` on the live (or QA) API host |
| [ ] | Webhook id | Keep for rotate-secret / support (dashboard or retrieve webhook) |
| [ ] | Event subscribe set | Lock 04: `ORDER_COMPLETED`, `ORDER_FAILED`, `ORDER_CANCELLED`, `ORDER_AUTHORISED`, `ORDER_PAYMENT_DECLINED`, `ORDER_PAYMENT_FAILED`, `SUBSCRIPTION_INITIATED`, `SUBSCRIPTION_CANCELLED`, `SUBSCRIPTION_OVERDUE`, `SUBSCRIPTION_FINISHED`, `DISPUTE_*` |
| [ ] | Eight variations exist | Created by the repo script for **this** Revolut account |

Example webhook URLs (hosts change per env — confirm before register):

- QA ACA default: `https://ca-tummly-qa-api.agreeablewater-62e50314.uksouth.azurecontainerapps.io/api/webhooks/revolut`
- QA custom (if DNS live): `https://api.qa.tummly.com/api/webhooks/revolut`
- Production: `https://{live-api-host}/api/webhooks/revolut`

### E. Apply to ACA / Key Vault

| Done | Step |
| --- | --- |
| [ ] | Copy keys from [`secrets.revolut.env.example`](./secrets.revolut.env.example) into the **gitignored** env file (`secrets.qa.env` or Prod twin) |
| [ ] | Fill values from Revolut dashboard / create script output only on the operator machine |
| [ ] | Run `./apply-aca-secrets.ps1` (or Prod twin) so ACA / Key Vault receives the keys |
| [ ] | Confirm revision ready; do **not** enable live paid conversion until webhook verify works |

---

## Fail-closed behaviour (live)

Before any live Merchant create that can take money (`POST /api/customers`,
`POST /api/subscriptions`, `POST /api/orders` used for pay), the app refuses
if any A/B config is empty or the target recurring SKU has no C map entry.

| Condition | Code / HTTP | Effect |
| --- | --- | --- |
| Any VAT A key missing | `vat_not_ready` / **503** | No redirect to HPP; no Merchant create |
| `Revolut__SecretKey` / `ApiBaseUrl` / `ApiVersion` missing | `revolut_not_ready` / **503** | No Merchant call |
| Target recurring `plan_variation_id` missing from C | `plan_variation_missing` / **503** | No subscription create / change onto that SKU |
| Empty or wrong `Revolut__WebhookSigningSecret` | Bad signature → **401/400** | No event row; do not enable live paid conversion in an env that cannot verify webhooks |

Gate: `RevolutMerchantCreateGate`. Webhook: `POST /api/webhooks/revolut`
(bad signature → **401/400**, no event row; current controller returns
Unauthorized / **401**).

Sandbox may use separate sandbox keys and a sandbox map. Live fail-closed
does **not** apply to pure Pilot / unpaid paths that never call Revolut.

---

## Rotation

1. Rotate Merchant secret and/or webhook signing secret in the Revolut
   Business dashboard (or rotate-secret API).
2. Update **only** ACA / Key Vault (edit gitignored `secrets*.env`, then
   `apply-aca-secrets.ps1` or Prod twin).
3. Never paste rotated values into the repo, tickets, or pack JSON.
4. After webhook secret rotate, confirm a test delivery verifies (no
   **401/400**) before treating the env as go-live ready.

---

## Out of this checklist

Pack §21 non-Revolut keys (Resend, Twilio, Azure, campaign-email binding,
sending domain, complaint/bounce monitoring, invoice email / support
contact) stay on their own handoffs (e.g. [`RESEND-HANDOFF.md`](./RESEND-HANDOFF.md)).
They do not block this Revolut money-movement checklist.
