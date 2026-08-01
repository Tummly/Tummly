import { isAxiosError } from "axios"

import {
  createGuestActivityTabModule,
  type GuestActivityTabAdapters,
  type GuestActivityTabModule,
} from "@/lib/operatorGuestProfile/createGuestActivityTabModule"
import {
  createGuestFeedbacksTabModule,
  type GuestFeedbacksTabAdapters,
  type GuestFeedbacksTabModule,
} from "@/lib/operatorGuestProfile/createGuestFeedbacksTabModule"
import {
  splitGuestName,
  validateGuestIdentityDraft,
  type GuestIdentityDraft,
  type GuestIdentityFieldErrors,
  type GuestIdentityPatchPayload,
} from "@/lib/operatorGuestProfile/guestIdentityForm"
import { GUEST_PROFILE_NOT_PROVIDED } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import {
  mapGuestNoteItemToRow,
  mapGuestProfileApiResponseToViewModel,
} from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorFeedback/createFeedbackDetailsModule"
import {
  isAddTagApplyDirty,
  tagSetsEqual,
} from "@/lib/operatorGuests/addTagDialogLogic"
import type { GuestTag } from "@/lib/operatorGuests/guestTag"
import type { GuestsExportQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import type {
  FeedbackSentiment,
  GuestNotesListResponse,
  GuestProfileRecentNoteItem,
  GuestProfileResponse,
} from "@/types/dashboard"
import type {
  OperatorGuestProfileNoteRow,
  OperatorGuestProfileTabId,
  OperatorGuestProfileViewModel,
} from "@/types/operatorGuestProfile"

export type OperatorGuestProfileWorkspaceInput = {
  /** Null when the route `:guestId` is missing or not a positive integer. */
  guestId: number | null
  selectedLocationId: number | null
}

export type OperatorGuestProfileNotesSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  items: OperatorGuestProfileNoteRow[]
  totalCount: number
  createStatus: "idle" | "saving" | "error"
}

export type OperatorGuestProfileViewAllFeedbacksNavigation = {
  guestId: number
  tab: Extract<OperatorGuestProfileTabId, "feedbacks">
}

export type OperatorGuestProfilePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "unavailable" | "error"
  viewModel: OperatorGuestProfileViewModel | null
  feedbackDetails: FeedbackDetailsSnapshot
  notes: OperatorGuestProfileNotesSnapshot
  draft: GuestIdentityDraft
  fieldErrors: GuestIdentityFieldErrors
  saveStatus: "idle" | "saving" | "error"
  saveError: string | null
  tagCatalog: readonly GuestTag[]
  serverTagIds: readonly string[]
  pendingTagIds: readonly string[]
  tagsDirty: boolean
  tagsApplyStatus: "idle" | "applying" | "error"
  tagsApplyError: string | null
  noteDraft: string
  noteSaveStatus: "idle" | "saving" | "error"
  noteSaveError: string | null
  exportStatus: "idle" | "exporting"
  deleteStatus: "idle" | "deleting"
}

export type OperatorGuestProfilePageAdapters = {
  getGuestProfile: (params: {
    guestId: number
    locationId: number
  }) => Promise<GuestProfileResponse>
  listGuestNotes: (params: {
    guestId: number
    locationId: number
    limit?: number
  }) => Promise<GuestNotesListResponse>
  createGuestNote: (params: {
    guestId: number
    locationId: number
    body: string
  }) => Promise<GuestProfileRecentNoteItem>
  updateGuestNote: (params: {
    guestId: number
    locationId: number
    noteId: number
    body: string
  }) => Promise<GuestProfileRecentNoteItem>
  softDeleteGuestNote: (params: {
    guestId: number
    locationId: number
    noteId: number
  }) => Promise<{ deletedAt: string; deletedByDisplayName: string }>
  patchGuestIdentity: (params: {
    guestId: number
    locationId: number
    body: GuestIdentityPatchPayload
  }) => Promise<{ success: boolean; changedFields: string[] }>
  listGuestTags: (params: { locationId: number }) => Promise<GuestTag[]>
  syncGuestTags: (params: {
    locationId: number
    guestIds: number[]
    tagIds: number[]
  }) => Promise<void>
  getGuestActivity: GuestActivityTabAdapters["getGuestActivity"]
  getGuestFeedbacks: GuestFeedbacksTabAdapters["getGuestFeedbacks"]
  getFeedbackDetails: FeedbackDetailsAdapters["getFeedbackDetails"]
  correctClassification: FeedbackDetailsAdapters["correctClassification"]
  createInternalNote: FeedbackDetailsAdapters["createInternalNote"]
  updateInternalNote: FeedbackDetailsAdapters["updateInternalNote"]
  deleteInternalNote: FeedbackDetailsAdapters["deleteInternalNote"]
  exportGuestsCsv: (
    params: GuestsExportQueryParams
  ) => Promise<{ blob: Blob; filename: string }>
  triggerBrowserDownload: (blob: Blob, filename: string) => void
  deleteLocationGuest: (params: {
    guestId: number
    locationId: number
  }) => Promise<void>
}

