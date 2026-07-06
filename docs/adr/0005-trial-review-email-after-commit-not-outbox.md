# Trial review emails dispatch after commit, not via outbox

The **Trial review transition** module persists the status change (and token rotation on Approve / ResendInvite) in one transaction, then dispatches the email through `IEmailService` after `SaveChangesAsync` succeeds. We rejected the outbox pattern for this flow.

The accepted tradeoff: if email send fails after a successful commit, the database holds the new **Trial review status** (and a freshly rotated **Operator Setup invitation** token) but the operator received nothing. The operator-visible failure is recoverable — an admin can **ResendInvite** from the `Approved` or `InviteSent` status, which re-runs the same transition and dispatches a fresh email. Trial review is a low-frequency, admin-driven flow (a handful of actions per day), so the operational cost of an occasional missed email is small and the recovery path is built into the same module.

We rejected the outbox pattern (a dedicated email-outbox table written in the same transaction, with a background sender retrying dispatch) because it adds a table, a sender loop, and retry/dedup logic for a flow that does not need guaranteed delivery. The admin can see the request is in `Approved` / `InviteSent` state and resend; there is no silent data loss.

We rejected keeping the current interleaving — `UpdateTrialStatusAsync` emails *before* save (so a save failure emails an un-persisted state), and `SendOperatorSetupInvitationAsync` saves *before* email (so an email failure leaves a rotated token the operator never saw, with no signal to the caller). Both directions have worse failure modes than after-commit: after-commit at least guarantees the email reflects persisted state.

Reopen this ADR if email reliability becomes a real problem, if a flow is added that requires guaranteed delivery (e.g. a regulatory notification), or if the resend-recovery path proves insufficient in practice.
