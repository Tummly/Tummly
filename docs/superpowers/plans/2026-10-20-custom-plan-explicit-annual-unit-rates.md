# Custom plan explicit annual unit rates Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make `/custom-plan` Annual totals use Admin `annualNetPence` per unit-rate row, with per-row monthly × 12 × 0.85 fallback when annual is unset.

**Architecture:** Keep schema and Admin fields as-is. Change only the frontend estimate (`customPlanEstimate.ts`) so annual lines resolve per rate, then update the product note. Billing still charges snapshotted `periodNetPence` from the encoded intent.

**Tech Stack:** TypeScript, Vitest, React (CustomPlanPage consumer unchanged if rates already include `annualNetPence`).

## Global Constraints

- Spec: `docs/superpowers/specs/2026-10-20-custom-plan-explicit-annual-unit-rates-design.md`
- `AnnualNetPence` = period net pence for that row’s `unitQuantity` for one year
- Missing / zero annual → that row only: `round(monthlyLine × 12 × 0.85)`
- Positive annual → `ceil(q × annualNetPence / unitQuantity)`
- No EF migration; no reseed required
- Do not change Revolut cycle-order services
- Commits: only when the user explicitly asks (repo user rule)

---

## File map

| File | Responsibility |
|------|----------------|
| `src/lib/marketingCustomPlan/customPlanEstimate.ts` | Line and period estimate for monthly/annual |
| `src/lib/marketingCustomPlan/customPlanEstimate.test.ts` | Vitest coverage for annual / fallback / monthly |
| `docs/product/group-custom-plan-support.md` | Product wording for annual rates |
| `docs/product/CHANGELOG.md` | One-line behaviour note |

`CustomPlanPage.tsx` and Admin panel need no structural change (Admin already edits `annualNetPence`; public API already returns it). Optional one-line Admin hint only if Task 3 includes it.

---

### Task 1: Estimate — failing tests then implementation

**Files:**
- Create: `src/lib/marketingCustomPlan/customPlanEstimate.test.ts`
- Modify: `src/lib/marketingCustomPlan/customPlanEstimate.ts`

**Interfaces:**
- Consumes: `AdminCommercialCustomUnitRate` (`id`, `rateKey`, `unitQuantity`, `monthlyNetPence`, `annualNetPence: number | null`)
- Produces: updated `lineNetPence(rate, quantity, cadence?)` and `estimateCustomPlanTotalPence` / `buildCustomPlanLines` behaviour per Global Constraints

- [ ] **Step 1: Write the failing tests**

Create `src/lib/marketingCustomPlan/customPlanEstimate.test.ts`:

