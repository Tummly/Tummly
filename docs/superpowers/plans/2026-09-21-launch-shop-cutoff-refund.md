# Launch Shop Cutoff / Refund Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add Admin production-start cutoff on Shop orders; pre-cutoff operator cancel calls Revolut full refund (fail closed); post-cutoff block operator cancel except Admin force cancel (audited).

**Architecture:** Stamp `ProductionStartedAtUtc` on `ShopOrder` via Admin API. Extend `ShopOrderCancelRules` + `ShopOrderCancelReorderService` to gate on null stamp and call `IRevolutMerchantClient.RefundOrderAsync` before cancelling fulfilment. Admin force cancel reuses the same refund helper with an audit append. Money truth (`paymentStatus` / TCN) stays on the existing refund webhook path.

**Tech Stack:** ASP.NET Core, EF Core, existing Revolut Merchant client + `FakeFirstPaidRevolutMerchantClient`, `IAdminAuditService`, xUnit InMemory / WebApplicationFactory, React Admin Shop drawer + Operator Shop detail.

**Spec:** [docs/superpowers/specs/2026-09-21-launch-shop-cutoff-refund-design.md](../specs/2026-09-21-launch-shop-cutoff-refund-design.md)

## Global Constraints

- Pre-cutoff paid cancel **does** call Revolut full refund (supersedes ADR 0046 for that path only).
- Cutoff = explicit Admin stamp, **not** auto on `in_transit`.
- Revolut fail → **do not** cancel fulfilment.
- Complimentary / no `RevolutOrderId` → cancel only, no Merchant call.
- `paymentStatus` stays `paid` until webhook → `refunded`.
- Stable refund idempotency key: `shop-cancel:{orderId:D}` (Guid format `N` or `D` — use invariant `order.Id.ToString("D")`).
- Force cancel default refunds when Revolut id present; `skipRefund: true` opt-out.
- Commit only when the human asks (skip Commit steps unless asked).
- Subagents: Cursor Auto / `inherit` only.
- Report to the human in ASD-STE100 Simplified Technical English.

---

## File map

| File | Role |
|---|---|
| `backend/TummlyBackend/Models/ShopOrder.cs` | `ProductionStartedAtUtc`, `ProductionStartedByAdminUserId` |
| `backend/TummlyBackend/Data/ApplicationDbContext.cs` | Optional FK/index if needed |
| EF migration `AddShopOrderProductionStarted` | Columns |
| `backend/TummlyBackend/Helpers/ShopOrderCancelRules.cs` | Stamp gate + `production_started` block reason |
| `backend/TummlyBackend/Helpers/ShopOrderCancelRefund.cs` | Shared full-refund attempt helper |
| `backend/TummlyBackend/Services/ShopOrderCancelReorderService.cs` | Refund then cancel |
| `backend/TummlyBackend/Models/AdminAuditEvent.cs` | `shop.production_started`, `shop.force_cancel`; target `shop_order` |
| `backend/TummlyBackend/Interfaces/IAdminShopOrderFulfilmentService.cs` | Mark started + force cancel |
| `backend/TummlyBackend/Services/AdminShopOrderFulfilmentService.cs` | Implement + audit |
| `backend/TummlyBackend/Controllers/AdminShopOrdersController.cs` | Two POST endpoints |
| `backend/TummlyBackend/DTOs/Admin/*` | Request/result DTOs |
| `docs/adr/0046-shop-operator-cancel-does-not-initiate-revolut-refund.md` | Amend supersession |
| `src/components/dashboard/operator/Shop/ShopOrderDetailSidebar.tsx` | `production_started` copy |
| `src/components/dashboard/admin/AdminShopOrderDetailDrawer.tsx` | Mark started + force cancel actions |
| `src/api/adminShopOrdersApi.ts` | Client calls |
| Tests under `backend/TummlyBackend.Tests/` | Rules, cancel, admin, integration |

---

### Task 1: Columns + cancel rules + migration

