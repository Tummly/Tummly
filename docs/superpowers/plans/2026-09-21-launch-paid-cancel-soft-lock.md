# Launch Paid Cancel Soft-lock Progression Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** When paid **Cancel plan** applies at renewal end, demote the Billing Account to unpaid Pilot Soft lock so existing Soft lock → Dormant clocks, gates, and notices run; call Revolut native cancel when a subscription is correlated.

**Architecture:** Add `ApplyPostCancelSoftLock` on `IBillingAccountLifecycle` that mutates the already-locked `BillingAccount` in the mint job transaction (same pattern as `PlanChangeService.ApplyScheduledChangeOnRenewalAsync` — no nested UPDLOCK). `IncludedPeriodMintService` cancel-apply path: clear slot → `CancelNativeSubscriptionAsync` → `ApplyPostCancelSoftLock` → `cancel_applied`. Tick / Soft lock gates unchanged.

**Tech Stack:** ASP.NET Core, existing `BillingAccountLifecycleService`, `IncludedPeriodMintService`, xUnit InMemory tests.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-paid-cancel-soft-lock-design.md](../specs/2026-09-21-launch-paid-cancel-soft-lock-design.md)

## Global Constraints

- Demote to **Pilot Soft lock** at `cancel_applied`; reuse Pilot clocks (`PilotDormantHours` = 15×24).
- No 90-day purge / anonymise in this plan (Retention P0).
- Leave `PaidExtraLocationCount` and Owned Locations unchanged.
- Credits: Soft-lock gates only; Dormant reuses existing Pilot leftover / hold release.
- Notices via **Tick only** (reset Pilot notice flags; do not send mail from apply).
- Lifecycle is the only Billing status writer (ADR 0043).
- `ApplyPostCancelSoftLock` must **not** open its own transaction — mint already holds the account.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Interfaces/IBillingAccountLifecycle.cs` | New `ApplyPostCancelSoftLock` |
| `backend/TummlyBackend/Services/BillingAccountLifecycleService.cs` | Implement demotion fields |
| `backend/TummlyBackend/Services/IncludedPeriodMintService.cs` | Cancel path: Revolut cancel + lifecycle |
| `backend/TummlyBackend.Tests/Helpers/NoOpBillingAccountLifecycle.cs` | Interface stub |
| Test stubs implementing `IBillingAccountLifecycle` | Add method (compile) |
| `backend/TummlyBackend.Tests/Services/BillingAccountLifecycleServiceTests.cs` | Demotion + idempotent + Tick after cancel |
| `backend/TummlyBackend.Tests/Services/IncludedPeriodMintServiceTests.cs` | Soft lock after cancel apply |
| `backend/TummlyBackend.Tests/Services/RevolutCancelAtPeriodEndAdapterTests.cs` | Expect native cancel on apply |

---

### Task 1: Lifecycle `ApplyPostCancelSoftLock`

**Files:**
- Modify: `backend/TummlyBackend/Interfaces/IBillingAccountLifecycle.cs`
- Modify: `backend/TummlyBackend/Services/BillingAccountLifecycleService.cs`
- Modify: `backend/TummlyBackend.Tests/Helpers/NoOpBillingAccountLifecycle.cs`
- Modify: every test stub that implements `IBillingAccountLifecycle` (grep `IBillingAccountLifecycle` under `backend/TummlyBackend.Tests/`)
- Test: `backend/TummlyBackend.Tests/Services/BillingAccountLifecycleServiceTests.cs`

**Interfaces:**
- Consumes: existing `BillingLifecycleCommandResult`, `BillingAccount`, `BillingStatuses`, `BillingSubscriptionPlans`
- Produces:

```csharp
/// <summary>
/// Mutates the locked BillingAccount in the caller transaction after paid
/// Cancel plan applies at renewal end. Does not open a nested transaction.
/// </summary>
BillingLifecycleCommandResult ApplyPostCancelSoftLock(
    BillingAccount billingAccount,
    DateTime renewalEndUtc
);
```

- [ ] **Step 1: Write the failing tests**

Add to `BillingAccountLifecycleServiceTests.cs`:

```csharp
[Fact]
public void ApplyPostCancelSoftLock_DemotesPaidToPilotSoftLock()
{
    var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    var account = new BillingAccount
    {
        RestaurantId = 1,
        SubscriptionPlan = BillingSubscriptionPlans.Growth,
        BillingCycle = BillingCycles.Monthly,
        BillingStatus = BillingStatuses.Active,
        ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
        StarterKitState = StarterKitStates.Unused,
        PaidExtraLocationCount = 2,
        DunningEpisodeStartedAt = renewalEnd.AddDays(-5),
        DunningFiredSteps = "0,3",
        DunningOutstandingOrderId = "ord_x",
        PilotSoftLockNotified = true,
        PilotDormantNotified = true,
    };

    var result = _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);

    Assert.True(result.Applied);
    Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
    Assert.Null(account.BillingCycle);
    Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
    Assert.Equal(renewalEnd, account.PilotPeriodEnd);
    Assert.Equal(renewalEnd, account.SoftLockEnteredAt);
    Assert.Null(account.DormantEnteredAt);
    Assert.Null(account.DunningEpisodeStartedAt);
    Assert.Null(account.DunningFiredSteps);
    Assert.Null(account.DunningOutstandingOrderId);
    Assert.Equal(2, account.PaidExtraLocationCount);
    Assert.False(account.PilotSoftLockNotified);
    Assert.False(account.PilotDormantNotified);
}

