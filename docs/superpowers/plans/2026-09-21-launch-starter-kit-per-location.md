# Launch Starter Kit Per Active Location Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Create one complimentary £0 Starter Kit Shop order only on first Active (signup or ActivateDraft), never on Draft insert or Resume; drop BA-lifetime kit state from writes and operator copy.

**Architecture:** Keep per-Location idempotent `EnsureForLocationAsync`. Add an Active lifecycle gate inside Ensure; remove the Draft insert call site; inject Ensure + print-ready into `ActivateDraftAsync`. Stop writing `BillingAccount.StarterKitState`. Replace Billing/FAQ BA-lifetime kit wording with static per-Location launch copy.

**Tech Stack:** ASP.NET Core + EF InMemory xUnit tests; Vitest presentation helpers; existing Shop complimentary order path.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-starter-kit-per-location-design.md](../specs/2026-09-21-launch-starter-kit-per-location-design.md)

## Global Constraints

- Create kit only when Location is **Active** on first enter (signup Active or Draft → Active).
- Do **not** create on Draft insert or Resume.
- Entitlement source of truth = complimentary `ShopOrder` per `LocationId` (unique index stays).
- Stop writing `BillingAccount.StarterKitState` on kit create; leave DB column; no drop migration.
- Existing Draft kits already in DB: leave as-is (no cancel/backfill).
- Report to the human in ASD-STE100 Simplified Technical English.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only (no Opus / GPT model overrides).

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Services/ComplimentaryStarterShopOrderService.cs` | Active gate; stop BA `StarterKitState` write |
| `backend/TummlyBackend/Services/OwnedLocationInsertService.cs` | Remove Ensure + print-ready on Draft create |
| `backend/TummlyBackend/Services/LocationsLifecycleWriteService.cs` | Inject Ensure + print-ready on ActivateDraft |
| `backend/TummlyBackend/Services/GuestLoopProvisioningService.cs` | Keep Ensure (locations already Active) — no logic change expected |
| `src/lib/operatorBillingCredits/billingCreditsPresentation.ts` | Static QR packs / starter launch label |
| `src/lib/operatorBillingCredits/managePlanPresentation.ts` | FAQ per-Location copy |
| `src/components/dashboard/operator/BillingCredits/BillingCreditsPage.tsx` | Use static label instead of BA state |
| Tests under `backend/TummlyBackend.Tests/` + Vitest siblings | Gate, call sites, FAQ |

---

### Task 1: Ensure Active gate + stop BA write

**Files:**
- Modify: `backend/TummlyBackend/Services/ComplimentaryStarterShopOrderService.cs`
- Modify: `backend/TummlyBackend.Tests/Services/ComplimentaryStarterShopOrderServiceTests.cs`
- Modify: `backend/TummlyBackend.Tests/Services/GuestLoopProvisioningServiceTests.cs` (drop `PendingDispatch` assertion)

**Interfaces:**
- Consumes: `IComplimentaryStarterShopOrderService.EnsureForLocationAsync` (unchanged signature)
- Produces: same `ComplimentaryStarterShopOrderResult`; when Location exists, is not Active, and no complimentary order yet → `(ShopOrderId: Guid.Empty, Created: false)`

- [ ] **Step 1: Write the failing tests**

In `ComplimentaryStarterShopOrderServiceTests.cs`:

1. Change `EnsureForLocationAsync_CreatesPaidZeroOrderWithThreeLines` — assert BA stays **`unused`** (not `PendingDispatch`).
2. Add Draft skip + Draft-then-Active create tests:

```csharp
[Fact]
public async Task EnsureForLocationAsync_DoesNotCreate_WhenLocationIsDraft()
{
    var location = await _context.RestaurantLocations.SingleAsync();
    location.LifecycleStatus = LocationLifecycleStatus.Draft;
    await _context.SaveChangesAsync();

    var result = await _service.EnsureForLocationAsync(
        _restaurantId,
        _locationId,
        _userId,
        "Alex Owner"
    );

    Assert.False(result.Created);
    Assert.Equal(Guid.Empty, result.ShopOrderId);
    Assert.Equal(0, await _context.ShopOrders.CountAsync());

    var billing = await _context.BillingAccounts.SingleAsync();
    Assert.Equal(StarterKitStates.Unused, billing.StarterKitState);
}