export type OperatorGuestProfileExportResult =
  | { status: "exported" }
  | { status: "error"; message: string }

export type OperatorGuestProfileSaveResult =
  | { status: "saved" }
  | { status: "validation" }
  | { status: "error"; message: string }

export type OperatorGuestProfileTagsApplyResult =
  | { status: "applied" }
  | { status: "noop" }
  | { status: "error"; message: string }

export type OperatorGuestProfileDeleteResult =
  | { status: "deleted" }
  | { status: "error"; message: string }

/** Explicit invalidate targets after write commands. */
export type OperatorGuestProfileInvalidateKey =
  | "profile"
  | "notes"
  | "activity"

export type OperatorGuestProfilePageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestProfilePageSnapshot
  /** Internal Activity tab module (Home-style child; not a sibling public provider). */
  activityTab: GuestActivityTabModule
  /** Internal Feedbacks tab module (Home-style child; not a sibling public provider). */
  feedbacksTab: GuestFeedbacksTabModule
  syncWorkspace: (input: OperatorGuestProfileWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  ensureNotesLoaded: () => Promise<void>
  retryNotesLoad: () => Promise<void>
  createNote: (body: string) => Promise<boolean>
  updateNote: (noteId: number, body: string) => Promise<boolean>
  softDeleteNote: (noteId: number) => Promise<boolean>
  setDraftField: <K extends keyof GuestIdentityDraft>(
    field: K,
    value: GuestIdentityDraft[K]
  ) => void
  saveChanges: () => Promise<OperatorGuestProfileSaveResult>
  stageTag: (tagId: string) => void
  unstageTag: (tagId: string) => void
  cancelTagDraft: () => void
  applyTags: () => Promise<OperatorGuestProfileTagsApplyResult>
  setNoteDraft: (value: string) => void
  cancelNoteDraft: () => void
  saveNote: () => Promise<boolean>
  exportGuestRecord: () => Promise<OperatorGuestProfileExportResult>
  deleteLocationGuest: () => Promise<OperatorGuestProfileDeleteResult>
  getViewAllFeedbacksNavigation: () => OperatorGuestProfileViewAllFeedbacksNavigation | null
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
  setFeedbackInternalNoteDraft: (value: string) => void
  createFeedbackInternalNote: () => Promise<boolean>
  startFeedbackNoteEdit: (noteId: number) => void
  setFeedbackNoteEditDraft: (value: string) => void
  cancelFeedbackNoteEdit: () => void
  saveFeedbackNoteEdit: () => Promise<boolean>
  startFeedbackNoteDelete: (noteId: number) => void
  cancelFeedbackNoteDelete: () => void
  confirmFeedbackNoteDelete: () => Promise<boolean>
}

