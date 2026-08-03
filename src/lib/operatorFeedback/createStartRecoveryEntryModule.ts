import type {
  ClassificationStatus,
  FeedbackDetailsResponse,
  FeedbackSentiment,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import {
  formatFeedbackReference,
  type SetWorkflowStatusResponse,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  buildStartRecoveryIntents,
  deriveStartRecoveryContactCapability,
  startRecoveryContactCapabilityLabel,
  type StartRecoveryIntentCard,
  type StartRecoveryIntentId,
} from "@/lib/operatorFeedback/startRecoveryPresentation"

const LOAD_ERROR = "Could not load recovery. Please try again."
const WORKFLOW_ADVANCE_ERROR =
  "Could not update follow-up status. Please try again."

export type StartRecoveryEntryAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  setWorkflowStatus: (
    feedbackId: number,
    workflowStatus: FeedbackWorkflowStatus
  ) => Promise<SetWorkflowStatusResponse>
}

export type StartRecoveryFeedbackSummary = {
  guestName: string
  classificationStatus: ClassificationStatus
  classificationSentiment: FeedbackSentiment | null
  contactLabel: string
  feedbackComment: string
  issueTagLabels: string[] | null
}

export type StartRecoveryEntrySnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  feedbackId: number | null
  loadError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  workflowAdvanceStatus: "idle" | "saving" | "error"
  workflowAdvanceError: string | null
  headerSubtitle: string | null
  summary: StartRecoveryFeedbackSummary | null
  intents: StartRecoveryIntentCard[]
  selectedIntentId: StartRecoveryIntentId | null
}

export type StartRecoveryEntryModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => StartRecoveryEntrySnapshot
  open: (feedbackId: number) => Promise<void>
  close: () => void
  retry: () => Promise<void>
  selectIntent: (intentId: StartRecoveryIntentId) => boolean
  clearSelectedIntent: () => void
}

type EntryState = {
  isOpen: boolean
  loadStatus: StartRecoveryEntrySnapshot["loadStatus"]
  feedbackId: number | null
  loadError: string | null
  loadGeneration: number
  workflowStatus: FeedbackWorkflowStatus | null
  workflowAdvanceStatus: StartRecoveryEntrySnapshot["workflowAdvanceStatus"]
  workflowAdvanceError: string | null
  headerSubtitle: string | null
  summary: StartRecoveryFeedbackSummary | null
  intents: StartRecoveryIntentCard[]
  selectedIntentId: StartRecoveryIntentId | null
  guestOffersOptOut: boolean
  contactCapability: ReturnType<typeof deriveStartRecoveryContactCapability> | null
}

function emptyState(): EntryState {
  return {
    isOpen: false,
    loadStatus: "idle",
    feedbackId: null,
    loadError: null,
    loadGeneration: 0,
    workflowStatus: null,
    workflowAdvanceStatus: "idle",
    workflowAdvanceError: null,
    headerSubtitle: null,
    summary: null,
    intents: [],
    selectedIntentId: null,
    guestOffersOptOut: false,
    contactCapability: null,
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
  const reference = formatFeedbackReference(feedbackId)
  const location = locationName.trim()
  const touchpoint = qrSource?.trim() ?? ""
  if (!touchpoint) {
    return `${reference} · ${location}`
  }
  return `${reference} · ${location} · ${touchpoint}`
}

function mapIssueTagLabels(
  classificationStatus: ClassificationStatus,
  detectedTags: string[] | null | undefined
): string[] | null {
  if (classificationStatus !== "Succeeded" || detectedTags == null) {
    return null
  }
  return detectedTags.map((key) => labelForDetectedTag(key))
}

function mapSummary(
  response: FeedbackDetailsResponse
): StartRecoveryFeedbackSummary {
  const contactCapability = deriveStartRecoveryContactCapability(
    response.contactType,
    response.guestContact
  )
  return {
    guestName: response.guestName,
    classificationStatus: response.classificationStatus,
    classificationSentiment: response.sentiment,
    contactLabel: startRecoveryContactCapabilityLabel(contactCapability),
    feedbackComment: response.comment,
    issueTagLabels: mapIssueTagLabels(
      response.classificationStatus,
      response.detectedTags
    ),
  }
}

function rebuildIntents(state: EntryState): StartRecoveryIntentCard[] {
  if (state.contactCapability == null || state.workflowStatus == null) {
    return []
  }
  return buildStartRecoveryIntents({
    contactCapability: state.contactCapability,
    guestOffersOptOut: state.guestOffersOptOut,
    workflowStatus: state.workflowStatus,
  })
}

function toSnapshot(state: EntryState): StartRecoveryEntrySnapshot {
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    feedbackId: state.feedbackId,
    loadError: state.loadError,
    workflowStatus: state.workflowStatus,
    workflowAdvanceStatus: state.workflowAdvanceStatus,
    workflowAdvanceError: state.workflowAdvanceError,
    headerSubtitle: state.headerSubtitle,
    summary: state.summary,
    intents: state.intents,
    selectedIntentId: state.selectedIntentId,
  }
}