[Fact]
public async Task EnsureForLocationAsync_Creates_WhenLocationIsActive()
{
    // Seed already Active — same as CreatesPaidZeroOrderWithThreeLines core asserts
    var result = await _service.EnsureForLocationAsync(
        _restaurantId,
        _locationId,
        _userId,
        "Alex Owner"
    );
    Assert.True(result.Created);
    Assert.NotEqual(Guid.Empty, result.ShopOrderId);
}

[Fact]
public async Task EnsureForLocationAsync_DoesNotWriteBillingAccountStarterKitState()
{
    var result = await _service.EnsureForLocationAsync(
        _restaurantId,
        _locationId,
        _userId,
        "Alex Owner"
    );
    Assert.True(result.Created);
    var billing = await _context.BillingAccounts.SingleAsync();
    Assert.Equal(StarterKitStates.Unused, billing.StarterKitState);
}
```

Also update the existing create test’s billing assert from `PendingDispatch` to `Unused`.

In `GuestLoopProvisioningServiceTests.cs` (~line 267): remove or change:

```csharp
Assert.Equal(StarterKitStates.PendingDispatch, billingAccount.StarterKitState);
```

to:

```csharp
Assert.Equal(StarterKitStates.Unused, billingAccount.StarterKitState);
```

Keep the complimentary order asserts.

- [ ] **Step 2: Run tests to verify they fail**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ComplimentaryStarterShopOrderServiceTests|FullyQualifiedName~GuestLoopProvisioningServiceTests" --no-restore 2>&1 | tail -40
```

Expected: FAIL on Draft skip (missing) and/or `PendingDispatch` vs `Unused`.

- [ ] **Step 3: Minimal implementation**

In `ComplimentaryStarterShopOrderService.EnsureForLocationAsync`, after loading `location` (and null check), **before** allocating order number:

```csharp
if (location.LifecycleStatus != LocationLifecycleStatus.Active)
{
    return new ComplimentaryStarterShopOrderResult(
        Guid.Empty,
        Created: false
    );
}
```

Delete the entire block that updates `billingAccount.StarterKitState` (the `BillingAccounts` lookup + `Unused` → `PendingDispatch` write). Keep `SaveChangesAsync` for the new order only.

Idempotent early return (existing complimentary order) stays **before** the Active check (spec: return existing regardless of lifecycle).

- [ ] **Step 4: Run tests to verify they pass**

Same filter as Step 2. Expected: PASS.

- [ ] **Step 5: Commit** (only if human asked)

```bash
git add backend/TummlyBackend/Services/ComplimentaryStarterShopOrderService.cs \
  backend/TummlyBackend.Tests/Services/ComplimentaryStarterShopOrderServiceTests.cs \
  backend/TummlyBackend.Tests/Services/GuestLoopProvisioningServiceTests.cs
git commit -m "$(cat <<'EOF'
fix: create complimentary starter kit only for Active locations

Stop BA lifetime StarterKitState writes so entitlement stays per Location.
EOF
)"
```

---

### Task 2: Remove kit create from Draft location insert

**Files:**
- Modify: `backend/TummlyBackend/Services/OwnedLocationInsertService.cs`
- Test: extend `backend/TummlyBackend.Tests/Services/LocationEntitlementServiceTests.cs` **or** add focused test class that wires real `ComplimentaryStarterShopOrderService` (preferred: new facts in a small test file `OwnedLocationInsertComplimentaryStarterTests.cs` if entitlement tests stay NoOp-only)

**Interfaces:**
- Consumes: `IComplimentaryStarterShopOrderService` may remain injected for DI stability **or** remove the field/ctor param if unused after this task (prefer **remove** unused dependency to avoid dead inject).
- Produces: Draft `AddAsync` creates Location + QR mint only; **zero** complimentary Shop orders.

- [ ] **Step 1: Write the failing test**

