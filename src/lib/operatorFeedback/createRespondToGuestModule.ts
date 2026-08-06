import type {
  ClassificationStatus,
  ContactType,
  FeedbackDetailsResponse,
  FeedbackSentiment,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import {
  deriveStartRecoveryContactCapability,
  type StartRecoveryContactCapability,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import { mapResponseSetupSummaryChrome } from "@/lib/operatorFeedback/responseSetupPresentation"
import {
  canContinueRespondToGuestMessage,
  canContinueRespondToGuestSetup,
  defaultRespondToGuestChannel,
  emptyRespondToGuestDraft,
  furthestRespondToGuestStep,
  labelForRespondToGuestPurpose,
  labelForRespondToGuestTone,
  maskRespondToGuestDestination,
  availableRespondToGuestChannels,
  type RespondToGuestChannel,
  type RespondToGuestDraft,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
  type RespondToGuestWizardStep,
  type RespondToGuestWriteEntry,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import type { InternalActionCategoryId } from "@/lib/operatorFeedback/internalActionPresentation"
import { GUEST_PREVIEW_SEND_TEST_ERROR } from "@/lib/operatorFeedback/guestPreviewPresentation"

const SEND_ERROR_MESSAGE =
  "Could not send the response. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark this recovery resolved. Please try again."
const AI_DRAFT_ERROR_MESSAGE = "We could not prepare a draft."

export type GuestResponseSentActivityEvent = {
  kind: "guest_response_sent"
  at: string
  actorDisplayName: string | null
  channel: RespondToGuestChannel
  maskedDestination: string
}

export type RecoveryCompletedActivityEvent = {
  kind: "recovery_completed"
  at: string
  actorDisplayName: string | null
  recoveryIntent:
    | "respond_to_guest"
    | "record_internal_action_only"
    | "respond_and_record_internal_action"
    | "respond_with_recovery_offer"
  fromWorkflowStatus: FeedbackWorkflowStatus
  toWorkflowStatus: "resolved"
}

export type SendGuestResponseRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  subject: string | null
  body: string
  intent: "respond_to_guest"
  purpose: RespondToGuestPurposeId
  tone: RespondToGuestToneId
  includeNotes: string | null
}

export type SendGuestResponseResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: GuestResponseSentActivityEvent
}

export type CompleteRecoveryResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: RecoveryCompletedActivityEvent
}

export type PrepareRecoveryDraftMode =
  | "prepare"
  | "rewrite_subject"
  | "rewrite_message"

export type PrepareRecoveryDraftRewriteTarget = "subject" | "message"

export type PrepareRecoveryDraftRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  purpose: RespondToGuestPurposeId
  tone: RespondToGuestToneId
  includeNotes: string | null
  mode: PrepareRecoveryDraftMode
  /** Current editor text — rewrite modes only. */
  currentBody: string | null
  currentSubject: string | null
  /** Optional confirmed internal action (Respond and record intent). */
  confirmedInternalAction?: {
    category: InternalActionCategoryId
    note: string
  } | null
}

export function isPrepareRecoveryDraftRewriteMode(
  mode: PrepareRecoveryDraftMode | null
): boolean {
  return mode === "rewrite_subject" || mode === "rewrite_message"
}

export type PrepareRecoveryDraftResult =
  | {
      status: "succeeded"
      body: string
      subject: string | null
      channel: RespondToGuestChannel
    }
  | {
      status: "failed"
      retryable: boolean
    }

export type RespondToGuestAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  sendGuestResponse: (
    request: SendGuestResponseRequest
  ) => Promise<SendGuestResponseResult>
  sendGuestPreviewTest: (request: {
    feedbackId: number
    subject: string
    body: string
    offer?: {
      title: string
      description: string
      expiryLabel: string
    } | null
  }) => Promise<void>
  completeRecovery: (
    feedbackId: number,
    intent:
      | "respond_to_guest"
      | "record_internal_action_only"
      | "respond_and_record_internal_action"
      | "respond_with_recovery_offer"
  ) => Promise<CompleteRecoveryResult>
  prepareRecoveryDraft: (
    request: PrepareRecoveryDraftRequest,
    signal?: AbortSignal
  ) => Promise<PrepareRecoveryDraftResult>
}