type ModuleState = {
  loadStatus: OperatorGuestProfilePageSnapshot["loadStatus"]
  viewModel: OperatorGuestProfileViewModel | null
  workspace: OperatorGuestProfileWorkspaceInput | null
  fetchedGuestId: number | null
  fetchedLocationId: number | null
  loadGeneration: number
  notesLoadStatus: OperatorGuestProfileNotesSnapshot["loadStatus"]
  notesItems: OperatorGuestProfileNoteRow[]
  notesTotalCount: number
  notesCreateStatus: OperatorGuestProfileNotesSnapshot["createStatus"]
  notesLoadGeneration: number
  notesFetchedGuestId: number | null
  notesFetchedLocationId: number | null
  draft: GuestIdentityDraft
  fieldErrors: GuestIdentityFieldErrors
  saveStatus: OperatorGuestProfilePageSnapshot["saveStatus"]
  saveError: string | null
  tagCatalog: GuestTag[]
  serverTagIds: string[]
  pendingTagIds: string[]
  tagsApplyStatus: OperatorGuestProfilePageSnapshot["tagsApplyStatus"]
  tagsApplyError: string | null
  noteDraft: string
  noteSaveStatus: OperatorGuestProfilePageSnapshot["noteSaveStatus"]
  noteSaveError: string | null
  exportStatus: OperatorGuestProfilePageSnapshot["exportStatus"]
  deleteStatus: OperatorGuestProfilePageSnapshot["deleteStatus"]
}

const emptyDraft = (): GuestIdentityDraft => ({
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
})

const emptyNotes = (): Pick<
  ModuleState,
  | "notesLoadStatus"
  | "notesItems"
  | "notesTotalCount"
  | "notesCreateStatus"
  | "notesLoadGeneration"
  | "notesFetchedGuestId"
  | "notesFetchedLocationId"
> => ({
  notesLoadStatus: "idle",
  notesItems: [],
  notesTotalCount: 0,
  notesCreateStatus: "idle",
  notesLoadGeneration: 0,
  notesFetchedGuestId: null,
  notesFetchedLocationId: null,
})

function emptyTagsState(): Pick<
  ModuleState,
  | "tagCatalog"
  | "serverTagIds"
  | "pendingTagIds"
  | "tagsApplyStatus"
  | "tagsApplyError"
> {
  return {
    tagCatalog: [],
    serverTagIds: [],
    pendingTagIds: [],
    tagsApplyStatus: "idle",
    tagsApplyError: null,
  }
}

function draftFromProfile(response: GuestProfileResponse): GuestIdentityDraft {
  const { firstName, lastName } = splitGuestName(response.name)
  return {
    firstName,
    lastName,
    email: response.profileSummary.email ?? "",
    phone: response.profileSummary.mobile ?? "",
  }
}

function tagIdsFromProfile(response: GuestProfileResponse): string[] {
  return response.profileSummary.guestTags.map((tag) => String(tag.id))
}

function mergeCatalogWithProfileTags(
  catalog: GuestTag[],
  response: GuestProfileResponse
): GuestTag[] {
  const byId = new Map(catalog.map((tag) => [tag.id, tag]))
  for (const tag of response.profileSummary.guestTags) {
    const id = String(tag.id)
    if (!byId.has(id)) {
      byId.set(id, { id, name: tag.name, guestCount: 0 })
    }
  }
  return [...byId.values()]
}

function applyPendingToViewModel(
  viewModel: OperatorGuestProfileViewModel,
  pendingTagIds: readonly string[],
  catalog: readonly GuestTag[]
): OperatorGuestProfileViewModel {
  const guestTags = pendingTagIds
    .map((id) => catalog.find((tag) => tag.id === id))
    .filter((tag): tag is GuestTag => tag != null)
    .map((tag) => ({ id: tag.id, name: tag.name }))

  const guestTagsDisplay =
    guestTags.length === 0
      ? GUEST_PROFILE_NOT_PROVIDED
      : guestTags.map((tag) => tag.name).join(", ")

  return {
    ...viewModel,
    profileSummary: {
      ...viewModel.profileSummary,
      guestTags,
      guestTagsDisplay,
    },
  }
}

function prependRecentNote(
  viewModel: OperatorGuestProfileViewModel,
  note: GuestProfileRecentNoteItem
): OperatorGuestProfileViewModel {
  const row = mapGuestNoteItemToRow(note)
  return {
    ...viewModel,
    recentNotes: [row, ...viewModel.recentNotes.filter((n) => n.id !== row.id)],
  }
}