Wire `OwnedLocationInsertService` with real `ComplimentaryStarterShopOrderService` + materials catalog (same pack resolve pattern as complimentary tests) + stub print-ready that records shop-order requests.

```csharp
[Fact]
public async Task AddAsync_DraftLocation_DoesNotCreateComplimentaryOrder()
{
    // Seed restaurant + billing with entitled plan (copy LocationEntitlementServiceTests seed)
    var result = await _insert.AddAsync(
        restaurantId,
        actorUserId,
        new AddOwnedLocationRequest
        {
            LocationName = "Branch",
            Address = "2 High Street",
            City = "Leeds",
            Postcode = "LS1 2AB",
        }
    );

    Assert.IsType<AddOwnedLocationResult.Created>(result);
    Assert.Equal(0, await _context.ShopOrders.CountAsync(o => o.IsComplimentary));
    Assert.Empty(_printWork.RequestedShopOrderIds); // if stub records them
}
```

If current `LocationEntitlementServiceTests` uses NoOp that returns a fake Guid and still calls print-ready, rewrite that fixture for this one test to use the real complimentary service (or new file).

- [ ] **Step 2: Run test to verify it fails**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~AddAsync_DraftLocation_DoesNotCreateComplimentaryOrder" -v n
```

Expected: FAIL because Draft insert still creates a complimentary order today.

- [ ] **Step 3: Minimal implementation**

In `OwnedLocationInsertService.AddLockedAsync`, delete:

```csharp
var complimentary =
    await _complimentaryStarterShopOrders.EnsureForLocationAsync(
        restaurantId,
        location.Id,
        actorUserId,
        actorDisplayName ?? string.Empty
    );
```

and the later:

```csharp
await _printReadyQrMaterialsWork.RequestShopOrderEnsureAsync(
    complimentary.ShopOrderId
);
```

If `_complimentaryStarterShopOrders` and/or `_printReadyQrMaterialsWork` become unused, remove fields, ctor params, and update `Program.cs` / test constructors accordingly. Keep `IQrCodeProvisioningService` mint as today.

- [ ] **Step 4: Run test to verify it passes**

Same filter + full `LocationEntitlementServiceTests` if ctor changed.

Expected: PASS.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 3: Create kit on ActivateDraft

**Files:**
- Modify: `backend/TummlyBackend/Services/LocationsLifecycleWriteService.cs`
- Modify: `backend/TummlyBackend/Program.cs` only if DI already resolves new deps via ctor (scoped registration already exists for complimentary + print-ready)
- Test: `backend/TummlyBackend.Tests/Services/LocationsLifecycleWriteActivateStarterTests.cs` (new) **and/or** extend `LocationsLifecycleWriteEndpointsTests` if easier with factory

**Interfaces:**
- Consumes: `IComplimentaryStarterShopOrderService`, `IPrintReadyQrMaterialsWork`
- Produces: after successful Draft → Active save, `EnsureForLocationAsync`; if `ShopOrderId != Guid.Empty`, call `RequestShopOrderEnsureAsync(ShopOrderId)` (covers Created and idempotent existing)

- [ ] **Step 1: Write the failing test**

Unit-style InMemory test:

```csharp
[Fact]
public async Task ActivateDraftAsync_CreatesComplimentaryStarterOrder()
{
    // Seed Draft location with complete activate fields (name, address, city, postcode)
    var write = new LocationsLifecycleWriteService(
        _context,
        complimentaryService,
        printWork
    );

    var result = await write.ActivateDraftAsync(restaurantId, locationId, actorUserId);

    Assert.IsType<LocationLifecycleWriteResult.Ok>(result);
    var location = await _context.RestaurantLocations.SingleAsync();
    Assert.Equal(LocationLifecycleStatus.Active, location.LifecycleStatus);

    var order = await _context.ShopOrders.SingleAsync(o => o.IsComplimentary);
    Assert.Equal(locationId, order.LocationId);
    Assert.Equal(0, order.GrossPence);
    Assert.Contains(order.Id, printWork.RequestedShopOrderIds);
}
```

Also add:

```csharp
[Fact]
public async Task ActivateDraftAsync_IsIdempotent_ForComplimentaryOrder()
{
    // Activate once, then call Ensure path again by activating is invalid —
    // instead: activate once, call Ensure again manually, assert single order.
}
```

Simpler second fact: after Activate, call `EnsureForLocationAsync` again → `Created: false`, count = 1.

- [ ] **Step 2: Run test to verify it fails**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~LocationsLifecycleWriteActivateStarterTests" -v n
```

