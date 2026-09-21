# Launch VAT Mode Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Env-driven VAT flag defaults to false so launch charges net only, skips `vat_not_ready`, hides customer VAT UI/invoices, and uses net Revolut plan variations; `true` restores today’s VAT-on path via a gross variation map.

**Architecture:** Bind `TUMMLY_VAT_MODE_ACTIVE` on `TummlySellerVatSettings`. Add `Revolut__PlanVariationsGross__*` beside the existing net map. Gate, Merchant client, pay sessions, invoice mint, and billing catalog `VatRateBps` all read effective mode. Frontend treats `VatRateBps === 0` (and optional `vatModeActive`) as no “+ VAT”.

**Tech Stack:** ASP.NET Core options binding, existing Revolut Merchant + Shop/Billing services, Vitest presentation helpers, xUnit backend tests.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-vat-mode-design.md](../specs/2026-09-21-launch-vat-mode-design.md)

## Global Constraints

- Default / unset / false `TUMMLY_VAT_MODE_ACTIVE` → VAT **off** (fail safe toward launch).
- False: net only; skip `vat_not_ready`; no new VAT invoices; no customer-facing VAT amount/number; Revolut recurring uses **net** `PlanVariations`.
- True: require seller `TUMMLY_VAT_*` keys; use **gross** `PlanVariationsGross`; 20% math and invoices as today.
- Do not infer mode from pricebook JSON `VatRateBps`; mode is env-only.
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Configurations/TummlySellerVatSettings.cs` | Add `IsActive` wire + `EffectiveVatRateBps` |
| `backend/TummlyBackend/Configurations/RevolutSettings.cs` | Add `PlanVariationsGross`; mode-aware `TryGetPlanVariationId` |
| `backend/TummlyBackend/Program.cs` | Bind `TUMMLY_VAT_MODE_ACTIVE`; health exposes `vatModeActive` |
| `backend/TummlyBackend/Services/RevolutMerchantCreateGate.cs` | Skip VAT complete check when OFF; resolve correct map |
| `backend/TummlyBackend/Services/RevolutMerchantClient.cs` | Resolve variation via mode-aware settings |
| `backend/TummlyBackend/Services/CreditTopUpPaySessionService.cs` | Effective rate for gross |
| `backend/TummlyBackend/Services/ShopOrderPlaceService.cs` | `VatPence` from effective rate |
| `backend/TummlyBackend/Services/ShopMaterialsOrderPaySessionService.cs` | Effective rate on pay + line taxes |
| `backend/TummlyBackend/Services/ExtraGroupLocationService.cs` | Gross from effective rate (not raw book when OFF) |
| `backend/TummlyBackend/Services/TummlyVatInvoiceService.cs` | Refuse / no-op mint when OFF |
| `backend/TummlyBackend/Services/RevolutOrderCompletedApplier.cs` | Skip mint when OFF; use effective rate where needed |
| `backend/TummlyBackend/Billing/Pricebook/PricebookCatalog.cs` | `BuildCurrentCatalog` accepts override rate **or** caller sets after |
| `backend/TummlyBackend/Services/BillingCreditsService.cs` | Set catalog `VatRateBps` + page `VatModeActive` |
| `backend/TummlyBackend/DTOs/BillingCredits/BillingCreditsDtos.cs` | Add `VatModeActive` on page DTO |
| `backend/TummlyBackend/.env.example` (+ revolut sandbox example) | Document mode + dual maps |
| `infra/qa/REVOLUT-QA-SANDBOX.md` / `REVOLUT-GO-LIVE.md` | Ops note |
| Frontend presentation modules under `src/lib/operatorBillingCredits/` | Hide “+ VAT” when rate 0 / mode off |
| Tests under `backend/TummlyBackend.Tests/` and Vitest siblings | Gate, pay, mint, UI |

---

### Task 1: VAT mode settings + Revolut dual maps

**Files:**
- Modify: `backend/TummlyBackend/Configurations/TummlySellerVatSettings.cs`
- Modify: `backend/TummlyBackend/Configurations/RevolutSettings.cs`
- Modify: `backend/TummlyBackend/Program.cs` (options bind ~105–120)
- Test: `backend/TummlyBackend.Tests/Services/RevolutSettingsBindTests.cs` (extend) **or** new `TummlySellerVatSettingsTests.cs`

**Interfaces:**
- Consumes: none
- Produces:
  - `TummlySellerVatSettings.ModeActiveKey = "TUMMLY_VAT_MODE_ACTIVE"`
  - `bool IsActive` (bound from env); `int EffectiveVatRateBps` → `0` when not active else `TummlyVatMath.DefaultVatRateBps`
  - `RevolutSettings.PlanVariationsGross`
  - `bool TryGetPlanVariationId(string lookupKey, bool useGrossMap, out string planVariationId)`
  - Keep overload `TryGetPlanVariationId(string lookupKey, out string id)` calling **net** map only for catalog/CLI tools that are net-oriented; Merchant/gate must use the two-arg form with `useGrossMap: vat.IsActive`

- [ ] **Step 1: Write the failing test**

```csharp
[Fact]
public void EffectiveVatRateBps_IsZero_WhenModeOffOrUnset()
{
    var off = new TummlySellerVatSettings { IsActive = false };
    Assert.False(off.IsActive);
    Assert.Equal(0, off.EffectiveVatRateBps);

    var unset = new TummlySellerVatSettings { IsActive = false };
    Assert.False(unset.IsActive);
    Assert.Equal(0, unset.EffectiveVatRateBps);
}

