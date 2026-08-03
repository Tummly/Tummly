import type {
  FeedbackDetailsResponse,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import {
  canContinueInternalActionRecorder,
  labelForInternalActionCategory,
  type InternalActionCategoryId,
} from "./internalActionPresentation"

export type RecordInternalActionWizardStep =
  | "recorder"
  | "review"
  | "success"

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
    | "respond_with_recovery_offer"
  fromWorkflowStatus: FeedbackWorkflowStatus
  toWorkflowStatus: "resolved"
}

export type RecordInternalActionRequest = {
  feedbackId: number
  category: InternalActionCategoryId
  note: string
  intent: "record_internal_action_only"
}

export type RecordInternalActionResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: InternalActionRecordedActivityEvent
}

export type CompleteRecoveryResult = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: RecoveryCompletedActivityEvent
}

export type RecordInternalActionAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  recordInternalAction: (
    request: RecordInternalActionRequest
  ) => Promise<RecordInternalActionResult>
  completeRecovery: (
    feedbackId: number,
    intent:
      | "respond_to_guest"
      | "record_internal_action_only"
      | "respond_with_recovery_offer"
  ) => Promise<CompleteRecoveryResult>
}

export type RecordInternalActionSummary = {
  guestName: string
  feedbackComment: string
  locationName: string
  classificationLabel: string
  categoryLabel: string | null
}

export type RecordInternalActionSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  feedbackId: number | null
  step: RecordInternalActionWizardStep
  headerSubtitle: string | null
  summary: RecordInternalActionSummary | null
  category: InternalActionCategoryId | null
  note: string
  canContinueRecorder: boolean
  recordConfirmOpen: boolean
  recordStatus: "idle" | "saving" | "error"
  recordError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
  /** Display-only chips — not a persisted recovery-status enum. */
  followUpStateLabel: string
  recoveryStatusLabel: string
}

export type RecordInternalActionBackResult = "return-to-shell" | "stayed"

export type RecordInternalActionModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => RecordInternalActionSnapshot
  open: (feedbackId: number) => Promise<void>
  saveAndExit: () => void
  close: () => void
  back: () => RecordInternalActionBackResult
  setCategory: (category: InternalActionCategoryId) => void
  setNote: (value: string) => void
  continueRecorder: () => void
  openRecordConfirm: () => void
  cancelRecordConfirm: () => void
  confirmRecord: () => Promise<void>
  keepInProgress: () => void
  markResolved: () => Promise<void>
}

type RecordInternalActionDraft = {
  category: InternalActionCategoryId | null
  note: string
  recorderComplete: boolean
}

type SessionState = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  loadGeneration: number
  feedbackId: number | null
  step: RecordInternalActionWizardStep
  headerSubtitle: string | null
  summary: RecordInternalActionSummary | null
  draft: RecordInternalActionDraft
  recordConfirmOpen: boolean
  recordStatus: "idle" | "saving" | "error"
  recordError: string | null
  completeStatus: "idle" | "saving" | "error"
  completeError: string | null
  workflowStatus: FeedbackWorkflowStatus | null
}

const RECORD_ERROR_MESSAGE =
  "Could not record the internal action. Please try again."
const COMPLETE_ERROR_MESSAGE =
  "Could not mark recovery resolved. Please try again."

const FOLLOW_UP_STATE_LABEL = "Mark follow-up complete"
const RECOVERY_STATUS_LABEL = "In progress"

function emptyDraft(): RecordInternalActionDraft {
  return {
    category: null,
    note: "",
    recorderComplete: false,
  }
}

function cloneDraft(draft: RecordInternalActionDraft): RecordInternalActionDraft {
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
    draft: emptyDraft(),
    recordConfirmOpen: false,
    recordStatus: "idle",
    recordError: null,
    completeStatus: "idle",
    completeError: null,
    workflowStatus: null,
  }
}

function buildHeaderSubtitle(
  feedbackId: number,
  locationName: string,
  qrSource: string | null | undefined
): string {
  const ref = `FDB-${String(feedbackId).padStart(4, "0")}`
  if (qrSource != null && qrSource.trim() !== "") {
    return `${ref} · ${locationName} · ${qrSource}`
  }
  return `${ref} · ${locationName}`
}

function classificationLabel(response: FeedbackDetailsResponse): string {
  if (response.classificationStatus !== "Succeeded" || response.sentiment == null) {
    return "Pending"
  }
  const sentiment =
    response.sentiment.charAt(0).toUpperCase() + response.sentiment.slice(1)
  return sentiment
}

