# Feedback AI classification: delayed auto-requeue of retryable Failed

ADR-0010 keeps **Pending** as the durable work queue and a product lifecycle of `Pending → Succeeded | Failed` with no operator-visible Retrying/Processing. That made every writable provider failure **terminal Failed**, which is right for unsupported language / invalid model output, but wrong for infra blips (missing deployment, 5xx, 429, timeouts, claim exhaustion): operators see permanent Failed and the worker never looks again. We add **delayed auto-requeue** for **retryable** Failed only: after backoff, flip **Failed → Pending**, reset soft-claim fields, publish the thin Feedback/Home signal, and reuse the existing Pending claim path. During the wait the drawer stays **Failed**; a brief **Pending** empty state during the live attempt is accepted. No new product status, no separate outbox.

**Retryable Failed:** Azure OpenAI HTTP 5xx / timeout / 429 / 401 / 403 / 404; claim-budget exhaustion; opaque provider `Failed` with no stronger non-retryable signal. **Not retryable:** unsupported-language / invalid-output paths the provider already treated as non-transient; mapping/code bugs persisted as Failed. Retry metadata on the Feedback row (`ClassificationRetryable`, `ClassificationRetryAfter`, `ClassificationDelayedReopenCount`) is implementation detail like claim columns — not API lifecycle.

**Schedule:** initial delay 5 minutes; exponential ×2 capped at 1 hour between delayed reopens; max **5** delayed Failed→Pending cycles, then Failed stays permanent for auto-requeue. Same classification hosted loop’s ~30s sweep reopens due rows (and wakes Pending work as today).

**Backfill:** existing Failed rows at ship time become retryable and due (reopen count 0) so QA infra failures recover without manual SQL.

We rejected auto-requeue of all Failed without a retryable bit (permanent cases burn tokens), a Retrying status (glossary/UI churn), classifying in place while status stays Failed (duplicates ADR-0010 claim/lease), a separate requeue outbox (second durable concept), and in-memory-only timers (lost on restart). Operator correct-classification remains Succeeded-only and is unaffected.

Amends ADR-0010’s “Failed is always forever” failure policy for retryable infra paths only; Pending remains the sole work queue. Reopen if reopen caps prove too aggressive/lenient, if we need operator-visible retry progress, or if failure reasons must be product-facing.
