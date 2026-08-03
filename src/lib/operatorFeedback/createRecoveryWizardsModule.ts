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

/**
 * Shared Start recovery orchestration (entry shell + the four recovery
 * wizards) so any page (Feedback, Guests, Guest Profile) can offer the same
 * end-to-end recovery path from a `feedbackId`, not just the Feedback inbox.
 */
export type RecoveryWizardsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  setWorkflowStatus: StartRecoveryEntryAdapters["setWorkflowStatus"]
  sendGuestResponse: RespondToGuestAdapters["sendGuestResponse"]
  completeRecovery: RespondToGuestAdapters["completeRecovery"]
  prepareRecoveryDraft: RespondToGuestAdapters["prepareRecoveryDraft"]
  recordInternalAction: RecordInternalActionAdapters["recordInternalAction"]
  sendAndRecord: RespondAndRecordAdapters["sendAndRecord"]
  sendAndIssueRecoveryOffer: RespondWithRecoveryOfferAdapters["sendAndIssueRecoveryOffer"]
  prepareRecoveryOfferDraft: RespondWithRecoveryOfferAdapters["prepareRecoveryDraft"]
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
    // Each wizard module declares its own narrower `CompleteRecoveryResult`
    // locally (pre-existing duplication tracked by ticket 23); the runtime
    // value always satisfies every module's shape.
    completeRecovery:
      adapters.completeRecovery as RespondAndRecordAdapters["completeRecovery"],
    prepareRecoveryDraft: adapters.prepareRecoveryDraft,
  })

  const respondWithRecoveryOffer = createRespondWithRecoveryOfferModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    sendAndIssueRecoveryOffer: adapters.sendAndIssueRecoveryOffer,
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
    back: () => TResult
  ): () => TResult {
    return () => {
      const feedbackId = getFeedbackId()
      const result = back()
      if (result === "return-to-shell" && feedbackId != null) {
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
      respondToGuest.back
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
      recordInternalAction.back
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
      respondAndRecord.back
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
      respondWithRecoveryOffer.back
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
      const selected = startRecovery.selectIntent(intentId)
      if (!selected) {
        return false
      }
      const feedbackId = startRecovery.getSnapshot().feedbackId
      if (feedbackId == null) {
        return true
      }
      if (intentId === "respond-to-guest") {
        void respondToGuest.open(feedbackId)
      }
      if (intentId === "record-internal-action-only") {
        void recordInternalAction.open(feedbackId)
      }
      if (intentId === "respond-and-record-internal-action") {
        void respondAndRecord.open(feedbackId)
      }
      if (intentId === "respond-with-recovery-offer") {
        void respondWithRecoveryOffer.open(feedbackId)
      }
      return true
    },
    respondToGuest: respondToGuestWrapped,
    recordInternalAction: recordInternalActionWrapped,
    respondAndRecord: respondAndRecordWrapped,
    respondWithRecoveryOffer: respondWithRecoveryOfferWrapped,
  }
}