function replaceRecentNote(
  viewModel: OperatorGuestProfileViewModel,
  note: GuestProfileRecentNoteItem
): OperatorGuestProfileViewModel {
  const row = mapGuestNoteItemToRow(note)
  return {
    ...viewModel,
    recentNotes: viewModel.recentNotes.map((item) =>
      item.id === row.id ? row : item
    ),
  }
}

function removeRecentNote(
  viewModel: OperatorGuestProfileViewModel,
  noteId: number
): OperatorGuestProfileViewModel {
  return {
    ...viewModel,
    recentNotes: viewModel.recentNotes.filter((note) => note.id !== noteId),
  }
}

function isUnavailableError(error: unknown): boolean {
  return (
    isAxiosError(error) &&
    (error.response?.status === 404 || error.response?.status === 403)
  )
}

function readApiErrorMessage(error: unknown, fallback: string): string {
  if (!isAxiosError(error)) {
    return fallback
  }
  const data = error.response?.data as { message?: unknown } | undefined
  if (typeof data?.message === "string" && data.message.trim().length > 0) {
    return data.message
  }
  return fallback
}

function buildSnapshot(
  state: ModuleState,
  feedbackDetails: FeedbackDetailsModule
): OperatorGuestProfilePageSnapshot {
  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    feedbackDetails: feedbackDetails.getSnapshot(),
    notes: {
      loadStatus: state.notesLoadStatus,
      items: state.notesItems,
      totalCount: state.notesTotalCount,
      createStatus: state.notesCreateStatus,
    },
    draft: state.draft,
    fieldErrors: state.fieldErrors,
    saveStatus: state.saveStatus,
    saveError: state.saveError,
    tagCatalog: state.tagCatalog,
    serverTagIds: state.serverTagIds,
    pendingTagIds: state.pendingTagIds,
    tagsDirty: isAddTagApplyDirty(state.serverTagIds, state.pendingTagIds),
    tagsApplyStatus: state.tagsApplyStatus,
    tagsApplyError: state.tagsApplyError,
    noteDraft: state.noteDraft,
    noteSaveStatus: state.noteSaveStatus,
    noteSaveError: state.noteSaveError,
    exportStatus: state.exportStatus,
    deleteStatus: state.deleteStatus,
  }
}

