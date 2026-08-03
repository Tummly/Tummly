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
} from "@/lib/operatorFeedback/respondToGuestPresentation"

const SEND_ERROR_MESSAGE =
  "Could not send the response. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark this recovery resolved. Please try again."

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
  recoveryIntent: "respond_to_guest"
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

export type RespondToGuestAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  sendGuestResponse: (
    request: SendGuestResponseRequest
  ) => Promise<SendGuestResponseResult>
  completeRecovery: (
    feedbackId: number,
    intent: "respond_to_guest"
  ) => Promise<CompleteRecoveryResult>
}

export type RespondToGuestSummary = {
  guestName: string
  contactCapability: StartRecoveryContactCapability
  feedbackComment: string
  purposeLabel: string | null
  toneLabel: string | null
}

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
  sendConfirmOpen: boolean
  sendStatus: "idle" | "saving" | "error"
  sendError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
}

export type RespondToGuestBackResult = "return-to-shell" | "stayed"

export type RespondToGuestModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RespondToGuestSnapshot
  open: (feedbackId: number) => Promise<void>
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
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  continueWrite: () => void
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
  sendConfirmOpen: boolean
  sendStatus: RespondToGuestSnapshot["sendStatus"]
  sendError: string | null
  completeStatus: RespondToGuestSnapshot["completeStatus"]
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
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
    canContinueWrite: canContinueRespondToGuestMessage({
      channel: draft.channel,
      subject: draft.subject,
      message: draft.message,
    }),
    sendConfirmOpen: state.sendConfirmOpen,
    sendStatus: state.sendStatus,
    sendError: state.sendError,
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
  }
}

function cloneDraft(draft: RespondToGuestDraft): RespondToGuestDraft {
  return { ...draft }
}

/**
 * Respond to the guest wizard (manual path) — setup → write → review → send →
 * success. Intent-scoped drafts survive Save and exit; cleared after send.
 */
export function createRespondToGuestModule(
  adapters: RespondToGuestAdapters
): RespondToGuestModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  /** Intent-scoped drafts keyed by Feedback id (Respond to the guest only). */
  const draftsByFeedbackId = new Map<number, RespondToGuestDraft>()

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
    state = {
      ...emptySession(),
      loadGeneration: state.loadGeneration,
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

      state = {
        ...emptySession(),
        loadGeneration: generation,
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
            purposeLabel: labelForRespondToGuestPurpose(draft.purpose),
            toneLabel: labelForRespondToGuestTone(draft.tone),
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
      persistDraftIfComposable()
      closeSession()
    },
    close() {
      closeSession()
    },
    back() {
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
        publish()
        return "stayed"
      }
      if (state.step === "review") {
        state = {
          ...state,
          step: "write",
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
      if (state.step !== "setup") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, channel, setupComplete: false },
      }
      publish()
    },
    setPurpose(purpose) {
      if (state.step !== "setup") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, purpose, setupComplete: false },
      }
      publish()
    },
    setTone(tone) {
      if (state.step !== "setup") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, tone, setupComplete: false },
      }
      publish()
    },
    setIncludeNotes(value) {
      if (state.step !== "setup") {
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
        || !canContinueRespondToGuestSetup(state.draft)
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, setupComplete: true },
        step: "write",
      }
      publish()
    },
    setSubject(value) {
      if (state.step !== "write") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, subject: value, messageComplete: false },
      }
      publish()
    },
    setMessage(value) {
      if (state.step !== "write") {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, message: value, messageComplete: false },
      }
      publish()
    },
    continueWrite() {
      if (
        state.step !== "write"
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
      publish()
    },
    openSendConfirm() {
      if (state.step !== "review") {
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
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
          workflowStatus: result.workflowStatus,
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
