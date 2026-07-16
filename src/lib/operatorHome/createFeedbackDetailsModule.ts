import type { ContactType, FeedbackDetailsResponse } from "@/types/dashboard"
import { labelForDetectedIssue } from "@/lib/operatorHome/detectedIssues"

const NEW_WINDOW_MS = 24 * 60 * 60 * 1000
const LOAD_ERROR = "Could not load Feedback details. Please try again."

export type { FeedbackDetailsResponse }

export type FeedbackDetailsActivityEvent = {
  kind: "feedback_received"
  at: string
}

export type FeedbackDetailsDetectedIssue = {
  key: string
  label: string
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
  sentiment: "positive" | "neutral" | "negative" | null
  detectedIssues: FeedbackDetailsDetectedIssue[] | null
  /** Phase 1b — Correct classification stays non-interactive. */
  canCorrectClassification: false
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
}

export type FeedbackDetailsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
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
}

type DetailsState = FeedbackDetailsSnapshot & {
  loadGeneration: number
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
      ? (response.detectedIssues ?? []).map((key) => ({
          key,
          label: labelForDetectedIssue(key),
        }))
      : null,
    canCorrectClassification: false,
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
        // Invalidate in-flight opens so stale resolutions are ignored.
        loadGeneration: state.loadGeneration + 1,
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
  }
}
