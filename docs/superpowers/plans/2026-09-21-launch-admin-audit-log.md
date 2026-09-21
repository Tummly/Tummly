# Launch Admin / Support Audit Log Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist an append-only `AdminAuditEvent` for covered Admin/support writes, expose `GET /api/admin/audit-events`, and fail closed so the action and audit share the same DB save.

**Architecture:** New `IAdminAuditService` appends tracked rows on the shared `ApplicationDbContext` (no nested save). Call sites: `TrialReviewTransition`, `AdminService` (extend/purge), `CreditLedgerService` staff paths, `AdminPaymentRefundService` completion save. Dual-write keeps existing `TrialRequests` review fields. Admin list API only — no UI.

**Tech Stack:** ASP.NET Core, EF Core, xUnit InMemory, existing Admin JWT integration test helpers.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-admin-audit-log-design.md](../specs/2026-09-21-launch-admin-audit-log-design.md)

## Global Constraints

- Actions only: trial approve/decline/request_more_info/resend_invite/purge; operator.extend_activation; credit.adjust; credit.reverse; payment.refund.
- Append-only table; no UPDATE/DELETE API; no FK cascade from parents onto audit rows.
- Fail closed: audit row must be in the same save as the covered mutation.
- Dual-write trial `ReviewedBy` / `ReviewedAt` / reason fields.
- Idempotent payment-refund replay that returns an existing completed refund does **not** append a second event.
- `DetailJson`: short reason/qty/channel/direction/amount/idempotency — no secrets / no full PII dumps.
- List `take` hard cap = 100; newest-first; Admin role only.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Models/AdminAuditEvent.cs` | Entity + action/target constants |
| `backend/TummlyBackend/Interfaces/IAdminAuditService.cs` | Append + List |
| `backend/TummlyBackend/Services/AdminAuditService.cs` | Implementation |
| `backend/TummlyBackend/DTOs/Admin/AdminAuditDtos.cs` | List request/response DTOs |
| `backend/TummlyBackend/Data/ApplicationDbContext.cs` | DbSet + column config |
| EF migration `AddAdminAuditEvents` | Schema |
| `backend/TummlyBackend/Program.cs` | DI register |
| `backend/TummlyBackend/DTOs/Admin/TrialReviewTypes.cs` | Optional `ActorAdminUserId` on context |
| `backend/TummlyBackend/Services/TrialReviewTransition.cs` | Append before save |
| `backend/TummlyBackend/Controllers/AdminController.cs` | BuildContext staff id; extend/purge actor; GET list |
| `backend/TummlyBackend/Interfaces/IAdminService.cs` | Actor args on extend/purge |
| `backend/TummlyBackend/Services/AdminService.cs` | Extend/purge append |
| `backend/TummlyBackend/Services/CreditLedgerService.cs` | Staff adjust/reverse append |
| `backend/TummlyBackend/Services/AdminPaymentRefundService.cs` | Completion-save append |
| `docs/product/admin.md` | Status → Shipped |
| `docs/product/security-and-rbac.md` | Status → Shipped |
| Tests under `backend/TummlyBackend.Tests/` | Unit + integration |

---

### Task 1: Model + `IAdminAuditService` + EF + unit tests

**Files:**
- Create: `backend/TummlyBackend/Models/AdminAuditEvent.cs`
- Create: `backend/TummlyBackend/Interfaces/IAdminAuditService.cs`
- Create: `backend/TummlyBackend/Services/AdminAuditService.cs`
- Create: `backend/TummlyBackend/DTOs/Admin/AdminAuditDtos.cs`
- Modify: `backend/TummlyBackend/Data/ApplicationDbContext.cs`
- Create: EF migration `AddAdminAuditEvents` (run `dotnet ef migrations add` from `backend/TummlyBackend`)
- Modify: `backend/TummlyBackend/Program.cs` — `AddScoped<IAdminAuditService, AdminAuditService>()`
- Test: `backend/TummlyBackend.Tests/Services/AdminAuditServiceTests.cs`

**Interfaces:**
- Consumes: `ApplicationDbContext`, `TimeProvider`
- Produces:

```csharp
// Models/AdminAuditEvent.cs
public class AdminAuditEvent
{
    public Guid Id { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    public string Action { get; set; } = string.Empty;
    public int? ActorAdminUserId { get; set; }
    public string ActorIdentity { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int? RestaurantId { get; set; }
    public string? DetailJson { get; set; }
    public bool Succeeded { get; set; } = true;
}

public static class AdminAuditActions
{
    public const string TrialApprove = "trial.approve";
    public const string TrialDecline = "trial.decline";
    public const string TrialRequestMoreInfo = "trial.request_more_info";
    public const string TrialResendInvite = "trial.resend_invite";
    public const string TrialPurge = "trial.purge";
    public const string OperatorExtendActivation = "operator.extend_activation";
    public const string CreditAdjust = "credit.adjust";
    public const string CreditReverse = "credit.reverse";
    public const string PaymentRefund = "payment.refund";
}

public static class AdminAuditTargetTypes
{
    public const string TrialRequest = "trial_request";
    public const string OperatorUser = "operator_user";
    public const string Restaurant = "restaurant";
    public const string PaymentOrder = "payment_order";
}

// Interfaces/IAdminAuditService.cs
public interface IAdminAuditService
{
    void Append(AdminAuditAppendRequest request);

    Task<AdminAuditListResult> ListAsync(
        AdminAuditListQuery query,
        CancellationToken cancellationToken = default
    );
}

public sealed record AdminAuditAppendRequest(
    string Action,
    string ActorIdentity,
    string TargetType,
    string TargetId,
    int? ActorAdminUserId = null,
    int? RestaurantId = null,
    string? DetailJson = null
);

public sealed record AdminAuditListQuery(
    string? Action = null,
    int? RestaurantId = null,
    DateTime? FromUtc = null,
    DateTime? ToUtc = null,
    int Skip = 0,
    int Take = 50
);

public sealed record AdminAuditListResult(
    IReadOnlyList<AdminAuditEventDto> Items,
    int TotalCount
);

// DTOs — AdminAuditEventDto mirrors entity fields for API
```

- [ ] **Step 1: Write the failing tests**

Create `AdminAuditServiceTests.cs`:

```csharp
[Fact]
public async Task Append_ThenList_ReturnsNewestFirst_RespectsTakeCap()
{
    var clock = new FakeTimeProvider(
        new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.Zero)
    );
    var svc = CreateService(out var db, clock);

    svc.Append(new AdminAuditAppendRequest(
        AdminAuditActions.TrialApprove,
        "admin@tummly.com",
        AdminAuditTargetTypes.TrialRequest,
        "1",
        ActorAdminUserId: 9
    ));
    clock.Advance(TimeSpan.FromMinutes(1));
    svc.Append(new AdminAuditAppendRequest(
        AdminAuditActions.CreditAdjust,
        "admin@tummly.com",
        AdminAuditTargetTypes.Restaurant,
        "42",
        RestaurantId: 42,
        DetailJson: """{"qty":10}"""
    ));
    await db.SaveChangesAsync();

    var page = await svc.ListAsync(new AdminAuditListQuery(Take: 1));
    Assert.Equal(2, page.TotalCount);
    Assert.Single(page.Items);
    Assert.Equal(AdminAuditActions.CreditAdjust, page.Items[0].Action);
}

[Fact]
public async Task List_FiltersByActionAndRestaurantId()
{
    // seed two events; filter Action=credit.adjust and RestaurantId=42
    // assert single match
}

[Fact]
public async Task List_ClampsTakeTo100()
{
    // Append is not needed; assert ListAsync with Take=500 returns at most 100 items
    // (seed 101 events or assert Take used internally via count of returned Items after seeding 3)
}
```

- [ ] **Step 2: Run tests — expect FAIL** (types / service missing)

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter FullyQualifiedName~AdminAuditServiceTests`

- [ ] **Step 3: Implement model, service, DbContext, migration, DI**

`Append` must only `_context.AdminAuditEvents.Add(...)` with `Id = Guid.NewGuid()`, `OccurredAtUtc = _clock.GetUtcNow().UtcDateTime`, `Succeeded = true`. Do **not** call `SaveChangesAsync`.

`ListAsync`: filter optional Action (ordinal), RestaurantId, FromUtc/ToUtc on OccurredAtUtc; `Skip`/`Take` with `Take = Math.Clamp(query.Take, 1, 100)`; `Skip = Math.Max(0, query.Skip)`; order `OccurredAtUtc` descending; return `TotalCount` of filtered set before paging.

DbContext: `DbSet<AdminAuditEvent> AdminAuditEvents`; MaxLength Action 64, ActorIdentity 320, TargetType 64, TargetId 128, DetailJson 4000; index `(OccurredAtUtc)`; index `(Action, OccurredAtUtc)`; index `(RestaurantId, OccurredAtUtc)` filtered where RestaurantId not null (optional — at least OccurredAtUtc + Action indexes).

- [ ] **Step 4: Run tests — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

```bash
git add backend/TummlyBackend/Models/AdminAuditEvent.cs \
  backend/TummlyBackend/Interfaces/IAdminAuditService.cs \
  backend/TummlyBackend/Services/AdminAuditService.cs \
  backend/TummlyBackend/DTOs/Admin/AdminAuditDtos.cs \
  backend/TummlyBackend/Data/ApplicationDbContext.cs \
  backend/TummlyBackend/Migrations/*AddAdminAuditEvents* \
  backend/TummlyBackend/Program.cs \
  backend/TummlyBackend.Tests/Services/AdminAuditServiceTests.cs
git commit -m "$(cat <<'EOF'
Add append-only AdminAuditEvents and AdminAuditService.

EOF
)"
```

---

### Task 2: Trial review append + `TrialReviewContext` actor id

**Files:**
- Modify: `backend/TummlyBackend/DTOs/Admin/TrialReviewTypes.cs`
- Modify: `backend/TummlyBackend/Services/TrialReviewTransition.cs`
- Modify: `backend/TummlyBackend/Controllers/AdminController.cs` (`BuildContext`)
- Modify: `backend/TummlyBackend/Services/AdminService.cs` — reminder `TrialReviewContext("System", null, null)` still compiles (optional 4th arg default)
- Test: `backend/TummlyBackend.Tests/Services/TrialReviewTransitionTests.cs`

**Interfaces:**
- Consumes: `IAdminAuditService.Append`, `AdminAuditActions`, `AdminAuditTargetTypes`
- Produces:

```csharp
public sealed record TrialReviewContext(
    string AdminIdentity,
    string? Reason,
    string? AdminNotes,
    int? ActorAdminUserId = null
);
```

Map decisions → actions:

| Decision | Action |
|----------|--------|
| Approve | `trial.approve` |
| Decline | `trial.decline` |
| RequestMoreInfo | `trial.request_more_info` |
| ResendInvite | `trial.resend_invite` |

- [ ] **Step 1: Write the failing tests**

In `TrialReviewTransitionTests`, inject `IAdminAuditService` (real `AdminAuditService` with same InMemory context + `TimeProvider.System` or fake). After approve:

```csharp
[Fact]
public async Task Approve_AppendsAdminAuditEvent_AndKeepsReviewedBy()
{
    var trial = Seed(TrialRequestStatus.EmailVerified);
    await _transition.ApplyTransitionAsync(
        trial.Id,
        TrialReviewDecision.Approve,
        new TrialReviewContext("admin@tummly.com", null, null, ActorAdminUserId: 7)
    );

    var row = Assert.Single(_context.AdminAuditEvents);
    Assert.Equal(AdminAuditActions.TrialApprove, row.Action);
    Assert.Equal("admin@tummly.com", row.ActorIdentity);
    Assert.Equal(7, row.ActorAdminUserId);
    Assert.Equal(AdminAuditTargetTypes.TrialRequest, row.TargetType);
    Assert.Equal(trial.Id.ToString(), row.TargetId);
    Assert.True(row.Succeeded);

    var reloaded = await _context.TrialRequests.SingleAsync(x => x.Id == trial.Id);
    Assert.Equal("admin@tummly.com", reloaded.ReviewedBy);
    Assert.NotNull(reloaded.ReviewedAt);
}

[Fact]
public async Task Decline_AppendsTrialDeclineAction()
{
    // EmailVerified → Decline with reason; assert Action == trial.decline and DetailJson contains reason snippet
}
```

Update constructor of `TrialReviewTransition` in existing tests to pass `AdminAuditService`.

- [ ] **Step 2: Run — expect FAIL**

Run: `dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter FullyQualifiedName~TrialReviewTransitionTests`

- [ ] **Step 3: Implement**

1. Extend `TrialReviewContext` with optional `ActorAdminUserId`.
2. Inject `IAdminAuditService` into `TrialReviewTransition`.
3. Before `SaveChangesAsync`, call `_audit.Append(...)` with mapped action; for decline/more-info put truncated reason in `DetailJson` e.g. `{"reason":"..."}` (max ~500 chars of reason).
4. `BuildContext` in `AdminController`:

```csharp
return new TrialReviewContext(
    adminIdentity,
    reason,
    adminNotes,
    ActorAdminUserId: GetStaffId()
);
```

5. System reminder path stays `new TrialReviewContext("System", null, null)` → actor id null; still appends `trial.resend_invite`.

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 3: Extend activation + purge trial

**Files:**
- Modify: `backend/TummlyBackend/Interfaces/IAdminService.cs`
- Modify: `backend/TummlyBackend/Services/AdminService.cs`
- Modify: `backend/TummlyBackend/Controllers/AdminController.cs`
- Test: `backend/TummlyBackend.Tests/Services/AdminServiceTrialRequestsTests.cs` (or new `AdminServiceAuditTests.cs` if cleaner)

**Interfaces:**
- Consumes: `IAdminAuditService`
- Produces:

```csharp
Task<bool> PurgeTrialRequestAsync(
    int trialRequestId,
    int? actorAdminUserId,
    string actorIdentity
);

Task<AdminTrialRequestDto?> ExtendActivationAsync(
    int userId,
    ExtendActivationDto dto,
    int? actorAdminUserId,
    string actorIdentity
);
```

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task ExtendActivation_AppendsOperatorExtendActivationAudit()
{
    // seed expired operator user + trial; call ExtendActivationAsync with actor;
    // assert AdminAuditEvents has operator.extend_activation, TargetType operator_user, TargetId = userId
}

[Fact]
public async Task PurgeTrialRequest_AppendsAudit_ThatSurvivesTrialDelete()
{
    // seed trial; PurgeTrialRequestAsync(...);
    // assert TrialRequests gone; AdminAuditEvents still has trial.purge with TargetId = former id
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

1. Inject `IAdminAuditService` into `AdminService`.
2. **Extend:** before `SaveChangesAsync` that persists new `ActivationExpiresAt`, `_audit.Append` with `operator.extend_activation`, target operator user id, `DetailJson` with new expiry ISO.
3. **Purge:** immediately after loading the trial (before any deletes), `_audit.Append` `trial.purge` with trial id as `TargetId`. Then existing delete flow. Same transaction/commits as today — audit row must be included in a `SaveChanges` that commits (if first save is user delete, ensure audit is added before that first `SaveChangesAsync`, or add an explicit `SaveChanges` after append before deletes — prefer: append then include audit in the first `SaveChangesAsync` of the purge path).
4. Controller: resolve `GetStaffId()` + email identity (same as `BuildContext`); pass into extend/purge. If staff id null on purge/extend, return 401 (same pattern as credit endpoints) **or** require identity string and allow null id — match credit endpoints: 401 when staff id missing.

Update all test call sites of `PurgeTrialRequestAsync` / `ExtendActivationAsync` to pass actor args (`null`, `"test-admin"` is fine).

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 4: Credit adjust + reverse audit

**Files:**
- Modify: `backend/TummlyBackend/Services/CreditLedgerService.cs`
- Test: `backend/TummlyBackend.Tests/Services/CreditLedgerServiceTests.cs` (existing staff tests) or new focused tests

**Interfaces:**
- Consumes: `IAdminAuditService` (inject optional with null-object **or** required — prefer required; update test constructors)
- Produces: audit rows on successful staff adjust/reverse only

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task StaffManualAdjust_Grant_AppendsCreditAdjustAudit()
{
    // arrange restaurant + billing; StaffManualAdjustAsync grant;
    // assert AdminAuditEvent Action=credit.adjust, RestaurantId set, DetailJson has channel/qty/direction
}

[Fact]
public async Task StaffManualAdjust_RestaurantMissing_DoesNotAppendAudit()
{
    // expect Fail restaurant_not_found; AdminAuditEvents empty
}

[Fact]
public async Task StaffReverse_AppendsCreditReverseAudit()
{
    // reverse a valid consumption/manual debit; assert credit.reverse + reversedEntryId in DetailJson
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

Inject `IAdminAuditService` into `CreditLedgerService`.

Before `SaveCommitAndNotifyThresholdAsync` (or immediately before the save that commits ledger rows) in `StaffManualAdjustLockedAsync` / `StaffReverseLockedAsync`:

```csharp
var identity = await ResolveAdminIdentityAsync(request.ActorStaffUserId, cancellationToken);
_audit.Append(new AdminAuditAppendRequest(
    AdminAuditActions.CreditAdjust, // or CreditReverse
    identity,
    AdminAuditTargetTypes.Restaurant,
    request.RestaurantId.ToString(), // reverse: liveTarget.RestaurantId
    ActorAdminUserId: request.ActorStaffUserId,
    RestaurantId: request.RestaurantId,
    DetailJson: /* channel, qty, direction or reversedEntryId + reason snippet */
));
```

`ResolveAdminIdentityAsync`: look up `Admins` by id → Email; if missing use `$"admin:{id}"`.

Do **not** append when AbortAsync / early Fail paths run.

Update test helpers that construct `CreditLedgerService` to pass `AdminAuditService` (same context).

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 5: Payment refund completion audit

**Files:**
- Modify: `backend/TummlyBackend/Services/AdminPaymentRefundService.cs`
- Test: `backend/TummlyBackend.Tests/Services/AdminPaymentRefundServiceTests.cs` (create if missing) or extend existing unit/integration coverage

**Interfaces:**
- Consumes: `IAdminAuditService`
- Produces: one `payment.refund` event when `RefundOrderId` is first set and saved

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact]
public async Task RefundAsync_OnSuccess_AppendsPaymentRefundAudit()
{
    // stub merchant success; assert AdminAuditEvent Action=payment.refund,
    // TargetType=payment_order, TargetId=source order id, RestaurantId set,
    // DetailJson includes refundOrderId + idempotencyKey
}

[Fact]
public async Task RefundAsync_IdempotentCompleted_DoesNotAppendSecondEvent()
{
    // seed intent with RefundOrderId already set; call RefundAsync; assert still single audit (or zero if none seeded — assert count unchanged)
}

[Fact]
public async Task RefundAsync_EarlyFail_DoesNotAppend()
{
    // missing idempotency → Fail; AdminAuditEvents empty
}
```

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement**

Inject `IAdminAuditService`. After merchant success, when setting `intent.RefundOrderId`:

```csharp
_audit.Append(new AdminAuditAppendRequest(
    AdminAuditActions.PaymentRefund,
    await ResolveAdminIdentityAsync(request.ActorStaffUserId, cancellationToken),
    AdminAuditTargetTypes.PaymentOrder,
    orderId,
    ActorAdminUserId: request.ActorStaffUserId,
    RestaurantId: request.RestaurantId,
    DetailJson: /* refundOrderId, amountMinor, idempotencyKey */
));
await _context.SaveChangesAsync(cancellationToken);
```

Early return when existing completed intent: **no** append.

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 6: List API + docs + integration tests

**Files:**
- Modify: `backend/TummlyBackend/Controllers/AdminController.cs`
- Modify: `docs/product/admin.md`
- Modify: `docs/product/security-and-rbac.md`
- Create: `backend/TummlyBackend.Tests/Integration/AdminAuditEventsEndpointsTests.cs`

**Interfaces:**
- Consumes: `IAdminAuditService.ListAsync`
- Produces: `GET /api/admin/audit-events`

- [ ] **Step 1: Write the failing integration tests**

```csharp
[Fact]
public async Task ListAuditEvents_AdminJwt_ReturnsRows()
{
    // seed AdminAuditEvent via DbContext; GET with admin JWT; assert 200 + data
}

[Fact]
public async Task ListAuditEvents_OperatorJwt_Returns403()
{
    // operator JWT → 403
}

[Fact]
public async Task ListAuditEvents_FiltersByAction()
{
    // seed two actions; query ?action=credit.adjust; assert only that action
}
```

Follow patterns in `AdminPaymentRefundEndpointsTests` / `StaffManualAdjustmentEndpointsTests` for seeding Admin + JWT.

- [ ] **Step 2: Run — expect FAIL**

- [ ] **Step 3: Implement GET**

```csharp
[HttpGet("audit-events")]
public async Task<IActionResult> ListAuditEvents(
    [FromQuery] string? action,
    [FromQuery] int? restaurantId,
    [FromQuery] DateTime? from,
    [FromQuery] DateTime? to,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50,
    CancellationToken cancellationToken = default
)
{
    var result = await _adminAudit.ListAsync(
        new AdminAuditListQuery(action, restaurantId, from, to, skip, take),
        cancellationToken
    );
    return Ok(new { success = true, data = result.Items, totalCount = result.TotalCount });
}
```

Inject `IAdminAuditService` into `AdminController`.

Docs: change “Immutable admin audit log | Planned” → “Shipped (append-only table + `GET /api/admin/audit-events`; no Admin UI)”.

- [ ] **Step 4: Run — expect PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 7: Full smoke + plan check

**Files:** none (verify only)

- [ ] **Step 1: Run focused test suite**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj \
  --filter "FullyQualifiedName~AdminAudit|FullyQualifiedName~TrialReviewTransition|FullyQualifiedName~AdminPaymentRefund|FullyQualifiedName~StaffManual|FullyQualifiedName~AdminServiceTrial"
```

Expected: all PASS.

- [ ] **Step 2: Spec coverage checklist**

| Spec item | Task |
|-----------|------|
| `AdminAuditEvents` model + migration | 1 |
| `IAdminAuditService` Append + List | 1 |
| Trial review dual-write + audit | 2 |
| Extend + purge audit | 3 |
| Credit adjust/reverse audit | 4 |
| Payment refund audit + idempotent no-dupe | 5 |
| GET list API Admin-only | 6 |
| Docs status flip | 6 |
| No UI / no purge retention / no IR | out of scope |

- [ ] **Step 3: Commit** (only if human asks)

---

## Spec coverage (self-review)

1. **Coverage:** All locked actions, fail-closed same-save, dual-write, list API, take cap, idempotent refund, docs — mapped to tasks. Retention purge / IR / UI left out of scope.
2. **Placeholders:** None — signatures and test skeletons are concrete.
3. **Types:** `AdminAuditAppendRequest` / `AdminAuditActions` / `TrialReviewContext` 4th arg consistent across tasks.
