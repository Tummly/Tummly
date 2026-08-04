import { isAxiosError } from "axios"

import { prepareFeedbackRecoveryDraft as prepareFeedbackRecoveryDraftApi } from "@/api/dashboardApi"
import type { PrepareRecoveryDraftResult } from "@/lib/operatorFeedback/createRespondToGuestModule"
import type { PrepareFeedbackRecoveryDraftRequest } from "@/types/dashboard"

const PREPARE_RECOVERY_DRAFT_TIMEOUT_MS = 60_000

/**
 * Shared HTTP + timeout/abort/retry wiring for the recovery-draft prepare
 * endpoint. Used by both the guest-message ("respond to guest") and
 * recovery-offer draft adapters, which differ only in the
 * confirmed-offer vs confirmed-internal-action fields on the request body.
 */
export async function prepareRecoveryDraft(
  feedbackId: number,
  body: PrepareFeedbackRecoveryDraftRequest,
  signal?: AbortSignal
): Promise<PrepareRecoveryDraftResult> {
  const timeoutController = new AbortController()
  const timeoutId = setTimeout(() => {
    timeoutController.abort()
  }, PREPARE_RECOVERY_DRAFT_TIMEOUT_MS)
  const onOuterAbort = () => {
    timeoutController.abort()
  }
  signal?.addEventListener("abort", onOuterAbort)
  try {
    const result = await prepareFeedbackRecoveryDraftApi(
      feedbackId,
      body,
      timeoutController.signal
    )
    if (!result.success || result.body == null || result.channel == null) {
      return {
        status: "failed",
        retryable: result.retryable !== false,
      }
    }
    return {
      status: "succeeded",
      body: result.body,
      subject: result.subject ?? null,
      channel: result.channel,
    }
  } catch (error) {
    if (signal?.aborted) {
      throw error
    }
    if (isAxiosError(error) && error.code === "ERR_CANCELED") {
      if (signal?.aborted) {
        throw error
      }
      return { status: "failed", retryable: true }
    }
    if (isAxiosError(error) && error.response?.status === 502) {
      const data = error.response.data as
        | { retryable?: boolean }
        | undefined
      return {
        status: "failed",
        retryable: data?.retryable !== false,
      }
    }
    return { status: "failed", retryable: true }
  } finally {
    clearTimeout(timeoutId)
    signal?.removeEventListener("abort", onOuterAbort)
  }
}