export type RespondToGuestSummary = {
  guestName: string
  contactCapability: StartRecoveryContactCapability
  feedbackComment: string
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  issueTagLabels: string[] | null
  purposeLabel: string | null
  toneLabel: string | null
}

export type RespondToGuestAiDraftStatus = "idle" | "running" | "failed"

export type RespondToGuestSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  feedbackId: number | null
  step: RespondToGuestWizardStep
  headerSubtitle: string | null
  summary: RespondToGuestSummary | null
  availableChannels: RespondToGuestChannel[]
  channel: RespondToGuestChannel | null
  purpose: RespondToGuestPurposeId | null
  tone: RespondToGuestToneId | null
  includeNotes: string
  subject: string
  message: string
  maskedDestination: string | null
  canContinueSetup: boolean
  canContinueWrite: boolean
  writeEntry: RespondToGuestWriteEntry
  aiDraftStatus: RespondToGuestAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  actionsLocked: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  /** Successful AI prepare/rewrite count for this open session. */
  aiActionCount: number
  /** Location chrome for Guest preview — from Feedback details. */
  locationName: string | null
  locationAddress: string | null
  /** Full-screen Guest preview overlay open on Review. */
  guestPreviewOpen: boolean
  sendConfirmOpen: boolean
  sendStatus: "idle" | "saving" | "error"
  sendError: string | null
  sendTestStatus: "idle" | "sending" | "success" | "error"
  sendTestError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  /** Retained after send for Success status rows (draft is cleared). */
  successReceipt: GuestResponseSentActivityEvent | null
}

export type RespondToGuestBackResult = "return-to-shell" | "stayed"

export type RespondToGuestModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RespondToGuestSnapshot
  open: (
    feedbackId: number,
    preloadedDetails?: FeedbackDetailsResponse
  ) => Promise<void>
  /** Persist draft and close → inbox. */
  saveAndExit: () => void
  /** Close without clearing draft (success Keep in progress / X). */
  close: () => void
  back: () => RespondToGuestBackResult
  setChannel: (channel: RespondToGuestChannel) => void
  setPurpose: (purpose: RespondToGuestPurposeId) => void
  setTone: (tone: RespondToGuestToneId) => void
  setIncludeNotes: (value: string) => void
  continueSetup: () => void
  writeManually: () => void
  prepareDraft: () => Promise<void>
  rewriteDraft: (target: PrepareRecoveryDraftRewriteTarget) => Promise<void>
  retryAiDraft: () => Promise<void>
  dismissPreparingOverlay: () => void
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  continueWrite: () => void
  /** Review → Guest response editor (no in-place edit on Review). */
  editText: () => void
  openGuestPreview: () => void
  closeGuestPreview: () => void
  sendGuestPreviewTest: () => Promise<void>
  openSendConfirm: () => void
  cancelSendConfirm: () => void
  confirmSend: () => Promise<void>
  keepInProgress: () => void
  markResolved: () => Promise<void>
}

type SessionState = {
  isOpen: boolean
  loadStatus: RespondToGuestSnapshot["loadStatus"]
  loadError: string | null
  loadGeneration: number
  feedbackId: number | null
  step: RespondToGuestWizardStep
  headerSubtitle: string | null
  summary: RespondToGuestSummary | null
  contactType: ContactType | null
  guestContact: string
  contactCapability: StartRecoveryContactCapability | null
  availableChannels: RespondToGuestChannel[]
  draft: RespondToGuestDraft
  maskedDestination: string | null
  aiDraftStatus: RespondToGuestAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  aiDraftGeneration: number
  aiActionCount: number
  locationName: string | null
  locationAddress: string | null
  guestPreviewOpen: boolean
  sendConfirmOpen: boolean
  sendStatus: RespondToGuestSnapshot["sendStatus"]
  sendError: string | null
  sendTestStatus: RespondToGuestSnapshot["sendTestStatus"]
  sendTestError: string | null
  completeStatus: RespondToGuestSnapshot["completeStatus"]
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  successReceipt: GuestResponseSentActivityEvent | null
}

