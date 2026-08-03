import type {
  ContactType,
  FeedbackDetailsResponse,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import {
  deriveStartRecoveryContactCapability,
  type StartRecoveryContactCapability,
} from "@/lib/operatorFeedback/startRecoveryPresentation"
import {
  INTERNAL_ACTION_FOLLOW_UP_STATE_LABEL,
  INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL,
  INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL,
  INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL,
  canContinueRespondAndRecordRecorder,
  labelForInternalActionCategory,
  type InternalActionCategoryId,
} from "@/lib/operatorFeedback/internalActionPresentation"
import {
  canContinueRespondToGuestMessage,
  canContinueRespondToGuestSetup,
  defaultRespondToGuestChannel,
  emptyRespondToGuestDraft,
  labelForRespondToGuestPurpose,
  labelForRespondToGuestTone,
  maskRespondToGuestDestination,
  availableRespondToGuestChannels,
  type RespondToGuestChannel,
  type RespondToGuestDraft,
  type RespondToGuestPurposeId,
  type RespondToGuestToneId,
  type RespondToGuestWriteEntry,
} from "@/lib/operatorFeedback/respondToGuestPresentation"

const SEND_ERROR_MESSAGE =
  "Could not send the response and record the internal action. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark this recovery resolved. Please try again."
const AI_DRAFT_ERROR_MESSAGE = "We could not prepare a draft."

export type RespondAndRecordWizardStep =
  | "recorder"
  | "setup"
  | "write"
  | "review"
  | "success"

export type GuestResponseSentActivityEvent = {
  kind: "guest_response_sent"
  at: string
  actorDisplayName: string | null
  channel: RespondToGuestChannel
  maskedDestination: string
}

export type InternalActionRecordedActivityEvent = {
  kind: "internal_action_recorded"
  at: string
  actorDisplayName: string | null
  category: InternalActionCategoryId
  categoryLabel: string
  note: string
}

export type RecoveryCompletedActivityEvent = {
  kind: "recovery_completed"
  at: string
  actorDisplayName: string | null
  recoveryIntent:
    | "respond_to_guest"
    | "record_internal_action_only"
    | "respond_and_record_internal_action"
  fromWorkflowStatus: FeedbackWorkflowStatus
  toWorkflowStatus: "resolved"
}

export type SendAndRecordRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  subject: string | null
  body: string
  purpose: RespondToGuestPurposeId
  tone: RespondToGuestToneId
  includeNotes: string | null
  category: InternalActionCategoryId
  note: string
  intent: "respond_and_record_internal_action"
}

export type SendAndRecordResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  guestResponseActivityEvent: GuestResponseSentActivityEvent
  internalActionActivityEvent: InternalActionRecordedActivityEvent
}

export type CompleteRecoveryResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: RecoveryCompletedActivityEvent
}

export type PrepareRecoveryDraftMode = "prepare" | "rewrite"

export type PrepareRecoveryDraftRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  purpose: RespondToGuestPurposeId
  tone: RespondToGuestToneId
  includeNotes: string | null
  mode: PrepareRecoveryDraftMode
  currentBody: string | null
  currentSubject: string | null
  /** When checkbox was used — category + note for the draft adapter. */
  confirmedInternalAction?: {
    category: InternalActionCategoryId
    note: string
  } | null
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

export type RespondAndRecordAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  sendAndRecord: (request: SendAndRecordRequest) => Promise<SendAndRecordResult>
  completeRecovery: (
    feedbackId: number,
    intent:
      | "respond_to_guest"
      | "record_internal_action_only"
      | "respond_and_record_internal_action"
  ) => Promise<CompleteRecoveryResult>
  prepareRecoveryDraft: (
    request: PrepareRecoveryDraftRequest,
    signal?: AbortSignal
  ) => Promise<PrepareRecoveryDraftResult>
}

export type RespondAndRecordSummary = {
  guestName: string
  contactCapability: StartRecoveryContactCapability
  feedbackComment: string
  locationName: string
  classificationLabel: string
  purposeLabel: string | null
  toneLabel: string | null
  categoryLabel: string | null
}

