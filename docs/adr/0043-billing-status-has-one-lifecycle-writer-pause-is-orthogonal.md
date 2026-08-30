# Billing status has one lifecycle writer; Pause is orthogonal

Tummly **Billing status** and lock clocks live on `BillingAccounts` and are written only by the **Billing Account lifecycle** module. Revolut adapters call its commands; they do not write the enum. **Pause workspace** stays `Restaurant.WorkspaceStatus` and never writes billing fields, so clocks and mint keep running. **Activation expired** no longer Blocks Sign-in: unpaid **Pilot** becomes **Soft lock**. Chargeback is an overlay, not a **Billing status**.

We rejected two writers (`ActivationGate` plus Revolut handlers): they race. We rejected deriving **Billing status** at read time: the operator snapshot is persisted (ADR 0034). We rejected Pause freezing billing: ticket **06** already mints from **Billing status** only. We rejected folding chargeback into **Soft lock**: that would lie on **Plan & subscription** and steal the dunning restoration CTA.

Product lock: `.scratch/credit-ledger-backend/issues/10-soft-lock-and-dormant-account-lifecycle.md`.