Expected: FAIL — ActivateDraft today does not create a Shop order.

- [ ] **Step 3: Minimal implementation**

```csharp
public sealed class LocationsLifecycleWriteService : ILocationsLifecycleWriteService
{
    private readonly ApplicationDbContext _context;
    private readonly IComplimentaryStarterShopOrderService _complimentaryStarterShopOrders;
    private readonly IPrintReadyQrMaterialsWork _printReadyQrMaterialsWork;

    public LocationsLifecycleWriteService(
        ApplicationDbContext context,
        IComplimentaryStarterShopOrderService complimentaryStarterShopOrders,
        IPrintReadyQrMaterialsWork printReadyQrMaterialsWork
    )
    {
        _context = context;
        _complimentaryStarterShopOrders = complimentaryStarterShopOrders;
        _printReadyQrMaterialsWork = printReadyQrMaterialsWork;
    }

    public async Task<LocationLifecycleWriteResult> ActivateDraftAsync(...)
    {
        // ... existing validation + set Active + EmitLifecycleAsync ...
        await _context.SaveChangesAsync();

        var actorDisplayName = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == actorUserId)
            .Select(u => u.FullName)
            .FirstOrDefaultAsync();

        var complimentary =
            await _complimentaryStarterShopOrders.EnsureForLocationAsync(
                restaurantId,
                locationId,
                actorUserId,
                actorDisplayName ?? string.Empty
            );

        if (complimentary.ShopOrderId != Guid.Empty)
        {
            await _printReadyQrMaterialsWork.RequestShopOrderEnsureAsync(
                complimentary.ShopOrderId
            );
        }

        return new LocationLifecycleWriteResult.Ok();
    }
}
```

Update any direct `new LocationsLifecycleWriteService(_context)` test constructors to pass stubs/real services.

Do **not** add Ensure to `ResumeAsync` in `LocationLifecycleService`.

- [ ] **Step 4: Run tests to verify they pass**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~LocationsLifecycleWriteActivateStarterTests|FullyQualifiedName~LocationsLifecycleWriteEndpointsTests" -v n
```

Expected: PASS.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 4: Operator FAQ + plan overview static copy

**Files:**
- Modify: `src/lib/operatorBillingCredits/managePlanPresentation.ts` (FAQ items `qr-materials`, `starter-kit-per-location`)
- Modify: `src/lib/operatorBillingCredits/billingCreditsPresentation.ts` (`formatQrPacksLabel` / new constant)
- Modify: `src/components/dashboard/operator/BillingCredits/BillingCreditsPage.tsx` (plan overview + usage cell)
- Test: `src/lib/operatorBillingCredits/managePlanPresentation.test.ts`
- Test: add or extend presentation test for QR packs label

**Interfaces:**
- Consumes: none from Tasks 1–3
- Produces: FAQ + UI copy matching per Active Location rule; UI does not use BA `starterKitState` as entitlement display

- [ ] **Step 1: Write the failing tests**

```typescript
it("FAQ starter kit items describe one kit per Active Location", () => {
  const items = buildManagePlanFaqItems({ vatRateBps: 0 })
  const perLocation = items.find((i) => i.id === "starter-kit-per-location")
  expect(perLocation?.answerParagraphs.join(" ")).toMatch(/Active Location/i)
  expect(perLocation?.answerParagraphs.join(" ")).not.toMatch(
    /Billing Account lifetime/i
  )

  const qr = items.find((i) => i.id === "qr-materials")
  expect(qr?.answerParagraphs.join(" ")).not.toMatch(
    /Billing Account lifetime/i
  )
})
```

```typescript
it("formatQrPacksLabel ignores BA state and returns launch copy", () => {
  expect(formatQrPacksLabel("unused")).toBe(
    "One complimentary kit per Active Location"
  )
  expect(formatQrPacksLabel("used")).toBe(
    "One complimentary kit per Active Location"
  )
})
```

Exact string must match what you put in presentation (single source constant preferred):

```typescript
export const LAUNCH_STARTER_KIT_QR_PACKS_LABEL =
  "One complimentary kit per Active Location"
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
npx vitest run src/lib/operatorBillingCredits/managePlanPresentation.test.ts src/lib/operatorBillingCredits/billingCreditsPresentation.ts
```

Prefer a dedicated `billingCreditsPresentation.test.ts` if none exists; otherwise put the label test next to managePlan tests by importing the constant.

Expected: FAIL on BA-lifetime strings still present / old formatQrPacksLabel switch.

- [ ] **Step 3: Minimal implementation**

FAQ answers (exact intent; keep short STE-friendly English):

`qr-materials`:

```typescript
"Tummly provides digital and self-print QR assets, and each newly activated Location can receive one complimentary physical starter kit.",
```

`starter-kit-per-location`:

```typescript
"Yes for each newly activated Location.",
"There is one complimentary physical starter kit per Active Location. It is not repeated on renewal or paid conversion for that Location.",
```

(Adjust question text only if needed so “Yes…” fits; if the question stays “Is the starter kit included with every Location?”, answer can start with “Each newly activated Location qualifies once.”)

```typescript
export const LAUNCH_STARTER_KIT_QR_PACKS_LABEL =
  "One complimentary kit per Active Location"

