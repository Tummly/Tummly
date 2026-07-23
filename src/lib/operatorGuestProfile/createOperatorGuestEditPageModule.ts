import { isAxiosError } from "axios"

import {
  splitGuestName,
  validateGuestIdentityDraft,
  type GuestIdentityDraft,
  type GuestIdentityFieldErrors,
  type GuestIdentityPatchPayload,
} from "@/lib/operatorGuestProfile/guestIdentityForm"
import { GUEST_PROFILE_NOT_PROVIDED } from "@/lib/operatorGuestProfile/guestProfilePresentation"
import { mapGuestProfileApiResponseToViewModel } from "@/lib/operatorGuestProfile/mapGuestProfileApiResponseToViewModel"
import {
  createFeedbackDetailsModule,
  type FeedbackDetailsAdapters,
  type FeedbackDetailsModule,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import {
  isAddTagApplyDirty,
  tagSetsEqual,
} from "@/lib/operatorGuests/addTagDialogLogic"
import type { GuestTag } from "@/lib/operatorGuests/guestTag"
import type { GuestsExportQueryParams } from "@/lib/operatorGuests/guestsListQueryParams"
import type {
  FeedbackSentiment,
  GuestProfileRecentNoteItem,
  GuestProfileResponse,
} from "@/types/dashboard"
import type {
  OperatorGuestProfileTabId,
  OperatorGuestProfileViewModel,
} from "@/types/operatorGuestProfile"

export type OperatorGuestEditWorkspaceInput = {
  guestId: number | null
  selectedLocationId: number | null
}

export type OperatorGuestEditViewAllFeedbacksNavigation = {
  guestId: number
  tab: Extract<OperatorGuestProfileTabId, "feedbacks">
}

export type OperatorGuestEditPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "unavailable" | "error"
  viewModel: OperatorGuestProfileViewModel | null
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
  feedbackDetails: FeedbackDetailsSnapshot
  exportStatus: "idle" | "exporting"
  deleteStatus: "idle" | "deleting"
}

export type OperatorGuestEditPageAdapters = {
  getGuestProfile: (params: {
    guestId: number
    locationId: number
  }) => Promise<GuestProfileResponse>
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
  deleteLocationGuest: (params: {
    guestId: number
    locationId: number
  }) => Promise<void>
}

export type OperatorGuestEditSaveResult =
  | { status: "saved" }
  | { status: "validation" }
  | { status: "error"; message: string }

export type OperatorGuestEditTagsApplyResult =
  | { status: "applied" }
  | { status: "noop" }
  | { status: "error"; message: string }

export type OperatorGuestEditExportResult =
  | { status: "exported" }
  | { status: "error"; message: string }

export type OperatorGuestEditDeleteResult =
  | { status: "deleted" }
  | { status: "error"; message: string }

export type OperatorGuestEditPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestEditPageSnapshot
  syncWorkspace: (input: OperatorGuestEditWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setDraftField: <K extends keyof GuestIdentityDraft>(
    field: K,
    value: GuestIdentityDraft[K]
  ) => void
  saveChanges: () => Promise<OperatorGuestEditSaveResult>
  stageTag: (tagId: string) => void
  unstageTag: (tagId: string) => void
  cancelTagDraft: () => void
  applyTags: () => Promise<OperatorGuestEditTagsApplyResult>
  setNoteDraft: (value: string) => void
  cancelNoteDraft: () => void
  saveNote: () => Promise<boolean>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
  getViewAllFeedbacksNavigation: () => OperatorGuestEditViewAllFeedbacksNavigation | null
  exportGuestRecord: () => Promise<OperatorGuestEditExportResult>
  deleteGuest: () => Promise<OperatorGuestEditDeleteResult>
}