**Files:**
- Modify: `backend/TummlyBackend/Models/ShopOrder.cs`
- Modify: `backend/TummlyBackend/Helpers/ShopOrderCancelRules.cs`
- Create: EF migration via `dotnet ef migrations add AddShopOrderProductionStarted`
- Test: `backend/TummlyBackend.Tests/Helpers/ShopOrderCancelRulesTests.cs` (create)

**Interfaces:**
- Consumes: `ShopOrder`, `ShopPaymentStatuses`, `ShopFulfilmentStatuses`
- Produces: `CanCancel` requires `ProductionStartedAtUtc == null`; `CancelBlockReason` returns `"production_started"` when stamp set and fulfilment is still `processing` (check stamp before returning null; keep `in_transit` / `delivered`)

- [ ] **Step 1: Write the failing tests**

```csharp
using TummlyBackend.Helpers;
using TummlyBackend.Models;
using Xunit;

namespace TummlyBackend.Tests.Helpers
{
    public class ShopOrderCancelRulesTests
    {
        private static ShopOrder PaidProcessing() =>
            new()
            {
                PaymentStatus = ShopPaymentStatuses.Paid,
                FulfilmentStatus = ShopFulfilmentStatuses.Processing,
            };

        [Fact]
        public void CanCancel_True_WhenPaidProcessingAndNoStamp()
        {
            Assert.True(ShopOrderCancelRules.CanCancel(PaidProcessing()));
        }

        [Fact]
        public void CanCancel_False_WhenProductionStarted()
        {
            var order = PaidProcessing();
            order.ProductionStartedAtUtc = DateTime.UtcNow;
            Assert.False(ShopOrderCancelRules.CanCancel(order));
            Assert.Equal(
                "production_started",
                ShopOrderCancelRules.CancelBlockReason(order)
            );
        }

        [Fact]
        public void CancelBlockReason_InTransit_Unchanged()
        {
            var order = PaidProcessing();
            order.FulfilmentStatus = ShopFulfilmentStatuses.InTransit;
            Assert.Equal(
                "in_transit",
                ShopOrderCancelRules.CancelBlockReason(order)
            );
        }
    }
}
```

- [ ] **Step 2: Run tests — expect FAIL** (missing properties / rules)

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ShopOrderCancelRulesTests"
```

- [ ] **Step 3: Add model columns**

On `ShopOrder`:

```csharp
public DateTime? ProductionStartedAtUtc { get; set; }

public int? ProductionStartedByAdminUserId { get; set; }
```

- [ ] **Step 4: Update `ShopOrderCancelRules`**

```csharp
public static bool CanCancel(ShopOrder order)
{
    return string.Equals(
            order.PaymentStatus,
            ShopPaymentStatuses.Paid,
            StringComparison.Ordinal
        )
        && string.Equals(
            order.FulfilmentStatus,
            ShopFulfilmentStatuses.Processing,
            StringComparison.Ordinal
        )
        && order.ProductionStartedAtUtc == null;
}

public static string? CancelBlockReason(ShopOrder order)
{
    if (
        string.Equals(
            order.FulfilmentStatus,
            ShopFulfilmentStatuses.InTransit,
            StringComparison.Ordinal
        )
    )
    {
        return "in_transit";
    }

    if (
        string.Equals(
            order.FulfilmentStatus,
            ShopFulfilmentStatuses.Delivered,
            StringComparison.Ordinal
        )
    )
    {
        return "delivered";
    }

    if (
        order.ProductionStartedAtUtc != null
        && string.Equals(
            order.FulfilmentStatus,
            ShopFulfilmentStatuses.Processing,
            StringComparison.Ordinal
        )
    )
    {
        return "production_started";
    }

    return null;
}
```

- [ ] **Step 5: Add EF migration**

```bash
dotnet ef migrations add AddShopOrderProductionStarted \
  --project backend/TummlyBackend/TummlyBackend.csproj \
  --startup-project backend/TummlyBackend/TummlyBackend.csproj