function emptySession(): SessionState {
  return {
    isOpen: false,
    loadStatus: "idle",
    loadError: null,
    loadGeneration: 0,
    feedbackId: null,
    step: "setup",
    headerSubtitle: null,
    summary: null,
    contactType: null,
    guestContact: "",
    contactCapability: null,
    availableChannels: [],
    draft: emptyRespondToGuestDraft(),
    maskedDestination: null,
    aiDraftStatus: "idle",
    aiDraftMode: null,
    preparingOverlayOpen: false,
    aiDraftError: null,
    aiDraftRetryable: true,
    aiDraftGeneration: 0,
    aiActionCount: 0,
    locationName: null,
    locationAddress: null,
    guestPreviewOpen: false,
    sendConfirmOpen: false,
    sendStatus: "idle",
    sendError: null,
    sendTestStatus: "idle",
    sendTestError: null,
    completeStatus: "idle",
    completeError: null,
    workflowStatus: null,
    successReceipt: null,
  }
}

function parseWorkflowStatus(
  value: FeedbackWorkflowStatus | undefined
): FeedbackWorkflowStatus {
  if (value === "in_progress" || value === "resolved") {
    return value
  }
  return "new"
}

function buildHeaderSubtitle(
  feedbackId: number,
  locationName: string,
  qrSource: string | null | undefined
): string {
  const reference = `FDB-${String(feedbackId).padStart(6, "0")}`
  const location = locationName.trim() || "Location"
  const touchpoint = qrSource?.trim() || "QR"
  return `${reference} · ${location} · ${touchpoint}`
}

function projectSummary(state: SessionState): RespondToGuestSummary | null {
  if (state.summary == null || state.contactCapability == null) {
    return state.summary
  }
  return {
    ...state.summary,
    purposeLabel: labelForRespondToGuestPurpose(state.draft.purpose),
    toneLabel: labelForRespondToGuestTone(state.draft.tone),
  }
}

function toSnapshot(state: SessionState): RespondToGuestSnapshot {
  const draft = state.draft
  const actionsLocked = state.aiDraftStatus === "running"
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    feedbackId: state.feedbackId,
    step: state.step,
    headerSubtitle: state.headerSubtitle,
    summary: projectSummary(state),
    availableChannels: state.availableChannels,
    channel: draft.channel,
    purpose: draft.purpose,
    tone: draft.tone,
    includeNotes: draft.includeNotes,
    subject: draft.subject,
    message: draft.message,
    maskedDestination: state.maskedDestination,
    canContinueSetup: canContinueRespondToGuestSetup(draft),
    canContinueWrite:
      draft.writeEntry === "editor"
      && !actionsLocked
      && canContinueRespondToGuestMessage({
        channel: draft.channel,
        subject: draft.subject,
        message: draft.message,
      }),
    writeEntry: draft.writeEntry,
    aiDraftStatus: state.aiDraftStatus,
    aiDraftMode: state.aiDraftMode,
    preparingOverlayOpen: state.preparingOverlayOpen,
    actionsLocked,
    aiDraftError: state.aiDraftError,
    aiDraftRetryable: state.aiDraftRetryable,
    aiActionCount: state.aiActionCount,
    locationName: state.locationName,
    locationAddress: state.locationAddress,
    guestPreviewOpen: state.guestPreviewOpen,
    sendConfirmOpen: state.sendConfirmOpen,
    sendStatus: state.sendStatus,
    sendError: state.sendError,
    sendTestStatus: state.sendTestStatus,
    sendTestError: state.sendTestError,
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
    successReceipt: state.successReceipt,
  }
}

