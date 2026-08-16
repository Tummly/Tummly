import type { FeedbackDetailsResponse } from "@/types/dashboard"
import {
  createStartRecoveryEntryModule,
  type StartRecoveryEntryAdapters,
  type StartRecoveryEntrySnapshot,
} from "@/lib/operatorFeedback/createStartRecoveryEntryModule"
import {
  createRespondToGuestModule,
  type RespondToGuestAdapters,
  type RespondToGuestModule,
  type RespondToGuestSnapshot,
} from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  createRecordInternalActionModule,
  type RecordInternalActionAdapters,
  type RecordInternalActionModule,
  type RecordInternalActionSnapshot,
} from "@/lib/operatorFeedback/createRecordInternalActionModule"
import {
  createRespondAndRecordInternalActionModule,
  type RespondAndRecordAdapters,
  type RespondAndRecordModule,
  type RespondAndRecordSnapshot,
} from "@/lib/operatorFeedback/createRespondAndRecordInternalActionModule"
import {
  createRespondWithRecoveryOfferModule,
  type RespondWithRecoveryOfferAdapters,
  type RespondWithRecoveryOfferModule,
  type RespondWithRecoveryOfferSnapshot,
} from "@/lib/operatorFeedback/createRespondWithRecoveryOfferModule"
import type { StartRecoveryIntentId } from "@/lib/operatorFeedback/startRecoveryPresentation"
import type { RecoveryDraftActionPayload } from "@/lib/operatorFeedback/recoveryDraftAction"
import type {
  RespondToGuestChannel,
  RespondToGuestPurposeId,
  RespondToGuestToneId,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import type { InternalActionCategoryId } from "@/lib/operatorFeedback/internalActionPresentation"

/**
 * Shared Start recovery orchestration (entry shell + the four recovery
 * wizards) so any page (Feedback, Guests, Guest Profile) can offer the same
 * end-to-end recovery path from a `feedbackId`, not just the Feedback inbox.
 */
export type RecoveryWizardsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  setWorkflowStatus: StartRecoveryEntryAdapters["setWorkflowStatus"]
  getRecoveryOfferAttach: RespondWithRecoveryOfferAdapters["getRecoveryOfferAttach"]
  setRecoveryOfferAttach: RespondWithRecoveryOfferAdapters["setRecoveryOfferAttach"]
  sendGuestResponse: RespondToGuestAdapters["sendGuestResponse"]
  sendGuestPreviewTest: RespondToGuestAdapters["sendGuestPreviewTest"]
  completeRecovery: RespondToGuestAdapters["completeRecovery"]
  prepareRecoveryDraft: RespondToGuestAdapters["prepareRecoveryDraft"]
  recordInternalAction: RecordInternalActionAdapters["recordInternalAction"]
  sendAndRecord: RespondAndRecordAdapters["sendAndRecord"]
  sendAndIssueRecoveryOffer: RespondWithRecoveryOfferAdapters["sendAndIssueRecoveryOffer"]
  prepareRecoveryOfferDraft: RespondWithRecoveryOfferAdapters["prepareRecoveryDraft"]
  createOffer?: RespondWithRecoveryOfferAdapters["createOffer"]
  getOffer?: RespondWithRecoveryOfferAdapters["getOffer"]
  updateOffer?: RespondWithRecoveryOfferAdapters["updateOffer"]
  listCatalogOffers?: RespondWithRecoveryOfferAdapters["listCatalogOffers"]
  getLocationId?: () => number | null
  /**
   * Called after any wizard action that can change the underlying
   * feedback's workflow status or attention state (save and exit, close,
   * keep in progress, mark resolved), so the host page can refresh its own
   * data (list rows, guest details, etc).
   */
  onMutated?: () => void | Promise<void>
}

export type RecoveryWizardsSnapshot = {
  startRecovery: StartRecoveryEntrySnapshot
  respondToGuest: RespondToGuestSnapshot
  recordInternalAction: RecordInternalActionSnapshot
  respondAndRecord: RespondAndRecordSnapshot
  respondWithRecoveryOffer: RespondWithRecoveryOfferSnapshot
}

export type RecoveryWizardsModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RecoveryWizardsSnapshot
  openStartRecovery: (feedbackId: number) => Promise<void>
  closeStartRecovery: () => void
  retryStartRecovery: () => Promise<void>
  selectStartRecoveryIntent: (intentId: StartRecoveryIntentId) => boolean
  openFromDraftAction: (payload: RecoveryDraftActionPayload) => Promise<void>
  respondToGuest: RespondToGuestModule
  recordInternalAction: RecordInternalActionModule
  respondAndRecord: RespondAndRecordModule
  respondWithRecoveryOffer: RespondWithRecoveryOfferModule
}