type ModuleState = {
  loadStatus: OperatorGuestEditPageSnapshot["loadStatus"]
  viewModel: OperatorGuestProfileViewModel | null
  workspace: OperatorGuestEditWorkspaceInput | null
  draft: GuestIdentityDraft
  fieldErrors: GuestIdentityFieldErrors
  saveStatus: OperatorGuestEditPageSnapshot["saveStatus"]
  saveError: string | null
  tagCatalog: GuestTag[]
  serverTagIds: string[]
  pendingTagIds: string[]
  tagsApplyStatus: OperatorGuestEditPageSnapshot["tagsApplyStatus"]
  tagsApplyError: string | null
  noteDraft: string
  noteSaveStatus: OperatorGuestEditPageSnapshot["noteSaveStatus"]
  noteSaveError: string | null
  exportStatus: OperatorGuestEditPageSnapshot["exportStatus"]
  deleteStatus: OperatorGuestEditPageSnapshot["deleteStatus"]
  loadGeneration: number
  fetchedGuestId: number | null
  fetchedLocationId: number | null
}

const emptyDraft = (): GuestIdentityDraft => ({
  firstName: "",
  lastName: "",
  email: "",
  phone: "",
})

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

function buildSnapshot(
  state: ModuleState,
  feedbackDetails: FeedbackDetailsModule
): OperatorGuestEditPageSnapshot {
  return {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
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
    feedbackDetails: feedbackDetails.getSnapshot(),
    exportStatus: state.exportStatus,
    deleteStatus: state.deleteStatus,
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

export function createOperatorGuestEditPageModule(
  adapters: OperatorGuestEditPageAdapters
): OperatorGuestEditPageModule {
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
  })

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
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
    loadGeneration: 0,
    fetchedGuestId: null,
    fetchedLocationId: null,
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

  const setState = (patch: Partial<ModuleState>) => {
    state = { ...state, ...patch }
    publish()
  }

  const loadProfile = async (
    guestId: number,
    locationId: number,
    generation: number
  ) => {
    setState({
      loadStatus: "loading",
      saveStatus: "idle",
      saveError: null,
      fieldErrors: {},
      ...emptyTagsState(),
      exportStatus: "idle",
      deleteStatus: "idle",
    })

    try {
      const [response, catalog] = await Promise.all([
        adapters.getGuestProfile({ guestId, locationId }),
        adapters.listGuestTags({ locationId }),
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
        fetchedLocationId: locationId,
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
          fetchedLocationId: locationId,
        })
        return
      }

      setState({
        loadStatus: "error",
        viewModel: null,
        draft: emptyDraft(),
        ...emptyTagsState(),
        fetchedGuestId: guestId,
        fetchedLocationId: locationId,
      })
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
      state = {
        ...state,
        workspace: input,
      }

      if (input.guestId == null || input.selectedLocationId == null) {
        const generation = state.loadGeneration + 1
        setState({
          loadStatus: "unavailable",
          viewModel: null,
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
          loadGeneration: generation,
          fetchedGuestId: null,
          fetchedLocationId: null,
        })
        return
      }

      if (
        state.fetchedGuestId === input.guestId &&
        state.fetchedLocationId === input.selectedLocationId &&
        (state.loadStatus === "loaded" ||
          state.loadStatus === "unavailable" ||
          state.loadStatus === "error")
      ) {
        return
      }

      const generation = state.loadGeneration + 1
      setState({ loadGeneration: generation })
      await loadProfile(input.guestId, input.selectedLocationId, generation)
    },
    async retryLoad() {
      const workspace = state.workspace
      if (
        workspace?.guestId == null ||
        workspace.selectedLocationId == null
      ) {
        return
      }

      const generation = state.loadGeneration + 1
      setState({
        loadGeneration: generation,
        fetchedGuestId: null,
        fetchedLocationId: null,
      })
      await loadProfile(
        workspace.guestId,
        workspace.selectedLocationId,
        generation
      )
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
        await adapters.createGuestNote({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
          body: trimmed,
        })

        const response = await adapters.getGuestProfile({
          guestId: workspace.guestId,
          locationId: workspace.selectedLocationId,
        })

        setState({
          noteDraft: "",
          noteSaveStatus: "idle",
          noteSaveError: null,
          viewModel: mapGuestProfileApiResponseToViewModel({ response }),
          fetchedGuestId: workspace.guestId,
          fetchedLocationId: workspace.selectedLocationId,
          loadStatus: "loaded",
        })
        return true
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
    getViewAllFeedbacksNavigation() {
      const guestId = state.workspace?.guestId
      if (guestId == null || state.loadStatus !== "loaded") {
        return null
      }
      return { guestId, tab: "feedbacks" }
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
    async deleteGuest() {
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
  }
}
