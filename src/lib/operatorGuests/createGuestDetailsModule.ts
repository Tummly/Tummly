import {
  GUEST_EDIT_PAGE,
  GUEST_PROFILE_NOT_PROVIDED,
} from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  formatGuestProfileAbsoluteDateTime,
  mapGuestNoteItemToRow,
} from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { isFeedbackNew } from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  formatRelativeTime,
  parseApiInstantMs,
} from "@/lib/operatorHome/relativeTime"
import type {
  GuestProfileLatestFeedbackItem,
  GuestProfileRecentNoteItem,
  GuestProfileResponse,
} from "@/types/dashboard"
import type { GuestMarketingStatusLabel } from "@/types/operatorGuests"
import type { OperatorGuestProfileNoteRow } from "@/types/operatorGuestProfile"

const LOAD_ERROR = "Could not load Guest details. Please try again."
const NOTE_CREATE_ERROR = "Could not add note. Please try again."

export type GuestDetailsTag = {
  id: string
  name: string
}

export type GuestDetailsLatestFeedback = {
  id: number
  quote: string
  sentiment: "positive" | "neutral" | "negative" | null
  isNew: boolean
}

export type GuestDetailsActivityEvent = {
  at: string
  description: string
}

export type GuestDetailsLoaded = {
  id: number
  locationId: number
  name: string
  marketingStatusLabel: GuestMarketingStatusLabel
  identitySubtitle: string
  email: string | null
  emailDisplay: string
  mobileDisplay: string
  emailMarketingLabel: string
  smsMarketingLabel: string
  consentCapturedDisplay: string
  firstCapturedDisplay: string
  locationName: string
  feedbackSubmissionCount: number
  offerRedemptionsDisplay: string
  guestTags: GuestDetailsTag[]
  latestFeedback: GuestDetailsLatestFeedback | null
  /** Named offers/campaigns are not on the profile payload yet. */
  hasOffersOrCampaigns: false
  recentNotes: OperatorGuestProfileNoteRow[]
  recentActivity: GuestDetailsActivityEvent[]
}

export type GuestDetailsSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  guestId: number | null
  locationId: number | null
  details: GuestDetailsLoaded | null
  loadError: string | null
  noteDraft: string
  noteCreateStatus: "idle" | "saving" | "error"
  noteCreateError: string | null
}

export type GuestDetailsAdapters = {
  getGuestProfile: (params: {
    guestId: number
    locationId: number
  }) => Promise<GuestProfileResponse>
  createGuestNote: (params: {
    guestId: number
    locationId: number
    body: string
  }) => Promise<GuestProfileRecentNoteItem>
}

export type GuestDetailsModuleOptions = {
  now?: () => number
}

export type GuestDetailsModule = {
  getSnapshot: () => GuestDetailsSnapshot
  subscribe: (listener: () => void) => () => void
  open: (params: { guestId: number; locationId: number }) => Promise<void>
  retry: () => Promise<void>
  close: () => void
  reset: () => void
  setNoteDraft: (value: string) => void
  createNote: () => Promise<boolean>
}

type DetailsState = {
  isOpen: boolean
  loadStatus: GuestDetailsSnapshot["loadStatus"]
  guestId: number | null
  locationId: number | null
  details: GuestDetailsLoaded | null
  loadError: string | null
  loadGeneration: number
  noteDraft: string
  noteCreateStatus: GuestDetailsSnapshot["noteCreateStatus"]
  noteCreateError: string | null
  noteCreateGeneration: number
}

type DetailsAction =
  | { type: "reset" }
  | {
      type: "open_started"
      generation: number
      guestId: number
      locationId: number
    }
  | {
      type: "open_succeeded"
      generation: number
      details: GuestDetailsLoaded
    }
  | { type: "open_failed"; generation: number; error: string }
  | { type: "note_draft_set"; value: string }
  | { type: "note_create_started"; generation: number }
  | {
      type: "note_create_succeeded"
      generation: number
      note: GuestProfileRecentNoteItem
    }
  | { type: "note_create_failed"; generation: number; error: string }

function formatGuestProfileDate(iso: string): string {
  const ms = parseApiInstantMs(iso)
  if (Number.isNaN(ms)) {
    return ""
  }

  return new Date(ms).toLocaleDateString("en-GB", {
    day: "numeric",
    month: "short",
    year: "numeric",
  })
}

function displayOrNotProvided(value: string | null | undefined): string {
  if (value == null || value.trim() === "") {
    return GUEST_PROFILE_NOT_PROVIDED
  }
  return value
}

function buildIdentitySubtitle(
  guestSinceDisplay: string,
  lastActivityDisplay: string | null
): string {
  if (lastActivityDisplay == null) {
    return `Guest since ${guestSinceDisplay}`
  }

  return `Guest since ${guestSinceDisplay} · Last activity ${lastActivityDisplay}`
}

function marketingLabelForStatus(
  status: GuestProfileResponse["contactEligibility"][number]["status"]
): string {
  return GUEST_EDIT_PAGE.consent.statusLabels[status]
}

