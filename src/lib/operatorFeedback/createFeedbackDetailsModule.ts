import type {
  ContactType,
  FeedbackDetailsActivityEventDto,
  FeedbackDetailsResponse,
  FeedbackInternalNoteItem,
  FeedbackSentiment,
} from "@/types/dashboard"
import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"

const NEW_WINDOW_MS = 24 * 60 * 60 * 1000
const LOAD_ERROR = "Could not load Feedback details. Please try again."
const SAVE_ERROR = "Could not save classification. Please try again."
const NOTE_CREATE_ERROR = "Could not add note. Please try again."
const NOTE_UPDATE_ERROR = "Could not save note. Please try again."
const NOTE_DELETE_ERROR = "Could not delete note. Please try again."
export const FEEDBACK_INTERNAL_NOTE_MAX_LENGTH = 5000

export type { FeedbackDetailsResponse }

export type FeedbackDetailsActivityEvent = FeedbackDetailsActivityEventDto

export type FeedbackDetailsNoteRow = FeedbackInternalNoteItem & {
  createdAtDisplay: string
  isEdited: boolean
}

export type FeedbackDetailsDetectedTag = {
  key: string
  label: string
}

/** Editor session state for correcting AI classification (not the persisted fact). */
export type FeedbackClassificationCorrectionEditor = {
  isEditing: boolean
  draftSentiment: FeedbackSentiment | null
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  canSave: boolean
}

export type FeedbackDetailsNoteEditEditor = {
  editingNoteId: number | null
  draft: string
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  canSave: boolean
}

export type FeedbackDetailsNoteDeleteEditor = {
  deletingNoteId: number | null
  deleteStatus: "idle" | "deleting" | "error"
  deleteError: string | null
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
  detectedTags: FeedbackDetailsDetectedTag[] | null
  canCorrectClassification: boolean
  locationGuestId: number | null
  canViewGuestProfile: boolean
  canAddInternalNote: true
  internalNotes: FeedbackDetailsNoteRow[]
  activityHistory: FeedbackDetailsActivityEvent[]
}

export type FeedbackDetailsSnapshot = {
  isOpen: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  feedbackId: number | null
  details: FeedbackDetailsLoaded | null
  loadError: string | null
  correction: FeedbackClassificationCorrectionEditor
  noteDraft: string
  noteCreateStatus: "idle" | "saving" | "error"
  noteCreateError: string | null
  noteEdit: FeedbackDetailsNoteEditEditor
  noteDelete: FeedbackDetailsNoteDeleteEditor
}

export type CorrectClassificationResponse = {
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  sentiment: FeedbackSentiment | null
  detectedTags: string[] | null
  activityEvent?: FeedbackDetailsActivityEvent | null
}

export type FeedbackDetailsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  correctClassification: (
    feedbackId: number,
    sentiment: FeedbackSentiment
  ) => Promise<CorrectClassificationResponse>
  createInternalNote: (
    feedbackId: number,
    body: string
  ) => Promise<FeedbackInternalNoteItem>
  updateInternalNote: (
    feedbackId: number,
    noteId: number,
    body: string
  ) => Promise<FeedbackInternalNoteItem>
  deleteInternalNote: (
    feedbackId: number,
    noteId: number
  ) => Promise<{ deletedAt: string; deletedByDisplayName: string }>
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
  setNoteDraft: (value: string) => void
  createNote: () => Promise<boolean>
  startEditNote: (noteId: number) => void
  setNoteEditDraft: (value: string) => void
  cancelEditNote: () => void
  saveEditNote: () => Promise<boolean>
  startDeleteNote: (noteId: number) => void
  cancelDeleteNote: () => void
  confirmDeleteNote: () => Promise<boolean>
}

