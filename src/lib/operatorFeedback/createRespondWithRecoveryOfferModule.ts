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
  defaultRespondToGuestChannel,
  labelForRespondToGuestTone,
  maskRespondToGuestDestination,
  availableRespondToGuestChannels,
  type RespondToGuestChannel,
  type RespondToGuestToneId,
  type RespondToGuestWriteEntry,
} from "@/lib/operatorFeedback/respondToGuestPresentation"
import type {
  CompleteRecoveryResult,
  PrepareRecoveryDraftMode,
  PrepareRecoveryDraftResult,
} from "@/lib/operatorFeedback/createRespondToGuestModule"
import {
  RECOVERY_OFFER_DESCRIPTION_MAX,
  RECOVERY_OFFER_PURPOSE_ID,
  RECOVERY_OFFER_PURPOSE_LABEL,
  RECOVERY_OFFER_TITLE_MAX,
  autoTitleForRecoveryOffer,
  canContinueRecoveryOfferDetails,
  canContinueRespondWithRecoveryOfferSetup,
  emptyRespondWithRecoveryOfferDraft,
  furthestRespondWithRecoveryOfferStep,
  labelForRecoveryOfferType,
  toConfirmedRecoveryOfferPayload,
  type ConfirmedRecoveryOfferPayload,
  type RecoveryOfferDetailsDraft,
  type RecoveryOfferPurchaseRequirementId,
  type RecoveryOfferTypeId,
  type RecoveryOfferValidityId,
  type RespondWithRecoveryOfferDraft,
  type RespondWithRecoveryOfferWizardStep,
} from "@/lib/operatorFeedback/recoveryOfferPresentation"

const SEND_ERROR_MESSAGE =
  "Could not send the response and issue the offer. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark this recovery resolved. Please try again."
const AI_DRAFT_ERROR_MESSAGE = "We could not prepare a draft."
const OFFER_DESCRIPTION_AI_ERROR_MESSAGE =
  "We could not prepare an offer description."

export type RecoveryOfferIssuedActivityEvent = {
  kind: "recovery_offer_issued"
  at: string
  actorDisplayName: string | null
  offerType: RecoveryOfferTypeId
  title: string
  validity: RecoveryOfferValidityId
  expiryAt: string | null
  redemptionCode: string
}

export type SendAndIssueRecoveryOfferRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  subject: string | null
  body: string
  intent: "respond_with_recovery_offer"
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  tone: RespondToGuestToneId
  includeNotes: string | null
  offer: ConfirmedRecoveryOfferPayload
}

export type SendAndIssueRecoveryOfferResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  guestResponseActivityEvent: {
    kind: "guest_response_sent"
    at: string
    actorDisplayName: string | null
    channel: RespondToGuestChannel
    maskedDestination: string
  }
  recoveryOfferActivityEvent: RecoveryOfferIssuedActivityEvent
  issuedOffer: {
    title: string
    redemptionCode: string
    expiryAt: string | null
    validity: RecoveryOfferValidityId
  }
}

export type PrepareRecoveryOfferDraftRequest = {
  feedbackId: number
  channel: RespondToGuestChannel
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  tone: RespondToGuestToneId
  includeNotes: string | null
  mode: PrepareRecoveryDraftMode
  currentBody: string | null
  currentSubject: string | null
  confirmedOffer: ConfirmedRecoveryOfferPayload | null
}

export type RespondWithRecoveryOfferAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  sendAndIssueRecoveryOffer: (
    request: SendAndIssueRecoveryOfferRequest
  ) => Promise<SendAndIssueRecoveryOfferResult>
  completeRecovery: (
    feedbackId: number,
    intent:
      | "respond_to_guest"
      | "record_internal_action_only"
      | "respond_with_recovery_offer"
  ) => Promise<CompleteRecoveryResult>
  prepareRecoveryDraft: (
    request: PrepareRecoveryOfferDraftRequest,
    signal?: AbortSignal
  ) => Promise<PrepareRecoveryDraftResult>
}

export type RespondWithRecoveryOfferSummary = {
  guestName: string
  contactCapability: StartRecoveryContactCapability
  feedbackComment: string
  locationName: string
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  issueTagLabels: string[] | null
  purposeLabel: string
  toneLabel: string | null
  offerTitle: string | null
  offerTypeLabel: string | null
}

export type RespondWithRecoveryOfferAiDraftStatus = "idle" | "running" | "failed"

export type RespondWithRecoveryOfferSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  feedbackId: number | null
  step: RespondWithRecoveryOfferWizardStep
  headerSubtitle: string | null
  summary: RespondWithRecoveryOfferSummary | null
  availableChannels: RespondToGuestChannel[]
  channel: RespondToGuestChannel | null
  purpose: typeof RECOVERY_OFFER_PURPOSE_ID
  purposeLabel: typeof RECOVERY_OFFER_PURPOSE_LABEL
  tone: RespondToGuestToneId | null
  includeNotes: string
  offer: RecoveryOfferDetailsDraft
  canContinueSetup: boolean
  canContinueOffer: boolean
  subject: string
  message: string
  maskedDestination: string | null
  canContinueWrite: boolean
  writeEntry: RespondToGuestWriteEntry
  aiDraftStatus: RespondWithRecoveryOfferAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  actionsLocked: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  offerDescriptionAiStatus: RespondWithRecoveryOfferAiDraftStatus
  offerDescriptionAiError: string | null
  sendConfirmOpen: boolean
  sendStatus: "idle" | "saving" | "error"
  sendError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  issuedOffer: SendAndIssueRecoveryOfferResult["issuedOffer"] | null
}

export type RespondWithRecoveryOfferBackResult = "return-to-shell" | "stayed"

export type RespondWithRecoveryOfferModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RespondWithRecoveryOfferSnapshot
  open: (feedbackId: number) => Promise<void>
  saveAndExit: () => void
  close: () => void
  back: () => RespondWithRecoveryOfferBackResult
  setChannel: (channel: RespondToGuestChannel) => void
  setTone: (tone: RespondToGuestToneId) => void
  setIncludeNotes: (value: string) => void
  continueSetup: () => void
  setOfferType: (offerType: RecoveryOfferTypeId) => void
  setDiscountPercentage: (value: string) => void
  setDiscountAmount: (value: string) => void
  setFreeItemText: (value: string) => void
  setPurchaseRequirement: (
    value: RecoveryOfferPurchaseRequirementId
  ) => void
  setMinimumSpend: (value: string) => void
  setAdditionalExclusions: (value: string) => void
  setReplacementItemText: (value: string) => void
  setOfferTitle: (value: string) => void
  setOfferDescription: (value: string) => void
  setOfferValidity: (value: RecoveryOfferValidityId) => void
  setExpiryDate: (value: string) => void
  setStaffInstructions: (value: string) => void
  prepareOfferDescription: () => Promise<void>
  continueOffer: () => void
  editOffer: () => void
  writeManually: () => void
  prepareDraft: () => Promise<void>
  rewriteDraft: () => Promise<void>
  retryAiDraft: () => Promise<void>
  dismissPreparingOverlay: () => void
  setSubject: (value: string) => void
  setMessage: (value: string) => void
  continueWrite: () => void
  editText: () => void
  openSendConfirm: () => void
  cancelSendConfirm: () => void
  confirmSend: () => Promise<void>
  keepInProgress: () => void
  markResolved: () => Promise<void>
}

