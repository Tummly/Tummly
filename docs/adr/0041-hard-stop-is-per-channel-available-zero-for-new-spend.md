# Hard stop is per-channel Available=0 for new spend

One account-level pool per channel. Operator remaining, **Hard stop**, and a new reserve use **Available credits**. Every `reservation`, `consumption`, and `release` stamps `LocationId`; allocations do not. Location usage is used + held on that shared pool — there is no per-Location remaining. Chrome and the 80/90/100 evaluator read a snapshot; spend still recomputes inside `UPDLOCK`. Fire with an open hold is credit-clear: treating remaining=0 as a fire block would Release a full-pool Campaign (ticket **04**).

We rejected per-Location pools: pack `shared_account_pool` is account-wide. We rejected blocking the holding Campaign at remaining 0. We rejected a cached remaining column as source of truth (ADR 0036).

Product lock: `.scratch/credit-ledger-backend/issues/09-out-of-credit-hard-stop-and-location-attribution.md`.