type DetailsState = {
  isOpen: boolean
  loadStatus: FeedbackDetailsSnapshot["loadStatus"]
  feedbackId: number | null
  details: FeedbackDetailsLoaded | null
  loadError: string | null
  loadGeneration: number
  saveGeneration: number
  noteCreateGeneration: number
  noteEditGeneration: number
  noteDeleteGeneration: number
  isEditing: boolean
  draftSentiment: FeedbackSentiment | null
  saveStatus: FeedbackClassificationCorrectionEditor["saveStatus"]
  saveError: string | null
  noteDraft: string
  noteCreateStatus: FeedbackDetailsSnapshot["noteCreateStatus"]
  noteCreateError: string | null
  editingNoteId: number | null
  noteEditDraft: string
  noteEditStatus: FeedbackDetailsNoteEditEditor["saveStatus"]
  noteEditError: string | null
  deletingNoteId: number | null
  noteDeleteStatus: FeedbackDetailsNoteDeleteEditor["deleteStatus"]
  noteDeleteError: string | null
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
      detectedTags: FeedbackDetailsDetectedTag[] | null
      activityEvent: FeedbackDetailsActivityEvent | null
    }
  | { type: "save_failed"; generation: number; error: string }
  | { type: "note_draft_set"; value: string }
  | { type: "note_create_started"; generation: number }
  | {
      type: "note_create_succeeded"
      generation: number
      note: FeedbackInternalNoteItem
    }
  | { type: "note_create_failed"; generation: number; error: string }
  | { type: "note_edit_started"; noteId: number; draft: string }
  | { type: "note_edit_draft_set"; value: string }
  | { type: "note_edit_cancelled" }
  | { type: "note_edit_save_started"; generation: number }
  | {
      type: "note_edit_save_succeeded"
      generation: number
      note: FeedbackInternalNoteItem
    }
  | { type: "note_edit_save_failed"; generation: number; error: string }
  | { type: "note_delete_started"; noteId: number }
  | { type: "note_delete_cancelled" }
  | { type: "note_delete_confirm_started"; generation: number }
  | {
      type: "note_delete_succeeded"
      generation: number
      noteId: number
      deletedAt: string
      actorDisplayName: string
    }
  | { type: "note_delete_failed"; generation: number; error: string }

function emptyNoteEditSession(): Pick<
  DetailsState,
  | "editingNoteId"
  | "noteEditDraft"
  | "noteEditStatus"
  | "noteEditError"
  | "noteEditGeneration"
> {
  return {
    editingNoteId: null,
    noteEditDraft: "",
    noteEditStatus: "idle",
    noteEditError: null,
    noteEditGeneration: 0,
  }
}

function emptyNoteDeleteSession(): Pick<
  DetailsState,
  | "deletingNoteId"
  | "noteDeleteStatus"
  | "noteDeleteError"
  | "noteDeleteGeneration"
> {
  return {
    deletingNoteId: null,
    noteDeleteStatus: "idle",
    noteDeleteError: null,
    noteDeleteGeneration: 0,
  }
}

function canSaveCorrection(state: DetailsState): boolean {
  return (
    state.isEditing
    && state.draftSentiment != null
    && state.details?.sentiment != null
    && state.draftSentiment !== state.details.sentiment
    && state.saveStatus !== "saving"
  )
}

function canSaveNoteEdit(state: DetailsState): boolean {
  const body = state.noteEditDraft.trim()
  return (
    state.editingNoteId != null
    && body.length > 0
    && body.length <= FEEDBACK_INTERNAL_NOTE_MAX_LENGTH
    && state.noteEditStatus !== "saving"
  )
}

function toCorrectionEditor(
  state: DetailsState
): FeedbackClassificationCorrectionEditor {
  return {
    isEditing: state.isEditing,
    draftSentiment: state.draftSentiment,
    saveStatus: state.saveStatus,
    saveError: state.saveError,
    canSave: canSaveCorrection(state),
  }
}

function toNoteEditEditor(state: DetailsState): FeedbackDetailsNoteEditEditor {
  return {
    editingNoteId: state.editingNoteId,
    draft: state.noteEditDraft,
    saveStatus: state.noteEditStatus,
    saveError: state.noteEditError,
    canSave: canSaveNoteEdit(state),
  }
}

