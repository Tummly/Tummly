import type {
  ContactType,
  FeedbackDetailsResponse,
  FeedbackSentiment,
} from "@/types/dashboard"
import { labelForDetectedIssue } from "@/lib/operatorHome/detectedIssues"

const NEW_WINDOW_MS = 24 * 60 * 60 * 1000
const LOAD_ERROR = "Could not load Feedback details. Please try again."
const SAVE_ERROR = "Could not save classification. Please try again."

export type { FeedbackDetailsResponse }

export type FeedbackDetailsActivityEvent = {
  kind: "feedback_received"
  at: string
}

export type FeedbackDetailsDetectedIssue = {
  key: string
  label: string
}

export type FeedbackClassificationCorrection = {
  isEditing: boolean
  draftSentiment: FeedbackSentiment | null
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  canSave: boolean
}

export type FeedbackDetailsLoaded = {
  id: number
  guestName: string
  guestContact: string
  contactType: ContactType
  comment: string
  createdAt: string
  locationName: string
  address: string
  venueLine: string
  isNew: boolean
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  sentiment: FeedbackSentiment | null
  detectedIssues: FeedbackDetailsDetectedIssue[] | null
  canCorrectClassification: boolean
  canViewGuestProfile: false
  canAddInternalNote: false
  activityHistory: FeedbackDetailsActivityEvent[]
}

export type FeedbackDetailsSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  feedbackId: number | null
  details: FeedbackDetailsLoaded | null
  loadError: string | null
  correction: FeedbackClassificationCorrection
}

export type CorrectClassificationResponse = {
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  sentiment: FeedbackSentiment | null
  detectedIssues: string[] | null
}

export type FeedbackDetailsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  correctClassification: (
    feedbackId: number,
    sentiment: FeedbackSentiment
  ) => Promise<CorrectClassificationResponse>
}

export type FeedbackDetailsModuleOptions = {
  now?: () => number
}

export type FeedbackDetailsModule = {
  getSnapshot: () => FeedbackDetailsSnapshot
  subscribe: (listener: () => void) => () => void
  open: (feedbackId: number) => Promise<void>
  retry: () => Promise<void>
  close: () => void
  reset: () => void
  startCorrection: () => void
  setDraftSentiment: (sentiment: FeedbackSentiment) => void
  cancelCorrection: () => void
  saveCorrection: () => Promise<void>
}

type DetailsState = {
  isOpen: boolean
  loadStatus: FeedbackDetailsSnapshot["loadStatus"]
  feedbackId: number | null
  details: FeedbackDetailsLoaded | null
  loadError: string | null
  loadGeneration: number
  saveGeneration: number
  isEditing: boolean
  draftSentiment: FeedbackSentiment | null
  saveStatus: FeedbackClassificationCorrection["saveStatus"]
  saveError: string | null
}

type DetailsAction =
  | { type: "reset" }
  | { type: "open_started"; generation: number; feedbackId: number }
  | {
      type: "open_succeeded"
      generation: number
      details: FeedbackDetailsLoaded
    }
  | { type: "open_failed"; generation: number; error: string }
  | { type: "correction_started"; draftSentiment: FeedbackSentiment }
  | { type: "draft_sentiment_set"; sentiment: FeedbackSentiment }
  | { type: "correction_cancelled" }
  | { type: "save_started"; generation: number }
  | {
      type: "save_succeeded"
      generation: number
      sentiment: FeedbackSentiment
      detectedIssues: FeedbackDetailsDetectedIssue[] | null
    }
  | { type: "save_failed"; generation: number; error: string }

const idleCorrection = (): FeedbackClassificationCorrection => ({
  isEditing: false,
  draftSentiment: null,
  saveStatus: "idle",
  saveError: null,
  canSave: false,
})

function canSaveCorrection(state: DetailsState): boolean {
  return (
    state.isEditing
    && state.draftSentiment != null
    && state.details?.sentiment != null
    && state.draftSentiment !== state.details.sentiment
    && state.saveStatus !== "saving"
  )
}

