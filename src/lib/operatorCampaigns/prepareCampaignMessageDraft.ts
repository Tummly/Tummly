import { isAxiosError } from "axios"

import { prepareCampaignMessageDraft as prepareCampaignMessageDraftApi } from "@/api/dashboardApi"
import type {
  PrepareCampaignMessageDraftRequest,
  PrepareCampaignMessageDraftResult,
} from "@/lib/operatorCampaigns/createCampaignWizardModule"
import type { PrepareCampaignMessageDraftApiRequest } from "@/types/operatorCampaigns"

const PREPARE_CAMPAIGN_MESSAGE_DRAFT_TIMEOUT_MS = 60_000

/**
 * HTTP + timeout wiring for POST /campaigns/message-draft.
 * Maps API envelopes to the wizard module result shape (ticket 33).
 */
export async function prepareCampaignMessageDraft(
  request: PrepareCampaignMessageDraftRequest,
  signal?: AbortSignal
): Promise<PrepareCampaignMessageDraftResult> {
  const timeoutController = new AbortController()
  const timeoutId = setTimeout(() => {
    timeoutController.abort()
  }, PREPARE_CAMPAIGN_MESSAGE_DRAFT_TIMEOUT_MS)
  const onOuterAbort = () => {
    timeoutController.abort()
  }
  signal?.addEventListener("abort", onOuterAbort)

  const body: PrepareCampaignMessageDraftApiRequest = {
    locationId: request.locationId,
    channel: request.channel,
    goalId: request.goalId,
    audienceKey: request.audienceKey,
    offerStance: request.offerStance,
    campaignName: request.campaignName,
    tone: request.tone,
    includeNotes: request.includeNotes,
    mode: request.mode,
    currentBody: request.currentBody,
    currentSubject: request.currentSubject,
  }

  try {
    const result = await prepareCampaignMessageDraftApi(
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
      const data = error.response.data as { retryable?: boolean } | undefined
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