function cloneDraft(draft: RespondToGuestDraft): RespondToGuestDraft {
  return { ...draft }
}

/**
 * Respond to the guest wizard — setup → Guest response (AI or manual) →
 * review → send → success. Intent-scoped drafts survive Save and exit.
 */
export function createRespondToGuestModule(
  adapters: RespondToGuestAdapters
): RespondToGuestModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  /** Intent-scoped drafts keyed by Feedback id (Respond to the guest only). */
  const draftsByFeedbackId = new Map<number, RespondToGuestDraft>()
  let aiAbortController: AbortController | null = null

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const persistDraftIfComposable = () => {
    if (state.feedbackId == null || state.step === "success") {
      return
    }
    draftsByFeedbackId.set(state.feedbackId, cloneDraft(state.draft))
  }

  const closeSession = () => {
    if (aiAbortController != null) {
      aiAbortController.abort()
      aiAbortController = null
    }
    state = {
      ...emptySession(),
      loadGeneration: state.loadGeneration,
      aiDraftGeneration: state.aiDraftGeneration,
    }
    publish()
  }

  const applyDraftDefaults = (
    response: FeedbackDetailsResponse,
    existing: RespondToGuestDraft | undefined
  ): RespondToGuestDraft => {
    const capability = deriveStartRecoveryContactCapability(
      response.contactType,
      response.guestContact
    )
    if (existing != null) {
      return cloneDraft(existing)
    }
    const draft = emptyRespondToGuestDraft()
    draft.channel = defaultRespondToGuestChannel(capability)
    return draft
  }

  const clearAiDraftUi = () => {
    state = {
      ...state,
      aiDraftStatus: "idle",
      aiDraftMode: null,
      preparingOverlayOpen: false,
      aiDraftError: null,
      aiDraftRetryable: true,
    }
  }

  const runAiDraft = async (mode: PrepareRecoveryDraftMode) => {
    if (
      state.step !== "write"
      || state.feedbackId == null
      || state.draft.channel == null
      || state.draft.purpose == null
      || state.draft.tone == null
      || state.aiDraftStatus === "running"
    ) {
      return
    }

    const feedbackId = state.feedbackId
    const channel = state.draft.channel
    const purpose = state.draft.purpose
    const tone = state.draft.tone
    const includeNotes =
      state.draft.includeNotes.trim() === ""
        ? null
        : state.draft.includeNotes.trim()
    const priorSubject = state.draft.subject
    const priorMessage = state.draft.message
    const generation = ++state.aiDraftGeneration

    if (aiAbortController != null) {
      aiAbortController.abort()
    }
    const controller = new AbortController()
    aiAbortController = controller

    const isRewrite = isPrepareRecoveryDraftRewriteMode(mode)

    state = {
      ...state,
      aiDraftStatus: "running",
      aiDraftMode: mode,
      preparingOverlayOpen: mode === "prepare",
      aiDraftError: null,
      aiDraftRetryable: true,
    }
    publish()

    const request: PrepareRecoveryDraftRequest = {
      feedbackId,
      channel,
      purpose,
      tone,
      includeNotes,
      mode,
      currentBody: isRewrite ? priorMessage : null,
      currentSubject: isRewrite && channel === "email" ? priorSubject : null,
    }

    try {
      const result = await adapters.prepareRecoveryDraft(
        request,
        controller.signal
      )

      if (
        generation !== state.aiDraftGeneration
        || controller.signal.aborted
      ) {
        return
      }

      if (result.status === "succeeded") {
        let nextSubject = priorSubject
        let nextMessage = priorMessage
        if (mode === "prepare") {
          nextSubject =
            channel === "email" ? (result.subject ?? "").trim() : ""
          nextMessage = result.body
        } else if (mode === "rewrite_subject") {
          nextSubject =
            channel === "email" ? (result.subject ?? "").trim() : ""
          nextMessage = priorMessage
        } else {
          nextSubject = priorSubject
          nextMessage = result.body
        }
        state = {
          ...state,
          draft: {
            ...state.draft,
            subject: nextSubject,
            message: nextMessage,
            writeEntry: "editor",
            messageComplete: false,
          },
          aiDraftStatus: "idle",
          aiDraftMode: null,
          preparingOverlayOpen: false,
          aiDraftError: null,
          aiDraftRetryable: true,
          aiActionCount: state.aiActionCount + 1,
        }
        publish()
        return
      }

      state = {
        ...state,
        draft: {
          ...state.draft,
          subject: mode === "prepare" ? "" : priorSubject,
          message: mode === "prepare" ? "" : priorMessage,
        },
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: AI_DRAFT_ERROR_MESSAGE,
        aiDraftRetryable: result.retryable,
      }
      publish()
    } catch (error) {
      if (
        generation !== state.aiDraftGeneration
        || controller.signal.aborted
        || (error instanceof DOMException && error.name === "AbortError")
      ) {
        return
      }

      state = {
        ...state,
        draft: {
          ...state.draft,
          subject: mode === "prepare" ? "" : priorSubject,
          message: mode === "prepare" ? "" : priorMessage,
        },
        aiDraftStatus: "failed",
        preparingOverlayOpen: false,
        aiDraftError: AI_DRAFT_ERROR_MESSAGE,
        aiDraftRetryable: true,
      }
      publish()
    } finally {
      if (aiAbortController === controller) {
        aiAbortController = null
      }
    }
  }

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    async open(feedbackId, preloadedDetails) {
      const generation = ++state.loadGeneration
      const existingDraft = draftsByFeedbackId.get(feedbackId)

      if (aiAbortController != null) {
        aiAbortController.abort()
        aiAbortController = null
      }

      const applyLoaded = (response: FeedbackDetailsResponse) => {
        const capability = deriveStartRecoveryContactCapability(
          response.contactType,
          response.guestContact
        )
        const draft = applyDraftDefaults(response, existingDraft)
        const step = furthestRespondToGuestStep(draft)
        const maskedDestination = maskRespondToGuestDestination(
          response.contactType,
          response.guestContact
        )

        state = {
          ...state,
          loadStatus: "loaded",
          loadError: null,
          feedbackId: response.id,
          step,
          headerSubtitle: buildHeaderSubtitle(
            response.id,
            response.locationName,
            response.qrSource
          ),
          summary: {
            guestName: response.guestName,
            contactCapability: capability,
            feedbackComment: response.comment,
            ...mapResponseSetupSummaryChrome(response, capability),
            purposeLabel: labelForRespondToGuestPurpose(draft.purpose),
            toneLabel: labelForRespondToGuestTone(draft.tone),
          },
          contactType: response.contactType,
          guestContact: response.guestContact,
          contactCapability: capability,
          availableChannels: availableRespondToGuestChannels(capability),
          draft,
          maskedDestination,
          locationName: response.locationName,
          locationAddress: response.address,
          workflowStatus: parseWorkflowStatus(response.workflowStatus),
        }
      }

      if (
        preloadedDetails != null
        && preloadedDetails.id === feedbackId
      ) {
        state = {
          ...emptySession(),
          loadGeneration: generation,
          aiDraftGeneration: state.aiDraftGeneration,
          isOpen: true,
          feedbackId,
        }
        applyLoaded(preloadedDetails)
        publish()
        return
      }

      state = {
        ...emptySession(),
        loadGeneration: generation,
        aiDraftGeneration: state.aiDraftGeneration,
        isOpen: true,
        loadStatus: "loading",
        feedbackId,
      }
      publish()

      try {
        const response = await adapters.getFeedbackDetails(feedbackId)
        if (generation !== state.loadGeneration) {
          return
        }
        applyLoaded(response)
        publish()
      } catch {
        if (generation !== state.loadGeneration) {
          return
        }
        state = {
          ...state,
          loadStatus: "error",
          loadError: "Could not load recovery. Please try again.",
        }
        publish()
      }
    },
    saveAndExit() {
      if (state.aiDraftStatus === "running") {
        return
      }
      persistDraftIfComposable()
      closeSession()
    },
    close() {
      closeSession()
    },
    back() {
      if (state.aiDraftStatus === "running") {
        return "stayed"
      }
      if (state.step === "setup") {
        persistDraftIfComposable()
        closeSession()
        return "return-to-shell"
      }
      if (state.step === "write") {
        state = {
          ...state,
          step: "setup",
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        clearAiDraftUi()
        publish()
        return "stayed"
      }
      if (state.step === "review") {
        state = {
          ...state,
          step: "write",
          draft: { ...state.draft, writeEntry: "editor" },
          guestPreviewOpen: false,
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        publish()
        return "stayed"
      }
      return "stayed"
    },
    setChannel(channel) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, channel, setupComplete: false },
      }
      publish()
    },
    setPurpose(purpose) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, purpose, setupComplete: false },
      }
      publish()
    },
    setTone(tone) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, tone, setupComplete: false },
      }
      publish()
    },
    setIncludeNotes(value) {
      if (state.step !== "setup" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, includeNotes: value },
      }
      publish()
    },
    continueSetup() {
      if (
        state.step !== "setup"
        || state.aiDraftStatus === "running"
        || !canContinueRespondToGuestSetup(state.draft)
      ) {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          setupComplete: true,
          writeEntry:
            state.draft.writeEntry === "editor" ? "editor" : "chooser",
        },
        step: "write",
      }
      clearAiDraftUi()
      publish()
    },
    writeManually() {
      if (state.step !== "write") {
        return
      }
      if (state.aiDraftStatus === "running") {
        state.aiDraftGeneration += 1
        if (aiAbortController != null) {
          aiAbortController.abort()
          aiAbortController = null
        }
      }
      state = {
        ...state,
        draft: { ...state.draft, writeEntry: "editor" },
        aiDraftStatus: "idle",
        aiDraftMode: null,
        preparingOverlayOpen: false,
        aiDraftError: null,
        aiDraftRetryable: true,
      }
      publish()
    },
    async prepareDraft() {
      await runAiDraft("prepare")
    },
    async rewriteDraft(target) {
      if (state.draft.writeEntry !== "editor") {
        return
      }
      if (target === "subject") {
        if (state.draft.channel !== "email") {
          return
        }
        await runAiDraft("rewrite_subject")
        return
      }
      await runAiDraft("rewrite_message")
    },
    async retryAiDraft() {
      if (
        state.aiDraftStatus !== "failed"
        || !state.aiDraftRetryable
        || state.aiDraftMode == null
      ) {
        return
      }
      await runAiDraft(state.aiDraftMode)
    },
    dismissPreparingOverlay() {
      if (!state.preparingOverlayOpen) {
        return
      }
      state = {
        ...state,
        preparingOverlayOpen: false,
      }
      publish()
    },
    setSubject(value) {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, subject: value, messageComplete: false },
        aiDraftStatus: "idle",
        aiDraftError: null,
      }
      publish()
    },
    setMessage(value) {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, message: value, messageComplete: false },
        aiDraftStatus: "idle",
        aiDraftError: null,
      }
      publish()
    },
    continueWrite() {
      if (
        state.step !== "write"
        || state.draft.writeEntry !== "editor"
        || state.aiDraftStatus === "running"
        || !canContinueRespondToGuestMessage({
          channel: state.draft.channel,
          subject: state.draft.subject,
          message: state.draft.message,
        })
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, messageComplete: true },
        step: "review",
      }
      clearAiDraftUi()
      publish()
    },
    editText() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        step: "write",
        draft: { ...state.draft, writeEntry: "editor", messageComplete: false },
        guestPreviewOpen: false,
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    openGuestPreview() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        guestPreviewOpen: true,
      }
      publish()
    },
    closeGuestPreview() {
      if (!state.guestPreviewOpen) {
        return
      }
      state = {
        ...state,
        guestPreviewOpen: false,
      }
      publish()
    },
    async sendGuestPreviewTest() {
      if (
        state.feedbackId == null
        || state.draft.channel !== "email"
        || state.step !== "review"
        || state.sendTestStatus === "sending"
        || state.aiDraftStatus === "running"
      ) {
        return
      }

      const subject = state.draft.subject.trim()
      const body = state.draft.message.trim()
      if (subject === "" || body === "") {
        return
      }

      const feedbackId = state.feedbackId
      state = {
        ...state,
        sendTestStatus: "sending",
        sendTestError: null,
      }
      publish()

      try {
        await adapters.sendGuestPreviewTest({
          feedbackId,
          subject,
          body,
        })
        state = {
          ...state,
          sendTestStatus: "success",
          sendTestError: null,
        }
        publish()
      } catch {
        state = {
          ...state,
          sendTestStatus: "error",
          sendTestError: GUEST_PREVIEW_SEND_TEST_ERROR,
        }
        publish()
      }
    },
    openSendConfirm() {
      if (state.step !== "review" || state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        sendConfirmOpen: true,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    cancelSendConfirm() {
      state = {
        ...state,
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      publish()
    },
    async confirmSend() {
      if (
        state.feedbackId == null
        || state.draft.channel == null
        || state.draft.purpose == null
        || state.draft.tone == null
        || (state.step !== "review" && !state.sendConfirmOpen)
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const purpose = state.draft.purpose
      const tone = state.draft.tone
      const subject =
        channel === "email" ? state.draft.subject.trim() : null
      const body = state.draft.message.trim()
      const includeNotes =
        state.draft.includeNotes.trim() === ""
          ? null
          : state.draft.includeNotes.trim()

      // Allow retry from confirm after failure without re-opening.
      if (state.step === "review" && !state.sendConfirmOpen) {
        state = { ...state, sendConfirmOpen: true }
      }

      state = {
        ...state,
        sendConfirmOpen: true,
        sendStatus: "saving",
        sendError: null,
      }
      publish()

      const request: SendGuestResponseRequest = {
        feedbackId,
        channel,
        subject,
        body,
        intent: "respond_to_guest",
        purpose,
        tone,
        includeNotes,
      }

      try {
        const result = await adapters.sendGuestResponse(request)
        draftsByFeedbackId.delete(feedbackId)
        state = {
          ...state,
          step: "success",
          guestPreviewOpen: false,
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
          workflowStatus: result.workflowStatus,
          successReceipt: result.activityEvent,
          maskedDestination:
            result.activityEvent.maskedDestination
            || state.maskedDestination,
          draft: emptyRespondToGuestDraft(),
        }
        publish()
      } catch {
        state = {
          ...state,
          step: "review",
          sendConfirmOpen: true,
          sendStatus: "error",
          sendError: SEND_ERROR_MESSAGE,
        }
        publish()
      }
    },
    keepInProgress() {
      if (state.step !== "success") {
        return
      }
      closeSession()
    },
    async markResolved() {
      if (state.step !== "success" || state.feedbackId == null) {
        return
      }
      const feedbackId = state.feedbackId
      state = {
        ...state,
        completeStatus: "saving",
        completeError: null,
      }
      publish()

      try {
        await adapters.completeRecovery(feedbackId, "respond_to_guest")
        closeSession()
      } catch {
        state = {
          ...state,
          completeStatus: "error",
          completeError: COMPLETE_ERROR_MESSAGE,
        }
        publish()
      }
    },
  }
}
