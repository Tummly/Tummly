# Custom plan: explicit annual unit rates

**Date:** 2026-10-20  
**Status:** Approved design (awaiting implementation)  
**Scope:** Marketing `/custom-plan` estimate + Admin-editable annual unit rates; no schema migration.

## Problem

`CommercialCustomUnitRateRow.AnnualNetPence` and Admin Billing catalog fields already exist. The `/custom-plan` estimate ignores them and always applies **monthly sum × 12 × 0.85** for Annual cadence.

## Locked decisions

- **Meaning:** `AnnualNetPence` is the **period** net price (pence) for that row’s `UnitQuantity` for one year (same role as `MonthlyNetPence` for a month).
- **Missing annual:** Per rate row, if `AnnualNetPence` is null or unset, fall back to that row’s monthly line × 12 × 0.85.
- **Zero:** Treat `0` / empty Admin input as unset (`null`) so fallback applies (match current Admin `n || null` behaviour).
- **No migration / reseed required.** Location seed may keep `39800`; other keys may stay null.
- **Billing:** Unchanged. Cycle order still charges snapshotted `periodNetPence` from continue/checkout.

## Behaviour

### Monthly cadence

Unchanged: each line uses monthly rate; `periodNetPence = monthlyNetPence` sum.

### Annual cadence

For each rate line with quantity `q` and `unitQuantity = u` (min 1):

1. If `annualNetPence` is a positive number:  
   `line = ceil(q × annualNetPence / u)`
2. Else:  
   `monthlyLine = ceil(q × monthlyNetPence / u)`  
   `line = round(monthlyLine × 12 × 0.85)`

`periodNetPence` = sum of annual lines.  
`monthlyNetPence` in the encoded intent remains the monthly-cadence sum (for display / comparison), not the annual period total.

### Admin UI

Custom unit rates tab already exposes Annual net pence. No new fields. Optional copy hint: empty = 15% annual fallback from monthly.

### Docs

Update `docs/product/group-custom-plan-support.md` (and CHANGELOG if needed): annual uses Admin annual unit rates, with per-row 15% fallback when annual is empty. Remove wording that annual is only a global 15% estimate.

## Out of scope

- Requiring annual on every key
- Backend “resolved annual” on the public API
- Changing Revolut cycle-order amount logic beyond the snapshot already produced by the estimate
- Mid-contract re-quote of Custom plans

## Implementation touchpoints

| Area | Files (indicative) |
|------|--------------------|
| Estimate | `src/lib/marketingCustomPlan/customPlanEstimate.ts` (+ unit tests if present / add focused tests) |
| UI consumer | `CustomPlanPage` — no API change if it already passes rates with `annualNetPence` |
| Product note | `docs/product/group-custom-plan-support.md` |

## Success criteria

- Visitor on Annual with all annuals filled: total equals sum of annual unit lines (no global 15%).
- Visitor on Annual with some annuals empty: filled rows use Admin annual; empty rows use 15% fallback.
- Monthly cadence unchanged.
- Existing Custom signup snapshot / cycle pay still use `periodNetPence` from encoded intent.
