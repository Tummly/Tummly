# Launch Retention Purge + IR Runbook Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** After 90 days of continuous Dormant, hard-delete restaurant Location Guests, stamp completion, audit once; ship an incident-response runbook with a first-30-minutes checklist.

**Architecture:** Extract shared `LocationGuestHardDelete` core from operator delete. New `IGuestRetentionPurgeService` selects eligible Billing Accounts, deletes guests in one transaction per restaurant, sets `GuestRetentionPurgedAtUtc`, appends `retention.guest_purge`. Hosted daily `GuestRetentionPurgeBackgroundService`. Restore paths that clear `DormantEnteredAt` also clear the stamp. IR doc under `docs/product/`.

**Tech Stack:** ASP.NET Core, EF Core, BackgroundService, xUnit InMemory, existing `IAdminAuditService`.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-retention-purge-ir-runbook-design.md](../specs/2026-09-21-launch-retention-purge-ir-runbook-design.md)

## Global Constraints

- Guest PII purge only — do **not** delete operators, Restaurant, Locations, Billing (except stamp), Shop, Campaigns, Offers, credit ledger.
- Hard-delete cascade = existing operator delete (Feedback unlink; Feedback PII snapshots stay; Assistant quotes stay).
- Eligible only when still `Dormant` + `DormantEnteredAt + 90 days` + `GuestRetentionPurgedAtUtc == null`.
- Restore paths that clear `DormantEnteredAt` must also set `GuestRetentionPurgedAtUtc = null`.
- One `AdminAuditEvent` per restaurant (`retention.guest_purge`, actor `system:retention`); fail closed with stamp in same save.
- Cap 25 restaurants per `ProcessOnceAsync` run; poll every 24 hours; skip `Testing` env.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|------|------|
| `backend/TummlyBackend/Models/BillingAccount.cs` | `GuestRetentionPurgedAtUtc` |
| `backend/TummlyBackend/Helpers/GuestRetentionEligibility.cs` | Pure eligibility + `GuestRetentionDays = 90` |
| `backend/TummlyBackend/Helpers/LocationGuestHardDelete.cs` | Shared hard-delete core (no SaveChanges) |
| `backend/TummlyBackend/Services/LocationGuestDeleteService.cs` | Call shared core after authz |
| `backend/TummlyBackend/Interfaces/IGuestRetentionPurgeService.cs` | ProcessOnce API |
| `backend/TummlyBackend/Services/GuestRetentionPurgeService.cs` | Batch purge + stamp + audit |
| `backend/TummlyBackend/Services/GuestRetentionPurgeBackgroundService.cs` | Daily host |
| `backend/TummlyBackend/Services/BillingAccountLifecycleService.cs` | Clear stamp on restore |
| `backend/TummlyBackend/Models/AdminAuditEvent.cs` | `RetentionGuestPurge` action |
| EF migration `AddBillingAccountGuestRetentionPurgedAt` | Schema |
| `backend/TummlyBackend/Program.cs` | DI + hosted service |
| `docs/product/incident-response.md` | IR runbook |
| `docs/product/support-playbooks.md` | Link + status |
| `docs/product/security-and-rbac.md` | Launch blocker update |
| Tests under `backend/TummlyBackend.Tests/` | Eligibility, hard-delete, purge, lifecycle stamp clear |

---

### Task 1: Column + eligibility helper + migration + clear stamp on restore

**Files:**
- Modify: `backend/TummlyBackend/Models/BillingAccount.cs` — add property after `DormantEnteredAt`
- Create: `backend/TummlyBackend/Helpers/GuestRetentionEligibility.cs`
- Create: EF migration `AddBillingAccountGuestRetentionPurgedAt` (run from `backend/TummlyBackend`)
- Modify: `backend/TummlyBackend/Services/BillingAccountLifecycleService.cs` — clear stamp in `ActivatePaidPlanAsync`, `ExtendPilotActivationAsync`, `RecoverDunningAsync`
- Test: `backend/TummlyBackend.Tests/Helpers/GuestRetentionEligibilityTests.cs`
- Test: extend `backend/TummlyBackend.Tests/Services/BillingAccountLifecycleServiceTests.cs`

**Interfaces:**
- Consumes: `BillingAccount`, `BillingStatuses`
- Produces:

