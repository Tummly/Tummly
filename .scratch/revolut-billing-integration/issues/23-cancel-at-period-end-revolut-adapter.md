# 23 — Cancel-at-period-end Revolut adapter

**Kind:** build

**What to build:** Operator cancel records cancel-at-period-end on Tummly and keeps the Revolut subscription active until period end. At period end, Tummly calls Revolut cancel (native immediate API) and ends entitlements on the Tummly clock.

**Blocked by:** 14 — First paid conversion Hosted Payment Page session; 16 — Setup and cycle ORDER_COMPLETED activation

**Status:** ready-for-human

## Parent

[PRD: Revolut billing integration](../PRD.md)

Product locks: [03](./03-saved-payment-methods-versus-tummly-controlled-plan-changes.md). Sister credit-ledger **25** owns Cancel plan slot/fields. If this ticket and a numbered lock disagree, the numbered lock wins. Do not reopen those decisions.

## Out of this ticket

- Immediate cancel on confirm day (not allowed). Soft lock / Dormant auto-cancel (never — see 24). Operator chrome (sister frontend).

- [x] Confirm day: Tummly records cancel-at-period-end; Revolut subscription stays active.
- [x] At period end: call Revolut cancel; sync state; entitlement end follows Tummly clocks / included job cancel apply.
- [x] No Revolut cancel page. Cancel starts only from Tummly `cancel-plan`.
- [x] HTTP tests cover: cancel confirm does not redirect to HPP for cancel. Service tests cover: Revolut cancel invoked at period end only, not on confirm day.

## Answer

Shipped on `feat/settings-billing`.

- Confirm day stays sister **25** `CancelPlanAsync` (slot only). No Revolut cancel and no HPP `redirectUrl`.
- `RevolutCancelAtPeriodEndAdapter` runs from included-period job cancel apply: resolve subscription id via `RevolutSubscriptionCorrelation`, native cancel, close open pay sessions, then clear slot / entitlement end on Tummly clocks. Missing correlation is a no-op (same as cycle-end change-plan). Revolut failure throws before the slot clears so the job can retry.
- Named HTTP: `PostCancelPlan_DoesNotRedirectToHpp_ForCancel`. Service: `RevolutCancelAtPeriodEndAdapterTests` (renewal / before-renewal / sync).

**Commits:** `5222dd47` (implement), `c3e2af14` (shared correlation), `aa4db9ca` (rebase fake markers), plus review sync-state coverage commit on this branch.

## Comments

### Review (post-merge)

- Rebased onto tickets **21** / **22**; kept change-plan fake fields and cancel call counts together.
- Reused `RevolutSubscriptionCorrelation` instead of a private resolve copy.
- Added service assert that cancel closes open pay sessions.