```

Confirm Designer only adds the two nullable columns on `ShopOrders`.

- [ ] **Step 6: Run tests — expect PASS**

- [ ] **Step 7: Commit** (only if human asks)

```bash
git add backend/TummlyBackend/Models/ShopOrder.cs \
  backend/TummlyBackend/Helpers/ShopOrderCancelRules.cs \
  backend/TummlyBackend/Migrations/*AddShopOrderProductionStarted* \
  backend/TummlyBackend/Migrations/ApplicationDbContextModelSnapshot.cs \
  backend/TummlyBackend.Tests/Helpers/ShopOrderCancelRulesTests.cs
git commit -m "$(cat <<'EOF'
feat(shop): add production-started cutoff columns and cancel gate

EOF
)"
```

---

### Task 2: Operator cancel → Revolut refund (fail closed)

**Files:**
- Create: `backend/TummlyBackend/Helpers/ShopOrderCancelRefund.cs`
- Modify: `backend/TummlyBackend/Services/ShopOrderCancelReorderService.cs`
- Modify: `backend/TummlyBackend.Tests/Helpers/FakeFirstPaidRevolutMerchantClient.cs` (optional `NextRefundFails`)
- Test: `backend/TummlyBackend.Tests/Services/ShopOrderCancelReorderServiceTests.cs` (create) and/or extend `ShopOrderCancelReorderEndpointsTests.cs`

**Interfaces:**
- Consumes: `IRevolutMerchantClient.RefundOrderAsync(string orderId, int? amountMinor, string idempotencyKey, CancellationToken)`
- Produces:

```csharp
internal static class ShopOrderCancelRefund
{
    public static string IdempotencyKey(Guid shopOrderId) =>
        $"shop-cancel:{shopOrderId:D}";

    /// <summary>
    /// Returns null when refund not needed or Merchant succeeded.
    /// Returns error code when Merchant failed (caller must not cancel).
    /// </summary>
    public static async Task<string?> TryFullRefundAsync(
        IRevolutMerchantClient merchant,
        ShopOrder order,
        CancellationToken cancellationToken
    );
}
```

Logic for `TryFullRefundAsync`:
- If `order.IsComplimentary` OR `string.IsNullOrWhiteSpace(order.RevolutOrderId)` → return `null` (no call).
- Else call `RefundOrderAsync(order.RevolutOrderId.Trim(), amountMinor: null, IdempotencyKey(order.Id), ct)`.
- If `!Succeeded` → return `refunded.ErrorCode ?? "revolut_refund_failed"`.
- Else return `null`.

`CancelAsync` after eligibility:
1. `var refundError = await ShopOrderCancelRefund.TryFullRefundAsync(...)`
2. If `refundError != null` → `ShopOrderCancelResult.Fail(refundError, "…")` without mutating order.
3. Else apply existing cancel field writes + `SaveChangesAsync`.

Inject `IRevolutMerchantClient` into `ShopOrderCancelReorderService`.

- [ ] **Step 1: Write failing service tests**

Use InMemory db + stub merchant (or factory Merchant). Cover:
- Paid + `RevolutOrderId` + success → fulfilment `cancelled`, `paymentStatus` still `paid`, `RefundOrderCallCount == 1`, idempotency key `shop-cancel:{id}`.
- Paid + Revolut fail → still `processing`, no cancel fields.
- Complimentary → cancel, `RefundOrderCallCount == 0`.
- Stamp set → `shop_order_not_cancellable`, no refund call.

- [ ] **Step 2: Run — expect FAIL**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ShopOrderCancelReorder"
```

- [ ] **Step 3: Implement helper + wire cancel service**

Add to fake merchant if needed:

```csharp
public bool NextRefundFails { get; set; }

// in RefundOrderAsync:
if (NextRefundFails)
{
    NextRefundFails = false;
    return Task.FromResult(
        new RevolutMerchantCreateResult(
            Succeeded: false,
            ErrorCode: "revolut_refund_failed"
        )
    );
}
```

- [ ] **Step 4: Extend integration test** — paid order with `RevolutOrderId` asserts Merchant refund called; existing no-RevolutId cancel still OK.

- [ ] **Step 5: Run — expect PASS**

- [ ] **Step 6: Commit** (only if human asks)

---

### Task 3: Admin mark production started + audit

**Files:**
- Modify: `backend/TummlyBackend/Models/AdminAuditEvent.cs` — add actions + `AdminAuditTargetTypes.ShopOrder = "shop_order"`
- Modify: `backend/TummlyBackend/Interfaces/IAdminShopOrderFulfilmentService.cs`
- Modify: `backend/TummlyBackend/Services/AdminShopOrderFulfilmentService.cs` — inject `IAdminAuditService`, `TimeProvider` if not already
- Modify: `backend/TummlyBackend/Controllers/AdminShopOrdersController.cs`
- DTOs: add result type reuse or small request-less POST
- Test: `backend/TummlyBackend.Tests/Services/AdminShopOrderFulfilmentServiceTests.cs` and/or integration

**Interfaces:**
- Produces:

```csharp
Task<AdminShopOrderFulfilmentResult> MarkProductionStartedAsync(
    Guid orderId,
    int actorAdminUserId,
    string actorIdentity,
    CancellationToken cancellationToken = default
);
```

Behaviour:
- Load order; if null → `order_not_found`.
- If `ProductionStartedAtUtc != null` → return Ok (idempotent; do not duplicate audit).
- Else set `ProductionStartedAtUtc = clock`, `ProductionStartedByAdminUserId = actorAdminUserId`, `UpdatedAtUtc`.
- `_audit.Append(new AdminAuditAppendRequest(AdminAuditActions.ShopProductionStarted, actorIdentity, AdminAuditTargetTypes.ShopOrder, orderId.ToString("D"), ActorAdminUserId: actorAdminUserId, RestaurantId: order.RestaurantId, DetailJson: null))`
- `SaveChangesAsync` once.

Controller:

```csharp
[HttpPost("{id:guid}/production-started")]
public async Task<IActionResult> MarkProductionStarted(Guid id, CancellationToken ct)
{
    var staffId = GetStaffId(); // duplicate small helper from AdminController or shared
    ...
}
```

Prefer copy the private `GetStaffId` claim parse into `AdminShopOrdersController` (same claim as AdminController) rather than a large refactor.

Constants:

```csharp
public const string ShopProductionStarted = "shop.production_started";
public const string ShopForceCancel = "shop.force_cancel"; // Task 4
```

- [ ] **Step 1: Failing tests** — stamp set + audit row; second call no second audit; operator cancel then blocked (`canCancel` false / 409).

- [ ] **Step 2: Implement + run PASS**

- [ ] **Step 3: Commit** (only if human asks)

---

### Task 4: Admin force cancel

**Files:**
- Modify: same Admin Shop service/controller/DTOs
- Reuse: `ShopOrderCancelRefund.TryFullRefundAsync`
- Test: force cancel after stamp; `skipRefund`; Revolut fail leaves fulfilment

**Interfaces:**
- Produces:

```csharp
public sealed class AdminShopForceCancelRequest
{
    public string Reason { get; init; } = string.Empty; // reuse ShopCancelReasons slug or free text max 500
    public bool SkipRefund { get; init; }
}

Task<AdminShopOrderFulfilmentResult> ForceCancelAsync(
    Guid orderId,
    AdminShopForceCancelRequest request,
    int actorAdminUserId,
    string actorIdentity,
    CancellationToken cancellationToken = default
);
```

Behaviour:
- Order not found → fail.
- Already cancelled → Ok (idempotent; no second refund).
- Must still be cancellable for fulfilment purposes: allow when `processing` **even if stamped**; reject `in_transit` / `delivered` with existing conflict codes.
- If `!SkipRefund`: `TryFullRefundAsync`; on error return fail without mutate.
- Set fulfilment cancelled + reason + `CancelledAtUtc` + `CancelledByUserId = null` (admin staff is not operator User) — leave `CancelledByUserId` null; put admin id in audit only **or** document using ops notes. Prefer: do not set `CancelledByUserId` (FK to Users); audit carries actor.
- Audit `shop.force_cancel` with DetailJson including `skipRefund`, `refundAttempted`, reason.

Controller: `POST api/admin/shop-orders/{id}/force-cancel` body `{ reason, skipRefund }`.

- [ ] **Step 1–4: TDD as above; PASS**

- [ ] **Step 5: Commit** (only if human asks)

---

### Task 5: Operator + Admin UI

**Files:**
- Modify: `src/components/dashboard/operator/Shop/ShopOrderDetailSidebar.tsx` — add branch for `production_started` message
- Modify: `src/api/adminShopOrdersApi.ts` — `markShopOrderProductionStarted`, `forceCancelShopOrder`
- Modify: `src/components/dashboard/admin/AdminShopOrderDetailDrawer.tsx` — buttons when `processing` and stamp null / stamped

Operator copy:

```ts
order.cancelBlockReason === "production_started"
  ? "Production has started for this order, so it cannot be cancelled here. Contact Tummly support if you need help."
```

Admin drawer (minimal):
- If `fulfilmentStatus === "processing"` && no `productionStartedAtUtc` on list/detail DTO: show **Mark production started**.
- If processing (with or without stamp): show **Force cancel** (confirm dialog; checkbox skip refund optional — default unchecked).

Expose `productionStartedAtUtc` on Admin list/detail DTO from `MapListItem` so the drawer can gate buttons.

- [ ] **Step 1: Wire API + UI**
- [ ] **Step 2: Manual smoke** in browser if QA env available; otherwise rely on API tests
- [ ] **Step 3: Commit** (only if human asks)

---

### Task 6: ADR + docs + smoke

**Files:**
- Modify: `docs/adr/0046-shop-operator-cancel-does-not-initiate-revolut-refund.md`
- Update design status line to point at this plan
- Optional: one line in `docs/product/` Shop section if present

ADR amendment text (replace lead paragraphs):

```markdown
# Shop cancel and Revolut refund (amended 2026-09-21)

**Pre-cutoff** (no `ProductionStartedAtUtc`): operator cancel on
`paymentStatus = paid` and `fulfilmentStatus = processing` calls Revolut
full refund via `RefundOrderAsync`, then sets fulfilment to `cancelled`.
`paymentStatus` stays `paid` until the refund webhook confirms and Tummly
mints a TCN / marks `refunded`.

**Post-cutoff:** operator cancel is blocked. Admin may force-cancel
(audited), with default full Revolut refund unless `skipRefund` is set.

Partials and Merchant-only ops refunds remain outside operator cancel.
```

- [ ] **Step 1: Amend ADR + link from design spec**
- [ ] **Step 2: Focused smoke**

```bash
dotnet test backend/TummlyBackend.Tests/TummlyBackend.Tests.csproj --filter "FullyQualifiedName~ShopOrderCancel|FullyQualifiedName~AdminShopOrder|FullyQualifiedName~ShopOrderCancelRules"
```

Expected: all PASS.

- [ ] **Step 3: Commit** (only if human asks)

---

## Self-review (plan vs spec)

| Spec requirement | Task |
|---|---|
| `ProductionStartedAtUtc` + Admin stamp | 1, 3 |
| Operator cancel gated on null stamp | 1, 2 |
| Pre-cutoff Revolut full refund fail-closed | 2 |
| Complimentary / no Revolut id cancel only | 2 |
| `paymentStatus` waits for webhook | 2 (assert still paid) |
| Admin force cancel + audit + skipRefund | 4 |
| Operator UI block copy | 5 |
| ADR 0046 supersession | 6 |
| Admin light UI | 5 |

No TBD placeholders. Types aligned: idempotency `shop-cancel:{Guid:D}`, audit actions `shop.production_started` / `shop.force_cancel`, target `shop_order`.