export function formatQrPacksLabel(_state: string): string {
  return LAUNCH_STARTER_KIT_QR_PACKS_LABEL
}
```

Optionally stop passing `starterKitState` into `QrPrintPacksUsageCell` / plan metric — leave prop unused or remove prop in the same task if TypeScript allows cleanly. Prefer remove unused prop from the cell if easy.

Keep API `StarterKitState` field for wire compatibility (spec allows).

- [ ] **Step 4: Run tests to verify they pass**

```bash
npx vitest run src/lib/operatorBillingCredits/managePlanPresentation.test.ts
# plus any new presentation test file
```

Expected: PASS.

- [ ] **Step 5: Commit** (only if human asked)

---

### Task 5: Whole-branch smoke

**Files:** none new — verify only

- [ ] **Step 1: Run focused backend suite**

```bash
cd backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ComplimentaryStarterShopOrderServiceTests|FullyQualifiedName~GuestLoopProvisioningServiceTests|FullyQualifiedName~LocationEntitlementServiceTests|FullyQualifiedName~LocationsLifecycleWriteActivateStarterTests|FullyQualifiedName~LocationsLifecycleWriteEndpointsTests|FullyQualifiedName~OwnedLocationInsert" -v n
```

Expected: PASS.

- [ ] **Step 2: Run focused frontend suite**

```bash
npx vitest run src/lib/operatorBillingCredits/managePlanPresentation.test.ts
```

Expected: PASS.

- [ ] **Step 3: Spec checklist**

Confirm against the design doc:

| Spec item | Task |
|-----------|------|
| Active gate in Ensure | 1 |
| No BA StarterKitState write | 1 |
| No Draft insert create | 2 |
| ActivateDraft create + print-ready | 3 |
| No Resume create | 3 (no change) + verify |
| FAQ / plan overview per Location | 4 |
| No DB column drop / no Draft kit cancel | out of scope |

- [ ] **Step 4: Commit** (only if human asked) — prefer one commit for the feature after Task 5 if human requests.

---

## Self-review (plan vs spec)

1. **Spec coverage:** Active gate, call-site move, ActivateDraft, BA write stop, FAQ/UI, idempotency, leave existing Draft kits — covered in Tasks 1–4. Resume no-create covered by leaving `LocationLifecycleService` alone. Column retention / no backfill — Global Constraints + out of scope.
2. **Placeholders:** Ensure skip result fixed as `Guid.Empty` + `Created: false`. FAQ strings specified.
3. **Types:** `ComplimentaryStarterShopOrderResult` unchanged; `LocationsLifecycleWriteService` ctor gains two deps — Task 3 notes test constructor updates.