```csharp
// Helpers/GuestRetentionEligibility.cs
public static class GuestRetentionEligibility
{
    public const int GuestRetentionDays = 90;

    public static bool IsEligible(BillingAccount account, DateTime nowUtc)
    {
        if (!string.Equals(
            account.BillingStatus,
            BillingStatuses.Dormant,
            StringComparison.Ordinal))
        {
            return false;
        }

        if (account.DormantEnteredAt is not DateTime dormantAt)
        {
            return false;
        }

        if (account.GuestRetentionPurgedAtUtc != null)
        {
            return false;
        }

        return nowUtc >= dormantAt.AddDays(GuestRetentionDays);
    }
}
```

- [ ] **Step 1: Write the failing eligibility tests**

```csharp
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class GuestRetentionEligibilityTests
    {
        private static BillingAccount Dormant(DateTime entered) =>
            new()
            {
                RestaurantId = 1,
                BillingStatus = BillingStatuses.Dormant,
                DormantEnteredAt = entered,
            };

        [Fact]
        public void IsEligible_False_WhenTooEarly()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    Dormant(entered),
                    entered.AddDays(89)
                )
            );
        }

        [Fact]
        public void IsEligible_True_WhenDue()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            Assert.True(
                GuestRetentionEligibility.IsEligible(
                    Dormant(entered),
                    entered.AddDays(90)
                )
            );
        }

        [Fact]
        public void IsEligible_False_WhenAlreadyStamped()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = Dormant(entered);
            account.GuestRetentionPurgedAtUtc = entered.AddDays(90);
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    account,
                    entered.AddDays(100)
                )
            );
        }

        [Fact]
        public void IsEligible_False_WhenNotDormant()
        {
            var entered = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            var account = Dormant(entered);
            account.BillingStatus = BillingStatuses.Active;
            Assert.False(
                GuestRetentionEligibility.IsEligible(
                    account,
                    entered.AddDays(100)
                )
            );
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL (type / helper missing)**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestRetentionEligibilityTests" --no-restore 2>&1 | tail -40
```

Expected: FAIL (compile or missing type).

- [ ] **Step 3: Add property + helper + migration**

On `BillingAccount` after `DormantEnteredAt`:

```csharp
/// <summary>
/// Set when guest retention purge completed for the current Dormant episode.
/// Cleared when Billing status leaves Dormant.
/// </summary>
public DateTime? GuestRetentionPurgedAtUtc { get; set; }
```

Create `GuestRetentionEligibility.cs` as in Interfaces block.

```bash
cd /run/media/salman/D/Freelance/Tummly/backend/TummlyBackend && dotnet ef migrations add AddBillingAccountGuestRetentionPurgedAt
```

- [ ] **Step 4: Clear stamp on restore paths**

In `BillingAccountLifecycleService`, wherever `DormantEnteredAt = null` is set, also set `GuestRetentionPurgedAtUtc = null`:

- `RecoverDunningAsync`
- `ActivatePaidPlanAsync`
- `ExtendPilotActivationAsync`

- [ ] **Step 5: Lifecycle tests for stamp clear**

Add to `BillingAccountLifecycleServiceTests.cs` (follow existing seed patterns):

```csharp
[Fact]
public async Task ActivatePaidPlan_ClearsGuestRetentionPurgedAt()
{
    // seed unpaid Pilot Dormant BA with GuestRetentionPurgedAtUtc set
    // call ActivatePaidPlanAsync
    // Assert.Null(after.GuestRetentionPurgedAtUtc)
    // Assert.Null(after.DormantEnteredAt)
}
```

Mirror for `ExtendPilotActivationAsync` and `RecoverDunningAsync` if those tests already seed Dormant.