/**
 * Shared Start recovery entry shell — load summary, New→In progress on open,
 * intent disable rules, and route-stub selection. Intent paths land in later tickets.
 */
export function createStartRecoveryEntryModule(
  adapters: StartRecoveryEntryAdapters
): StartRecoveryEntryModule {
  let state = emptyState()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const applyLoadedDetails = (response: FeedbackDetailsResponse) => {
    const workflowStatus = parseWorkflowStatus(response.workflowStatus)
    const contactCapability = deriveStartRecoveryContactCapability(
      response.contactType,
      response.guestContact
    )
    const guestOffersOptOut = response.guestOffersOptOut === true

    state = {
      ...state,
      loadStatus: "loaded",
      loadError: null,
      workflowStatus,
      workflowAdvanceStatus: "idle",
      workflowAdvanceError: null,
      headerSubtitle: buildHeaderSubtitle(
        response.id,
        response.locationName,
        response.qrSource
      ),
      summary: mapSummary(response),
      guestOffersOptOut,
      contactCapability,
      intents: [],
    }
    state = {
      ...state,
      intents: rebuildIntents(state),
    }
  }

  const advanceNewToInProgress = async (feedbackId: number) => {
    if (state.workflowStatus !== "new") {
      return
    }

    state = {
      ...state,
      workflowAdvanceStatus: "saving",
      workflowAdvanceError: null,
    }
    publish()

    try {
      const result = await adapters.setWorkflowStatus(
        feedbackId,
        "in_progress"
      )
      if (state.feedbackId !== feedbackId || !state.isOpen) {
        return
      }
      state = {
        ...state,
        workflowStatus: result.workflowStatus,
        workflowAdvanceStatus: "idle",
        workflowAdvanceError: null,
      }
      state = {
        ...state,
        intents: rebuildIntents(state),
      }
      publish()
    } catch {
      if (state.feedbackId !== feedbackId || !state.isOpen) {
        return
      }
      state = {
        ...state,
        workflowAdvanceStatus: "error",
        workflowAdvanceError: WORKFLOW_ADVANCE_ERROR,
      }
      publish()
    }
  }

  const load = async (feedbackId: number) => {
    const generation = state.loadGeneration + 1
    state = {
      ...emptyState(),
      isOpen: true,
      loadStatus: "loading",
      feedbackId,
      loadGeneration: generation,
      selectedIntentId: null,
    }
    publish()

    try {
      const response = await adapters.getFeedbackDetails(feedbackId)
      if (state.loadGeneration !== generation) {
        return
      }
      applyLoadedDetails(response)
      publish()
      await advanceNewToInProgress(feedbackId)
    } catch {
      if (state.loadGeneration !== generation) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        loadError: LOAD_ERROR,
        summary: null,
        intents: [],
        headerSubtitle: null,
      }
      publish()
    }
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    open: (feedbackId) => load(feedbackId),
    retry: async () => {
      if (state.feedbackId == null) {
        return
      }
      await load(state.feedbackId)
    },
    close: () => {
      state = emptyState()
      publish()
    },
    selectIntent: (intentId) => {
      if (!state.isOpen || state.loadStatus !== "loaded") {
        return false
      }
      const intent = state.intents.find((item) => item.id === intentId)
      if (intent == null || !intent.enabled) {
        return false
      }
      const feedbackId = state.feedbackId
      state = {
        ...emptyState(),
        selectedIntentId: intentId,
        feedbackId,
      }
      publish()
      return true
    },
    clearSelectedIntent: () => {
      if (state.selectedIntentId == null) {
        return
      }
      state = {
        ...state,
        selectedIntentId: null,
        feedbackId: null,
      }
      publish()
    },
  }
}