type SessionState = {
  isOpen: boolean
  loadStatus: RespondWithRecoveryOfferSnapshot["loadStatus"]
  loadError: string | null
  loadGeneration: number
  feedbackId: number | null
  step: RespondWithRecoveryOfferWizardStep
  headerSubtitle: string | null
  summary: RespondWithRecoveryOfferSummary | null
  contactType: ContactType | null
  guestContact: string
  contactCapability: StartRecoveryContactCapability | null
  availableChannels: RespondToGuestChannel[]
  draft: RespondWithRecoveryOfferDraft
  maskedDestination: string | null
  aiDraftStatus: RespondWithRecoveryOfferAiDraftStatus
  aiDraftMode: PrepareRecoveryDraftMode | null
  preparingOverlayOpen: boolean
  aiDraftError: string | null
  aiDraftRetryable: boolean
  aiDraftGeneration: number
  offerDescriptionAiStatus: RespondWithRecoveryOfferAiDraftStatus
  offerDescriptionAiError: string | null
  sendConfirmOpen: boolean
  sendStatus: RespondWithRecoveryOfferSnapshot["sendStatus"]
  sendError: string | null
  completeStatus: RespondWithRecoveryOfferSnapshot["completeStatus"]
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  issuedOffer: SendAndIssueRecoveryOfferResult["issuedOffer"] | null
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
    draft: emptyRespondWithRecoveryOfferDraft(),
    maskedDestination: null,
    aiDraftStatus: "idle",
    aiDraftMode: null,
    preparingOverlayOpen: false,
    aiDraftError: null,
    aiDraftRetryable: true,
    aiDraftGeneration: 0,
    offerDescriptionAiStatus: "idle",
    offerDescriptionAiError: null,
    sendConfirmOpen: false,
    sendStatus: "idle",
    sendError: null,
    completeStatus: "idle",
    completeError: null,
    workflowStatus: null,
    issuedOffer: null,
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

function withAutoTitle(
  offer: RecoveryOfferDetailsDraft
): RecoveryOfferDetailsDraft {
  if (offer.titleTouched) {
    return offer
  }
  return {
    ...offer,
    title: autoTitleForRecoveryOffer(offer),
  }
}

function cloneDraft(
  draft: RespondWithRecoveryOfferDraft
): RespondWithRecoveryOfferDraft {
  return {
    ...draft,
    offer: { ...draft.offer },
  }
}

function projectSummary(
  state: SessionState
): RespondWithRecoveryOfferSummary | null {
  if (state.summary == null || state.contactCapability == null) {
    return state.summary
  }
  return {
    ...state.summary,
    purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
    toneLabel: labelForRespondToGuestTone(state.draft.tone),
    offerTitle: state.draft.offer.title.trim() || null,
    offerTypeLabel: labelForRecoveryOfferType(state.draft.offer.offerType),
  }
}

function toSnapshot(state: SessionState): RespondWithRecoveryOfferSnapshot {
  const draft = state.draft
  const actionsLocked =
    state.aiDraftStatus === "running"
    || state.offerDescriptionAiStatus === "running"
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
    purpose: RECOVERY_OFFER_PURPOSE_ID,
    purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
    tone: draft.tone,
    includeNotes: draft.includeNotes,
    offer: draft.offer,
    canContinueSetup: canContinueRespondWithRecoveryOfferSetup(draft),
    canContinueOffer: canContinueRecoveryOfferDetails(draft.offer),
    subject: draft.subject,
    message: draft.message,
    maskedDestination: state.maskedDestination,
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
    offerDescriptionAiStatus: state.offerDescriptionAiStatus,
    offerDescriptionAiError: state.offerDescriptionAiError,
    sendConfirmOpen: state.sendConfirmOpen,
    sendStatus: state.sendStatus,
    sendError: state.sendError,
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
    issuedOffer: state.issuedOffer,
  }
}

/**
 * Respond with a recovery offer — setup → offer → Guest response →
 * review → Send and issue offer → success. Intent-scoped drafts.
 */
