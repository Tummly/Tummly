# Guest response email delivery: save then async retry

**Guest response email delivery** persists the guest-response fact (and **Recovery offer**, when issued) first, marks email-channel delivery as Pending, then attempts Resend with a background retry until Resend accepts the mail. Confirm Send may succeed while delivery is still Pending. Pending delivery is not an operator-facing status in the current slice.

This slice wires Pending delivery for no-offer Confirm Send intents (`respond_to_guest`, `respond_and_record_internal_action`) via the no-offer **Guest response email** template. `respond_with_recovery_offer` joins the same Pending queue when the with-offer template lands (separate ticket).

This differs from ADR 0005 (trial review emails after commit, not via outbox). Trial review is low-frequency and admin-driven: if the after-commit sync send fails, an admin can **ResendInvite** from `Approved` / `InviteSent`. Guest response email has no equivalent operator resend of the same outbound mail in this slice — the guest must eventually receive the message after Confirm Send succeeds. Guaranteed eventual delivery therefore requires a durable Pending queue and background retry, not sync after-commit alone.

We rejected sync after-commit without retry (ADR 0005 shape) because a Resend blip would leave a recorded guest response with no mail and no recovery path. We rejected blocking Confirm Send on Resend acceptance because the operator UX must complete when the fact is saved. We rejected a product-named “email outbox” table as a second durable concept when Pending on `FeedbackGuestResponse` already carries the work (same durability idea as ADR 0010’s Pending classification queue on Feedback). Channel wake is best-effort; correctness survives process restart via startup sweep and periodic reclaim of Pending rows.

SMS Confirm Send stays fact-only (NotApplicable) — no SMS provider in this effort. **Guest preview send test** stays synchronous and is not queued.

Reopen this ADR if operator-visible delivery status is required, if SMS delivery is added, or if Pending-on-fact proves too heavy and a dedicated delivery table is warranted.
