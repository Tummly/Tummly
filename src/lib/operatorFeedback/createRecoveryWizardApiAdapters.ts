import { isAxiosError } from "axios"

import {
  completeFeedbackRecovery,
  createCatalogOffer,
  getCatalogOfferById,
  getFeedbackDetails,
  getFeedbackRecoveryOfferAttach,
  prepareFeedbackRecoveryDraft,
  recordFeedbackInternalAction,
  respondAndRecordInternalAction,
  sendAndIssueFeedbackRecoveryOffer,
  sendFeedbackGuestResponse,
  sendGuestPreviewTest as sendGuestPreviewTestApi,
  setFeedbackRecoveryOfferAttach,
  setFeedbackWorkflowStatus,
  updateCatalogOffer,
} from "@/api/dashboardApi"
import { labelForInternalActionCategory } from "@/lib/operatorFeedback/internalActionPresentation"
import type {
  PrepareRecoveryDraftResult,
} from "@/lib/operatorFeedback/createRespondToGuestModule"
import type { RecoveryWizardsAdapters } from "@/lib/operatorFeedback/createRecoveryWizardsModule"

/**
 * The Start recovery entry shell + four recovery wizards need the same API
 * wiring wherever they are mounted (Feedback inbox, Guests, Guest Profile).
 * `onMutated` is deliberately excluded — that hook is page-specific (each
 * host page decides what to refresh once a wizard resolves the feedback).
 */
export function createRecoveryWizardApiAdapters(): Omit<
  RecoveryWizardsAdapters,
  "onMutated"