export function createRespondWithRecoveryOfferModule(
  adapters: RespondWithRecoveryOfferAdapters
): RespondWithRecoveryOfferModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  const draftsByFeedbackId = new Map<number, RespondWithRecoveryOfferDraft>()
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
    existing: RespondWithRecoveryOfferDraft | undefined
  ): RespondWithRecoveryOfferDraft => {
    const capability = deriveStartRecoveryContactCapability(
      response.contactType,
      response.guestContact
    )
    if (existing != null) {
      return cloneDraft(existing)
    }
    const draft = emptyRespondWithRecoveryOfferDraft()
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

  const patchOffer = (
    patch: Partial<RecoveryOfferDetailsDraft>,
    options?: { autoTitle?: boolean }
  ) => {
    if (state.step !== "offer") {
      return
    }
    let offer: RecoveryOfferDetailsDraft = {
      ...state.draft.offer,
      ...patch,
      offerComplete: false,
    }
    if (options?.autoTitle !== false) {
      offer = withAutoTitle(offer)
    }
    state = {
      ...state,
      draft: { ...state.draft, offer },
    }
    publish()
  }

  const runAiDraft = async (mode: PrepareRecoveryDraftMode) => {
    if (
      state.step !== "write"
      || state.feedbackId == null
      || state.draft.channel == null
      || state.draft.tone == null
      || state.aiDraftStatus === "running"
    ) {
      return
    }

    const confirmedOffer = toConfirmedRecoveryOfferPayload(state.draft.offer)
    if (confirmedOffer == null) {
      return
    }

    const feedbackId = state.feedbackId
    const channel = state.draft.channel
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

    state = {
      ...state,
      aiDraftStatus: "running",
      aiDraftMode: mode,
      preparingOverlayOpen: true,
      aiDraftError: null,
      aiDraftRetryable: true,
    }
    publish()

    const request: PrepareRecoveryOfferDraftRequest = {
      feedbackId,
      channel,
      purpose: RECOVERY_OFFER_PURPOSE_ID,
      tone,
      includeNotes,
      mode,
      currentBody: mode === "rewrite" ? priorMessage : null,
      currentSubject:
        mode === "rewrite" && channel === "email" ? priorSubject : null,
      confirmedOffer,
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
        const step = furthestRespondWithRecoveryOfferStep(draft)
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
            ...mapResponseSetupSummaryChrome(response, capability),
            purposeLabel: RECOVERY_OFFER_PURPOSE_LABEL,
            toneLabel: labelForRespondToGuestTone(draft.tone),
            offerTitle: draft.offer.title.trim() || null,
            offerTypeLabel: labelForRecoveryOfferType(draft.offer.offerType),
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
      if (state.step === "setup") {
        persistDraftIfComposable()
        closeSession()
        return "return-to-shell"
      }
      if (state.step === "offer") {
        state = {
          ...state,
          step: "setup",
          offerDescriptionAiStatus: "idle",
          offerDescriptionAiError: null,
        }
        publish()
        return "stayed"
      }
      if (state.step === "write") {
        state = {
          ...state,
          step: "offer",
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
        || !canContinueRespondWithRecoveryOfferSetup(state.draft)
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, setupComplete: true },
        step: "offer",
      }
      publish()
    },
    setOfferType(offerType) {
      patchOffer({
        offerType,
        titleTouched: false,
      })
    },
    setDiscountPercentage(value) {
      patchOffer({ discountPercentage: value })
    },
    setDiscountAmount(value) {
      patchOffer({ discountAmount: value })
    },
    setFreeItemText(value) {
      patchOffer({ freeItemText: value })
    },
    setPurchaseRequirement(value) {
      patchOffer({ purchaseRequirement: value })
    },
    setMinimumSpend(value) {
      patchOffer({ minimumSpend: value })
    },
    setAdditionalExclusions(value) {
      patchOffer({ additionalExclusions: value }, { autoTitle: false })
    },
    setReplacementItemText(value) {
      patchOffer({ replacementItemText: value })
    },
    setOfferTitle(value) {
      patchOffer(
        {
          title: value.slice(0, RECOVERY_OFFER_TITLE_MAX),
          titleTouched: true,
        },
        { autoTitle: false }
      )
    },
    setOfferDescription(value) {
      patchOffer(
        {
          description: value.slice(0, RECOVERY_OFFER_DESCRIPTION_MAX),
        },
        { autoTitle: false }
      )
    },
    setOfferValidity(value) {
      patchOffer({ validity: value }, { autoTitle: false })
    },
    setExpiryDate(value) {
      patchOffer({ expiryDate: value }, { autoTitle: false })
    },
    setStaffInstructions(value) {
      patchOffer({ staffInstructions: value }, { autoTitle: false })
    },
    async prepareOfferDescription() {
      if (
        state.step !== "offer"
        || state.feedbackId == null
        || state.draft.channel == null
        || state.draft.tone == null
        || state.offerDescriptionAiStatus === "running"
      ) {
        return
      }

      const offerForDraft = withAutoTitle(state.draft.offer)
      const confirmedOffer = toConfirmedRecoveryOfferPayload({
        ...offerForDraft,
        description:
          offerForDraft.description.trim() === ""
            ? "placeholder"
            : offerForDraft.description,
      })
      // Allow AI when description empty: temporarily satisfy validator.
      const offerPayload: ConfirmedRecoveryOfferPayload | null =
        confirmedOffer
        ?? (offerForDraft.offerType != null
          ? {
              offerType: offerForDraft.offerType,
              title: offerForDraft.title.trim() || autoTitleForRecoveryOffer(offerForDraft),
              description: offerForDraft.description.trim() || offerForDraft.title.trim() || "Recovery offer",
              validity: offerForDraft.validity,
              expiryDate:
                offerForDraft.validity === "choose_expiry_date"
                  ? offerForDraft.expiryDate.trim() || null
                  : null,
              discountPercentage: null,
              discountAmount: null,
              freeItemText: offerForDraft.freeItemText.trim() || null,
              purchaseRequirement: offerForDraft.purchaseRequirement,
              minimumSpend: null,
              additionalExclusions:
                offerForDraft.additionalExclusions.trim() || null,
              replacementItemText:
                offerForDraft.replacementItemText.trim() || null,
              staffInstructions:
                offerForDraft.staffInstructions.trim() || null,
            }
          : null)

      if (offerPayload == null) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const tone = state.draft.tone
      const generation = ++state.aiDraftGeneration

      if (aiAbortController != null) {
        aiAbortController.abort()
      }
      const controller = new AbortController()
      aiAbortController = controller

      state = {
        ...state,
        offerDescriptionAiStatus: "running",
        offerDescriptionAiError: null,
      }
      publish()

      try {
        const result = await adapters.prepareRecoveryDraft(
          {
            feedbackId,
            channel,
            purpose: RECOVERY_OFFER_PURPOSE_ID,
            tone,
            includeNotes:
              "Write a short guest-facing offer description only (max 240 characters).",
            mode: "prepare",
            currentBody: null,
            currentSubject: null,
            confirmedOffer: offerPayload,
          },
          controller.signal
        )

        if (
          generation !== state.aiDraftGeneration
          || controller.signal.aborted
        ) {
          return
        }

        if (result.status === "succeeded") {
          state = {
            ...state,
            draft: {
              ...state.draft,
              offer: {
                ...state.draft.offer,
                description: result.body
                  .trim()
                  .slice(0, RECOVERY_OFFER_DESCRIPTION_MAX),
                offerComplete: false,
              },
            },
            offerDescriptionAiStatus: "idle",
            offerDescriptionAiError: null,
          }
          publish()
          return
        }

        state = {
          ...state,
          offerDescriptionAiStatus: "failed",
          offerDescriptionAiError: OFFER_DESCRIPTION_AI_ERROR_MESSAGE,
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
          offerDescriptionAiStatus: "failed",
          offerDescriptionAiError: OFFER_DESCRIPTION_AI_ERROR_MESSAGE,
        }
        publish()
      } finally {
        if (aiAbortController === controller) {
          aiAbortController = null
        }
      }
    },
    continueOffer() {
      if (
        state.step !== "offer"
        || !canContinueRecoveryOfferDetails(state.draft.offer)
      ) {
        return
      }
      state = {
        ...state,
        draft: {
          ...state.draft,
          offer: { ...state.draft.offer, offerComplete: true },
          writeEntry:
            state.draft.writeEntry === "editor" ? "editor" : "chooser",
        },
        step: "write",
        offerDescriptionAiStatus: "idle",
        offerDescriptionAiError: null,
      }
      clearAiDraftUi()
      publish()
    },
    editOffer() {
      if (state.step !== "write" && state.step !== "review") {
        return
      }
      if (state.aiDraftStatus === "running") {
        return
      }
      state = {
        ...state,
        step: "offer",
        draft: {
          ...state.draft,
          offer: { ...state.draft.offer, offerComplete: false },
          messageComplete: false,
        },
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
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
    editText() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        step: "write",
        draft: { ...state.draft, writeEntry: "editor", messageComplete: false },
        sendConfirmOpen: false,
        sendStatus: "idle",
        sendError: null,
      }
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
      const offer = toConfirmedRecoveryOfferPayload(state.draft.offer)
      if (
        state.feedbackId == null
        || state.draft.channel == null
        || state.draft.tone == null
        || offer == null
        || (state.step !== "review" && !state.sendConfirmOpen)
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const channel = state.draft.channel
      const tone = state.draft.tone
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

      const request: SendAndIssueRecoveryOfferRequest = {
        feedbackId,
        channel,
        subject,
        body,
        intent: "respond_with_recovery_offer",
        purpose: RECOVERY_OFFER_PURPOSE_ID,
        tone,
        includeNotes,
        offer,
      }

      try {
        const result = await adapters.sendAndIssueRecoveryOffer(request)
        draftsByFeedbackId.delete(feedbackId)
        state = {
          ...state,
          step: "success",
          sendConfirmOpen: false,
          sendStatus: "idle",
          sendError: null,
          workflowStatus: result.workflowStatus,
          issuedOffer: result.issuedOffer,
          draft: emptyRespondWithRecoveryOfferDraft(),
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
          "respond_with_recovery_offer"
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