function toCorrection(
  state: DetailsState
): FeedbackClassificationCorrection {
  return {
    isEditing: state.isEditing,
    draftSentiment: state.draftSentiment,
    saveStatus: state.saveStatus,
    saveError: state.saveError,
    canSave: canSaveCorrection(state),
  }
}

export function isFeedbackNew(
  createdAt: string,
  nowMs: number = Date.now()
): boolean {
  const thenMs = new Date(createdAt).getTime()
  if (Number.isNaN(thenMs)) {
    return false
  }
  const ageMs = nowMs - thenMs
  return ageMs >= 0 && ageMs < NEW_WINDOW_MS
}

export function formatFeedbackVenueLine(
  locationName: string,
  address: string
): string {
  const name = locationName.trim()
  const trimmedAddress = address.trim()
  if (!trimmedAddress) {
    return name
  }
  return `${name} · ${trimmedAddress}`
}

function mapDetectedIssues(
  keys: string[] | null | undefined
): FeedbackDetailsDetectedIssue[] | null {
  if (keys == null) {
    return null
  }
  return keys.map((key) => ({
    key,
    label: labelForDetectedIssue(key),
  }))
}

function toLoadedDetails(
  response: FeedbackDetailsResponse,
  nowMs: number
): FeedbackDetailsLoaded {
  const succeeded = response.classificationStatus === "Succeeded"

  return {
    id: response.id,
    guestName: response.guestName,
    guestContact: response.guestContact,
    contactType: response.contactType,
    comment: response.comment,
    createdAt: response.createdAt,
    locationName: response.locationName,
    address: response.address,
    venueLine: formatFeedbackVenueLine(
      response.locationName,
      response.address
    ),
    isNew: isFeedbackNew(response.createdAt, nowMs),
    classificationStatus: response.classificationStatus,
    sentiment: succeeded ? response.sentiment : null,
    detectedIssues: succeeded
      ? mapDetectedIssues(response.detectedIssues ?? [])
      : null,
    canCorrectClassification: succeeded,
    canViewGuestProfile: false,
    canAddInternalNote: false,
    activityHistory: [
      {
        kind: "feedback_received",
        at: response.createdAt,
      },
    ],
  }
}

function reduce(state: DetailsState, action: DetailsAction): DetailsState {
  switch (action.type) {
    case "reset":
      return {
        ...state,
        isOpen: false,
        loadStatus: "idle",
        feedbackId: null,
        details: null,
        loadError: null,
        loadGeneration: state.loadGeneration + 1,
        saveGeneration: state.saveGeneration + 1,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
      }
    case "open_started":
      return {
        ...state,
        isOpen: true,
        loadStatus: "loading",
        loadGeneration: action.generation,
        feedbackId: action.feedbackId,
        details: null,
        loadError: null,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
      }
    case "open_succeeded":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "loaded",
        details: action.details,
        loadError: null,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
      }
    case "open_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "error",
        details: null,
        loadError: action.error,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
      }
    case "correction_started":
      return {
        ...state,
        isEditing: true,
        draftSentiment: action.draftSentiment,
        saveStatus: "idle",
        saveError: null,
      }
    case "draft_sentiment_set":
      if (!state.isEditing) {
        return state
      }
      return {
        ...state,
        draftSentiment: action.sentiment,
        saveError: null,
        saveStatus:
          state.saveStatus === "error" ? "idle" : state.saveStatus,
      }
    case "correction_cancelled":
      return {
        ...state,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
        saveGeneration: state.saveGeneration + 1,
      }
    case "save_started":
      return {
        ...state,
        saveGeneration: action.generation,
        saveStatus: "saving",
        saveError: null,
      }
    case "save_succeeded":
      if (action.generation !== state.saveGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      return {
        ...state,
        details: {
          ...state.details,
          sentiment: action.sentiment,
          detectedIssues: action.detectedIssues,
        },
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
      }
    case "save_failed":
      if (action.generation !== state.saveGeneration) {
        return state
      }
      return {
        ...state,
        saveStatus: "error",
        saveError: action.error,
      }
    default:
      return state
  }
}