- [ ] **Step 6: Run tests — expect PASS**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestRetentionEligibilityTests|FullyQualifiedName~BillingAccountLifecycleServiceTests" --no-restore 2>&1 | tail -50
```

Expected: PASS for new eligibility + stamp-clear tests; existing lifecycle tests still PASS.

- [ ] **Step 7: Commit** (only if human asks)

---

### Task 2: Extract `LocationGuestHardDelete` shared core

**Files:**
- Create: `backend/TummlyBackend/Helpers/LocationGuestHardDelete.cs`
- Modify: `backend/TummlyBackend/Services/LocationGuestDeleteService.cs` — call helper; keep authz + SaveChanges
- Test: existing `LocationGuestDeleteServiceTests` must still PASS; add one helper unit test if useful

**Interfaces:**
- Consumes: `ApplicationDbContext`, tracked `LocationGuest`
- Produces:

```csharp
// Helpers/LocationGuestHardDelete.cs
internal static class LocationGuestHardDelete
{
    /// <summary>
    /// Stages hard-delete for one Location Guest (no SaveChanges).
    /// Unlinks Feedback; removes notes/tags/activity/ledger; removes guest;
    /// removes orphan Master Guest.
    /// </summary>
    public static async Task ApplyAsync(
        ApplicationDbContext context,
        LocationGuest locationGuest,
        CancellationToken cancellationToken = default
    );
}
```

- [ ] **Step 1: Move delete body into helper**

Copy the cascade block from `LocationGuestDeleteService.DeleteAsync` (Feedback unlink through Master Guest remove) into `LocationGuestHardDelete.ApplyAsync`. Do **not** call `SaveChangesAsync` inside the helper.

`LocationGuestDeleteService` after authz + load:

```csharp
await LocationGuestHardDelete.ApplyAsync(
    _context,
    locationGuest,
    cancellationToken
);
await _context.SaveChangesAsync(cancellationToken);
return LocationGuestDeleteOutcome.Deleted();
```

- [ ] **Step 2: Run operator delete tests — expect PASS**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~LocationGuestDeleteServiceTests" --no-restore 2>&1 | tail -40
```

Expected: PASS (behaviour unchanged).

- [ ] **Step 3: Commit** (only if human asks)

---

### Task 3: `IGuestRetentionPurgeService` + audit constant + unit tests

**Files:**
- Modify: `backend/TummlyBackend/Models/AdminAuditEvent.cs` — add `RetentionGuestPurge = "retention.guest_purge"`
- Create: `backend/TummlyBackend/Interfaces/IGuestRetentionPurgeService.cs`
- Create: `backend/TummlyBackend/Services/GuestRetentionPurgeService.cs`
- Modify: `backend/TummlyBackend/Program.cs` — `AddScoped<IGuestRetentionPurgeService, GuestRetentionPurgeService>()`
- Test: `backend/TummlyBackend.Tests/Services/GuestRetentionPurgeServiceTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `IAdminAuditService`, `LocationGuestHardDelete`, `GuestRetentionEligibility`
- Produces:

```csharp
public interface IGuestRetentionPurgeService
{
    Task<GuestRetentionPurgeBatchResult> ProcessOnceAsync(
        DateTime nowUtc,
        CancellationToken cancellationToken = default
    );
}

public sealed record GuestRetentionPurgeBatchResult(
    int EligibleFound,
    int Purged,
    int Failed,
    int GuestsDeleted
);
```

**Service behaviour (exact):**

1. Query eligible Billing Accounts: `BillingStatus == Dormant`, `DormantEnteredAt != null`, `GuestRetentionPurgedAtUtc == null`, `DormantEnteredAt <= nowUtc.AddDays(-90)`, `Take(25)`, order by `DormantEnteredAt` ascending.
2. For each restaurantId:
   - Begin transaction (skip nested begin on InMemory — follow Included-period / AdminService pattern: begin only if relational).
   - Load BA tracking; if `!GuestRetentionEligibility.IsEligible` → rollback / skip.
   - Load Location Guest ids for locations where `RestaurantId == restaurantId`.
   - For each guest: load tracked entity; `await LocationGuestHardDelete.ApplyAsync(...)`.
   - Set `ba.GuestRetentionPurgedAtUtc = nowUtc`.
   - `_audit.Append(new AdminAuditAppendRequest(Action: AdminAuditActions.RetentionGuestPurge, ActorIdentity: "system:retention", TargetType: AdminAuditTargetTypes.Restaurant, TargetId: restaurantId.ToString(), RestaurantId: restaurantId, DetailJson: $"{{\"guestsDeleted\":{count}}}"))`.
   - SaveChanges + commit.
   - On exception: rollback, log, `Failed++`, continue.
3. Return batch counts.

- [ ] **Step 1: Write failing service tests**

```csharp
[Fact]
public async Task ProcessOnce_DeletesGuests_Stamps_AndAudits_WhenDue()
{ /* seed Dormant BA DormantEnteredAt = now-90d, 2 Location Guests; ProcessOnce; assert guests gone, stamp set, one AdminAuditEvent retention.guest_purge */ }

[Fact]
public async Task ProcessOnce_StampsEmptyRestaurant()
{ /* eligible BA, zero guests; stamp + audit with guestsDeleted=0 */ }