> {
  return {
    getFeedbackDetails,
    setWorkflowStatus: async (feedbackId, workflowStatus) => {
      const result = await setFeedbackWorkflowStatus(
        feedbackId,
        workflowStatus
      )
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        activityEvent: result.activityEvent ?? null,
      }
    },
    getRecoveryOfferAttach: async (feedbackId) => {
      const result = await getFeedbackRecoveryOfferAttach(feedbackId)
      return result.offerId
    },
    setRecoveryOfferAttach: async (feedbackId, offerId) => {
      await setFeedbackRecoveryOfferAttach(feedbackId, offerId)
    },
    createOffer: async (body) => {
      const response = await createCatalogOffer(body)
      if (!response.success || response.offer == null) {
        throw new Error("Offers catalog create failed.")
      }
      return response.offer
    },
    updateOffer: async (offerId, body) => {
      const response = await updateCatalogOffer(offerId, body)
      if (!response.success || response.offer == null) {
        throw new Error("Offers catalog update failed.")
      }
      return response.offer
    },
    getOffer: async (offerId) => {
      const response = await getCatalogOfferById(offerId)
      if (!response.success || response.offer == null) {
        throw new Error("Offers catalog load failed.")
      }
      return response.offer
    },
    sendGuestResponse: async (request) => {
      const result = await sendFeedbackGuestResponse(request.feedbackId, {
        channel: request.channel,
        subject: request.subject,
        body: request.body,
        intent: request.intent,
        purpose: request.purpose,
        tone: request.tone,
        includeNotes: request.includeNotes,
      })
      const activity = result.activityEvent
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        activityEvent: {
          kind: "guest_response_sent",
          at: activity.at,
          actorDisplayName: activity.actorDisplayName ?? null,
          channel: activity.channel ?? request.channel,
          maskedDestination: activity.maskedDestination ?? "",
        },
      }
    },
    sendGuestPreviewTest: async (request) => {
      await sendGuestPreviewTestApi(request.feedbackId, {
        subject: request.subject,
        body: request.body,
        offer: request.offer ?? null,
      })
    },
    completeRecovery: async (feedbackId, intent) => {
      const result = await completeFeedbackRecovery(feedbackId, { intent })
      const activity = result.activityEvent
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        activityEvent: {
          kind: "recovery_completed",
          at: activity.at,
          actorDisplayName: activity.actorDisplayName ?? null,
          recoveryIntent: activity.recoveryIntent ?? intent,
          fromWorkflowStatus: activity.fromWorkflowStatus ?? "in_progress",
          toWorkflowStatus: "resolved",
        },
      }
    },
    recordInternalAction: async (request) => {
      const result = await recordFeedbackInternalAction(request.feedbackId, {
        category: request.category,
        note: request.note,
        intent: request.intent,
      })
      const activity = result.activityEvent
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        activityEvent: {
          kind: "internal_action_recorded",
          at: activity.at,
          actorDisplayName: activity.actorDisplayName ?? null,
          category: (activity.category
            ?? request.category) as typeof request.category,
          categoryLabel:
            activity.categoryLabel
            ?? labelForInternalActionCategory(request.category)
            ?? request.category,
          note: activity.note ?? request.note,
        },
      }
    },
    sendAndRecord: async (request) => {
      const result = await respondAndRecordInternalAction(request.feedbackId, {
        channel: request.channel,
        subject: request.subject,
        body: request.body,
        intent: request.intent,
        purpose: request.purpose,
        tone: request.tone,
        includeNotes: request.includeNotes,
        category: request.category,
        note: request.note,
      })
      const guestActivity = result.guestResponseActivityEvent
      const internalActivity = result.internalActionActivityEvent
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        guestResponseActivityEvent: {
          kind: "guest_response_sent",
          at: guestActivity.at,
          actorDisplayName: guestActivity.actorDisplayName ?? null,
          channel: guestActivity.channel ?? request.channel,
          maskedDestination: guestActivity.maskedDestination ?? "",
        },
        internalActionActivityEvent: {
          kind: "internal_action_recorded",
          at: internalActivity.at,
          actorDisplayName: internalActivity.actorDisplayName ?? null,
          category: (internalActivity.category
            ?? request.category) as typeof request.category,
          categoryLabel:
            internalActivity.categoryLabel
            ?? labelForInternalActionCategory(request.category)
            ?? request.category,
          note: internalActivity.note ?? request.note,
        },
      }
    },
    sendAndIssueRecoveryOffer: async (request) => {
      const result = await sendAndIssueFeedbackRecoveryOffer(
        request.feedbackId,
        {
          channel: request.channel,
          subject: request.subject,
          body: request.body,
          intent: request.intent,
          purpose: request.purpose,
          tone: request.tone,
          includeNotes: request.includeNotes,
          offer: request.offer,
        }
      )
      const guestActivity = result.activityEvent
      const offerActivity = result.recoveryOfferActivityEvent
      return {
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        guestResponseActivityEvent: {
          kind: "guest_response_sent",
          at: guestActivity.at,
          actorDisplayName: guestActivity.actorDisplayName ?? null,
          channel: guestActivity.channel ?? request.channel,
          maskedDestination: guestActivity.maskedDestination ?? "",
        },
        recoveryOfferActivityEvent: {
          kind: "recovery_offer_issued",
          at: offerActivity.at,
          actorDisplayName: offerActivity.actorDisplayName ?? null,
          offerType:
            (offerActivity.offerType as typeof request.offer.offerType)
            ?? request.offer.offerType,
          title: offerActivity.offerTitle ?? request.offer.title,
          validity:
            (offerActivity.offerValidity as typeof request.offer.validity)
            ?? request.offer.validity,
          expiryAt: offerActivity.offerExpiryAt ?? null,
          redemptionCode:
            offerActivity.redemptionCode
            ?? result.recoveryOffer.redemptionCode,
        },
        issuedOffer: {
          title: result.recoveryOffer.title,
          redemptionCode: result.recoveryOffer.redemptionCode,
          expiryAt: result.recoveryOffer.expiryAt,
          validity:
            result.recoveryOffer.validity as typeof request.offer.validity,
        },
      }
    },
    prepareRecoveryOfferDraft: async (request, signal) =>
      prepareRecoveryDraftViaApi(
        request.feedbackId,
        {
          channel: request.channel,
          purpose: request.purpose,
          tone: request.tone,
          includeNotes: request.includeNotes,
          mode: request.mode,
          currentBody: request.currentBody,
          currentSubject: request.currentSubject,
          confirmedOffer: request.confirmedOffer,
        },
        signal
      ),
    prepareRecoveryDraft: async (request, signal) =>
      prepareRecoveryDraftViaApi(
        request.feedbackId,
        {
          channel: request.channel,
          purpose: request.purpose,
          tone: request.tone,
          includeNotes: request.includeNotes,
          mode: request.mode,
          currentBody: request.currentBody,
          currentSubject: request.currentSubject,
          confirmedInternalActionCategory:
            request.confirmedInternalAction?.category ?? null,
          confirmedInternalActionNote:
            request.confirmedInternalAction?.note ?? null,
        },
        signal
      ),
  }
}

async function prepareRecoveryDraftViaApi(
  feedbackId: number,
  body: Parameters<typeof prepareFeedbackRecoveryDraft>[1],
  signal?: AbortSignal
): Promise<PrepareRecoveryDraftResult> {
  const timeoutMs = 60_000
  const timeoutController = new AbortController()
  const timeoutId = window.setTimeout(() => {
    timeoutController.abort()
  }, timeoutMs)
  const onOuterAbort = () => {
    timeoutController.abort()
  }
  signal?.addEventListener("abort", onOuterAbort)
  try {
    const result = await prepareFeedbackRecoveryDraft(
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
      const data = error.response.data as { retryable?: boolean } | undefined
      return {
        status: "failed",
        retryable: data?.retryable !== false,
      }
    }
    return { status: "failed", retryable: true }
  } finally {
    window.clearTimeout(timeoutId)
    signal?.removeEventListener("abort", onOuterAbort)
  }
}
