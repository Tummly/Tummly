# Campaign credit reservation opens at schedule commit

A **Campaign** opens a **Credit reservation** at **Campaign schedule commit**, Resume, and Retry remaining. Fire revalidates drop-only, submits, settles provider-accepted units, and releases unused. `ICampaignBillingReserve.IsLive` is true only when production DI registers an adapter that writes `CreditLedgerEntries`.

We rejected reserve-at-fire: schedule-later would hold no credits, and ticket **03** forbids an account lock during provider send. We rejected a residual hold on **Partially sent**: Pause already releases, and Retry remaining would double-hold. Pack unused-release on partial send matches a new Reserve on reviewed retry.

Product lock: `.scratch/credit-ledger-backend/issues/04-campaign-reserve-settle-and-release.md`.
