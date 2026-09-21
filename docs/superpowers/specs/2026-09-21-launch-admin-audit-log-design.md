# Launch Admin / support audit log

**Status:** Design approved (chat 2026-09-21); plan at `docs/superpowers/plans/2026-09-21-launch-admin-audit-log.md`  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive (Retention/security P0 — admin/support audit slice)  
**Clears:** Material admin/support audit coverage for covered Admin writes. Retention purge and IR runbook stay separate slices.

## Problem

Product docs mark an **immutable admin audit log** as Planned. Today only partial trial metadata exists (`TrialRequests.ReviewedBy` / `ReviewedAt` / reason fields). Extend activation, QA purge, staff credit adjust/reverse, and payment refunds leave no append-only staff trail.

`RestaurantBillingActivity` is operator-facing billing timeline — not a staff audit.

## Locked decisions

| Topic | Choice |
|-------|--------|
| Retention P0 slice | **B** — immutable admin/support audit (not 90-day purge, not IR runbook) |
| Actions covered | Trial approve / decline / request-more-info / resend-invite; extend activation; purge trial; credit adjust; credit reverse; payment refund |
| Storage | New append-only `AdminAuditEvents` table |
| Read surface | Admin list API only — no Admin UI page |
| Fail behaviour | Fail closed: audit row in the same DB save as the action |
| Trial review fields | Dual-write: keep `ReviewedBy` / `ReviewedAt` / reason fields **and** append audit |
| Wiring | `IAdminAuditService` called from service paths (not controller-only) |

## 1. Data model

**Table:** `AdminAuditEvents` (append-only; no update/delete API)

| Column | Notes |
|--------|--------|
| `Id` | Guid PK |
| `OccurredAtUtc` | Clock time |
| `Action` | Stable string (see Action catalogue) |
| `ActorAdminUserId` | int? — JWT staff id when known |
| `ActorIdentity` | string — email / display (same idea as trial `ReviewedBy`) |
| `TargetType` | e.g. `trial_request`, `operator_user`, `restaurant`, `payment_order` |
| `TargetId` | string (int or Revolut order id as text) |
| `RestaurantId` | int? when known |
| `DetailJson` | Short JSON: reason snippet, qty/channel/direction, amount minor, idempotency key — **no secrets / no full PII dumps** |
| `Succeeded` | bool — always `true` for rows this slice writes (append only after the action succeeds; reserved if failed-attempt logging is added later) |

**Action catalogue**

| Action | Target |
|--------|--------|
| `trial.approve` | `trial_request` |
| `trial.decline` | `trial_request` |
| `trial.request_more_info` | `trial_request` |
| `trial.resend_invite` | `trial_request` |
| `trial.purge` | `trial_request` |
| `operator.extend_activation` | `operator_user` |
| `credit.adjust` | `restaurant` |
| `credit.reverse` | `restaurant` (detail includes reversed entry id) |
| `payment.refund` | `payment_order` |

**Immutability (launch):** App-only — no UPDATE/DELETE endpoints; no EF cascade delete from parents. DB triggers optional later (out of scope).

**Dual-write:** `TrialRequests.ReviewedBy` / `ReviewedAt` / decline / more-info message fields stay as today.

## 2. Write path + list API

### Service

- `IAdminAuditService.Append(...)` adds a tracked row on the shared `ApplicationDbContext` (no nested save).
- `ListAsync(filters, paging)` for Admin GET.
- Register scoped in `Program.cs`.

### Call sites (same transaction as the action save)

| Action | Where append runs |
|--------|-------------------|
| Trial approve / decline / more-info / resend | `TrialReviewTransition` before `SaveChangesAsync`. Extend `TrialReviewContext` with optional `ActorAdminUserId` so JWT staff id can be stored. |
| Extend activation | `AdminService.ExtendActivationAsync` before save — extend signature to accept `ActorAdminUserId` + `ActorIdentity` from the controller |
| Purge trial | `AdminService.PurgeTrialRequestAsync` — same actor args; append **before** deletes in the same transaction |
| Credit adjust / reverse | `CreditLedgerService` staff paths before their save (actor already on request); resolve `ActorIdentity` from Admin row by id when needed |
| Payment refund | On successful refund-order id write (completion save) — fail closed for that save. Idempotent replay that returns an existing completed refund does **not** append a second event |

Scheduled invite-reminder transitions that use a system identity still append with that identity (actor id null).

### List API

- `GET /api/admin/audit-events?action=&restaurantId=&from=&to=&skip=&take=`
- Admin role only; newest-first; hard cap on `take` (100).
- No new Admin UI page.

### Docs

- Flip status in `docs/product/admin.md` and `docs/product/security-and-rbac.md`: Admin audit log → Shipped (list API; no UI).

## 3. Tests

- Unit: `AdminAuditService` append + list filters / take cap.
- `TrialReviewTransition`: approve (and one other decision) creates `AdminAuditEvent` with correct `Action` / actor / target; trial `ReviewedBy` still set.
- Extend + purge: one event each; purge event remains after trial row delete (no FK cascade onto audit).
- Credit adjust + reverse: event on success; no event when ledger refuses.
- Payment refund: event when refund order id is saved; none on early fail.
- Integration: `GET /api/admin/audit-events` Admin-only; operator JWT → 403; filter by `action` / `restaurantId`.

## Out of scope

- Admin audit UI page
- Print-materials ensure/retry
- Sign-in history beyond existing device email
- Guest data export / deletion audit
- DB UPDATE/DELETE immutability triggers
- 90-day Dormant purge / anonymise (Retention slice A)
- Incident-response runbook (Retention slice C)
- Backfill of historical admin actions
- Shop cutoff / refund; UK data residency; cookies / a11y QA pack
