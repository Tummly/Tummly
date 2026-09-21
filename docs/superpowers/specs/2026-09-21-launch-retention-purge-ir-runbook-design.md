# Launch Retention purge + Incident-response runbook

**Status:** Design approved (chat 2026-09-21); awaiting user review of this file before plan/build  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive  
**Clears P0 (remaining):** Retention implementation + incident-response runbook. Admin/support audit already shipped.

## Problem

- Soft lock → Dormant clocks exist. Billing pack: **Dormant retention = 90 days**. CONTEXT: 90-day retention is **not** a further billing status.
- No job runs after `DormantEnteredAt + 90 days` to remove guest personal data.
- No incident-response runbook file (support playbooks list escalation placeholders only).
- Admin audit is already shipped (`AdminAuditEvents` + list API).

## Goals

1. After 90 days of continuous Dormant, hard-delete guest personal data for that restaurant (Location Guests), stamp completion, audit once.
2. Ship a usable IR runbook (phases + first-30-minutes checklist) under product docs.

## Non-goals

- Hard-delete restaurant / operators / billing / Shop / Campaigns / Offers
- Anonymise-in-place (blank/hash) instead of hard delete
- Scrub Feedback name/contact snapshots
- Automated paging / status page / containment feature flags beyond documentation
- UK data residency (separate ops P0)
- Cookies / accessibility evidence pack (separate P0)

## Locked decisions

| Decision | Choice |
|----------|--------|
| Scope | Guest PII purge **and** IR runbook in one plan |
| Purge target | Guest PII only (Location Guests + orphan Master Guests) |
| Delete style | Reuse operator hard-delete cascade (Feedback unlink; snapshots stay) |
| Eligibility | Still `Dormant` + `DormantEnteredAt + 90d` + not yet stamped |
| Restore | Leaving Dormant clears clocks **and** `GuestRetentionPurgedAtUtc` so a later Dormant episode can purge again |
| Idempotency | `BillingAccount.GuestRetentionPurgedAtUtc` while still Dormant |
| Runner | `BackgroundService` (daily poll) |
| Audit | One `retention.guest_purge` `AdminAuditEvent` per restaurant |
| IR | `docs/product/incident-response.md` with phases + 30-min checklist |
| Architecture | `IGuestRetentionPurgeService` + shared internal delete core |

## Design

### Eligibility

A Billing Account is eligible when all of:

1. `BillingStatus == Dormant`
2. `DormantEnteredAt != null`
3. `nowUtc >= DormantEnteredAt + GuestRetentionDays` (`GuestRetentionDays = 90`)
4. `GuestRetentionPurgedAtUtc == null`

Restore (paid activate / Extend activation / dunning recover) clears Dormant clocks **and** the purge stamp, so a later Dormant episode can become eligible again.

### Data model

| Column | Type | Notes |
|--------|------|-------|
| `BillingAccount.GuestRetentionPurgedAtUtc` | `DateTime?` | Set when guest purge for that restaurant completes (including zero guests). Cleared whenever Billing status leaves Dormant (paid activate, Extend activation, dunning recover, and any other restore that clears `DormantEnteredAt`). |

EF migration required. No backfill.

### Delete semantics

Extract a shared **internal** hard-delete helper used by:

- `LocationGuestDeleteService` (after Owned-location authz — behaviour unchanged)
- Retention purge (restaurant-scoped; no operator user id)

Cascade matches today:

- Remove Location Guest notes, tags, activity events, permission ledger entries
- Unlink Feedback (`LocationGuestId = null`); **Feedback PII snapshots remain**
- Remove Location Guest; remove Master Guest if no other Location Guests remain
- Assistant conversation quotes unchanged

Purge covers all Location Guests under locations belonging to the restaurant. Do **not** delete operators, Restaurant, Locations, BillingAccount (except stamp), Shop orders, Campaigns, Offers, or credit ledger.

### Job

**`IGuestRetentionPurgeService`**

- `ProcessOnceAsync(DateTime nowUtc, CancellationToken)` — select eligible accounts (cap per run: 25)
- Per restaurant, **one** DB transaction:
  1. Fresh-read Billing Account; re-check eligibility; abort if no longer eligible
  2. Hard-delete each Location Guest via shared core (tracked deletes; no intermediate save)
  3. Set `GuestRetentionPurgedAtUtc = nowUtc`
  4. Append `AdminAuditEvent` with:
     - `Action = retention.guest_purge`
     - `ActorIdentity = system:retention`
     - `ActorAdminUserId = null`
     - `TargetType = restaurant`
     - `TargetId = {restaurantId}`
     - `RestaurantId = restaurantId`
     - `DetailJson` includes `guestsDeleted` count
  5. Single `SaveChanges` + commit

Empty guest set still stamps and audits (marks done).

Per-restaurant failure: log, roll back that restaurant, continue the batch.

**Restore paths must clear the stamp** wherever they clear `DormantEnteredAt` (at minimum: `ActivatePaidPlanAsync`, `ExtendPilotActivationAsync`, `RecoverDunningAsync`).

**`GuestRetentionPurgeBackgroundService`**

- Hosted BackgroundService; poll every 24 hours; skip when environment is `Testing` (same pattern as Included-period)
- Calls `ProcessOnceAsync` with clock UTC now

### Audit constant

Add `AdminAuditActions.RetentionGuestPurge = "retention.guest_purge"`.

Readable via existing `GET /api/admin/audit-events`. No new UI.

### Incident-response runbook

**New file:** `docs/product/incident-response.md`

Must include:

1. Purpose / scope (security & availability affecting production personal data or Guest Loop availability)
2. Severity P1 / P2 / P3 aligned with `support-playbooks.md`
3. Roles (Incident Lead, Comms, Engineering) with contact placeholders
4. First 30 minutes checklist (detect → declare → contain → preserve evidence → notify)
5. Phases: Detect → Contain → Eradicate → Recover → Post-mortem (short template)
6. Evidence sources: logs / App Insights; admin audit API; provider dashboards; approved DB path only
7. Personal-data incidents: ICO assessment reminder + legal contact placeholder

**Doc updates**

- Link from `docs/product/support-playbooks.md` Related documentation; note IR shipped
- Update `docs/product/security-and-rbac.md` launch-blocker row when purge + IR land

### Testing

- Unit: eligibility (too early / restored / already stamped / due)
- Unit: restore paths clear `GuestRetentionPurgedAtUtc`
- Unit/service: purge deletes guests, stamps, audits; empty restaurant stamps; fail does not stamp
- Unit: operator `LocationGuestDeleteService` still passes existing tests after extract
- Optional: BackgroundService skips Testing env
- Docs: IR file present and linked

## Out of scope

- Cookies / a11y evidence
- UK residency
- Guest data export/deletion audit beyond the one retention event
- Live on-call tooling