[Fact]
public async Task ProcessOnce_Skips_WhenNotDue()
{ /* Dormant 10 days; no stamp; guests remain */ }

[Fact]
public async Task ProcessOnce_Skips_WhenAlreadyStamped()
{ /* eligible clock but stamp set; guests remain */ }
```

Use InMemory `ApplicationDbContext`, real `AdminAuditService` + `TimeProvider.System` or fixed clock, seed Restaurant + Location + MasterGuest + LocationGuest + BillingAccount similar to `LocationGuestDeleteServiceTests`.

- [ ] **Step 2: Run tests — expect FAIL**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestRetentionPurgeServiceTests" --no-restore 2>&1 | tail -40
```

- [ ] **Step 3: Implement service + action constant + DI**

- [ ] **Step 4: Run tests — expect PASS**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet test TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~GuestRetentionPurgeServiceTests|FullyQualifiedName~LocationGuestDeleteServiceTests" --no-restore 2>&1 | tail -50
```

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 4: BackgroundService host

**Files:**
- Create: `backend/TummlyBackend/Services/GuestRetentionPurgeBackgroundService.cs`
- Modify: `backend/TummlyBackend/Program.cs` — `AddHostedService<GuestRetentionPurgeBackgroundService>()`

**Interfaces:**
- Consumes: `IGuestRetentionPurgeService`, `IHostEnvironment`, `TimeProvider` (optional — may use `DateTime.UtcNow` like Included-period)
- Produces: hosted loop

```csharp
public sealed class GuestRetentionPurgeBackgroundService : BackgroundService
{
    public static readonly TimeSpan PollInterval = TimeSpan.FromHours(24);

    // ExecuteAsync: if Testing return; else loop ProcessOnceAsync via scope + Delay(PollInterval)
}
```

Mirror `IncludedPeriodBackgroundService` structure (scope factory / `IServiceProvider.CreateScope`, catch non-cancel exceptions, log batch when `Purged > 0 || Failed > 0`).

- [ ] **Step 1: Implement BackgroundService + register hosted service**

- [ ] **Step 2: Smoke build**

```bash
cd /run/media/salman/D/Freelance/Tummly/backend && dotnet build TummlyBackend/TummlyBackend.csproj --no-restore 2>&1 | tail -30
```

Expected: Build succeeded.

- [ ] **Step 3: Commit** (only if human asks)

---

### Task 5: Incident-response runbook + product doc links

**Files:**
- Create: `docs/product/incident-response.md`
- Modify: `docs/product/support-playbooks.md` — Related documentation + status note
- Modify: `docs/product/security-and-rbac.md` — Audit / launch blocker row for retention + IR

**Interfaces:** none (docs)

- [ ] **Step 1: Write `docs/product/incident-response.md`**

Must include sections from the spec:

1. Purpose / scope
2. Severity P1 / P2 / P3 (align examples with support-playbooks)
3. Roles: Incident Lead, Comms, Engineering — contact placeholders `TBD`
4. First 30 minutes checklist (bullet steps)
5. Phases: Detect → Contain → Eradicate → Recover → Post-mortem (short template fields)
6. Evidence sources: App Insights/logs; `GET /api/admin/audit-events`; Revolut / Twilio / Resend dashboards; approved DB path only
7. Personal-data incidents: ICO 72-hour assessment reminder; legal contact `TBD`

- [ ] **Step 2: Update cross-links**

In `support-playbooks.md` Related documentation table add IR link; in Status summary note IR runbook Shipped.

In `security-and-rbac.md` change launch-blocker note to: retention purge + IR runbook shipped (or clear the remaining retention/IR clause).

- [ ] **Step 3: Commit** (only if human asks)

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|------------------|------|
| Eligibility Dormant + 90d + unstamped | Task 1 |
| `GuestRetentionPurgedAtUtc` column + migration | Task 1 |
| Clear stamp on restore | Task 1 |
| Shared hard-delete core | Task 2 |
| Operator delete unchanged | Task 2 |
| Purge service + audit + empty stamp | Task 3 |
| BackgroundService daily / skip Testing | Task 4 |
| IR runbook + doc links | Task 5 |
| No restaurant hard-delete | Global Constraints |

No TBD placeholders in steps. Types consistent: `GuestRetentionDays`, `RetentionGuestPurge`, `system:retention`, batch cap 25.