export function createRecoveryWizardsModule(
  adapters: RecoveryWizardsAdapters
): RecoveryWizardsModule {
  const startRecovery = createStartRecoveryEntryModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    setWorkflowStatus: adapters.setWorkflowStatus,
  })

  const respondToGuest = createRespondToGuestModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    sendGuestResponse: adapters.sendGuestResponse,
    sendGuestPreviewTest: adapters.sendGuestPreviewTest,
    completeRecovery: adapters.completeRecovery,
    prepareRecoveryDraft: adapters.prepareRecoveryDraft,
  })

  const recordInternalAction = createRecordInternalActionModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    recordInternalAction: adapters.recordInternalAction,
    completeRecovery: adapters.completeRecovery,
  })

  const respondAndRecord = createRespondAndRecordInternalActionModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    sendAndRecord: adapters.sendAndRecord,
    sendGuestPreviewTest: adapters.sendGuestPreviewTest,
    // Each wizard module declares its own narrower `CompleteRecoveryResult`
    // locally (pre-existing duplication tracked by ticket 23); the runtime
    // value always satisfies every module's shape.
    completeRecovery:
      adapters.completeRecovery as RespondAndRecordAdapters["completeRecovery"],
    prepareRecoveryDraft: adapters.prepareRecoveryDraft,
  })

  const respondWithRecoveryOffer = createRespondWithRecoveryOfferModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    getRecoveryOfferAttach: adapters.getRecoveryOfferAttach,
    setRecoveryOfferAttach: adapters.setRecoveryOfferAttach,
    getLocationId: adapters.getLocationId,
    createOffer: adapters.createOffer,
    getOffer: adapters.getOffer,
    updateOffer: adapters.updateOffer,
    listCatalogOffers: adapters.listCatalogOffers,
    sendAndIssueRecoveryOffer: adapters.sendAndIssueRecoveryOffer,
    sendGuestPreviewTest: adapters.sendGuestPreviewTest,
    completeRecovery: adapters.completeRecovery,
    prepareRecoveryDraft: adapters.prepareRecoveryOfferDraft,
  })

  const listeners = new Set<() => void>()
  const publish = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  startRecovery.subscribe(publish)
  respondToGuest.subscribe(publish)
  recordInternalAction.subscribe(publish)
  respondAndRecord.subscribe(publish)
  respondWithRecoveryOffer.subscribe(publish)

  const notifyMutated = async () => {
    await adapters.onMutated?.()
  }

  function wrapBack<TResult extends "return-to-shell" | "stayed">(
    getFeedbackId: () => number | null,
    back: () => TResult,
    isDraftActionSession: () => boolean
  ): () => TResult {
    return () => {
      const feedbackId = getFeedbackId()
      const fromDraftAction = isDraftActionSession()
      const result = back()
      if (
        result === "return-to-shell"
        && feedbackId != null
        && !fromDraftAction
      ) {
        void startRecovery.open(feedbackId)
      }
      return result
    }
  }

  const respondToGuestWrapped: RespondToGuestModule = {
    ...respondToGuest,
    saveAndExit: () => {
      respondToGuest.saveAndExit()
      void notifyMutated()
    },
    close: () => {
      respondToGuest.close()
      void notifyMutated()
    },
    back: wrapBack(
      () => respondToGuest.getSnapshot().feedbackId,
      respondToGuest.back,
      () => respondToGuest.getSnapshot().openedFromDraftAction
    ),
    keepInProgress: () => {
      respondToGuest.keepInProgress()
      void notifyMutated()
    },
    markResolved: async () => {
      await respondToGuest.markResolved()
      await notifyMutated()
    },
  }

  const recordInternalActionWrapped: RecordInternalActionModule = {
    ...recordInternalAction,
    saveAndExit: () => {
      recordInternalAction.saveAndExit()
      void notifyMutated()
    },
    close: () => {
      recordInternalAction.close()
      void notifyMutated()
    },
    back: wrapBack(
      () => recordInternalAction.getSnapshot().feedbackId,
      recordInternalAction.back,
      () => recordInternalAction.getSnapshot().openedFromDraftAction
    ),
    keepInProgress: () => {
      recordInternalAction.keepInProgress()
      void notifyMutated()
    },
    markResolved: async () => {
      await recordInternalAction.markResolved()
      await notifyMutated()
    },
  }

  const respondAndRecordWrapped: RespondAndRecordModule = {
    ...respondAndRecord,
    saveAndExit: () => {
      respondAndRecord.saveAndExit()
      void notifyMutated()
    },
    close: () => {
      respondAndRecord.close()
      void notifyMutated()
    },
    back: wrapBack(
      () => respondAndRecord.getSnapshot().feedbackId,
      respondAndRecord.back,
      () => respondAndRecord.getSnapshot().openedFromDraftAction
    ),
    keepInProgress: () => {
      respondAndRecord.keepInProgress()
      void notifyMutated()
    },
    markResolved: async () => {
      await respondAndRecord.markResolved()
      await notifyMutated()
    },
  }

  const respondWithRecoveryOfferWrapped: RespondWithRecoveryOfferModule = {
    ...respondWithRecoveryOffer,
    saveAndExit: () => {
      respondWithRecoveryOffer.saveAndExit()
      void notifyMutated()
    },
    close: () => {
      respondWithRecoveryOffer.close()
      void notifyMutated()
    },
    back: wrapBack(
      () => respondWithRecoveryOffer.getSnapshot().feedbackId,
      respondWithRecoveryOffer.back,
      () => respondWithRecoveryOffer.getSnapshot().openedFromDraftAction
    ),
    keepInProgress: () => {
      respondWithRecoveryOffer.keepInProgress()
      void notifyMutated()
    },
    markResolved: async () => {
      await respondWithRecoveryOffer.markResolved()
      await notifyMutated()
    },
  }

  return {
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot: () => ({
      startRecovery: startRecovery.getSnapshot(),
      respondToGuest: respondToGuest.getSnapshot(),
      recordInternalAction: recordInternalAction.getSnapshot(),
      respondAndRecord: respondAndRecord.getSnapshot(),
      respondWithRecoveryOffer: respondWithRecoveryOffer.getSnapshot(),
    }),
    openStartRecovery: (feedbackId) => startRecovery.open(feedbackId),
    closeStartRecovery: () => {
      startRecovery.close()
    },
    retryStartRecovery: () => startRecovery.retry(),
    selectStartRecoveryIntent: (intentId) => {
      const details = startRecovery.getLoadedDetails()
      const selected = startRecovery.selectIntent(intentId)
      if (!selected) {
        return false
      }
      const feedbackId = startRecovery.getSnapshot().feedbackId
      if (feedbackId == null) {
        return true
      }
      if (intentId === "respond-to-guest") {
        void respondToGuest.open(feedbackId, details ?? undefined)
      }
      if (intentId === "record-internal-action-only") {
        void recordInternalAction.open(feedbackId, details ?? undefined)
      }
      if (intentId === "respond-and-record-internal-action") {
        void respondAndRecord.open(feedbackId, details ?? undefined)
      }
      if (intentId === "respond-with-recovery-offer") {
        void respondWithRecoveryOffer.open(feedbackId, details ?? undefined)
      }
      return true
    },
    async openFromDraftAction(payload) {
      startRecovery.close()
      if (payload.intent === "respond-to-guest") {
        await respondToGuest.openFromDraftAction({
          feedbackId: payload.feedbackId,
          channel: payload.channel as RespondToGuestChannel,
          purpose: payload.purpose as RespondToGuestPurposeId,
          tone: payload.tone as RespondToGuestToneId,
          includeNotes: payload.includeNotes ?? "",
          subject: payload.subject ?? "",
          message: payload.message ?? "",
        })
        return
      }
      if (payload.intent === "respond-and-record-internal-action") {
        await respondAndRecord.openFromDraftAction({
          feedbackId: payload.feedbackId,
          channel: payload.channel as RespondToGuestChannel,
          purpose: payload.purpose as RespondToGuestPurposeId,
          tone: payload.tone as RespondToGuestToneId,
          includeNotes: payload.includeNotes ?? "",
          subject: payload.subject ?? "",
          message: payload.message ?? "",
          category: payload.category as InternalActionCategoryId,
          note: payload.note ?? "",
        })
        return
      }
      if (payload.intent === "record-internal-action-only") {
        await recordInternalAction.openFromDraftAction({
          feedbackId: payload.feedbackId,
          category: payload.category as InternalActionCategoryId,
          note: payload.note ?? "",
        })
        return
      }
      if (payload.intent === "respond-with-recovery-offer") {
        if (payload.offerId == null) {
          throw new Error("Recovery offer draft requires offerId")
        }
        await respondWithRecoveryOffer.openFromDraftAction({
          feedbackId: payload.feedbackId,
          channel: payload.channel as RespondToGuestChannel,
          tone: payload.tone as RespondToGuestToneId,
          includeNotes: payload.includeNotes ?? "",
          subject: payload.subject ?? "",
          message: payload.message ?? "",
          offerId: payload.offerId,
        })
      }
    },
    respondToGuest: respondToGuestWrapped,
    recordInternalAction: recordInternalActionWrapped,
    respondAndRecord: respondAndRecordWrapped,
    respondWithRecoveryOffer: respondWithRecoveryOfferWrapped,
  }
}