function toNoteDeleteEditor(
  state: DetailsState
): FeedbackDetailsNoteDeleteEditor {
  return {
    deletingNoteId: state.deletingNoteId,
    deleteStatus: state.noteDeleteStatus,
    deleteError: state.noteDeleteError,
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

function mapDetectedTags(
  keys: string[] | null | undefined
): FeedbackDetailsDetectedTag[] | null {
  if (keys == null) {
    return null
  }
  return keys.map((key) => ({
    key,
    label: labelForDetectedTag(key),
  }))
}

function mapNoteRow(note: FeedbackInternalNoteItem): FeedbackDetailsNoteRow {
  return {
    ...note,
    createdAtDisplay: formatGuestProfileAbsoluteDateTime(note.createdAt),
    isEdited: note.updatedAt != null && note.updatedAt !== "",
  }
}

/** Legacy fixture fallback when activityHistory is omitted (notes only — no corrections). */
function deriveActivityHistoryFromNotes(
  createdAt: string,
  notesNewestFirst: FeedbackInternalNoteItem[]
): FeedbackDetailsActivityEvent[] {
  const events: FeedbackDetailsActivityEvent[] = [
    { kind: "feedback_received", at: createdAt },
  ]
  const chronological = [...notesNewestFirst].sort((a, b) => {
    const byTime = a.createdAt.localeCompare(b.createdAt)
    if (byTime !== 0) {
      return byTime
    }
    return a.id - b.id
  })
  for (const note of chronological) {
    events.push({
      kind: "note_added",
      at: note.createdAt,
      actorDisplayName: note.authorDisplayName,
    })
  }
  return events
}

function toLoadedDetails(
  response: FeedbackDetailsResponse,
  nowMs: number
): FeedbackDetailsLoaded {
  const succeeded = response.classificationStatus === "Succeeded"
  const internalNotes = (response.internalNotes ?? []).map(mapNoteRow)
  // Prefer server/adapter activityHistory (includes classification corrections).
  const activityHistory =
    response.activityHistory
    ?? deriveActivityHistoryFromNotes(
      response.createdAt,
      response.internalNotes ?? []
    )

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
    detectedTags: succeeded
      ? mapDetectedTags(response.detectedTags ?? [])
      : null,
    canCorrectClassification: succeeded,
    locationGuestId: response.locationGuestId,
    canViewGuestProfile: response.locationGuestId != null,
    canAddInternalNote: true,
    internalNotes,
    activityHistory,
  }
}

function replaceNoteInList(
  notes: FeedbackDetailsNoteRow[],
  note: FeedbackInternalNoteItem
): FeedbackDetailsNoteRow[] {
  const row = mapNoteRow(note)
  return notes.map((item) => (item.id === row.id ? row : item))
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
        noteCreateGeneration: state.noteCreateGeneration + 1,
        isEditing: false,
        draftSentiment: null,
        saveStatus: "idle",
        saveError: null,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
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
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
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
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
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
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
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
          detectedTags: action.detectedTags,
          activityHistory:
            action.activityEvent == null
              ? state.details.activityHistory
              : [
                  ...state.details.activityHistory,
                  action.activityEvent,
                ],
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
      const row = mapNoteRow(action.note)
      const internalNotes = [
        row,
        ...state.details.internalNotes.filter((n) => n.id !== row.id),
      ]
      return {
        ...state,
        details: {
          ...state.details,
          internalNotes,
          activityHistory: [
            ...state.details.activityHistory,
            {
              kind: "note_added",
              at: action.note.createdAt,
              actorDisplayName: action.note.authorDisplayName,
            },
          ],
        },
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
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
    case "note_edit_started":
      return {
        ...state,
        editingNoteId: action.noteId,
        noteEditDraft: action.draft,
        noteEditStatus: "idle",
        noteEditError: null,
        deletingNoteId: null,
        noteDeleteStatus: "idle",
        noteDeleteError: null,
      }
    case "note_edit_draft_set":
      if (state.editingNoteId == null) {
        return state
      }
      return {
        ...state,
        noteEditDraft: action.value,
        noteEditError:
          state.noteEditStatus === "error" ? null : state.noteEditError,
        noteEditStatus:
          state.noteEditStatus === "error" ? "idle" : state.noteEditStatus,
      }
    case "note_edit_cancelled":
      return {
        ...state,
        ...emptyNoteEditSession(),
      }
    case "note_edit_save_started":
      return {
        ...state,
        noteEditGeneration: action.generation,
        noteEditStatus: "saving",
        noteEditError: null,
      }
    case "note_edit_save_succeeded": {
      if (action.generation !== state.noteEditGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      return {
        ...state,
        details: {
          ...state.details,
          internalNotes: replaceNoteInList(
            state.details.internalNotes,
            action.note
          ),
        },
        ...emptyNoteEditSession(),
      }
    }
    case "note_edit_save_failed":
      if (action.generation !== state.noteEditGeneration) {
        return state
      }
      return {
        ...state,
        noteEditStatus: "error",
        noteEditError: action.error,
      }
    case "note_delete_started":
      return {
        ...state,
        deletingNoteId: action.noteId,
        noteDeleteStatus: "idle",
        noteDeleteError: null,
      }
    case "note_delete_cancelled":
      return {
        ...state,
        ...emptyNoteDeleteSession(),
      }
    case "note_delete_confirm_started":
      return {
        ...state,
        noteDeleteGeneration: action.generation,
        noteDeleteStatus: "deleting",
        noteDeleteError: null,
      }
    case "note_delete_succeeded": {
      if (action.generation !== state.noteDeleteGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      const closeEdit =
        state.editingNoteId === action.noteId
          ? emptyNoteEditSession()
          : {}
      return {
        ...state,
        details: {
          ...state.details,
          internalNotes: state.details.internalNotes.filter(
            (note) => note.id !== action.noteId
          ),
          activityHistory: [
            ...state.details.activityHistory,
            {
              kind: "note_deleted",
              at: action.deletedAt,
              actorDisplayName: action.actorDisplayName,
            },
          ],
        },
        ...emptyNoteDeleteSession(),
        ...closeEdit,
      }
    }
    case "note_delete_failed":
      if (action.generation !== state.noteDeleteGeneration) {
        return state
      }
      return {
        ...state,
        noteDeleteStatus: "error",
        noteDeleteError: action.error,
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
    correction: toCorrectionEditor(state),
    noteDraft: state.noteDraft,
    noteCreateStatus: state.noteCreateStatus,
    noteCreateError: state.noteCreateError,
    noteEdit: toNoteEditEditor(state),
    noteDelete: toNoteDeleteEditor(state),
  }
}

function withNotesDefaults(
  details: FeedbackDetailsResponse
): FeedbackDetailsResponse {
  const internalNotes = details.internalNotes ?? []
  return {
    ...details,
    internalNotes,
    activityHistory:
      details.activityHistory
      ?? deriveActivityHistoryFromNotes(details.createdAt, internalNotes),
  }
}

export function createInMemoryFeedbackDetailsAdapters(
  initial: Record<number, FeedbackDetailsResponse> = {}
): FeedbackDetailsAdapters {
  const store = new Map<number, FeedbackDetailsResponse>(
    Object.entries(initial).map(([id, details]) => [
      Number(id),
      withNotesDefaults({ ...details }),
    ])
  )

  let nextNoteId = 1000

  return {
    getFeedbackDetails: async (feedbackId) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      return {
        ...details,
        internalNotes: [...(details.internalNotes ?? [])],
        activityHistory: [...(details.activityHistory ?? [])],
      }
    },
    correctClassification: async (feedbackId, sentiment) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      if (details.classificationStatus !== "Succeeded") {
        throw new Error("Classification not correctable")
      }
      const fromSentiment = details.sentiment
      const activityEvent: FeedbackDetailsActivityEvent | null =
        fromSentiment != null && fromSentiment !== sentiment
          ? {
              kind: "classification_corrected",
              at: new Date().toISOString(),
              actorDisplayName: "Ada Operator",
              fromSentiment,
              toSentiment: sentiment,
            }
          : null
      const activityHistory =
        activityEvent == null
          ? details.activityHistory
          : [...(details.activityHistory ?? []), activityEvent]
      const updated: FeedbackDetailsResponse = {
        ...details,
        sentiment,
        activityHistory,
      }
      store.set(feedbackId, updated)
      return {
        classificationStatus: "Succeeded",
        sentiment,
        detectedTags: updated.detectedTags ?? [],
        activityEvent,
      }
    },
    createInternalNote: async (feedbackId, body) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      const note: FeedbackInternalNoteItem = {
        id: nextNoteId++,
        body,
        authorDisplayName: "Ada Operator",
        createdAt: new Date().toISOString(),
      }
      const internalNotes = [note, ...(details.internalNotes ?? [])]
      const priorHistory =
        details.activityHistory
        ?? deriveActivityHistoryFromNotes(
          details.createdAt,
          details.internalNotes ?? []
        )
      store.set(feedbackId, {
        ...details,
        internalNotes,
        activityHistory: [
          ...priorHistory,
          {
            kind: "note_added",
            at: note.createdAt,
            actorDisplayName: note.authorDisplayName,
          },
        ],
      })
      return note
    },
    updateInternalNote: async (feedbackId, noteId, body) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      const existing = (details.internalNotes ?? []).find(
        (note) => note.id === noteId
      )
      if (existing == null) {
        throw new Error("Note not found")
      }
      const updatedNote: FeedbackInternalNoteItem = {
        ...existing,
        body,
        updatedAt: new Date().toISOString(),
      }
      const internalNotes = (details.internalNotes ?? []).map((note) =>
        note.id === noteId ? updatedNote : note
      )
      store.set(feedbackId, {
        ...details,
        internalNotes,
      })
      return updatedNote
    },
    deleteInternalNote: async (feedbackId, noteId) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      const existing = (details.internalNotes ?? []).find(
        (note) => note.id === noteId
      )
      if (existing == null) {
        throw new Error("Note not found")
      }
      store.set(feedbackId, {
        ...details,
        internalNotes: (details.internalNotes ?? []).filter(
          (note) => note.id !== noteId
        ),
      })
      return {
        deletedAt: new Date().toISOString(),
        deletedByDisplayName: "Ada Operator",
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
    noteCreateGeneration: 0,
    isEditing: false,
    draftSentiment: null,
    saveStatus: "idle",
    saveError: null,
    noteDraft: "",
    noteCreateStatus: "idle",
    noteCreateError: null,
    ...emptyNoteEditSession(),
    ...emptyNoteDeleteSession(),
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
          detectedTags: mapDetectedTags(result.detectedTags),
          activityEvent: result.activityEvent ?? null,
        })
      } catch {
        dispatch({
          type: "save_failed",
          generation,
          error: SAVE_ERROR,
        })
      }
    },
    setNoteDraft: (value) => {
      dispatch({ type: "note_draft_set", value })
    },
    createNote: async () => {
      const body = state.noteDraft.trim()
      if (
        state.feedbackId == null
        || state.details == null
        || body.length === 0
        || body.length > FEEDBACK_INTERNAL_NOTE_MAX_LENGTH
        || state.noteCreateStatus === "saving"
      ) {
        return false
      }

      const feedbackId = state.feedbackId
      const generation = state.noteCreateGeneration + 1
      dispatch({ type: "note_create_started", generation })

      try {
        const note = await adapters.createInternalNote(feedbackId, body)
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
    startEditNote: (noteId) => {
      const note = state.details?.internalNotes.find((item) => item.id === noteId)
      if (note == null) {
        return
      }
      dispatch({ type: "note_edit_started", noteId, draft: note.body })
    },
    setNoteEditDraft: (value) => {
      dispatch({ type: "note_edit_draft_set", value })
    },
    cancelEditNote: () => {
      if (state.editingNoteId == null) {
        return
      }
      dispatch({ type: "note_edit_cancelled" })
    },
    saveEditNote: async () => {
      const body = state.noteEditDraft.trim()
      if (
        state.feedbackId == null
        || state.editingNoteId == null
        || body.length === 0
        || body.length > FEEDBACK_INTERNAL_NOTE_MAX_LENGTH
        || state.noteEditStatus === "saving"
      ) {
        return false
      }

      const feedbackId = state.feedbackId
      const noteId = state.editingNoteId
      const generation = state.noteEditGeneration + 1
      dispatch({ type: "note_edit_save_started", generation })

      try {
        const note = await adapters.updateInternalNote(
          feedbackId,
          noteId,
          body
        )
        dispatch({ type: "note_edit_save_succeeded", generation, note })
        return true
      } catch {
        dispatch({
          type: "note_edit_save_failed",
          generation,
          error: NOTE_UPDATE_ERROR,
        })
        return false
      }
    },
    startDeleteNote: (noteId) => {
      if (state.details?.internalNotes.some((note) => note.id === noteId) !== true) {
        return
      }
      dispatch({ type: "note_delete_started", noteId })
    },
    cancelDeleteNote: () => {
      if (state.deletingNoteId == null) {
        return
      }
      dispatch({ type: "note_delete_cancelled" })
    },
    confirmDeleteNote: async () => {
      if (
        state.feedbackId == null
        || state.deletingNoteId == null
        || state.noteDeleteStatus === "deleting"
      ) {
        return false
      }

      const feedbackId = state.feedbackId
      const noteId = state.deletingNoteId
      const generation = state.noteDeleteGeneration + 1
      dispatch({ type: "note_delete_confirm_started", generation })

      try {
        const deleted = await adapters.deleteInternalNote(feedbackId, noteId)
        dispatch({
          type: "note_delete_succeeded",
          generation,
          noteId,
          deletedAt: deleted.deletedAt,
          actorDisplayName: deleted.deletedByDisplayName,
        })
        return true
      } catch {
        dispatch({
          type: "note_delete_failed",
          generation,
          error: NOTE_DELETE_ERROR,
        })
        return false
      }
    },
  }
}
