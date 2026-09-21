# Launch VAT mode (env OFF default)

**Status:** Design approved (chat 2026-09-21); awaiting spec review before plan/build  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive (VAT launch = `not_registered`/OFF)  
**Clears conflict:** Billing pack v3.0 “20% VAT on all launch charges” vs launch OFF

## Problem

Runtime always applies 20% VAT on one-time pays, mints VAT invoices, shows “+ VAT” in operator UI, and fails closed with `vat_not_ready` until seller `TUMMLY_VAT_*` keys are set. Recurring Revolut `plan_variation_id` amounts are fixed in Merchant — Tummly cannot add/remove VAT on those amounts at create time.

Launch must charge **net only**, skip `vat_not_ready`, show **no** customer-facing VAT amount / number / invoice, and ensure Revolut does **not** charge +20%.

## Decision

**Approach A:** Env-driven VAT mode with **two** Revolut plan-variation maps.

| Mode | Env | Revolut recurring map | One-time pay | Seller VAT keys | Invoice / UI |
|------|-----|----------------------|--------------|-----------------|--------------|
| OFF (launch default) | `TUMMLY_VAT_MODE_ACTIVE=false` | `Revolut__PlanVariations__*` = **net** GBP | Amount = net; `VatPence = 0` | Optional; gate skips `vat_not_ready` | No VAT invoice mint; no “+ VAT” / VAT number |
| ACTIVE | `TUMMLY_VAT_MODE_ACTIVE=true` | `Revolut__PlanVariationsGross__*` = **gross** (net + 20%) | Net + 20% (current math) | Required (existing completeness) | Current mint + PDF + UI |

Mode is **env-only**. Do not infer from pricebook `VatRateBps` (pricebook may keep 2000 for ACTIVE).

## Config

```bash
# Default when unset: false
TUMMLY_VAT_MODE_ACTIVE=false   # true | false

# OFF → net variations (existing keys)
Revolut__PlanVariations__tummly_starter_monthly_gbp_v3=...
# ... same eight lookup keys as today

# ACTIVE → gross variations (new parallel map)
Revolut__PlanVariationsGross__tummly_starter_monthly_gbp_v3=...
# ... same eight lookup keys

# Required only when TUMMLY_VAT_MODE_ACTIVE=true
TUMMLY_VAT_REGISTRATION_NUMBER=
TUMMLY_VAT_EFFECTIVE_DATE=
TUMMLY_LEGAL_NAME=
TUMMLY_REGISTERED_ADDRESS=
```

Unset / empty / false → VAT **off** (fail safe toward launch).

## Behaviour detail

### Merchant create gate (`RevolutMerchantCreateGate`)

1. If mode is `off`: do **not** return `vat_not_ready`. Resolve plan variation from **net** map when a lookup key is passed.
2. If mode is `active`: keep today’s `vat_not_ready` when seller VAT incomplete. Resolve plan variation from **gross** map when a lookup key is passed.
3. Missing variation in the **active** map for the current mode → `plan_variation_missing`.

### One-time pays

Call sites that use `TummlyVatMath.VatPenceFromNetPence` / `GrossMinorFromNetPence` (Shop place/pay, credit top-up, order completed applier amounts, etc.) must take **effective rate**:

- `off` → `vatRateBps = 0` (gross = net)
- `active` → `2000` (or pricebook rate when already passed)

Shop / billing rows store `VatPence = 0` when OFF.

### Invoices / credit notes

When `off`: do **not** mint `TummlyVatInvoice` / VAT PDF / VAT email for new completed orders or refunds.  
When `active`: current `TummlyVatInvoiceService` path.

Historical invoices minted while ACTIVE remain immutable evidence.

### Operator UI

Expose mode on an existing billing/health (or billing credits) API field, e.g. `vatModeActive: boolean`. Frontend must **not** use a separate Vite flag.

When `false`: hide “+ VAT”, gross-incl-VAT labels, and any customer-facing VAT number. Show net prices only.  
When `true`: keep current presentation.

### Revolut ops

Before flipping to `active`, create matching **gross** plan variations in Merchant and fill `Revolut__PlanVariationsGross__*`. Net map remains for OFF / rollback.

## Fail-closed

1. `active` + incomplete seller VAT → `vat_not_ready`.
2. `active` + missing gross variation for required key → `plan_variation_missing`.
3. `off` + missing net variation for required key → `plan_variation_missing`.
4. Never charge gross while mode is `off` (code rate 0 + net variation IDs).

## Out of scope

- Starter Kit Billing-Account vs per-Location entitlement
- Shop production-start cutoff / ADR 0046 refund-on-cancel
- Rewriting the signed billing-pack PDF (this spec **overrides** pack VAT-on for launch; document in `.env.example` + this file)
- Tax-free “receipt” product (only: no VAT invoice when OFF)

## Docs to update at build time

- `backend/TummlyBackend/.env.example`
- `backend/TummlyBackend/.env.revolut.sandbox.local.example` (if present)
- `infra/qa/REVOLUT-QA-SANDBOX.md` / `REVOLUT-GO-LIVE.md` (mode + dual maps)
- Tests: gate OFF skips VAT; OFF pay amounts net; ACTIVE still fail-closed; variation map selection

## Success criteria

1. Default / unset mode → net checkout, no `vat_not_ready`, no VAT UI, no new VAT invoices.
2. Revolut recurring create under OFF uses net `plan_variation_id` only.
3. Setting `TUMMLY_VAT_MODE_ACTIVE=true` with complete seller keys + gross map restores current VAT-on behaviour.
4. Billing pack conflict is resolved by env launch OFF without inventing alternate legal tax logic.