[Fact]
public void EffectiveVatRateBps_Is2000_WhenModeActive()
{
    var active = new TummlySellerVatSettings { IsActive = true };
    Assert.True(active.IsActive);
    Assert.Equal(2000, active.EffectiveVatRateBps);
}

[Fact]
public void TryGetPlanVariationId_SelectsGrossMap_WhenRequested()
{
    var settings = new RevolutSettings
    {
        PlanVariations = new Dictionary<string, string>
        {
            [RevolutPlanVariationKeys.StarterMonthly] = "net-id",
        },
        PlanVariationsGross = new Dictionary<string, string>
        {
            [RevolutPlanVariationKeys.StarterMonthly] = "gross-id",
        },
    };

    Assert.True(
        settings.TryGetPlanVariationId(
            RevolutPlanVariationKeys.StarterMonthly,
            useGrossMap: true,
            out var gross
        )
    );
    Assert.Equal("gross-id", gross);

    Assert.True(
        settings.TryGetPlanVariationId(
            RevolutPlanVariationKeys.StarterMonthly,
            useGrossMap: false,
            out var net
        )
    );
    Assert.Equal("net-id", net);
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~EffectiveVatRateBps|FullyQualifiedName~TryGetPlanVariationId_SelectsGrossMap" --no-restore`  
(restore first if needed). Expected: FAIL (members missing).

- [ ] **Step 3: Minimal implementation**

On `TummlySellerVatSettings`:

```csharp
public const string ModeActiveKey = "TUMMLY_VAT_MODE_ACTIVE";

/// <summary>Bound from ModeActiveKey. Unset / false → VAT off.</summary>
public bool IsActive { get; set; }

public int EffectiveVatRateBps =>
    IsActive ? TummlyBackend.Helpers.TummlyVatMath.DefaultVatRateBps : 0;
```

On `RevolutSettings`: add `PlanVariationsGross` dictionary (same comparer as net). Implement two-arg `TryGetPlanVariationId` selecting the map; keep one-arg as net-only wrapper.

In `Program.cs` Configure for VAT:

```csharp
options.IsActive = configuration.GetValue(
    TummlySellerVatSettings.ModeActiveKey,
    false
);
```

`PlanVariationsGross` binds automatically via `Configure<RevolutSettings>(GetSection("Revolut"))` as `Revolut__PlanVariationsGross__{key}`.

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 2: Merchant create gate respects OFF + gross map when ACTIVE

**Files:**
- Modify: `backend/TummlyBackend/Services/RevolutMerchantCreateGate.cs`
- Modify: `backend/TummlyBackend.Tests/Services/RevolutMerchantCreateGateTests.cs`
- Modify: `backend/TummlyBackend/Program.cs` `/health/revolut` (~1210–1243)

**Interfaces:**
- Consumes: `TummlySellerVatSettings.IsActive`, `RevolutSettings.TryGetPlanVariationId(..., useGrossMap)`
- Produces: gate behaviour per spec; health JSON includes `vatModeActive` (bool) and uses mode-aware variation count for the **active** map

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public void Evaluate_WhenModeOff_SkipsVatComplete_EvenIfSellerVatEmpty()
{
    var vat = new TummlySellerVatSettings { IsActive = false };
    var revolut = FullLiveRevolut(withStarterMonthly: true);
    var gate = CreateGate(vat, revolut);
    Assert.Null(gate.Evaluate(StarterMonthly));
}

[Fact]
public void Evaluate_WhenModeActive_UsesGrossMap_NotNetMap()
{
    var vat = FullVat();
    vat.IsActive = true;
    var revolut = FullLiveRevolut(withStarterMonthly: false);
    revolut.PlanVariationsGross = new Dictionary<string, string>
    {
        [StarterMonthly] = "gross-uuid",
    };
    var gate = CreateGate(vat, revolut);
    Assert.Null(gate.Evaluate(StarterMonthly));
}

[Fact]
public void Evaluate_WhenModeActive_MissingGross_ReturnsPlanVariationMissing()
{
    var vat = FullVat();
    vat.IsActive = true;
    var revolut = FullLiveRevolut(withStarterMonthly: true); // net only
    var gate = CreateGate(vat, revolut);
    Assert.Equal(
        RevolutMerchantCreateGate.PlanVariationMissing,
        gate.Evaluate(StarterMonthly)
    );
}
```

Update existing tests that expect `vat_not_ready`: set `IsActive = true` on `FullVat()` helper (ACTIVE is explicit).

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement gate**

```csharp
public string? Evaluate(string? planVariationLookupKey = null)
{
    if (_vat.IsActive && !_vat.IsComplete)
    {
        return VatNotReady;
    }
    // ... revolut ready / sandbox checks unchanged ...
    if (string.IsNullOrWhiteSpace(planVariationLookupKey))
    {
        return null;
    }
    if (!_revolut.TryGetPlanVariationId(
            planVariationLookupKey.Trim(),
            useGrossMap: _vat.IsActive,
            out _))
    {
        return PlanVariationMissing;
    }
    return null;
}
```

Health: `vatModeActive = vat.IsActive`; count variations from the map matching mode.

- [ ] **Step 4: Run gate tests — PASS**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 3: RevolutMerchantClient resolves variation by mode

**Files:**
- Modify: `backend/TummlyBackend/Services/RevolutMerchantClient.cs` (ctor inject `IOptions<TummlySellerVatSettings>`; CreateSubscription / ChangeSubscriptionPlan / any `TryGetPlanVariationId` call)
- Modify tests that construct `RevolutMerchantClient` (`RevolutMerchantClientTests.cs`, `RevolutMerchantClientChangePlanTests.cs`)

**Interfaces:**
- Consumes: `vat.IsActive` + two-arg `TryGetPlanVariationId`
- Produces: subscription/change-plan uses gross IDs only when ACTIVE

- [ ] **Step 1: Failing test** — change-plan / create-subscription with ACTIVE + only gross map succeeds; with ACTIVE + only net map fails with `plan_variation_missing`.

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Implementation** — store `_vat`; replace variation lookups:

```csharp
_settings.TryGetPlanVariationId(
    lookupKey,
    useGrossMap: _vat.IsActive,
    out var variationId
)
```

- [ ] **Step 4: PASS**

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 4: One-time pay paths use EffectiveVatRateBps

**Files:**
- Modify: `CreditTopUpPaySessionService.cs` (~126–129)
- Modify: `ShopOrderPlaceService.cs` (~186)
- Modify: `ShopMaterialsOrderPaySessionService.cs` (~134, ~234)
- Modify: `ExtraGroupLocationService.cs` (~530)
- Modify: `RevolutOrderCompletedApplier.cs` (~716) if it recomputes VAT for non-invoice paths
- Inject `IOptions<TummlySellerVatSettings>` where missing
- Tests: extend existing pay/place tests or add focused unit tests asserting `VatPence == 0` and Revolut `AmountMinor == net` when mode off

**Interfaces:**
- Consumes: `EffectiveVatRateBps`
- Produces: OFF shop/top-up/extra-location amounts with zero VAT

- [ ] **Step 1: Failing test example (top-up)**

```csharp
// Arrange service with Mode=off, pack net 1000
// Act StartAsync
// Assert merchant create AmountMinor == 1000 and line tax amount 0 / absent
```

- [ ] **Step 2: Run — FAIL**

- [ ] **Step 3: Replace**

```csharp
var vatRateBps = _sellerVat.EffectiveVatRateBps;
var vat = TummlyVatMath.VatPenceFromNetPence(net, vatRateBps);
```

Same pattern for Shop place/pay and ExtraGroupLocation (`GrossMinorFromNetPence(net, _sellerVat.EffectiveVatRateBps)`). When building Revolut line `taxes`, omit or use 0% when rate is 0.

- [ ] **Step 4: PASS** for OFF and ACTIVE (ACTIVE still 20%)

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 5: Skip VAT invoice mint when OFF

**Files:**
- Modify: `backend/TummlyBackend/Interfaces/ITummlyVatInvoiceService.cs` — return `Task<TummlyVatInvoice?>` **or** add `bool ShouldMint` and keep callers checking mode (prefer: applier checks `_sellerVat.IsActive` and skips mint; mint methods throw `InvalidOperationException` if called while OFF — fail closed)
- Modify: `TummlyVatInvoiceService.cs`
- Modify: `RevolutOrderCompletedApplier.cs` (call site ~901)
- Modify: refund credit-note mint path similarly
- Tests: `TummlyVatInvoiceServiceTests.cs` — OFF does not insert row; ACTIVE still fail-closed without seller keys

**Recommended pattern (minimal interface churn):**

```csharp
// Applier / refund handler
if (_sellerVat.IsActive)
{
    var invoice = await _vatInvoices.MintForCompletedOrderAsync(...);
    // existing email/delivery
}
```

Inside mint, if `!_sellerVat.IsActive` throw so misuse is loud.

- [ ] **Step 1–4:** TDD as above; PASS when OFF leaves invoice table empty after ORDER_COMPLETED fixture.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 6: Billing credits API exposes effective VAT to UI

**Files:**
- Modify: `BillingCreditsDtos.cs` — `public bool VatModeActive { get; set; }` on `BillingCreditsPageDto`
- Modify: `BillingCreditsService.cs` after `BuildCurrentCatalog` — set `CurrentCatalog.VatRateBps = _sellerVat.EffectiveVatRateBps` and `VatModeActive = _sellerVat.IsActive`
- Inject seller VAT options into `BillingCreditsService`
- Test: integration or unit asserting page DTO `VatRateBps == 0` and `VatModeActive == false` by default

**Interfaces:**
- Consumes: `EffectiveVatRateBps` / `IsActive`
- Produces: frontend can hide VAT using catalog rate without a Vite flag

- [ ] **Steps 1–4:** TDD bind + assert

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 7: Frontend hide “+ VAT” when rate is 0

**Files:**
- Modify: `src/lib/operatorBillingCredits/creditTopUpPresentation.ts` — derive rate from catalog `vatRateBps / 10000` (default 0 if missing); no hard `VAT_RATE = 0.2` when rate is 0; labels omit `+ VAT` / “incl. VAT”
- Modify: `src/lib/operatorBillingCredits/managePlanPresentation.ts` — price suffix `/ month` vs `/ month + VAT` from rate
- Modify: `src/lib/operatorBillingCredits/billingCreditsPresentation.ts` — `plusVat` only when rate &gt; 0
- Wire page module to pass `vatRateBps` / `vatModeActive` from API snapshot into presentation
- Tests: update `managePlanPresentation.test.ts`, `creditTopUpPresentation.test.ts`, page module tests for OFF (no `+ VAT`) and ACTIVE (unchanged copy)

**Interfaces:**
- Consumes: `BillingCreditsPageDto.vatModeActive` + `currentCatalog.vatRateBps` (camelCase from API)
- Produces: net-only chrome when OFF

- [ ] **Step 1: Failing Vitest** — with `vatRateBps: 0`, headline is `£39 / month` not `+ VAT`

- [ ] **Step 2: Run** `npx vitest run src/lib/operatorBillingCredits/managePlanPresentation.test.ts` — FAIL

- [ ] **Step 3: Implement** presentation helpers taking `vatRateBps: number`

- [ ] **Step 4: PASS** both OFF and ACTIVE fixtures

- [ ] **Step 5: Commit** (only if human asked)

Shop UI: if Shop place sends `expectedGrossPence` from a client-side VAT calc, apply the same rate source (billing catalog or a small shop quote field). Grep `expectedGrossPence` / `0.2` under `src/lib/operatorShop` and align in this task.

---

### Task 8: Env + QA docs

**Files:**
- Modify: `backend/TummlyBackend/.env.example`
- Modify: `backend/TummlyBackend/.env.revolut.sandbox.local.example` (if present)
- Modify: `infra/qa/REVOLUT-QA-SANDBOX.md`, `infra/qa/REVOLUT-GO-LIVE.md`

Document:

```bash
# Launch default: false (net charges, no vat_not_ready, no VAT invoices/UI).
# Unset / empty → false.
TUMMLY_VAT_MODE_ACTIVE=false
# true → require TUMMLY_VAT_* keys + Revolut__PlanVariationsGross__* (gross GBP)

# Net map (used when TUMMLY_VAT_MODE_ACTIVE=false) — existing Revolut__PlanVariations__*
# Gross map (used when true):
Revolut__PlanVariationsGross__tummly_starter_monthly_gbp_v3=
# ... same eight keys
```

Note: billing pack v3.0 VAT-on is **overridden** for launch by this flag; set `true` only after HMRC registration + gross variations exist.

- [ ] **Step 1:** Edit docs (no test)
- [ ] **Step 2:** Human skim
- [ ] **Step 3: Commit** (only if human asked)

---

## Spec coverage self-check

| Spec requirement | Task |
|---|---|
| Default OFF | 1 |
| Dual maps net/gross | 1–3 |
| Skip `vat_not_ready` when OFF | 2 |
| One-time net / Revolut amount | 4 |
| No VAT invoice when OFF | 5 |
| No customer VAT UI | 6–7 |
| ACTIVE restores current path | 2–5, 7 |
| Docs / .env | 8 |
| Fail-closed ACTIVE without keys/map | 2 |

## Placeholder scan

None intentional. Shop client gross quote called out in Task 7 via grep.

## Type consistency

- `IsActive` / `EffectiveVatRateBps` on `TummlySellerVatSettings`
- `TryGetPlanVariationId(key, useGrossMap, out id)` on `RevolutSettings`
- Wire flag: bool; API `vatModeActive` boolean