[Fact]
public void ApplyPostCancelSoftLock_IdempotentWhenAlreadyPilotSoftLock()
{
    var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    var account = new BillingAccount
    {
        RestaurantId = 1,
        SubscriptionPlan = BillingSubscriptionPlans.Pilot,
        BillingStatus = BillingStatuses.SoftLock,
        ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
        StarterKitState = StarterKitStates.Unused,
        PilotPeriodEnd = renewalEnd,
        SoftLockEnteredAt = renewalEnd,
    };

    var result = _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd.AddDays(1));

    Assert.False(result.Applied);
    Assert.False(result.Refused);
    Assert.Equal(renewalEnd, account.SoftLockEnteredAt);
    Assert.Equal(renewalEnd, account.PilotPeriodEnd);
}

[Fact]
public async Task Tick_AfterPostCancelSoftLock_ReachesDormantAtPilotClock()
{
    var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    var seeded = await SeedPaidAsync();
    var account = await ReloadAsync(seeded.RestaurantId);
    account.SubscriptionPlan = BillingSubscriptionPlans.Growth;
    account.BillingCycle = BillingCycles.Monthly;
    account.BillingStatus = BillingStatuses.Active;
    await _context.SaveChangesAsync();

    _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);
    await _context.SaveChangesAsync();

    var dormantAt = renewalEnd.AddHours(
        BillingAccountLifecycleService.PilotDormantHours
    );
    await _lifecycle.TickAsync(seeded.RestaurantId, dormantAt);

    var after = await ReloadAsync(seeded.RestaurantId);
    Assert.Equal(BillingStatuses.Dormant, after.BillingStatus);
    Assert.Equal(renewalEnd, after.SoftLockEnteredAt);
    Assert.Equal(dormantAt, after.DormantEnteredAt);
    Assert.Single(_notifier.PilotLockEnters);
    Assert.Single(_notifier.PilotDormantEnters);
}
```

Note: `ApplyPostCancelSoftLock` is sync on the interface. If the test constructor only has `_lifecycle` as `BillingAccountLifecycleService`, call it directly. Prefer declaring the method on the interface so stubs compile.

- [ ] **Step 2: Run tests to verify they fail**

Run:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~BillingAccountLifecycleServiceTests.ApplyPostCancelSoftLock|FullyQualifiedName~BillingAccountLifecycleServiceTests.Tick_AfterPostCancelSoftLock" --no-restore
```

Expected: FAIL (method missing / compile error).

- [ ] **Step 3: Implement**

On `IBillingAccountLifecycle`:

```csharp
BillingLifecycleCommandResult ApplyPostCancelSoftLock(
    BillingAccount billingAccount,
    DateTime renewalEndUtc
);
```

On `BillingAccountLifecycleService`:

```csharp
public BillingLifecycleCommandResult ApplyPostCancelSoftLock(
    BillingAccount billingAccount,
    DateTime renewalEndUtc
)
{
    if (
        string.Equals(
            billingAccount.SubscriptionPlan,
            BillingSubscriptionPlans.Pilot,
            StringComparison.Ordinal
        )
        && (
            billingAccount.BillingStatus == BillingStatuses.SoftLock
            || billingAccount.BillingStatus == BillingStatuses.Dormant
        )
        && billingAccount.SoftLockEnteredAt != null
    )
    {
        return BillingLifecycleCommandResult.NoOp();
    }

    billingAccount.SubscriptionPlan = BillingSubscriptionPlans.Pilot;
    billingAccount.BillingCycle = null;
    billingAccount.BillingStatus = BillingStatuses.SoftLock;
    billingAccount.PilotPeriodEnd = renewalEndUtc;
    billingAccount.SoftLockEnteredAt = renewalEndUtc;
    billingAccount.DormantEnteredAt = null;
    billingAccount.DunningEpisodeStartedAt = null;
    billingAccount.DunningFiredSteps = null;
    billingAccount.DunningOutstandingOrderId = null;
    billingAccount.PilotSoftLockNotified = false;
    billingAccount.PilotDormantNotified = false;
    return BillingLifecycleCommandResult.Ok();
}
```

Update `NoOpBillingAccountLifecycle` and every test stub:

```csharp
public BillingLifecycleCommandResult ApplyPostCancelSoftLock(
    BillingAccount billingAccount,
    DateTime renewalEndUtc
) => BillingLifecycleCommandResult.NoOp();
```

(Recording stubs may ignore args; chargeback stub same.)

- [ ] **Step 4: Run tests to verify they pass**

Same filter as Step 2. Expected: PASS.

- [ ] **Step 5: Commit** (skip unless human asks)

```bash
git add backend/TummlyBackend/Interfaces/IBillingAccountLifecycle.cs \
  backend/TummlyBackend/Services/BillingAccountLifecycleService.cs \
  backend/TummlyBackend.Tests/Helpers/NoOpBillingAccountLifecycle.cs \
  backend/TummlyBackend.Tests/Services/BillingAccountLifecycleServiceTests.cs \
  backend/TummlyBackend.Tests/Services/RevolutWebhookServiceTests.cs \
  backend/TummlyBackend.Tests/Services/BillingCreditsServiceTests.cs \
  backend/TummlyBackend.Tests/Services/BillingActivityServiceTests.cs \
  backend/TummlyBackend.Tests/Services/RevolutPaymentRefundHandlerTests.cs
git commit -m "$(cat <<'EOF'
feat(billing): demote paid cancel apply to Pilot Soft lock

EOF
)"
```

---

### Task 2: Mint cancel path — Soft lock + Revolut native cancel

**Files:**
- Modify: `backend/TummlyBackend/Services/IncludedPeriodMintService.cs`
- Modify: `backend/TummlyBackend.Tests/Services/IncludedPeriodMintServiceTests.cs`
- Modify: `backend/TummlyBackend.Tests/Services/RevolutCancelAtPeriodEndAdapterTests.cs`
- Modify: any test harness that constructs `IncludedPeriodMintService` if the constructor gains `IBillingAccountLifecycle` (optional param with null → no-op apply is OK for most mint tests)

**Interfaces:**
- Consumes: `IBillingAccountLifecycle.ApplyPostCancelSoftLock`, `IRevolutCancelAtPeriodEndAdapter.CancelNativeSubscriptionAsync` (already injected as `_revolutCancel`)
- Produces: cancel apply leaves Pilot Soft lock + may call Revolut cancel once

- [ ] **Step 1: Write / update failing tests**

Update `IncludedPeriodMintServiceTests.ProcessJobForRestaurant_CancelAtRenewalDate_AppliesAndSkipsMint` assertions after reload:

```csharp
Assert.Equal(BillingSubscriptionPlans.Pilot, account.SubscriptionPlan);
Assert.Null(account.BillingCycle);
Assert.Equal(BillingStatuses.SoftLock, account.BillingStatus);
Assert.Equal(renewal, account.SoftLockEnteredAt);
Assert.Equal(renewal, account.PilotPeriodEnd);
Assert.Equal("cancel_applied", result.SkipReason); // if SkipReason exists; else assert Succeeded + empty allocations as today
```