```typescript
import { describe, expect, it } from "vitest"

import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import {
  estimateCustomPlanTotalPence,
  lineNetPence,
} from "@/lib/marketingCustomPlan/customPlanEstimate"

function rate(
  partial: Partial<AdminCommercialCustomUnitRate> & {
    rateKey: string
    monthlyNetPence: number
  }
): AdminCommercialCustomUnitRate {
  return {
    id: partial.id ?? "r1",
    rateKey: partial.rateKey,
    unitQuantity: partial.unitQuantity ?? 1,
    monthlyNetPence: partial.monthlyNetPence,
    annualNetPence: partial.annualNetPence ?? null,
  }
}

describe("lineNetPence", () => {
  it("uses monthly for monthly cadence", () => {
    expect(
      lineNetPence(rate({ rateKey: "location", monthlyNetPence: 3900 }), 2, "monthly")
    ).toBe(7800)
  })

  it("uses annual when set for annual cadence", () => {
    expect(
      lineNetPence(
        rate({
          rateKey: "location",
          monthlyNetPence: 3900,
          annualNetPence: 39800,
        }),
        2,
        "annual"
      )
    ).toBe(79600)
  })

  it("falls back to monthly × 12 × 0.85 when annual unset", () => {
    // monthly line for qty 1 = 300; annual fallback = round(300 * 12 * 0.85) = 3060
    expect(
      lineNetPence(
        rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: null }),
        1,
        "annual"
      )
    ).toBe(3060)
  })

  it("treats annual 0 as unset for fallback", () => {
    expect(
      lineNetPence(
        rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: 0 }),
        1,
        "annual"
      )
    ).toBe(3060)
  })
})

describe("estimateCustomPlanTotalPence", () => {
  const rates = [
    rate({ rateKey: "location", monthlyNetPence: 3900, annualNetPence: 39800 }),
    rate({ rateKey: "team_user", monthlyNetPence: 300, annualNetPence: null }),
  ]

  it("sums mixed annual and fallback rows for annual cadence", () => {
    const result = estimateCustomPlanTotalPence(
      rates,
      {
        locations: 1,
        teamUsers: 1,
        qrPlacements: 0,
        offers: 0,
        emailCredits: 0,
        smsCredits: 0,
        aiCredits: 0,
      },
      "annual"
    )
    // location annual 39800 + team fallback 3060
    expect(result.displayNetPence).toBe(42860)
    expect(result.monthlyNetPence).toBe(4200)
  })

  it("keeps monthly as sum of monthly lines", () => {
    const result = estimateCustomPlanTotalPence(
      rates,
      {
        locations: 1,
        teamUsers: 1,
        qrPlacements: 0,
        offers: 0,
        emailCredits: 0,
        smsCredits: 0,
        aiCredits: 0,
      },
      "monthly"
    )
    expect(result.displayNetPence).toBe(4200)
    expect(result.monthlyNetPence).toBe(4200)
  })
})
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `npx vitest run src/lib/marketingCustomPlan/customPlanEstimate.test.ts`

Expected: FAIL (signature / behaviour still monthly-only × global 0.85).

- [ ] **Step 3: Implement estimate**

Replace `lineNetPence`, `buildCustomPlanLines`, and `estimateCustomPlanTotalPence` in `src/lib/marketingCustomPlan/customPlanEstimate.ts` with:

```typescript
import type { AdminCommercialCustomUnitRate } from "@/api/adminCommercialCatalogApi"
import type { BillingCadence } from "@/lib/operatorBillingCredits/managePlanPresentation"

export const CUSTOM_PLAN_MAX_LOCATIONS = 500
export const ANNUAL_FALLBACK_FACTOR = 0.85

// ... keep CustomPlanQuantities, CustomPlanLine, RATE_TO_QTY, LABELS, EMPTY_*, formatPenceGbp, encodeCustomPlanIntent structure ...

function monthlyLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number
): number {
  if (quantity <= 0) {
    return 0
  }
  const unitQty = Math.max(1, rate.unitQuantity)
  return Math.ceil((quantity * rate.monthlyNetPence) / unitQty)
}

function annualLineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number
): number {
  if (quantity <= 0) {
    return 0
  }
  const unitQty = Math.max(1, rate.unitQuantity)
  const annual = rate.annualNetPence
  if (annual != null && annual > 0) {
    return Math.ceil((quantity * annual) / unitQty)
  }
  return Math.round(monthlyLineNetPence(rate, quantity) * 12 * ANNUAL_FALLBACK_FACTOR)
}

/** Net pence for `quantity` natural units at the given rate row and cadence. */
export function lineNetPence(
  rate: AdminCommercialCustomUnitRate,
  quantity: number,
  cadence: BillingCadence = "monthly"
): number {
  return cadence === "annual"
    ? annualLineNetPence(rate, quantity)
    : monthlyLineNetPence(rate, quantity)
}

export function buildCustomPlanLines(
  rates: AdminCommercialCustomUnitRate[],
  quantities: CustomPlanQuantities,
  cadence: BillingCadence = "monthly"
): CustomPlanLine[] {
  const lines: CustomPlanLine[] = []
  for (const rate of rates) {
    const qtyKey = RATE_TO_QTY[rate.rateKey]
    if (qtyKey == null) {
      continue
    }
    const quantity = quantities[qtyKey]
    const net = lineNetPence(rate, quantity, cadence)
    if (quantity <= 0 && rate.rateKey !== "location") {
      continue
    }
    lines.push({
      rateKey: rate.rateKey,
      label: LABELS[rate.rateKey] ?? rate.rateKey,
      quantity,
      netPence: net,
    })
  }
  return lines
}

