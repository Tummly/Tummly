import type {
  ContactType,
  FeedbackClassificationCorrectionReason,
  FeedbackDetailsActivityEventDto,
  FeedbackDetailsResponse,
  FeedbackInternalNoteItem,
  FeedbackSentiment,
  FeedbackWorkflowStatus,
} from "@/types/dashboard"
import { formatGuestProfileAbsoluteDateTime } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import { labelForDetectedTag } from "@/lib/operatorHome/detectedTags"
import {
  canConfirmFeedbackCloseOut,
  type FeedbackCloseOutIntent,
  type FeedbackCloseOutReason,
} from "@/lib/operatorFeedback/feedbackCloseOutPresentation"
import { canSaveFeedbackClassificationCorrection } from "@/lib/operatorFeedback/feedbackClassificationCorrectionPresentation"

const NEW_WINDOW_MS = 24 * 60 * 60 * 1000
const LOAD_ERROR = "Could not load Feedback details. Please try again."
const SAVE_ERROR = "Could not save classification. Please try again."
const WORKFLOW_STATUS_ERROR =
  "Could not update follow-up status. Please try again."
const NOTE_CREATE_ERROR = "Could not add note. Please try again."
const NOTE_UPDATE_ERROR = "Could not save note. Please try again."
const NOTE_DELETE_ERROR = "Could not delete note. Please try again."
const CLOSE_OUT_ERROR = "Could not close out feedback. Please try again."
export const FEEDBACK_INTERNAL_NOTE_MAX_LENGTH = 5000

export type { FeedbackCloseOutIntent, FeedbackCloseOutReason }

export type { FeedbackDetailsResponse, FeedbackWorkflowStatus }

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
  draftReason: FeedbackClassificationCorrectionReason | null
  draftNote: string
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

export type FeedbackDetailsCloseOutEditor = {
  isOpen: boolean
  intent: FeedbackCloseOutIntent | null
  reason: FeedbackCloseOutReason | null
  noteDraft: string
  acknowledged: boolean
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  canConfirm: boolean
}

export type FeedbackDetailsLoaded = {
  id: number
  guestName: string
  guestContact: string
  contactType: ContactType
  /** Follow-up Contact availability badge — Email / Phone / No contact. */
  contactAvailability: "Email" | "Phone" | "No contact"
  comment: string
  createdAt: string
  /** ISO datetime when AI classification completed; falls back to createdAt. */
  classifiedAt: string
  locationName: string
  address: string
  /** QR type label or Digital guest link Link name; null when unknown. */
  qrSource: string | null
  /** `{locationName} · {qrSource}` or locationName alone when QR unknown. */
  venueLine: string
  /** Operator-facing `FDB-{padded id}`. */
  feedbackReference: string
  /**
   * Absolute datetime of the newest note / workflow-status change /
   * classification correction, or “No follow-up recorded”.
   */
  lastFollowUpDisplay: string
  isNew: boolean
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  sentiment: FeedbackSentiment | null
  detectedTags: FeedbackDetailsDetectedTag[] | null
  canCorrectClassification: boolean
  locationGuestId: number | null
  canViewGuestProfile: boolean
  canAddInternalNote: true
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  canReopen: boolean
  canMarkNoActionNeeded: boolean
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
  workflowSaveStatus: "idle" | "saving" | "error"
  workflowSaveError: string | null
  noteDraft: string
  noteCreateStatus: "idle" | "saving" | "error"
  noteCreateError: string | null
  noteEdit: FeedbackDetailsNoteEditEditor
  noteDelete: FeedbackDetailsNoteDeleteEditor
  closeOut: FeedbackDetailsCloseOutEditor
}

export type CorrectClassificationResponse = {
  classificationStatus: "Pending" | "Succeeded" | "Failed"
  sentiment: FeedbackSentiment | null
  detectedTags: string[] | null
  activityEvent?: FeedbackDetailsActivityEvent | null
}

export type SetWorkflowStatusResponse = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent?: FeedbackDetailsActivityEvent | null
}