export type RespondAndRecordAiDraftStatus = "idle" | "running" | "failed"

export type RespondAndRecordSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  feedbackId: number | null
  step: RespondAndRecordWizardStep
  headerSubtitle: string | null
  summary: RespondAndRecordSummary | null
  category: InternalActionCategoryId | null
  note: string
  useConfirmedActionForGuestResponse: boolean
  canContinueRecorder: boolean
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
  aiDraftStatus: RespondAndRecordAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  actionsLocked: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  sendConfirmOpen: boolean
  sendStatus: "idle" | "saving" | "error"
  sendError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  /** Display-only chips — not persisted recovery-status enums. */
  followUpStateLabel: string
  followUpStatusLabel: string
  recoveryStatusLabel: string
  workflowStatusLabel: string
}

export type RespondAndRecordBackResult = "return-to-shell" | "stayed"

export type RespondAndRecordModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RespondAndRecordSnapshot
  open: (feedbackId: number) => Promise<void>
  saveAndExit: () => void
  close: () => void
  back: () => RespondAndRecordBackResult
  setCategory: (category: InternalActionCategoryId) => void
  setNote: (value: string) => void
  setUseConfirmedActionForGuestResponse: (value: boolean) => void
  continueRecorder: () => void
  editInternalAction: () => void
  setChannel: (channel: RespondToGuestChannel) => void
  setPurpose: (purpose: RespondToGuestPurposeId) => void
  setTone: (tone: RespondToGuestToneId) => void
  setIncludeNotes: (value: string) => void
  continueSetup: () => void
  writeManually: () => void
  prepareDraft: () => Promise<void>
  rewriteDraft: () => Promise<void>
  retryAiDraft: () => Promise<void>
  dismissPreparingOverlay: () => void
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  continueWrite: () => void
  openSendConfirm: () => void
  cancelSendConfirm: () => void
  confirmSend: () => Promise<void>
  keepInProgress: () => void
  markResolved: () => Promise<void>
}

type RespondAndRecordDraft = RespondToGuestDraft & {
  category: InternalActionCategoryId | null
  note: string
  useConfirmedActionForGuestResponse: boolean
  recorderComplete: boolean
}

type SessionState = {
  isOpen: boolean
  loadStatus: RespondAndRecordSnapshot["loadStatus"]
  loadError: string | null
  loadGeneration: number
  feedbackId: number | null
  step: RespondAndRecordWizardStep
  headerSubtitle: string | null
  summary: RespondAndRecordSummary | null
  contactType: ContactType | null
  guestContact: string
  contactCapability: StartRecoveryContactCapability | null
  availableChannels: RespondToGuestChannel[]
  draft: RespondAndRecordDraft
  maskedDestination: string | null
  aiDraftStatus: RespondAndRecordAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  aiDraftGeneration: number
  sendConfirmOpen: boolean
  sendStatus: RespondAndRecordSnapshot["sendStatus"]
  sendError: string | null
  completeStatus: RespondAndRecordSnapshot["completeStatus"]
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
}

function emptyDraft(): RespondAndRecordDraft {
  return {
    ...emptyRespondToGuestDraft(),
    category: null,
    note: "",
    useConfirmedActionForGuestResponse: false,
    recorderComplete: false,
  }
}

function cloneDraft(draft: RespondAndRecordDraft): RespondAndRecordDraft {
  return { ...draft }
}