export function estimateCustomPlanTotalPence(
  rates: AdminCommercialCustomUnitRate[],
  quantities: CustomPlanQuantities,
  cadence: BillingCadence
): { monthlyNetPence: number; displayNetPence: number; lines: CustomPlanLine[] } {
  const monthlyLines = buildCustomPlanLines(rates, quantities, "monthly")
  const monthlyNetPence = monthlyLines.reduce((sum, line) => sum + line.netPence, 0)
  if (cadence === "monthly") {
    return { monthlyNetPence, displayNetPence: monthlyNetPence, lines: monthlyLines }
  }
  const lines = buildCustomPlanLines(rates, quantities, "annual")
  const displayNetPence = lines.reduce((sum, line) => sum + line.netPence, 0)
  return { monthlyNetPence, displayNetPence, lines }
}
```

Keep `encodeCustomPlanIntent` calling `estimateCustomPlanTotalPence` as today (`periodNetPence: displayNetPence`).

- [ ] **Step 4: Run tests to verify they pass**

Run: `npx vitest run src/lib/marketingCustomPlan/customPlanEstimate.test.ts`

Expected: PASS (all tests).

- [ ] **Step 5: Commit (only if user asked)**

```bash
git add src/lib/marketingCustomPlan/customPlanEstimate.ts src/lib/marketingCustomPlan/customPlanEstimate.test.ts
git commit -m "$(cat <<'EOF'
fix(custom-plan): use Admin annual unit rates with 15% row fallback

EOF
)"
```

---

### Task 2: Product docs

**Files:**
- Modify: `docs/product/group-custom-plan-support.md`
- Modify: `docs/product/CHANGELOG.md`

**Interfaces:**
- Consumes: Task 1 behaviour (per-row annual + fallback)
- Produces: accurate product wording

- [ ] **Step 1: Update the Custom price row**

In `docs/product/group-custom-plan-support.md`, replace the Custom price cell text that mentions a global 15% rule with:

`Sum of Admin custom unit rates (annual uses each row’s AnnualNetPence; if empty, that row falls back to monthly × 12 × 0.85)`

- [ ] **Step 2: Changelog entry**

Under the latest `## 2026-10-20` block in `docs/product/CHANGELOG.md`, add under Updated:

`- Custom plan annual estimate uses Admin annual unit rates (15% fallback per empty row)`

- [ ] **Step 3: Commit (only if user asked)**

```bash
git add docs/product/group-custom-plan-support.md docs/product/CHANGELOG.md
git commit -m "$(cat <<'EOF'
docs: custom plan annual unit rates wording

EOF
)"
```

---

### Task 3: Optional Admin hint (same PR if quick)

**Files:**
- Modify: `src/components/dashboard/admin/AdminCommercialCatalogPanel.tsx`

**Interfaces:**
- Consumes: existing `penceField("Annual net pence", …)`
- Produces: clearer empty = fallback

- [ ] **Step 1: Clarify Annual label**

Change the Custom unit rates annual field label from `"Annual net pence"` to `"Annual net pence (empty = 15% fallback)"`.

Keep `updateRate(index, { annualNetPence: n || null })` so `0` clears to null.

- [ ] **Step 2: Smoke check**

Manually confirm Admin Custom unit rates tab still saves null when the annual field is cleared.

- [ ] **Step 3: Commit (only if user asked)**

```bash
git add src/components/dashboard/admin/AdminCommercialCatalogPanel.tsx
git commit -m "$(cat <<'EOF'
chore(admin): clarify custom unit annual fallback label

EOF
)"
```

---

## Spec coverage check

| Spec requirement | Task |
|------------------|------|
| Per-row annual when set | Task 1 |
| Per-row 15% fallback when null/0 | Task 1 |
| Monthly unchanged | Task 1 |
| No migration / billing change | (none — out of scope) |
| Product docs | Task 2 |
| Admin empty = null | Task 3 (label); behaviour already present |

## Placeholder scan

No TBD / “implement later” steps. Test code and implementation snippets are complete.
