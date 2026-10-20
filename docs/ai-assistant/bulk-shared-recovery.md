# Bulk shared recovery (Assistant)

## Intent

When the operator prepares recovery for the **needs-attention Feedback set** (including the Prepare recovery CTA), the Assistant shows a **count**, one **shared draft**, and asks for edits — then opens **Respond to guest → Review and send** in bulk mode to confirm and **Send to all N**.

This is an intentional client exception to billing-pack prose that said “No bulk recovery use”. Persistence remains **one guest-response fact per Feedback**; a **`BulkRecoveryBatch`** aggregate holds the shared template and member rows.

## Behaviour

- **Set / plural / “all” prepare** → create `BulkRecoveryBatch` (Draft) + shared template with `{{guest_first_name}}`; Assistant CTAs: **Review recovery** (`open-bulk-recovery`) and optional **Send to all N**.
- **Review recovery** opens Feedback Review and send in bulk mode (`batchId` on the recovery draft payload): N guests, N destinations, **Send to all N**.
- **Named or numbered one guest** → existing single prepare + Review recovery (`open-recovery`).
- **AI prepare** bills once for the shared draft.
- **Send** (wizard or Assistant shortcut) calls `POST .../bulk-recovery-batches/{id}/send`, substitutes `{{guest_first_name}}` per member, loops `IFeedbackGuestResponsesService.SendAsync`, reports partial success on the batch.
- **SMS / email delivery costs** stay per recipient (see ADR 0038 for SMS reserves).

## Key types

- `BulkRecoveryBatch` / `BulkRecoveryBatchMember` (SQL)
- `AssistantRecoveryWorkState.BulkShared`, `FeedbackIds`, `BatchId`
- Actions `open-bulk-recovery`, `send-bulk-recovery`