function furthestStep(
  draft: RecordInternalActionDraft
): RecordInternalActionWizardStep {
  if (draft.recorderComplete && canContinueInternalActionRecorder(draft)) {
    return "review"
  }
  return "recorder"
}

function parseWorkflowStatus(
  value: string | null | undefined
): FeedbackWorkflowStatus | null {
  if (value === "new" || value === "in_progress" || value === "resolved") {
    return value
  }
  return null
}

function toSnapshot(state: SessionState): RecordInternalActionSnapshot {
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    feedbackId: state.feedbackId,
    step: state.step,
    headerSubtitle: state.headerSubtitle,
    summary: state.summary,
    category: state.draft.category,
    note: state.draft.note,
    canContinueRecorder: canContinueInternalActionRecorder(state.draft),
    recordConfirmOpen: state.recordConfirmOpen,
    recordStatus: state.recordStatus,
    recordError: state.recordError,
    completeStatus: state.completeStatus,
    completeError: state.completeError,
    workflowStatus: state.workflowStatus,
    followUpStateLabel: FOLLOW_UP_STATE_LABEL,
    recoveryStatusLabel: RECOVERY_STATUS_LABEL,
  }
}

/**
 * Record an internal action only — recorder → review → confirm → success.
 * Intent-scoped drafts survive Save and exit. No guest message.
 */
export function createRecordInternalActionModule(
  adapters: RecordInternalActionAdapters
): RecordInternalActionModule {
  let state = emptySession()
  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()
  /** Intent-scoped drafts keyed by Feedback id (Record internal action only). */
  const draftsByFeedbackId = new Map<number, RecordInternalActionDraft>()

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
    existing: RecordInternalActionDraft | undefined
  ): RecordInternalActionDraft => {
    if (existing != null) {
      return cloneDraft(existing)
    }
    return emptyDraft()
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

        const draft = applyDraftDefaults(existingDraft)
        const step = furthestStep(draft)

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
            feedbackComment: response.comment,
            locationName: response.locationName,
            classificationLabel: classificationLabel(response),
            categoryLabel: labelForInternalActionCategory(draft.category),
          },
          draft,
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
      if (state.step === "recorder") {
        persistDraftIfComposable()
        closeSession()
        return "return-to-shell"
      }
      if (state.step === "review") {
        state = {
          ...state,
          step: "recorder",
          recordConfirmOpen: false,
          recordStatus: "idle",
          recordError: null,
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
    continueRecorder() {
      if (
        state.step !== "recorder"
        || !canContinueInternalActionRecorder(state.draft)
      ) {
        return
      }
      state = {
        ...state,
        draft: { ...state.draft, recorderComplete: true },
        step: "review",
      }
      publish()
    },
    openRecordConfirm() {
      if (state.step !== "review") {
        return
      }
      state = {
        ...state,
        recordConfirmOpen: true,
        recordStatus: "idle",
        recordError: null,
      }
      publish()
    },
    cancelRecordConfirm() {
      state = {
        ...state,
        recordConfirmOpen: false,
        recordStatus: "idle",
        recordError: null,
      }
      publish()
    },
    async confirmRecord() {
      if (
        state.feedbackId == null
        || state.draft.category == null
        || (state.step !== "review" && !state.recordConfirmOpen)
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const category = state.draft.category
      const note = state.draft.note.trim()
      const categoryLabel =
        labelForInternalActionCategory(category) ?? category

      if (state.step === "review" && !state.recordConfirmOpen) {
        state = { ...state, recordConfirmOpen: true }
      }

      state = {
        ...state,
        recordConfirmOpen: true,
        recordStatus: "saving",
        recordError: null,
      }
      publish()

      const request: RecordInternalActionRequest = {
        feedbackId,
        category,
        note,
        intent: "record_internal_action_only",
      }

      try {
        const result = await adapters.recordInternalAction(request)
        draftsByFeedbackId.delete(feedbackId)
        state = {
          ...state,
          step: "success",
          recordConfirmOpen: false,
          recordStatus: "idle",
          recordError: null,
          workflowStatus: result.workflowStatus,
          draft: emptyDraft(),
          summary:
            state.summary == null
              ? null
              : {
                  ...state.summary,
                  categoryLabel,
                },
        }
        publish()
      } catch {
        state = {
          ...state,
          step: "review",
          recordConfirmOpen: true,
          recordStatus: "error",
          recordError: RECORD_ERROR_MESSAGE,
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
          "record_internal_action_only"
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