Check `IncludedPeriodMintResult` for how skip reason is exposed — use the existing field name (`SkippedReason` / `SkipReason` / similar). If only `Succeeded` + empty ids today, add Soft lock field asserts only.

Update `RevolutCancelAtPeriodEndAdapterTests.ProcessJob_AtRenewal_ClearsCancelSlot_WithoutImmediateRevolutCancel`:

- Rename to `ProcessJob_AtRenewal_ClearsCancelSlot_AndCallsNativeCancel`
- Change harness so mint receives both `RevolutCancelAtPeriodEndAdapter` **and** a real or recording lifecycle that applies Soft lock (or the real `BillingAccountLifecycleService`)
- Assert `merchant.CancelCallCount == 1` (or `>= 1`) when subscription correlated
- Assert Soft lock fields after apply

Keep `ProcessJob_BeforeRenewal_DoesNotCallRevolutCancel` at 0 cancel calls.

Add a focused test if harness allows injecting recording lifecycle:

```csharp
[Fact]
public async Task ProcessJob_AtRenewal_AppliesPostCancelSoftLock()
{
    // Seed paid + ScheduledCancelPlan + RenewalDateUtc == now
    // Mint with real BillingAccountLifecycleService (or stub that records ApplyPostCancelSoftLock)
    // Assert Soft lock demotion + slot cleared + no new included mint
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ProcessJobForRestaurant_CancelAtRenewalDate|FullyQualifiedName~ProcessJob_AtRenewal|FullyQualifiedName~ProcessJob_BeforeRenewal" --no-restore
```

Expected: FAIL on Soft lock asserts and/or CancelCallCount.

- [ ] **Step 3: Implement mint wiring**

Constructor: add optional `IBillingAccountLifecycle? lifecycle = null` (or required if DI always provides it). Prefer required DI param + update Program registration only if needed (scoped lifecycle already registered).

In cancel branch of `ProcessJobForRestaurantAsync` (where `ScheduledCancelPlan && now >= RenewalDateUtc`):

```csharp
var renewalEnd = billingAccount.RenewalDateUtc!.Value;
billingAccount.ClearScheduledChangeSlot();

await _revolutCancel.CancelNativeSubscriptionAsync(
    billingAccount.RestaurantId,
    cancellationToken
);

_lifecycle.ApplyPostCancelSoftLock(billingAccount, renewalEnd);

return await FinishCancelApplyAsync(
    expiryRowsWritten,
    session,
    cancellationToken
);
```

Use the existing `_revolutCancel` field. If `_lifecycle` is optional for unit tests, use a null-object that NoOps `ApplyPostCancelSoftLock` only when null — production must inject real lifecycle.

Update `IncludedPeriodMintService` harness constructors in tests to pass `new BillingAccountLifecycleService(context, new NoOpNotifier())` or `NoOpBillingAccountLifecycle` where Soft lock is not under test — **cancel tests must pass a real lifecycle**.

For `RevolutCancelAtPeriodEndAdapterTests` harness: pass `new RevolutCancelAtPeriodEndAdapter(context, merchant)` as today **and** a real `BillingAccountLifecycleService` so Soft lock applies.

- [ ] **Step 4: Run tests to verify they pass**

Same filter as Step 2, plus:

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~IncludedPeriodMintServiceTests|FullyQualifiedName~RevolutCancelAtPeriodEndAdapterTests" --no-restore
```

Expected: PASS.

- [ ] **Step 5: Commit** (skip unless human asks)

```bash
git add backend/TummlyBackend/Services/IncludedPeriodMintService.cs \
  backend/TummlyBackend.Tests/Services/IncludedPeriodMintServiceTests.cs \
  backend/TummlyBackend.Tests/Services/RevolutCancelAtPeriodEndAdapterTests.cs
git commit -m "$(cat <<'EOF'
feat(billing): Soft lock and Revolut cancel on paid cancel apply