export type CloseOutFeedbackResponse = {
  workflowStatus: FeedbackWorkflowStatus
  needsAttention: boolean
  activityEvent: FeedbackDetailsActivityEvent
  noteActivityEvent?: FeedbackDetailsActivityEvent | null
  note?: FeedbackInternalNoteItem | null
}

export type FeedbackDetailsAdapters = {
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  correctClassification: (
    feedbackId: number,
    input: {
      sentiment: FeedbackSentiment
      reason: FeedbackClassificationCorrectionReason
      noteBody?: string
    }
  ) => Promise<CorrectClassificationResponse>
  setWorkflowStatus: (
    feedbackId: number,
    workflowStatus: FeedbackWorkflowStatus
  ) => Promise<SetWorkflowStatusResponse>
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
  closeOutFeedback: (
    feedbackId: number,
    input: {
      intent: FeedbackCloseOutIntent
      reason: FeedbackCloseOutReason
      noteBody?: string
    }
  ) => Promise<CloseOutFeedbackResponse>
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
  setDraftReason: (reason: FeedbackClassificationCorrectionReason) => void
  setDraftNote: (note: string) => void
  cancelCorrection: () => void
  saveCorrection: () => Promise<void>
  setWorkflowStatus: (status: FeedbackWorkflowStatus) => Promise<boolean>
  reopen: () => Promise<boolean>
  startMarkNoActionNeeded: () => boolean
  startMarkResolved: () => boolean
  startCloseOut: (intent: FeedbackCloseOutIntent) => boolean
  setCloseOutReason: (reason: FeedbackCloseOutReason) => void
  setCloseOutNoteDraft: (value: string) => void
  setCloseOutAcknowledged: (value: boolean) => void
  cancelCloseOut: () => void
  confirmCloseOut: () => Promise<boolean>
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
  workflowSaveGeneration: number
  noteCreateGeneration: number
  noteEditGeneration: number
  noteDeleteGeneration: number
  isEditing: boolean
  draftSentiment: FeedbackSentiment | null
  draftReason: FeedbackClassificationCorrectionReason | null
  draftNote: string
  saveStatus: FeedbackClassificationCorrectionEditor["saveStatus"]
  saveError: string | null
  workflowSaveStatus: FeedbackDetailsSnapshot["workflowSaveStatus"]
  workflowSaveError: string | null
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
  closeOutIsOpen: boolean
  closeOutIntent: FeedbackCloseOutIntent | null
  closeOutReason: FeedbackCloseOutReason | null
  closeOutNoteDraft: string
  closeOutAcknowledged: boolean
  closeOutSaveStatus: FeedbackDetailsCloseOutEditor["saveStatus"]
  closeOutSaveError: string | null
  closeOutSaveGeneration: number
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
  | { type: "correction_started" }
  | { type: "draft_sentiment_set"; sentiment: FeedbackSentiment }
  | { type: "draft_reason_set"; reason: FeedbackClassificationCorrectionReason }
  | { type: "draft_note_set"; value: string }
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
  | { type: "workflow_save_started"; generation: number }
  | {
      type: "workflow_save_succeeded"
      generation: number
      workflowStatus: FeedbackWorkflowStatus
      needsAttention: boolean
      activityEvent: FeedbackDetailsActivityEvent | null
    }
  | { type: "workflow_save_failed"; generation: number; error: string }
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
  | { type: "close_out_started"; intent: FeedbackCloseOutIntent }
  | { type: "close_out_reason_set"; reason: FeedbackCloseOutReason }
  | { type: "close_out_note_draft_set"; value: string }
  | { type: "close_out_acknowledged_set"; value: boolean }
  | { type: "close_out_cancelled" }
  | { type: "close_out_save_started"; generation: number }
  | {
      type: "close_out_save_succeeded"
      generation: number
      workflowStatus: FeedbackWorkflowStatus
      needsAttention: boolean
      activityEvent: FeedbackDetailsActivityEvent
      noteActivityEvent: FeedbackDetailsActivityEvent | null
      note: FeedbackInternalNoteItem | null
    }
  | { type: "close_out_save_failed"; generation: number; error: string }

function emptyCloseOutSession(): Pick<
  DetailsState,
  | "closeOutIsOpen"
  | "closeOutIntent"
  | "closeOutReason"
  | "closeOutNoteDraft"
  | "closeOutAcknowledged"
  | "closeOutSaveStatus"
  | "closeOutSaveError"
  | "closeOutSaveGeneration"
> {
  return {
    closeOutIsOpen: false,
    closeOutIntent: null,
    closeOutReason: null,
    closeOutNoteDraft: "",
    closeOutAcknowledged: false,
    closeOutSaveStatus: "idle",
    closeOutSaveError: null,
    closeOutSaveGeneration: 0,
  }
}

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
  return canSaveFeedbackClassificationCorrection({
    currentSentiment: state.details?.sentiment ?? null,
    draftSentiment: state.draftSentiment,
    reason: state.draftReason,
    noteDraft: state.draftNote,
    saveStatus: state.saveStatus,
  })
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
    draftReason: state.draftReason,
    draftNote: state.draftNote,
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

function toCloseOutEditor(state: DetailsState): FeedbackDetailsCloseOutEditor {
  return {
    isOpen: state.closeOutIsOpen,
    intent: state.closeOutIntent,
    reason: state.closeOutReason,
    noteDraft: state.closeOutNoteDraft,
    acknowledged: state.closeOutAcknowledged,
    saveStatus: state.closeOutSaveStatus,
    saveError: state.closeOutSaveError,
    canConfirm: canConfirmFeedbackCloseOut({
      intent: state.closeOutIntent,
      reason: state.closeOutReason,
      noteDraft: state.closeOutNoteDraft,
      acknowledged: state.closeOutAcknowledged,
    }),
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

export function deriveFeedbackNeedsAttention(
  classificationStatus: FeedbackDetailsLoaded["classificationStatus"],
  sentiment: FeedbackSentiment | null,
  workflowStatus: FeedbackWorkflowStatus
): boolean {
  return (
    classificationStatus === "Succeeded"
    && sentiment === "negative"
    && workflowStatus !== "resolved"
  )
}

export function feedbackWorkflowStatusLabel(
  status: FeedbackWorkflowStatus
): string {
  switch (status) {
    case "new":
      return "New"
    case "in_progress":
      return "In progress"
    case "resolved":
      return "Resolved"
  }
}

function parseWorkflowStatus(
  value: FeedbackWorkflowStatus | undefined
): FeedbackWorkflowStatus {
  if (value === "new" || value === "in_progress" || value === "resolved") {
    return value
  }
  return "new"
}

function withWorkflowFlags(
  details: Pick<
    FeedbackDetailsLoaded,
    "classificationStatus" | "sentiment" | "workflowStatus"
  >
): Pick<
  FeedbackDetailsLoaded,
  "needsAttention" | "canReopen" | "canMarkNoActionNeeded"
> {
  return {
    needsAttention: deriveFeedbackNeedsAttention(
      details.classificationStatus,
      details.sentiment,
      details.workflowStatus
    ),
    canReopen: details.workflowStatus === "resolved",
    canMarkNoActionNeeded: details.workflowStatus !== "resolved",
  }
}

export function formatFeedbackVenueLine(
  locationName: string,
  qrSource: string | null | undefined
): string {
  const name = locationName.trim()
  const source = qrSource?.trim() ?? ""
  if (!source) {
    return name
  }
  return `${name} · ${source}`
}

/** Operator-facing Feedback reference — `FDB-{padded numeric id}`. */
export function formatFeedbackReference(feedbackId: number): string {
  const padded = String(Math.trunc(feedbackId)).padStart(6, "0")
  return `FDB-${padded}`
}

export type FeedbackContactAvailability = "Email" | "Phone" | "No contact"

export function deriveFeedbackContactAvailability(
  contactType: ContactType,
  guestContact: string
): FeedbackContactAvailability {
  const hasContact = guestContact.trim() !== ""
  if (!hasContact || contactType === "Unknown") {
    return "No contact"
  }
  if (contactType === "Email") {
    return "Email"
  }
  if (contactType === "Phone") {
    return "Phone"
  }
  return "No contact"
}

const NO_FOLLOW_UP_RECORDED = "No follow-up recorded"

/** Newest note / workflow-status / classification-correction → display string. */
export function deriveLastFollowUpDisplay(
  notes: Array<{ createdAt: string }>,
  activityHistory: FeedbackDetailsActivityEvent[]
): string {
  let newestMs = Number.NEGATIVE_INFINITY

  for (const note of notes) {
    const createdMs = Date.parse(note.createdAt)
    if (!Number.isNaN(createdMs) && createdMs > newestMs) {
      newestMs = createdMs
    }
  }

  for (const event of activityHistory) {
    if (
      event.kind !== "workflow_status_changed"
      && event.kind !== "classification_corrected"
      && event.kind !== "feedback_closed_out"
    ) {
      continue
    }
    const atMs = Date.parse(event.at)
    if (!Number.isNaN(atMs) && atMs > newestMs) {
      newestMs = atMs
    }
  }

  if (!Number.isFinite(newestMs) || newestMs < 0) {
    return NO_FOLLOW_UP_RECORDED
  }

  const formatted = formatGuestProfileAbsoluteDateTime(
    new Date(newestMs).toISOString()
  )
  return formatted === "" ? NO_FOLLOW_UP_RECORDED : formatted
}

function withLastFollowUp(
  details: Omit<FeedbackDetailsLoaded, "lastFollowUpDisplay"> & {
    lastFollowUpDisplay?: string
  }
): FeedbackDetailsLoaded {
  return {
    ...details,
    lastFollowUpDisplay: deriveLastFollowUpDisplay(
      details.internalNotes,
      details.activityHistory
    ),
  }
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
  const sentiment = succeeded ? response.sentiment : null
  const workflowStatus = parseWorkflowStatus(response.workflowStatus)
  const internalNotes = (response.internalNotes ?? []).map(mapNoteRow)
  // Prefer server/adapter activityHistory (includes classification corrections).
  const activityHistory =
    response.activityHistory
    ?? deriveActivityHistoryFromNotes(
      response.createdAt,
      response.internalNotes ?? []
    )

  const derivedNeeds = deriveFeedbackNeedsAttention(
    response.classificationStatus,
    sentiment,
    workflowStatus
  )

  return withLastFollowUp({
    id: response.id,
    guestName: response.guestName,
    guestContact: response.guestContact,
    contactType: response.contactType,
    contactAvailability: deriveFeedbackContactAvailability(
      response.contactType,
      response.guestContact
    ),
    comment: response.comment,
    createdAt: response.createdAt,
    classifiedAt: response.classifiedAt ?? response.createdAt,
    locationName: response.locationName,
    address: response.address,
    qrSource:
      response.qrSource != null && response.qrSource.trim() !== ""
        ? response.qrSource.trim()
        : null,
    venueLine: formatFeedbackVenueLine(
      response.locationName,
      response.qrSource
    ),
    feedbackReference: formatFeedbackReference(response.id),
    isNew: isFeedbackNew(response.createdAt, nowMs),
    classificationStatus: response.classificationStatus,
    sentiment,
    detectedTags: succeeded
      ? mapDetectedTags(response.detectedTags ?? [])
      : null,
    canCorrectClassification: succeeded,
    locationGuestId: response.locationGuestId,
    canViewGuestProfile: response.locationGuestId != null,
    canAddInternalNote: true,
    workflowStatus,
    needsAttention: response.needsAttention ?? derivedNeeds,
    canReopen: workflowStatus === "resolved",
    canMarkNoActionNeeded: workflowStatus !== "resolved",
    internalNotes,
    activityHistory,
  })
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
        workflowSaveGeneration: state.workflowSaveGeneration + 1,
        noteCreateGeneration: state.noteCreateGeneration + 1,
        isEditing: false,
        draftSentiment: null,
        draftReason: null,
        draftNote: "",
        saveStatus: "idle",
        saveError: null,
        workflowSaveStatus: "idle",
        workflowSaveError: null,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
        ...emptyCloseOutSession(),
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
        draftReason: null,
        draftNote: "",
        saveStatus: "idle",
        saveError: null,
        workflowSaveStatus: "idle",
        workflowSaveError: null,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
        ...emptyCloseOutSession(),
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
        draftReason: null,
        draftNote: "",
        saveStatus: "idle",
        saveError: null,
        workflowSaveStatus: "idle",
        workflowSaveError: null,
        noteDraft: "",
        noteCreateStatus: "idle",
        noteCreateError: null,
        ...emptyNoteEditSession(),
        ...emptyNoteDeleteSession(),
        ...emptyCloseOutSession(),
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
        draftReason: null,
        draftNote: "",
        saveStatus: "idle",
        saveError: null,
        workflowSaveStatus: "idle",
        workflowSaveError: null,
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
        draftSentiment: null,
        draftReason: null,
        draftNote: "",
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
    case "draft_reason_set":
      if (!state.isEditing) {
        return state
      }
      return {
        ...state,
        draftReason: action.reason,
        saveError: null,
        saveStatus:
          state.saveStatus === "error" ? "idle" : state.saveStatus,
      }
    case "draft_note_set":
      if (!state.isEditing) {
        return state
      }
      return {
        ...state,
        draftNote: action.value,
        saveError: null,
        saveStatus:
          state.saveStatus === "error" ? "idle" : state.saveStatus,
      }
    case "correction_cancelled":
      return {
        ...state,
        isEditing: false,
        draftSentiment: null,
        draftReason: null,
        draftNote: "",
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
        details: withLastFollowUp({
          ...state.details,
          sentiment: action.sentiment,
          detectedTags: action.detectedTags,
          ...withWorkflowFlags({
            classificationStatus: state.details.classificationStatus,
            sentiment: action.sentiment,
            workflowStatus: state.details.workflowStatus,
          }),
          activityHistory:
            action.activityEvent == null
              ? state.details.activityHistory
              : [
                  ...state.details.activityHistory,
                  action.activityEvent,
                ],
        }),
        isEditing: false,
        draftSentiment: null,
        draftReason: null,
        draftNote: "",
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
    case "workflow_save_started":
      return {
        ...state,
        workflowSaveGeneration: action.generation,
        workflowSaveStatus: "saving",
        workflowSaveError: null,
      }
    case "workflow_save_succeeded":
      if (action.generation !== state.workflowSaveGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      return {
        ...state,
        details: withLastFollowUp({
          ...state.details,
          workflowStatus: action.workflowStatus,
          needsAttention: action.needsAttention,
          canReopen: action.workflowStatus === "resolved",
          canMarkNoActionNeeded: action.workflowStatus !== "resolved",
          activityHistory:
            action.activityEvent == null
              ? state.details.activityHistory
              : [
                  ...state.details.activityHistory,
                  action.activityEvent,
                ],
        }),
        workflowSaveStatus: "idle",
        workflowSaveError: null,
      }
    case "workflow_save_failed":
      if (action.generation !== state.workflowSaveGeneration) {
        return state
      }
      return {
        ...state,
        workflowSaveStatus: "error",
        workflowSaveError: action.error,
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
        details: withLastFollowUp({
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
        }),
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
        details: withLastFollowUp({
          ...state.details,
          internalNotes: replaceNoteInList(
            state.details.internalNotes,
            action.note
          ),
        }),
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
        details: withLastFollowUp({
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
        }),
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
    case "close_out_started":
      if (state.details == null || !state.details.canMarkNoActionNeeded) {
        return state
      }
      return {
        ...state,
        closeOutIsOpen: true,
        closeOutIntent: action.intent,
        closeOutReason: null,
        closeOutNoteDraft: "",
        closeOutAcknowledged: false,
        closeOutSaveStatus: "idle",
        closeOutSaveError: null,
      }
    case "close_out_reason_set":
      if (!state.closeOutIsOpen) {
        return state
      }
      return {
        ...state,
        closeOutReason: action.reason,
        closeOutNoteDraft:
          action.reason === "other" ? state.closeOutNoteDraft : "",
        closeOutSaveError:
          state.closeOutSaveStatus === "error" ? null : state.closeOutSaveError,
        closeOutSaveStatus:
          state.closeOutSaveStatus === "error"
            ? "idle"
            : state.closeOutSaveStatus,
      }
    case "close_out_note_draft_set":
      if (!state.closeOutIsOpen) {
        return state
      }
      return {
        ...state,
        closeOutNoteDraft: action.value,
        closeOutSaveError:
          state.closeOutSaveStatus === "error" ? null : state.closeOutSaveError,
        closeOutSaveStatus:
          state.closeOutSaveStatus === "error"
            ? "idle"
            : state.closeOutSaveStatus,
      }
    case "close_out_acknowledged_set":
      if (!state.closeOutIsOpen) {
        return state
      }
      return {
        ...state,
        closeOutAcknowledged: action.value,
        closeOutSaveError:
          state.closeOutSaveStatus === "error" ? null : state.closeOutSaveError,
        closeOutSaveStatus:
          state.closeOutSaveStatus === "error"
            ? "idle"
            : state.closeOutSaveStatus,
      }
    case "close_out_cancelled":
      return {
        ...state,
        ...emptyCloseOutSession(),
        closeOutSaveGeneration: state.closeOutSaveGeneration + 1,
      }
    case "close_out_save_started":
      return {
        ...state,
        closeOutSaveGeneration: action.generation,
        closeOutSaveStatus: "saving",
        closeOutSaveError: null,
      }
    case "close_out_save_succeeded": {
      if (action.generation !== state.closeOutSaveGeneration) {
        return state
      }
      if (state.details == null) {
        return state
      }
      const activityHistory = [
        ...state.details.activityHistory,
        action.activityEvent,
      ]
      const withNoteActivity =
        action.noteActivityEvent == null
          ? activityHistory
          : [...activityHistory, action.noteActivityEvent]
      const internalNotes =
        action.note == null
          ? state.details.internalNotes
          : [
              mapNoteRow(action.note),
              ...state.details.internalNotes.filter(
                (item) => item.id !== action.note!.id
              ),
            ]
      return {
        ...state,
        details: withLastFollowUp({
          ...state.details,
          workflowStatus: action.workflowStatus,
          needsAttention: action.needsAttention,
          canReopen: action.workflowStatus === "resolved",
          canMarkNoActionNeeded: action.workflowStatus !== "resolved",
          internalNotes,
          activityHistory: withNoteActivity,
        }),
        ...emptyCloseOutSession(),
      }
    }
    case "close_out_save_failed":
      if (action.generation !== state.closeOutSaveGeneration) {
        return state
      }
      return {
        ...state,
        closeOutSaveStatus: "error",
        closeOutSaveError: action.error,
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
    workflowSaveStatus: state.workflowSaveStatus,
    workflowSaveError: state.workflowSaveError,
    noteDraft: state.noteDraft,
    noteCreateStatus: state.noteCreateStatus,
    noteCreateError: state.noteCreateError,
    noteEdit: toNoteEditEditor(state),
    noteDelete: toNoteDeleteEditor(state),
    closeOut: toCloseOutEditor(state),
  }
}

function withNotesDefaults(
  details: FeedbackDetailsResponse
): FeedbackDetailsResponse {
  const internalNotes = details.internalNotes ?? []
  const workflowStatus = parseWorkflowStatus(details.workflowStatus)
  const succeeded = details.classificationStatus === "Succeeded"
  const sentiment = succeeded ? details.sentiment : null
  return {
    ...details,
    workflowStatus,
    needsAttention:
      details.needsAttention
      ?? deriveFeedbackNeedsAttention(
        details.classificationStatus,
        sentiment,
        workflowStatus
      ),
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
    correctClassification: async (feedbackId, input) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      if (details.classificationStatus !== "Succeeded") {
        throw new Error("Classification not correctable")
      }
      const { sentiment } = input
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
      const workflowStatus = parseWorkflowStatus(details.workflowStatus)
      const updated: FeedbackDetailsResponse = {
        ...details,
        sentiment,
        needsAttention: deriveFeedbackNeedsAttention(
          "Succeeded",
          sentiment,
          workflowStatus
        ),
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
    setWorkflowStatus: async (feedbackId, workflowStatus) => {
      if (workflowStatus === "resolved") {
        throw new Error("Use closeOutFeedback to resolve feedback")
      }
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      const fromStatus = parseWorkflowStatus(details.workflowStatus)
      const activityEvent: FeedbackDetailsActivityEvent | null =
        fromStatus !== workflowStatus
          ? {
              kind: "workflow_status_changed",
              at: new Date().toISOString(),
              actorDisplayName: "Ada Operator",
              fromWorkflowStatus: fromStatus,
              toWorkflowStatus: workflowStatus,
            }
          : null
      const activityHistory =
        activityEvent == null
          ? details.activityHistory
          : [...(details.activityHistory ?? []), activityEvent]
      const succeeded = details.classificationStatus === "Succeeded"
      const sentiment = succeeded ? details.sentiment : null
      const needsAttention = deriveFeedbackNeedsAttention(
        details.classificationStatus,
        sentiment,
        workflowStatus
      )
      const updated: FeedbackDetailsResponse = {
        ...details,
        workflowStatus,
        needsAttention,
        activityHistory,
      }
      store.set(feedbackId, updated)
      return {
        workflowStatus,
        needsAttention,
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
    closeOutFeedback: async (feedbackId, input) => {
      const details = store.get(feedbackId)
      if (details == null) {
        throw new Error("Feedback not found")
      }
      const fromStatus = parseWorkflowStatus(details.workflowStatus)
      if (fromStatus === "resolved") {
        throw new Error("Feedback already resolved")
      }
      const noteBody = input.noteBody?.trim() ?? ""
      if (input.reason === "other" && noteBody.length === 0) {
        throw new Error("Note required when reason is Other")
      }

      const activityEvent: FeedbackDetailsActivityEvent = {
        kind: "feedback_closed_out",
        at: new Date().toISOString(),
        actorDisplayName: "Ada Operator",
        fromWorkflowStatus: fromStatus,
        toWorkflowStatus: "resolved",
        closeOutIntent: input.intent,
        closeOutReason: input.reason,
      }

      let note: FeedbackInternalNoteItem | null = null
      let noteActivityEvent: FeedbackDetailsActivityEvent | null = null
      let internalNotes = details.internalNotes ?? []
      let activityHistory = [...(details.activityHistory ?? []), activityEvent]

      if (input.reason === "other") {
        note = {
          id: nextNoteId++,
          body: noteBody,
          authorDisplayName: "Ada Operator",
          createdAt: new Date().toISOString(),
        }
        internalNotes = [note, ...internalNotes]
        noteActivityEvent = {
          kind: "note_added",
          at: note.createdAt,
          actorDisplayName: note.authorDisplayName,
        }
        activityHistory = [...activityHistory, noteActivityEvent]
      }

      const workflowStatus: FeedbackWorkflowStatus = "resolved"
      const succeeded = details.classificationStatus === "Succeeded"
      const sentiment = succeeded ? details.sentiment : null
      const needsAttention = deriveFeedbackNeedsAttention(
        details.classificationStatus,
        sentiment,
        workflowStatus
      )

      store.set(feedbackId, {
        ...details,
        workflowStatus,
        needsAttention,
        internalNotes,
        activityHistory,
      })

      return {
        workflowStatus,
        needsAttention,
        activityEvent,
        noteActivityEvent,
        note,
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
    workflowSaveGeneration: 0,
    noteCreateGeneration: 0,
    isEditing: false,
    draftSentiment: null,
    draftReason: null,
    draftNote: "",
    saveStatus: "idle",
    saveError: null,
    workflowSaveStatus: "idle",
    workflowSaveError: null,
    noteDraft: "",
    noteCreateStatus: "idle",
    noteCreateError: null,
    ...emptyNoteEditSession(),
    ...emptyNoteDeleteSession(),
    ...emptyCloseOutSession(),
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

  const setWorkflowStatus = async (
    status: FeedbackWorkflowStatus
  ): Promise<boolean> => {
    if (
      state.feedbackId == null
      || state.details == null
      || state.workflowSaveStatus === "saving"
    ) {
      return false
    }

    const feedbackId = state.feedbackId
    const generation = state.workflowSaveGeneration + 1
    dispatch({ type: "workflow_save_started", generation })

    try {
      const result = await adapters.setWorkflowStatus(feedbackId, status)
      dispatch({
        type: "workflow_save_succeeded",
        generation,
        workflowStatus: result.workflowStatus,
        needsAttention: result.needsAttention,
        activityEvent: result.activityEvent ?? null,
      })
      return true
    } catch {
      dispatch({
        type: "workflow_save_failed",
        generation,
        error: WORKFLOW_STATUS_ERROR,
      })
      return false
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
      dispatch({ type: "correction_started" })
    },
    setDraftSentiment: (sentiment) => {
      dispatch({ type: "draft_sentiment_set", sentiment })
    },
    setDraftReason: (reason) => {
      dispatch({ type: "draft_reason_set", reason })
    },
    setDraftNote: (value) => {
      dispatch({ type: "draft_note_set", value })
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
        || state.draftReason == null
      ) {
        return
      }

      const feedbackId = state.feedbackId
      const sentiment = state.draftSentiment
      const reason = state.draftReason
      const trimmedNote = state.draftNote.trim()
      const generation = state.saveGeneration + 1
      dispatch({ type: "save_started", generation })

      try {
        const result = await adapters.correctClassification(feedbackId, {
          sentiment,
          reason,
          ...(trimmedNote.length > 0 || reason === "other"
            ? { noteBody: trimmedNote }
            : {}),
        })
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
    setWorkflowStatus,
    reopen: async () => {
      if (state.details == null || !state.details.canReopen) {
        return false
      }
      return setWorkflowStatus("in_progress")
    },
    startCloseOut: (intent) => {
      if (state.details == null || state.details.workflowStatus === "resolved") {
        return false
      }
      dispatch({ type: "close_out_started", intent })
      return true
    },
    startMarkResolved: () => {
      if (state.details == null || state.details.workflowStatus === "resolved") {
        return false
      }
      dispatch({ type: "close_out_started", intent: "mark_resolved" })
      return true
    },
    setCloseOutReason: (reason) => {
      dispatch({ type: "close_out_reason_set", reason })
    },
    setCloseOutNoteDraft: (value) => {
      dispatch({ type: "close_out_note_draft_set", value })
    },
    setCloseOutAcknowledged: (value) => {
      dispatch({ type: "close_out_acknowledged_set", value })
    },
    cancelCloseOut: () => {
      if (!state.closeOutIsOpen) {
        return
      }
      dispatch({ type: "close_out_cancelled" })
    },
    confirmCloseOut: async () => {
      if (
        state.feedbackId == null
        || state.details == null
        || !state.closeOutIsOpen
        || state.closeOutIntent == null
        || state.closeOutReason == null
        || !canConfirmFeedbackCloseOut({
          intent: state.closeOutIntent,
          reason: state.closeOutReason,
          noteDraft: state.closeOutNoteDraft,
          acknowledged: state.closeOutAcknowledged,
        })
        || state.closeOutSaveStatus === "saving"
      ) {
        return false
      }

      const feedbackId = state.feedbackId
      const intent = state.closeOutIntent
      const reason = state.closeOutReason
      const noteBody =
        reason === "other" ? state.closeOutNoteDraft.trim() : undefined
      const generation = state.closeOutSaveGeneration + 1
      dispatch({ type: "close_out_save_started", generation })

      try {
        const result = await adapters.closeOutFeedback(feedbackId, {
          intent,
          reason,
          noteBody,
        })
        dispatch({
          type: "close_out_save_succeeded",
          generation,
          workflowStatus: result.workflowStatus,
          needsAttention: result.needsAttention,
          activityEvent: result.activityEvent,
          noteActivityEvent: result.noteActivityEvent ?? null,
          note: result.note ?? null,
        })
        return true
      } catch {
        dispatch({
          type: "close_out_save_failed",
          generation,
          error: CLOSE_OUT_ERROR,
        })
        return false
      }
    },
    startMarkNoActionNeeded: () => {
      if (state.details == null || state.details.workflowStatus === "resolved") {
        return false
      }
      dispatch({ type: "close_out_started", intent: "mark_no_action_needed" })
      return true
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
