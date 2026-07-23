import { isAxiosError } from "axios"

import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import {
  mapGuestNoteItemToRow,
  mapGuestProfileApiResponseToViewModel,
} from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import type { GuestsExportQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import type {
  FeedbackSentiment,
  GuestNotesListResponse,
  GuestProfileRecentNoteItem,
  GuestProfileResponse,
} from "@/types/dashboard"
import type {
  OperatorGuestProfileNoteRow,
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

export type OperatorGuestProfilePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "unavailable" | "error"
  viewModel: OperatorGuestProfileViewModel | null
  feedbackDetails: FeedbackDetailsSnapshot
  notes: OperatorGuestProfileNotesSnapshot
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
  getFeedbackDetails: FeedbackDetailsAdapters["getFeedbackDetails"]
  correctClassification: FeedbackDetailsAdapters["correctClassification"]
  exportGuestsCsv: (
    params: GuestsExportQueryParams
  ) => Promise<{ blob: Blob; filename: string }>
  triggerBrowserDownload: (blob: Blob, filename: string) => void
}

export type OperatorGuestProfileExportResult =
  | { status: "exported" }
  | { status: "error"; message: string }

export type OperatorGuestProfilePageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestProfilePageSnapshot
  syncWorkspace: (input: OperatorGuestProfileWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  ensureNotesLoaded: () => Promise<void>
  retryNotesLoad: () => Promise<void>
  createNote: (body: string) => Promise<boolean>
  exportGuestRecord: () => Promise<OperatorGuestProfileExportResult>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
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
  exportInFlight: boolean
}

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

function isUnavailableError(error: unknown): boolean {
  return (
    isAxiosError(error) &&
    (error.response?.status === 404 || error.response?.status === 403)
  )
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
  }
}

export function createOperatorGuestProfilePageModule(
  adapters: OperatorGuestProfilePageAdapters
): OperatorGuestProfilePageModule {
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
  })

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    fetchedGuestId: null,
    fetchedLocationId: null,
    loadGeneration: 0,
    exportInFlight: false,
    ...emptyNotes(),
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

    state = {
      ...state,
      loadStatus: "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getGuestProfile({
        guestId,
        locationId: selectedLocationId,
      })

      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: mapGuestProfileApiResponseToViewModel({ response }),
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      }
      publish()
    } catch (error) {
      if (generation !== state.loadGeneration) {
        return
      }

      if (isUnavailableError(error)) {
        state = {
          ...state,
          loadStatus: "unavailable",
          viewModel: null,
          fetchedGuestId: guestId,
          fetchedLocationId: selectedLocationId,
          ...emptyNotes(),
        }
        publish()
        return
      }

      state = {
        ...state,
        loadStatus: "error",
        viewModel: null,
        fetchedGuestId: guestId,
        fetchedLocationId: selectedLocationId,
      }
      publish()
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
    state = {
      ...state,
      notesLoadStatus: "loading",
      notesLoadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.listGuestNotes({
        guestId,
        locationId: selectedLocationId,
      })

      if (generation !== state.notesLoadGeneration) {
        return
      }

      state = {
        ...state,
        notesLoadStatus: "loaded",
        notesItems: response.items.map(mapGuestNoteItemToRow),
        notesTotalCount: response.totalCount,
        notesFetchedGuestId: guestId,
        notesFetchedLocationId: selectedLocationId,
      }
      publish()
    } catch {
      if (generation !== state.notesLoadGeneration) {
        return
      }

      state = {
        ...state,
        notesLoadStatus: "error",
        notesFetchedGuestId: guestId,
        notesFetchedLocationId: selectedLocationId,
      }
      publish()
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
    async syncWorkspace(input) {
      if (input.guestId == null) {
        state = {
          ...state,
          loadStatus: "unavailable",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
          ...emptyNotes(),
        }
        publish()
        return
      }

      if (input.selectedLocationId == null) {
        state = {
          ...state,
          loadStatus: "idle",
          viewModel: null,
          workspace: input,
          fetchedGuestId: null,
          fetchedLocationId: null,
          ...emptyNotes(),
        }
        publish()
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

      state = {
        ...state,
        notesCreateStatus: "saving",
      }
      publish()

      try {
        await adapters.createGuestNote({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
          body: trimmed,
        })

        state = {
          ...state,
          notesCreateStatus: "idle",
          fetchedGuestId: null,
          fetchedLocationId: null,
          notesFetchedGuestId: null,
          notesFetchedLocationId: null,
        }
        publish()

        await Promise.all([fetchProfile(), fetchNotes({ force: true })])
        return state.loadStatus === "loaded"
      } catch {
        state = {
          ...state,
          notesCreateStatus: "error",
        }
        publish()
        return false
      }
    },
    async exportGuestRecord() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null ||
        state.loadStatus !== "loaded" ||
        state.exportInFlight
      ) {
        return {
          status: "error",
          message: "Guest record is not ready to export.",
        }
      }

      state = {
        ...state,
        exportInFlight: true,
      }
      publish()

      try {
        const result = await adapters.exportGuestsCsv({
          locationId: workspace.selectedLocationId,
          smartGroup: "all-guests",
          q: "",
          sort: "recent-activity",
          guestIds: [workspace.guestId],
        })
        adapters.triggerBrowserDownload(result.blob, result.filename)
        state = {
          ...state,
          exportInFlight: false,
        }
        publish()
        return { status: "exported" }
      } catch {
        state = {
          ...state,
          exportInFlight: false,
        }
        publish()
        return {
          status: "error",
          message: "Could not export guest record. Please try again.",
        }
      }
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
  }
}
