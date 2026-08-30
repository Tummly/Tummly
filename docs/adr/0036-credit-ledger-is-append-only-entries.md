# Credit ledger is one append-only entries table

Credit grants, holds, burns, and corrections for a **Billing Account** live in one INSERT-only table `CreditLedgerEntries`. An allocation-class row **is** the **Credit allocation**. **Available credits** are computed (Remaining − Held, then zeroed when `ExpiresAtUtc` has passed). Concurrent writes take `UPDLOCK` on the `BillingAccounts` row in a short READ COMMITTED transaction so two Campaigns cannot spend the same units.

We rejected a mutable remaining column or a cached pool row as source of truth: it would drift from the journal and could go negative. We rejected binding slices at settle: concurrent reserves could over-commit one allocation. We rejected `SERIALIZABLE` without an explicit account lock: ticket **01** already named **Billing Account** as the lock target.

Product lock: `.scratch/credit-ledger-backend/issues/03-three-pool-ledger-allocations-and-consumption-order.md`.