function consentCapturedDisplay(
  rows: GuestProfileResponse["contactEligibility"],
  fallbackAt: string | null
): string {
  const withConsent = rows.find(
    (row) => row.detailKind === "consent_captured"
  )
  if (withConsent == null) {
    return GUEST_PROFILE_NOT_PROVIDED
  }

  // API historically left detailAt null for consent_captured; fall back to
  // guestSinceAt (Location Guest created) until the profile payload always
  // includes the captured timestamp.
  const at = withConsent.detailAt ?? fallbackAt
  if (at == null) {
    return GUEST_PROFILE_NOT_PROVIDED
  }
  const formatted = formatGuestProfileAbsoluteDateTime(at)
  return formatted === "" ? GUEST_PROFILE_NOT_PROVIDED : formatted
}

function mapLatestFeedback(
  item: GuestProfileLatestFeedbackItem | undefined,
  nowMs: number
): GuestDetailsLatestFeedback | null {
  if (item == null) {
    return null
  }
  const succeeded = item.classificationStatus === "Succeeded"
  return {
    id: item.id,
    quote: item.comment.trim(),
    sentiment: succeeded ? item.sentiment : null,
    isNew: isFeedbackNew(item.createdAt, nowMs),
  }
}

/**
 * Drawer Recent activity uses the latest feedback on the profile payload as the
 * guest’s last activity (Figma). Full Location Guest activity events stay on
 * Guest Profile → Activity; escalate via View full activity.
 */
function mapRecentActivityFromLatestFeedback(
  item: GuestProfileLatestFeedbackItem | undefined
): GuestDetailsActivityEvent[] {
  if (item == null) {
    return []
  }

  const succeeded = item.classificationStatus === "Succeeded"
  const sentiment = succeeded ? item.sentiment : null
  const description =
    sentiment == null
      ? "Feedback received."
      : `Feedback received and classified as ${
          sentiment === "positive"
            ? "Positive"
            : sentiment === "neutral"
              ? "Neutral"
              : "Negative"
        }.`

  return [
    {
      at: item.createdAt,
      description,
    },
  ]
}

export function mapGuestProfileToGuestDetails(
  response: GuestProfileResponse,
  nowMs: number = Date.now()
): GuestDetailsLoaded {
  const guestSinceDisplay = formatGuestProfileDate(response.guestSinceAt)
  const lastActivityRelative =
    response.lastActivityAt == null
      ? null
      : formatRelativeTime(response.lastActivityAt, nowMs) || null
  const email = response.profileSummary.email?.trim() || null
  const emailRow = response.contactEligibility.find(
    (row) => row.channel === "email"
  )
  const smsRow = response.contactEligibility.find(
    (row) => row.channel === "sms"
  )
  const latest = (response.latestFeedback ?? [])[0]

  return {
    id: response.id,
    locationId: response.locationId,
    name: response.name,
    marketingStatusLabel:
      response.marketingStatus as GuestMarketingStatusLabel,
    identitySubtitle: buildIdentitySubtitle(
      guestSinceDisplay,
      lastActivityRelative
    ),
    email,
    emailDisplay: displayOrNotProvided(email),
    mobileDisplay: displayOrNotProvided(response.profileSummary.mobile),
    emailMarketingLabel: marketingLabelForStatus(
      emailRow?.status ?? "not_provided"
    ),
    smsMarketingLabel: marketingLabelForStatus(
      smsRow?.status ?? "not_provided"
    ),
    consentCapturedDisplay: consentCapturedDisplay(
      response.contactEligibility,
      response.guestSinceAt
    ),
    firstCapturedDisplay: formatGuestProfileDate(
      response.profileSummary.firstCapturedAt
    ),
    locationName: response.profileSummary.locationName,
    feedbackSubmissionCount: response.profileSummary.feedbackSubmissionCount,
    offerRedemptionsDisplay: String(
      response.profileSummary.offerClaimsAndRedemptions
    ),
    guestTags: response.profileSummary.guestTags.map((tag) => ({
      id: String(tag.id),
      name: tag.name,
    })),
    latestFeedback: mapLatestFeedback(latest, nowMs),
    hasOffersOrCampaigns: false,
    recentNotes: (response.recentNotes ?? []).map(mapGuestNoteItemToRow),
    recentActivity: mapRecentActivityFromLatestFeedback(latest),
  }
}