function toSnapshot(state: DetailsState): FeedbackDetailsSnapshot {
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    feedbackId: state.feedbackId,
    details: state.details,
    loadError: state.loadError,
    correction: toCorrection(state),
  }
}

export function createInMemoryFeedbackDetailsAdapters(
  initial: Record<number, FeedbackDetailsResponse> = {}
): FeedbackDetailsAdapters {
  const store = new Map<number, FeedbackDetailsResponse>(
    Object.entries(initial).map(([id, details]) => [
      Number(id),
      { ...details },
    ])
  )

  return {
    getFeedbackDetails: async (feedbackId) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      return { ...details }
    },
    correctClassification: async (feedbackId, sentiment) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      if (details.classificationStatus !== "Succeeded") {
        throw new Error("Classification not correctable")
      }
      const updated: FeedbackDetailsResponse = {
        ...details,
        sentiment,
      }
      store.set(feedbackId, updated)
      return {
        classificationStatus: "Succeeded",
        sentiment,
        detectedIssues: updated.detectedIssues ?? [],
      }
    },
  }
}

export function createFeedbackDetailsModule(
  adapters: FeedbackDetailsAdapters,
  options: FeedbackDetailsModuleOptions = {}
): FeedbackDetailsModule {
  const now = options.now ?? (() => Date.now())

  let state: DetailsState = {
    isOpen: false,
    loadStatus: "idle",
    feedbackId: null,
    details: null,
    loadError: null,
    loadGeneration: 0,
    saveGeneration: 0,
    isEditing: false,
    draftSentiment: null,
    saveStatus: "idle",
    saveError: null,
  }

  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = toSnapshot(state)
    emit()
  }

  const dispatch = (action: DetailsAction) => {
    state = reduce(state, action)
    publish()
  }

  const load = async (feedbackId: number) => {
    const generation = state.loadGeneration + 1
    dispatch({ type: "open_started", generation, feedbackId })

    try {
      const result = await adapters.getFeedbackDetails(feedbackId)
      dispatch({
        type: "open_succeeded",
        generation,
        details: toLoadedDetails(result, now()),
      })
    } catch {
      dispatch({
        type: "open_failed",
        generation,
        error: LOAD_ERROR,
      })
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
      dispatch({ type: "reset" })
    },
    reset: () => {
      dispatch({ type: "reset" })
    },
    startCorrection: () => {
      const details = state.details
      if (
        details == null
        || !details.canCorrectClassification
        || details.sentiment == null
        || state.isEditing
      ) {
        return
      }
      dispatch({
        type: "correction_started",
        draftSentiment: details.sentiment,
      })
    },
    setDraftSentiment: (sentiment) => {
      dispatch({ type: "draft_sentiment_set", sentiment })
    },
    cancelCorrection: () => {
      if (!state.isEditing) {
        return
      }
      dispatch({ type: "correction_cancelled" })
    },
    saveCorrection: async () => {
      if (
        state.feedbackId == null
        || state.details == null
        || !canSaveCorrection(state)
        || state.draftSentiment == null
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const sentiment = state.draftSentiment
      const generation = state.saveGeneration + 1
      dispatch({ type: "save_started", generation })

      try {
        const result = await adapters.correctClassification(
          feedbackId,
          sentiment
        )
        if (result.sentiment == null) {
          throw new Error("missing sentiment")
        }
        dispatch({
          type: "save_succeeded",
          generation,
          sentiment: result.sentiment,
          detectedIssues: mapDetectedIssues(result.detectedIssues),
        })
      } catch {
        dispatch({
          type: "save_failed",
          generation,
          error: SAVE_ERROR,
        })
      }
    },
  }
}
