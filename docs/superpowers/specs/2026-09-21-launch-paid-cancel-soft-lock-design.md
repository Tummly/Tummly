# Launch Paid Cancel — Soft lock → Dormant progression

**Status:** Design approved (chat 2026-09-21); plan at `docs/superpowers/plans/2026-09-21-launch-paid-cancel-soft-lock.md`  
**Authority:** Tummly Launch Reconciliation — Engineering Build Directive  
**Clears P0:** Deterministic post-renewal-end access / write / export state after paid **Cancel plan** (Soft lock → Dormant). Retention purge stays a separate P0.

## Problem

Operator **Cancel plan** already schedules Revolut cancel at cycle end and the included-period job clears the slot at renewal (`cancel_applied`). That path does **not** enter Soft lock or Dormant.

Soft lock → Dormant clocks and write/export gates already exist for **unpaid Pilot** and **dunning**. Voluntary paid cancel never joins that progression, so post-cancel access state is undefined relative to launch truth.

## Locked decisions

| Topic | Choice |
|-------|--------|
| Post-`cancel_applied` state | Demote to unpaid **Pilot Soft lock**; reuse Pilot clocks |
| Scope vs Retention P0 | Soft lock → Dormant + existing gates only; **no** 90-day purge here |
| Credits | Soft-lock gates block new spend; balances stay until normal expiry; Dormant reuses Pilot leftover / hold release |
| Entitlements | Set plan/cycle/clocks; leave `PaidExtraLocationCount` and Owned Locations unchanged |
| Notices | Reuse unpaid-Pilot Soft lock / Dormant notice path |
| Writer | New lifecycle command (ADR 0043 — one Billing status writer) |

## 1. State at `cancel_applied`

When the included-period job applies a scheduled cancel (`ScheduledCancelPlan` and `now ≥ RenewalDateUtc`):

1. Clear the scheduled-change slot (existing behaviour).
2. Call lifecycle `ApplyPostCancelSoftLock(billingAccount, renewalEndUtc)` on the account already locked in the mint transaction (no nested UPDLOCK; same pattern as scheduled plan-change apply).
3. Lifecycle sets:
   - `SubscriptionPlan = Pilot`
   - `BillingCycle = null`
   - `BillingStatus = Soft lock`
   - `PilotPeriodEnd = SoftLockEnteredAt = renewalEndUtc`
   - Clear open dunning fields (`DunningEpisodeStartedAt`, `DunningFiredSteps`, `DunningOutstandingOrderId`)
   - Leave `PaidExtraLocationCount`, Owned Locations, and credit ledger rows unchanged
   - Reset `PilotSoftLockNotified` / `PilotDormantNotified` to false so notices can fire
4. Return skip reason `cancel_applied` (no included mint).
5. Existing Soft lock gates apply immediately (paid writes, export, Shop non-restoration, campaigns, recovery sends).
6. Later `TickAsync` → `AdvanceUnpaidPilot` advances Soft lock → Dormant at `PilotPeriodEnd + PilotDormantHours` (15 days). On Dormant entry, existing Pilot leftover expiry / hold release still runs.
7. Restoration: existing **Choose a plan** / first-paid conversion ends Soft lock via `ActivatePaidPlanAsync` (unchanged).

## 2. Wiring + surfaces

### Lifecycle (ADR 0043)

- Add sync `ApplyPostCancelSoftLock(BillingAccount, DateTime renewalEndUtc)` on `IBillingAccountLifecycle` / `BillingAccountLifecycleService`.
- Only this command writes Soft lock / Pilot demotion fields on the cancel-apply path.
- Idempotent: if `BillingStatus` is already Soft lock or Dormant **and** `SubscriptionPlan` is Pilot **and** `SoftLockEnteredAt` is set, return NoOp without changing clocks.

### Mint apply path

- `IncludedPeriodMintService` cancel branch: clear slot → `CancelNativeSubscriptionAsync` when a Revolut subscription is correlated → `ApplyPostCancelSoftLock` → finish `cancel_applied`.
- Operator confirm already schedules cancel-at-cycle-end via `CycleEndPlanCancelService`; do not redesign that schedule path. Native cancel on apply closes the gap where `_revolutCancel` is injected but unused today.

### Gates / UI

- No new Soft lock evaluator rules — reuse `OperatorBillingLockGate` and guest QR Soft lock / Dormant behaviour.
- Manage plan / Lock Alert Soft lock + Choose a plan remain; no new Legal/FAQ copy in this P0.

### Notices

- Reuse Pilot Soft lock / Dormant notifier via **Tick only** (`EmitPilotLock` / `EmitPilotDormant`). `ApplyPostCancelSoftLock` does not send mail; it resets notice flags so the next `TickAsync` fires Soft lock once, then Dormant later.

## 3. Tests

- Lifecycle: enter Soft lock → Pilot + Soft lock + clocks; clears dunning; idempotent re-entry.
- Mint job: scheduled cancel at/after renewal → slot cleared, Soft lock entered, `cancel_applied`, no included mint.
- Soft lock gates: paid write / export still denied after cancel apply.
- Tick: Soft lock → Dormant at `PilotPeriodEnd + 15d`; Pilot leftover expiry on Dormant entry still runs.
- Restore: first paid / `ActivatePaidPlan` clears Soft lock clocks and returns Active (regression).
- Revolut: if apply invokes native cancel, assert adapter once when subscription correlated.

## Out of scope

- 90-day dormant purge / anonymise (Retention P0)
- Incident-response runbook / immutable admin audit (Retention P0)
- Soft lock allow/block matrix changes
- Archiving excess Owned Locations on demotion
- Shop cutoff / refund
- New cancel-specific email copy

## Success

After paid Cancel plan reaches renewal end, the account is Soft-locked with Pilot clocks; write/export/Shop/campaign gates match Soft lock; Dormant follows on the Pilot timer; operators can restore via Choose a plan. Evidence is tests + status fields on apply — not a purge job.