function emptySession(): SessionState {
  return {
    isOpen: false,
    loadStatus: "idle",
    loadError: null,
    loadGeneration: 0,
    feedbackId: null,
    step: "recorder",
    headerSubtitle: null,
    summary: null,
    contactType: null,
    guestContact: "",
    contactCapability: null,
    availableChannels: [],
    draft: emptyDraft(),
    maskedDestination: null,
    aiDraftStatus: "idle",
    aiDraftMode: null,
    preparingOverlayOpen: false,
    aiDraftError: null,
    aiDraftRetryable: true,
    aiDraftGeneration: 0,
    sendConfirmOpen: false,
    sendStatus: "idle",
    sendError: null,
    completeStatus: "idle",
    completeError: null,
    workflowStatus: null,
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

function classificationLabel(response: FeedbackDetailsResponse): string {
  if (response.classificationStatus !== "Succeeded" || response.sentiment == null) {
    return "Pending"
  }
  return (
    response.sentiment.charAt(0).toUpperCase() + response.sentiment.slice(1)
  )
}

function furthestStep(draft: RespondAndRecordDraft): RespondAndRecordWizardStep {
  if (
    !draft.recorderComplete
    || !canContinueRespondAndRecordRecorder(draft)
  ) {
    return "recorder"
  }
  if (!draft.setupComplete) {
    return "setup"
  }
  if (!draft.messageComplete) {
    return "write"
  }
  return "review"
}

function projectSummary(state: SessionState): RespondAndRecordSummary | null {
  if (state.summary == null || state.contactCapability == null) {
    return state.summary
  }
  return {
    ...state.summary,
    purposeLabel: labelForRespondToGuestPurpose(state.draft.purpose),
    toneLabel: labelForRespondToGuestTone(state.draft.tone),
    categoryLabel: labelForInternalActionCategory(state.draft.category),
  }
}

function toSnapshot(state: SessionState): RespondAndRecordSnapshot {
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
    category: draft.category,
    note: draft.note,
    useConfirmedActionForGuestResponse: draft.useConfirmedActionForGuestResponse,
    canContinueRecorder: canContinueRespondAndRecordRecorder(draft),
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
    sendConfirmOpen: state.sendConfirmOpen,
    sendStatus: state.sendStatus,
    sendError: state.sendError,
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
    followUpStateLabel: INTERNAL_ACTION_FOLLOW_UP_STATE_LABEL,
    followUpStatusLabel: INTERNAL_ACTION_FOLLOW_UP_STATUS_LABEL,
    recoveryStatusLabel: INTERNAL_ACTION_RECOVERY_RECORDED_STATUS_LABEL,
    workflowStatusLabel: INTERNAL_ACTION_WORKFLOW_IN_PROGRESS_LABEL,
  }
}

/**
 * Respond and record an internal action — recorder (with Continue-gating
 * checkbox) → Response setup → Guest response → Review → Send and record →
 * success. Intent-scoped drafts survive Save and exit.
 */
export function createRespondAndRecordInternalActionModule(
  adapters: RespondAndRecordAdapters
): RespondAndRecordModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  /** Intent-scoped drafts (not shared with Respond-to-guest / Record-only). */
  const draftsByFeedbackId = new Map<number, RespondAndRecordDraft>()
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
    existing: RespondAndRecordDraft | undefined
  ): RespondAndRecordDraft => {
    const capability = deriveStartRecoveryContactCapability(
      response.contactType,
      response.guestContact
    )
    if (existing != null) {
      return cloneDraft(existing)
    }
    const draft = emptyDraft()
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

  const confirmedInternalActionForDraft = (): {
    category: InternalActionCategoryId
    note: string
  } | null => {
    if (
      !state.draft.useConfirmedActionForGuestResponse
      || state.draft.category == null
      || state.draft.note.trim() === ""
    ) {
      return null
    }
    return {
      category: state.draft.category,
      note: state.draft.note.trim(),
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
    const confirmedInternalAction = confirmedInternalActionForDraft()
    const generation = ++state.aiDraftGeneration

    if (aiAbortController != null) {
      aiAbortController.abort()
    }
    const controller = new AbortController()
    aiAbortController = controller

    state = {
      ...state,
      aiDraftStatus: "running",
      aiDraftMode: mode,
      preparingOverlayOpen: true,
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
      currentBody: mode === "rewrite" ? priorMessage : null,
      currentSubject:
        mode === "rewrite" && channel === "email" ? priorSubject : null,
      confirmedInternalAction,
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
        const subject =
          channel === "email" ? (result.subject ?? "").trim() : ""
        state = {
          ...state,
          draft: {
            ...state.draft,
            subject,
            message: result.body,
            writeEntry: "editor",
            messageComplete: false,
          },
          aiDraftStatus: "idle",
          aiDraftMode: null,
          preparingOverlayOpen: false,
          aiDraftError: null,
          aiDraftRetryable: true,
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
    async open(feedbackId) {
      const generation = ++state.loadGeneration
      const existingDraft = draftsByFeedbackId.get(feedbackId)

      if (aiAbortController != null) {
        aiAbortController.abort()
        aiAbortController = null
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

        const capability = deriveStartRecoveryContactCapability(
          response.contactType,
          response.guestContact
        )
        const draft = applyDraftDefaults(response, existingDraft)
        const step = furthestStep(draft)
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
            locationName: response.locationName,
            classificationLabel: classificationLabel(response),
            purposeLabel: labelForRespondToGuestPurpose(draft.purpose),
            toneLabel: labelForRespondToGuestTone(draft.tone),
            categoryLabel: labelForInternalActionCategory(draft.category),
          },
          contactType: response.contactType,
          guestContact: response.guestContact,
          contactCapability: capability,
          availableChannels: availableRespondToGuestChannels(capability),
          draft,
          maskedDestination,
          workflowStatus: parseWorkflowStatus(response.workflowStatus),
        }
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
      if (state.step === "recorder") {
        persistDraftIfComposable()
        closeSession()
        return "return-to-shell"
      }
      if (state.step === "setup") {
        state = {
          ...state,
          step: "recorder",
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        clearAiDraftUi()
        publish()
        return "stayed"
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
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
        }
        publish()
        return "stayed"
      }
      return "stayed"
    },
    setCategory(category) {
      if (state.step !== "recorder") {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          category,
          recorderComplete: false,
        },
        summary:
          state.summary == null
            ? null
            : {
                ...state.summary,
                categoryLabel: labelForInternalActionCategory(category),
              },
      }
      publish()
    },
    setNote(value) {
      if (state.step !== "recorder") {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          note: value,
          recorderComplete: false,
        },
      }
      publish()
    },
    setUseConfirmedActionForGuestResponse(value) {
      if (state.step !== "recorder") {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          useConfirmedActionForGuestResponse: value,
          recorderComplete: false,
        },
      }
      publish()
    },
    continueRecorder() {
      if (
        state.step !== "recorder"
        || !canContinueRespondAndRecordRecorder(state.draft)
      ) {
        return
      }
      const nextStep = furthestStep({
        ...state.draft,
        recorderComplete: true,
      })
      state = {
        ...state,
        draft: { ...state.draft, recorderComplete: true },
        step: nextStep === "recorder" ? "setup" : nextStep,
      }
      publish()
    },
    editInternalAction() {
      if (
        state.step !== "setup"
        && state.step !== "write"
        && state.step !== "review"
      ) {
        return
      }
      if (state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        step: "recorder",
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
      clearAiDraftUi()
      publish()
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
    async rewriteDraft() {
      if (state.draft.writeEntry !== "editor") {
        return
      }
      await runAiDraft("rewrite")
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
        || state.draft.category == null
        || (state.step !== "review" && !state.sendConfirmOpen)
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const purpose = state.draft.purpose
      const tone = state.draft.tone
      const category = state.draft.category
      const note = state.draft.note.trim()
      const subject =
        channel === "email" ? state.draft.subject.trim() : null
      const body = state.draft.message.trim()
      const includeNotes =
        state.draft.includeNotes.trim() === ""
          ? null
          : state.draft.includeNotes.trim()

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

      const request: SendAndRecordRequest = {
        feedbackId,
        channel,
        subject,
        body,
        purpose,
        tone,
        includeNotes,
        category,
        note,
        intent: "respond_and_record_internal_action",
      }

      try {
        const result = await adapters.sendAndRecord(request)
        draftsByFeedbackId.delete(feedbackId)
        state = {
          ...state,
          step: "success",
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
          workflowStatus: result.workflowStatus,
          draft: emptyDraft(),
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
        await adapters.completeRecovery(
          feedbackId,
          "respond_and_record_internal_action"
        )
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