EOF
)"
```

---

### Task 3: Gate + restore regression smoke

**Files:**
- Test: `backend/TummlyBackend.Tests/Services/BillingAccountLifecycleServiceTests.cs` (or a small helper assert)
- Optionally reuse `OperatorBillingLockEvaluator` unit asserts if a dedicated test file exists

**Interfaces:**
- Consumes: Soft lock account after `ApplyPostCancelSoftLock`; `ActivatePaidPlanAsync`
- Produces: confidence Soft lock deny + restore clear clocks

- [ ] **Step 1: Write failing / extend tests**

```csharp
[Fact]
public void ApplyPostCancelSoftLock_PaidWriteDeny_IsSoftLock()
{
    var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    var account = new BillingAccount
    {
        RestaurantId = 1,
        SubscriptionPlan = BillingSubscriptionPlans.Growth,
        BillingCycle = BillingCycles.Monthly,
        BillingStatus = BillingStatuses.Active,
        ContractedPricebookId = "TUMMLY-UK-GBP-2026-08-V3",
        StarterKitState = StarterKitStates.Unused,
    };
    _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);

    var deny = OperatorBillingLockEvaluator.EvaluatePaidWriteDeny(
        OperatorBillingLockEvaluator.FromBillingAccount(account)
    );
    Assert.Equal(/* existing Soft lock code string — match OperatorBillingLockEvaluator */, deny);
}

[Fact]
public async Task ActivatePaidPlan_AfterPostCancelSoftLock_ClearsClocks()
{
    var renewalEnd = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);
    var seeded = await SeedPaidAsync();
    var account = await ReloadAsync(seeded.RestaurantId);
    _lifecycle.ApplyPostCancelSoftLock(account, renewalEnd);
    await _context.SaveChangesAsync();

    var now = renewalEnd.AddDays(1);
    await _lifecycle.ActivatePaidPlanAsync(seeded.RestaurantId, now);

    var after = await ReloadAsync(seeded.RestaurantId);
    Assert.Equal(BillingStatuses.Active, after.BillingStatus);
    Assert.Null(after.SoftLockEnteredAt);
    Assert.Null(after.DormantEnteredAt);
    Assert.Null(after.PilotPeriodEnd);
}
```

Look up the exact Soft lock deny code string in `OperatorBillingLockEvaluator` (e.g. `soft_lock` / `account_soft_locked`) and use it verbatim.

- [ ] **Step 2: Run tests — expect fail only if ActivatePaidPlan behaviour differs**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ApplyPostCancelSoftLock_PaidWriteDeny|FullyQualifiedName~ActivatePaidPlan_AfterPostCancelSoftLock" --no-restore
```

- [ ] **Step 3: Fix only if needed** — prefer no production change; Soft lock evaluator and `ActivatePaidPlanAsync` already exist.

- [ ] **Step 4: Pass**

- [ ] **Step 5: Commit** (skip unless human asks)

---

### Task 4: Full smoke + plan check

- [ ] **Step 1: Run focused suite**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~BillingAccountLifecycleServiceTests|FullyQualifiedName~IncludedPeriodMintServiceTests|FullyQualifiedName~RevolutCancelAtPeriodEndAdapterTests|FullyQualifiedName~OperatorBillingLock" --no-restore
```

Expected: PASS (or only pre-existing unrelated failures — do not ignore new failures in these filters).

- [ ] **Step 2: Spec coverage checklist**

| Spec requirement | Task |
|---|---|
| Demote Pilot Soft lock at cancel apply | 1–2 |
| Clear dunning; keep PaidExtraLocationCount | 1 |
| Tick Soft lock → Dormant | 1 |
| Notices via Tick | 1 |
| Revolut native cancel on apply | 2 |
| Soft lock gates | 3 |
| ActivatePaidPlan restore | 3 |
| No purge job | (out of scope) |

- [ ] **Step 3: Commit** (skip unless human asks)

---

## Spec self-review (plan author)

1. **Coverage:** Soft lock demotion, mint wiring, Revolut cancel, Tick Dormant, gates, restore — covered. Purge explicitly out of scope.
2. **Nested transaction:** Spec named `EnterPostCancelSoftLockAsync`; plan uses sync `ApplyPostCancelSoftLock` on the locked account to match PlanChange and ADR without nested UPDLOCK. Same product fields.
3. **Test flip:** Old “WithoutImmediateRevolutCancel” expectation is intentionally reversed to match the approved design.
