# Billed AI consumes on success; Recovery SMS reserves

Billed **AI action**s (Assistant answer, recovery/campaign **Prepare**, regenerate) check **Available credits**, call Azure with no hold, then write `consumption` only if a usable output can be returned. Recovery SMS opens a **Credit reservation** at Confirm/Send, then settles accepted Twilio segments. We rejected reserve-first for AI: a 60s hold is worse than a wasted Azure call. We rejected consume-on-success for Recovery SMS: Twilio money has left and ticket **03** forbids a negative pool. We rejected `ICampaignBillingReserve` for recovery: that seam requires `CampaignId`. Pack prose that billed operator recommendation cards is superseded by pack JSON and existing Home-free locks.

Product lock: `.scratch/credit-ledger-backend/issues/05-ai-recovery-and-non-campaign-burn-points.md`.