export function createOperatorGuestProfilePageModule(
  adapters: OperatorGuestProfilePageAdapters
): OperatorGuestProfilePageModule {
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
    createInternalNote: adapters.createInternalNote,
    updateInternalNote: adapters.updateInternalNote,
    deleteInternalNote: adapters.deleteInternalNote,
  })
  const activityTab = createGuestActivityTabModule({
    getGuestActivity: adapters.getGuestActivity,
  })
  const feedbacksTab = createGuestFeedbacksTabModule({
    getGuestFeedbacks: adapters.getGuestFeedbacks,
  })

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    fetchedGuestId: null,
    fetchedLocationId: null,
    loadGeneration: 0,
    ...emptyNotes(),
    draft: emptyDraft(),
    fieldErrors: {},
    saveStatus: "idle",
    saveError: null,
    ...emptyTagsState(),
    noteDraft: "",
    noteSaveStatus: "idle",
    noteSaveError: null,
    exportStatus: "idle",
    deleteStatus: "idle",
  }
  let snapshot = buildSnapshot(state, feedbackDetails)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = buildSnapshot(state, feedbackDetails)
    emit()
  }

  const setState = (patch: Partial<ModuleState>) => {
    state = { ...state, ...patch }
    publish()
  }

  feedbackDetails.subscribe(() => {
    publish()
  })

  const fetchProfile = async () => {
    const workspace = state.workspace
    if (
      workspace == null ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return
    }

    const { guestId, selectedLocationId } = workspace
    const generation = state.loadGeneration + 1

    setState({
      loadStatus: "loading",
      loadGeneration: generation,
      saveStatus: "idle",
      saveError: null,
      fieldErrors: {},
      exportStatus: "idle",
      deleteStatus: "idle",
    })

    try {
      const [response, catalog] = await Promise.all([
        adapters.getGuestProfile({
          guestId,
          locationId: selectedLocationId,
        }),
        adapters.listGuestTags({ locationId: selectedLocationId }),
      ])

      if (generation !== state.loadGeneration) {
        return
      }

      const serverTagIds = tagIdsFromProfile(response)
      setState({
        loadStatus: "loaded",
        viewModel: mapGuestProfileApiResponseToViewModel({ response }),
        draft: draftFromProfile(response),
        fieldErrors: {},
        tagCatalog: mergeCatalogWithProfileTags(catalog, response),
        serverTagIds,
        pendingTagIds: [...serverTagIds],
        tagsApplyStatus: "idle",
        tagsApplyError: null,
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      })
    } catch (error) {
      if (generation !== state.loadGeneration) {
        return
      }

      if (isUnavailableError(error)) {
        setState({
          loadStatus: "unavailable",
          viewModel: null,
          draft: emptyDraft(),
          ...emptyTagsState(),
          fetchedGuestId: guestId,
          fetchedLocationId: selectedLocationId,
          ...emptyNotes(),
        })
        return
      }

      setState({
        loadStatus: "error",
        viewModel: null,
        draft: emptyDraft(),
        ...emptyTagsState(),
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      })
    }
  }

  const fetchNotes = async (options?: { force?: boolean }) => {
    const workspace = state.workspace
    if (
      workspace == null ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return
    }

    const { guestId, selectedLocationId } = workspace

    const alreadyLoaded =
      !options?.force &&
      state.notesLoadStatus === "loaded" &&
      state.notesFetchedGuestId === guestId &&
      state.notesFetchedLocationId === selectedLocationId

    if (alreadyLoaded) {
      return
    }

    const generation = state.notesLoadGeneration + 1
    setState({
      notesLoadStatus: "loading",
      notesLoadGeneration: generation,
    })

    try {
      const response = await adapters.listGuestNotes({
        guestId,
        locationId: selectedLocationId,
      })

      if (generation !== state.notesLoadGeneration) {
        return
      }

      setState({
        notesLoadStatus: "loaded",
        notesItems: response.items.map(mapGuestNoteItemToRow),
        notesTotalCount: response.totalCount,
        notesFetchedGuestId: guestId,
        notesFetchedLocationId: selectedLocationId,
      })
    } catch {
      if (generation !== state.notesLoadGeneration) {
        return
      }

      setState({
        notesLoadStatus: "error",
        notesFetchedGuestId: guestId,
        notesFetchedLocationId: selectedLocationId,
      })
    }
  }

  const invalidate = async (keys: OperatorGuestProfileInvalidateKey[]) => {
    const tasks: Promise<void>[] = []
    if (keys.includes("profile")) {
      state = {
        ...state,
        fetchedGuestId: null,
        fetchedLocationId: null,
      }
      tasks.push(fetchProfile())
    }
    if (keys.includes("notes")) {
      tasks.push(fetchNotes({ force: true }))
    }
    if (keys.includes("activity")) {
      tasks.push(activityTab.invalidate())
    }
    await Promise.all(tasks)
  }

  const postNoteAndInvalidate = async (trimmed: string): Promise<boolean> => {
    const workspace = state.workspace
    if (
      workspace == null ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return false
    }

    const created = await adapters.createGuestNote({
      guestId: workspace.guestId,
      locationId: workspace.selectedLocationId,
      body: trimmed,
    })

    if (state.viewModel != null) {
      setState({
        viewModel: prependRecentNote(state.viewModel, created),
        notesCreateStatus: "idle",
        noteDraft: "",
        noteSaveStatus: "idle",
        noteSaveError: null,
      })
    } else {
      setState({
        notesCreateStatus: "idle",
        noteDraft: "",
        noteSaveStatus: "idle",
        noteSaveError: null,
      })
    }

    // Note create → notes + Activity; Overview recentNotes patched locally (no full profile refetch).
    await invalidate(["notes", "activity"])
    return state.loadStatus === "loaded"
  }

  const patchNoteWriteAndInvalidate = async (
    noteId: number,
    mutate: () => Promise<GuestProfileRecentNoteItem | "deleted">
  ): Promise<boolean> => {
    const workspace = state.workspace
    if (
      workspace == null ||
      workspace.guestId == null ||
      workspace.selectedLocationId == null
    ) {
      return false
    }

    const result = await mutate()

    if (result === "deleted") {
      if (state.viewModel != null) {
        setState({
          viewModel: removeRecentNote(state.viewModel, noteId),
          notesItems: state.notesItems.filter((note) => note.id !== noteId),
          notesTotalCount: Math.max(0, state.notesTotalCount - 1),
        })
      } else {
        setState({
          notesItems: state.notesItems.filter((note) => note.id !== noteId),
          notesTotalCount: Math.max(0, state.notesTotalCount - 1),
        })
      }
    } else if (state.viewModel != null) {
      const row = mapGuestNoteItemToRow(result)
      setState({
        viewModel: replaceRecentNote(state.viewModel, result),
        notesItems: state.notesItems.map((note) =>
          note.id === row.id ? row : note
        ),
      })
    } else {
      const row = mapGuestNoteItemToRow(result)
      setState({
        notesItems: state.notesItems.map((note) =>
          note.id === row.id ? row : note
        ),
      })
    }

    await invalidate(["notes", "profile", "activity"])
    return state.loadStatus === "loaded"
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
    activityTab,
    feedbacksTab,
    async syncWorkspace(input) {
      if (input.guestId == null) {
        setState({
          loadStatus: "unavailable",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
          draft: emptyDraft(),
          fieldErrors: {},
          saveStatus: "idle",
          saveError: null,
          ...emptyTagsState(),
          noteDraft: "",
          noteSaveStatus: "idle",
          noteSaveError: null,
          exportStatus: "idle",
          deleteStatus: "idle",
          ...emptyNotes(),
        })
        return
      }

      if (input.selectedLocationId == null) {
        setState({
          loadStatus: "idle",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
          draft: emptyDraft(),
          fieldErrors: {},
          saveStatus: "idle",
          saveError: null,
          ...emptyTagsState(),
          noteDraft: "",
          noteSaveStatus: "idle",
          noteSaveError: null,
          exportStatus: "idle",
          deleteStatus: "idle",
          ...emptyNotes(),
        })
        return
      }

      const samePair =
        state.fetchedGuestId === input.guestId &&
        state.fetchedLocationId === input.selectedLocationId &&
        (state.loadStatus === "loaded" ||
          state.loadStatus === "unavailable" ||
          state.loadStatus === "error")

      const guestOrLocationChanged =
        state.workspace?.guestId !== input.guestId ||
        state.workspace?.selectedLocationId !== input.selectedLocationId

      state = {
        ...state,
        workspace: input,
        ...(guestOrLocationChanged && !samePair ? emptyNotes() : {}),
      }

      if (samePair) {
        publish()
        return
      }

      await fetchProfile()
    },
    async retryLoad() {
      if (
        state.workspace == null ||
        state.workspace.guestId == null ||
        state.workspace.selectedLocationId == null
      ) {
        return
      }

      state = {
        ...state,
        fetchedGuestId: null,
        fetchedLocationId: null,
      }
      await fetchProfile()
    },
    async ensureNotesLoaded() {
      await fetchNotes()
    },
    async retryNotesLoad() {
      await fetchNotes({ force: true })
    },
    async createNote(body) {
      const workspace = state.workspace
      if (
        workspace == null ||
        workspace.guestId == null ||
        workspace.selectedLocationId == null
      ) {
        return false
      }

      const trimmed = body.trim()
      if (trimmed.length === 0) {
        return false
      }

      setState({ notesCreateStatus: "saving" })

      try {
        return await postNoteAndInvalidate(trimmed)
      } catch {
        setState({ notesCreateStatus: "error" })
        return false
      }
    },
    async updateNote(noteId, body) {
      const workspace = state.workspace
      if (
        workspace == null ||
        workspace.guestId == null ||
        workspace.selectedLocationId == null
      ) {
        return false
      }

      const trimmed = body.trim()
      if (trimmed.length === 0) {
        return false
      }

      try {
        return await patchNoteWriteAndInvalidate(noteId, async () =>
          adapters.updateGuestNote({
            guestId: workspace.guestId!,
            locationId: workspace.selectedLocationId!,
            noteId,
            body: trimmed,
          })
        )
      } catch {
        return false
      }
    },
    async softDeleteNote(noteId) {
      const workspace = state.workspace
      if (
        workspace == null ||
        workspace.guestId == null ||
        workspace.selectedLocationId == null
      ) {
        return false
      }

      try {
        return await patchNoteWriteAndInvalidate(noteId, async () => {
          await adapters.softDeleteGuestNote({
            guestId: workspace.guestId!,
            locationId: workspace.selectedLocationId!,
            noteId,
          })
          return "deleted"
        })
      } catch {
        return false
      }
    },
    setDraftField(field, value) {
      const nextErrors = { ...state.fieldErrors }
      delete nextErrors[field]
      delete nextErrors.form
      setState({
        draft: { ...state.draft, [field]: value },
        fieldErrors: nextErrors,
        saveError: null,
        saveStatus: "idle",
      })
    },
    async saveChanges() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded"
      ) {
        return {
          status: "error",
          message: "Guest details are not ready to save.",
        }
      }

      const validated = validateGuestIdentityDraft(state.draft)
      if (!validated.ok) {
        setState({
          fieldErrors: validated.errors,
          saveStatus: "idle",
          saveError: validated.errors.form ?? null,
        })
        return { status: "validation" }
      }

      setState({
        saveStatus: "saving",
        saveError: null,
        fieldErrors: {},
      })

      try {
        await adapters.patchGuestIdentity({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
          body: validated.payload,
        })
        setState({ saveStatus: "idle", saveError: null })
        // Identity save → full profile snapshot (Overview/header fields).
        await invalidate(["profile"])
        return { status: "saved" }
      } catch (error) {
        const message = readApiErrorMessage(
          error,
          "Could not save guest details. Please try again."
        )
        setState({
          saveStatus: "error",
          saveError: message,
        })
        return { status: "error", message }
      }
    },
    stageTag(tagId) {
      if (state.pendingTagIds.includes(tagId)) {
        return
      }
      setState({
        pendingTagIds: [...state.pendingTagIds, tagId],
        tagsApplyError: null,
        tagsApplyStatus: "idle",
      })
    },
    unstageTag(tagId) {
      setState({
        pendingTagIds: state.pendingTagIds.filter((id) => id !== tagId),
        tagsApplyError: null,
        tagsApplyStatus: "idle",
      })
    },
    cancelTagDraft() {
      if (tagSetsEqual(state.serverTagIds, state.pendingTagIds)) {
        return
      }
      setState({
        pendingTagIds: [...state.serverTagIds],
        tagsApplyError: null,
        tagsApplyStatus: "idle",
      })
    },
    async applyTags() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded" ||
        state.viewModel == null
      ) {
        return {
          status: "error",
          message: "Guest tags are not ready to apply.",
        }
      }

      if (!isAddTagApplyDirty(state.serverTagIds, state.pendingTagIds)) {
        return { status: "noop" }
      }

      const pendingTagIds = [...state.pendingTagIds]
      setState({
        tagsApplyStatus: "applying",
        tagsApplyError: null,
      })

      try {
        await adapters.syncGuestTags({
          locationId: workspace.selectedLocationId,
          guestIds: [workspace.guestId],
          tagIds: pendingTagIds.map((id) => Number(id)),
        })

        const nextViewModel = applyPendingToViewModel(
          state.viewModel,
          pendingTagIds,
          state.tagCatalog
        )

        setState({
          serverTagIds: pendingTagIds,
          pendingTagIds,
          viewModel: nextViewModel,
          tagsApplyStatus: "idle",
          tagsApplyError: null,
        })
        // Tag sync → local profile tags + Activity tab.
        await invalidate(["activity"])
        return { status: "applied" }
      } catch (error) {
        const message = readApiErrorMessage(
          error,
          "Could not apply tags. Please try again."
        )
        setState({
          tagsApplyStatus: "error",
          tagsApplyError: message,
        })
        return { status: "error", message }
      }
    },
    setNoteDraft(value) {
      setState({
        noteDraft: value,
        noteSaveError: null,
        noteSaveStatus:
          state.noteSaveStatus === "error" ? "idle" : state.noteSaveStatus,
      })
    },
    cancelNoteDraft() {
      setState({
        noteDraft: "",
        noteSaveError: null,
        noteSaveStatus: "idle",
      })
    },
    async saveNote() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded"
      ) {
        return false
      }

      const trimmed = state.noteDraft.trim()
      if (trimmed.length === 0) {
        return false
      }

      setState({
        noteSaveStatus: "saving",
        noteSaveError: null,
      })

      try {
        return await postNoteAndInvalidate(trimmed)
      } catch (error) {
        const message = readApiErrorMessage(
          error,
          "Could not save note. Please try again."
        )
        setState({
          noteSaveStatus: "error",
          noteSaveError: message,
        })
        return false
      }
    },
    async exportGuestRecord() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded" ||
        state.exportStatus === "exporting"
      ) {
        return {
          status: "error",
          message: "Guest record is not ready to export.",
        }
      }

      setState({ exportStatus: "exporting" })

      try {
        const result = await adapters.exportGuestsCsv({
          locationId: workspace.selectedLocationId,
          smartGroup: "all-guests",
          q: "",
          sort: "recent-activity",
          guestIds: [workspace.guestId],
        })
        adapters.triggerBrowserDownload(result.blob, result.filename)
        setState({ exportStatus: "idle" })
        return { status: "exported" }
      } catch {
        setState({ exportStatus: "idle" })
        return {
          status: "error",
          message: "Could not export guest record. Please try again.",
        }
      }
    },
    async deleteLocationGuest() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded" ||
        state.deleteStatus === "deleting"
      ) {
        return {
          status: "error",
          message: "Guest is not ready to delete.",
        }
      }

      setState({ deleteStatus: "deleting" })

      try {
        await adapters.deleteLocationGuest({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
        })
        setState({ deleteStatus: "idle" })
        return { status: "deleted" }
      } catch (error) {
        const message = readApiErrorMessage(
          error,
          "Could not delete guest data. Please try again."
        )
        setState({ deleteStatus: "idle" })
        return { status: "error", message }
      }
    },
    getViewAllFeedbacksNavigation() {
      const guestId = state.workspace?.guestId
      if (guestId == null || state.loadStatus !== "loaded") {
        return null
      }
      return { guestId, tab: "feedbacks" }
    },
    openFeedbackDetails: (feedbackId) => feedbackDetails.open(feedbackId),
    closeFeedbackDetails: () => {
      feedbackDetails.close()
    },
    retryFeedbackDetails: () => feedbackDetails.retry(),
    startClassificationCorrection: () => {
      feedbackDetails.startCorrection()
    },
    setClassificationDraftSentiment: (sentiment) => {
      feedbackDetails.setDraftSentiment(sentiment)
    },
    cancelClassificationCorrection: () => {
      feedbackDetails.cancelCorrection()
    },
    saveClassificationCorrection: () => feedbackDetails.saveCorrection(),
    setFeedbackInternalNoteDraft: (value) => {
      feedbackDetails.setNoteDraft(value)
    },
    createFeedbackInternalNote: () => feedbackDetails.createNote(),
    startFeedbackNoteEdit: (noteId) => {
      feedbackDetails.startEditNote(noteId)
    },
    setFeedbackNoteEditDraft: (value) => {
      feedbackDetails.setNoteEditDraft(value)
    },
    cancelFeedbackNoteEdit: () => {
      feedbackDetails.cancelEditNote()
    },
    saveFeedbackNoteEdit: () => feedbackDetails.saveEditNote(),
    startFeedbackNoteDelete: (noteId) => {
      feedbackDetails.startDeleteNote(noteId)
    },
    cancelFeedbackNoteDelete: () => {
      feedbackDetails.cancelDeleteNote()
    },
    confirmFeedbackNoteDelete: () => feedbackDetails.confirmDeleteNote(),
  }
}