function reduce(state: DetailsState, action: DetailsAction): DetailsState {
  switch (action.type) {
    case "reset":
      return {
        ...state,
        isOpen: false,
        loadStatus: "idle",
        guestId: null,
        locationId: null,
        details: null,
        loadError: null,
        loadGeneration: state.loadGeneration + 1,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        noteCreateGeneration: state.noteCreateGeneration + 1,
      }
    case "open_started":
      return {
        ...state,
        isOpen: true,
        loadStatus: "loading",
        loadGeneration: action.generation,
        guestId: action.guestId,
        locationId: action.locationId,
        details: null,
        loadError: null,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        noteCreateGeneration: state.noteCreateGeneration + 1,
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
    case "note_draft_set":
      return {
        ...state,
        noteDraft: action.value,
        noteCreateError:
          state.noteCreateStatus === "error" ? null : state.noteCreateError,
        noteCreateStatus:
          state.noteCreateStatus === "error" ? "idle" : state.noteCreateStatus,
      }
    case "note_create_started":
      return {
        ...state,
        noteCreateGeneration: action.generation,
        noteCreateStatus: "saving",
        noteCreateError: null,
      }
    case "note_create_succeeded": {
      if (action.generation !== state.noteCreateGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      const row = mapGuestNoteItemToRow(action.note)
      return {
        ...state,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        details: {
          ...state.details,
          recentNotes: [
            row,
            ...state.details.recentNotes.filter((n) => n.id !== row.id),
          ],
        },
      }
    }
    case "note_create_failed":
      if (action.generation !== state.noteCreateGeneration) {
        return state
      }
      return {
        ...state,
        noteCreateStatus: "error",
        noteCreateError: action.error,
      }
    default:
      return state
  }
}

function toSnapshot(state: DetailsState): GuestDetailsSnapshot {
  return {
    isOpen: state.isOpen,
    loadStatus: state.loadStatus,
    guestId: state.guestId,
    locationId: state.locationId,
    details: state.details,
    loadError: state.loadError,
    noteDraft: state.noteDraft,
    noteCreateStatus: state.noteCreateStatus,
    noteCreateError: state.noteCreateError,
  }
}

export function createInMemoryGuestDetailsAdapters(initial: {
  profiles?: Record<string, GuestProfileResponse>
  notes?: GuestProfileRecentNoteItem[]
} = {}): GuestDetailsAdapters {
  const profiles = new Map<string, GuestProfileResponse>(
    Object.entries(initial.profiles ?? {}).map(([key, profile]) => [
      key,
      { ...profile },
    ])
  )
  let nextNoteId =
    (initial.notes ?? []).reduce(
      (max, note) => Math.max(max, note.id),
      0
    ) + 1

  const profileKey = (guestId: number, locationId: number) =>
    `${locationId}:${guestId}`

  return {
    getGuestProfile: async ({ guestId, locationId }) => {
      const profile = profiles.get(profileKey(guestId, locationId))
      if (profile == null) {
        throw new Error("Guest not found")
      }
      return { ...profile }
    },
    createGuestNote: async ({ guestId, locationId, body }) => {
      const key = profileKey(guestId, locationId)
      const profile = profiles.get(key)
      if (profile == null) {
        throw new Error("Guest not found")
      }
      const note: GuestProfileRecentNoteItem = {
        id: nextNoteId++,
        body,
        authorDisplayName: "You",
        createdAt: new Date().toISOString(),
      }
      profiles.set(key, {
        ...profile,
        recentNotes: [note, ...(profile.recentNotes ?? [])],
      })
      return { ...note }
    },
  }
}

export function createGuestDetailsModule(
  adapters: GuestDetailsAdapters,
  options: GuestDetailsModuleOptions = {}
): GuestDetailsModule {
  const now = options.now ?? (() => Date.now())

  let state: DetailsState = {
    isOpen: false,
    loadStatus: "idle",
    guestId: null,
    locationId: null,
    details: null,
    loadError: null,
    loadGeneration: 0,
    noteDraft: "",
    noteCreateStatus: "idle",
    noteCreateError: null,
    noteCreateGeneration: 0,
  }

  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state)
    for (const listener of listeners) {
      listener()
    }
  }

  const dispatch = (action: DetailsAction) => {
    state = reduce(state, action)
    publish()
  }

  const load = async (guestId: number, locationId: number) => {
    const generation = state.loadGeneration + 1
    dispatch({ type: "open_started", generation, guestId, locationId })

    try {
      const result = await adapters.getGuestProfile({ guestId, locationId })
      dispatch({
        type: "open_succeeded",
        generation,
        details: mapGuestProfileToGuestDetails(result, now()),
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
    open: ({ guestId, locationId }) => load(guestId, locationId),
    retry: async () => {
      if (state.guestId == null || state.locationId == null) {
        return
      }
      await load(state.guestId, state.locationId)
    },
    close: () => {
      dispatch({ type: "reset" })
    },
    reset: () => {
      dispatch({ type: "reset" })
    },
    setNoteDraft: (value) => {
      dispatch({ type: "note_draft_set", value })
    },
    createNote: async () => {
      const body = state.noteDraft.trim()
      if (
        state.guestId == null
        || state.locationId == null
        || state.details == null
        || body.length === 0
        || state.noteCreateStatus === "saving"
      ) {
        return false
      }

      const guestId = state.guestId
      const locationId = state.locationId
      const generation = state.noteCreateGeneration + 1
      dispatch({ type: "note_create_started", generation })

      try {
        const note = await adapters.createGuestNote({
          guestId,
          locationId,
          body,
        })
        dispatch({ type: "note_create_succeeded", generation, note })
        return true
      } catch {
        dispatch({
          type: "note_create_failed",
          generation,
          error: NOTE_CREATE_ERROR,
        })
        return false
      }
    },
  }
}
