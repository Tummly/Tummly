import { createFinishSettingUpAcksModule } from "@/lib/operatorHome/createFinishSettingUpAcksModule"
import { buildOperatorHomeViewModel } from "@/lib/operatorHome/buildHomeViewModel"
import type {
  ChecklistAcksResponse,
  FeedbackResponse,
  LocationItem,
  UpdateChecklistAcksRequest,
} from "@/types/dashboard"
import type {
  OperatorHomeChecklistAcks,
  OperatorHomeViewModel,
} from "@/types/operatorHome"

export type OperatorHomeWorkspaceInput = {
  locations: LocationItem[]
  selectedLocationId: number | null
}

export type OperatorHomePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorHomeViewModel | null
  previewBusy: boolean
  downloadBusy: boolean
  actionError: string | null
}

export type OperatorHomePageAdapters = {
  getFeedback: (locationId: number) => Promise<FeedbackResponse>
  getChecklistAcks: (locationId: number) => Promise<ChecklistAcksResponse>
  setChecklistAcks: (
    locationId: number,
    body: UpdateChecklistAcksRequest
  ) => Promise<ChecklistAcksResponse>
  downloadQr: (input: {
    locationId: number
    locationName: string
  }) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink: (url: string) => void
}

export type OperatorHomePageModule = {
  getSnapshot: () => OperatorHomePageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorHomeWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  previewGuestForm: () => void
  downloadQr: () => void
}

type HomeState = {
  loadStatus: OperatorHomePageSnapshot["loadStatus"]
  workspace: OperatorHomeWorkspaceInput | null
  feedback: { total: number; recent: FeedbackResponse["recent"] } | null
  viewModel: OperatorHomeViewModel | null
  downloadBusy: boolean
  actionError: string | null
  loadGeneration: number
}

type HomeAction =
  | { type: "workspace_cleared" }
  | {
      type: "workspace_synced"
      workspace: OperatorHomeWorkspaceInput
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "workspace_fields_updated"
      workspace: OperatorHomeWorkspaceInput
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "load_started"; generation: number }
  | {
      type: "load_succeeded"
      generation: number
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "load_failed"; generation: number }
  | {
      type: "view_model_updated"
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "download_busy"; busy: boolean }
  | { type: "action_error"; error: string | null }

function assembleViewModel(
  workspace: OperatorHomeWorkspaceInput,
  checklistAcks: OperatorHomeChecklistAcks,
  feedback: HomeState["feedback"]
): OperatorHomeViewModel | null {
  if (workspace.selectedLocationId == null) {
    return null
  }

  return buildOperatorHomeViewModel({
    locations: workspace.locations,
    selectedLocationId: workspace.selectedLocationId,
    feedback,
    checklistAcks,
  })
}

function reduce(state: HomeState, action: HomeAction): HomeState {
  switch (action.type) {
    case "workspace_cleared":
      return {
        ...state,
        loadStatus: "idle",
        workspace: null,
        feedback: null,
        viewModel: null,
        downloadBusy: false,
        actionError: null,
      }
    case "workspace_synced":
      return {
        ...state,
        workspace: action.workspace,
        feedback: null,
        viewModel: action.viewModel,
        actionError: null,
      }
    case "workspace_fields_updated":
      return {
        ...state,
        workspace: action.workspace,
        viewModel: action.viewModel,
      }
    case "load_started":
      return {
        ...state,
        loadStatus: "loading",
        loadGeneration: action.generation,
      }
    case "load_succeeded":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "loaded",
        feedback: action.feedback,
        viewModel: action.viewModel,
      }
    case "load_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return { ...state, loadStatus: "error" }
    case "view_model_updated":
      return {
        ...state,
        viewModel: action.viewModel,
      }
    case "download_busy":
      return { ...state, downloadBusy: action.busy }
    case "action_error":
      return { ...state, actionError: action.error }
    default:
      return state
  }
}

export function createOperatorHomePageModule(
  adapters: OperatorHomePageAdapters
): OperatorHomePageModule {
  const acks = createFinishSettingUpAcksModule({
    getChecklistAcks: adapters.getChecklistAcks,
    setChecklistAcks: adapters.setChecklistAcks,
  })

  let state: HomeState = {
    loadStatus: "idle",
    workspace: null,
    feedback: null,
    viewModel: null,
    downloadBusy: false,
    actionError: null,
    loadGeneration: 0,
  }

  let snapshot: OperatorHomePageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    previewBusy: false,
    downloadBusy: state.downloadBusy,
    actionError: null,
  }

  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const currentAcks = (): OperatorHomeChecklistAcks => {
    const ackSnapshot = acks.getSnapshot()
    return {
      guestFormPreviewed: ackSnapshot.guestFormPreviewed,
      qrPlacementGuideViewed: ackSnapshot.qrPlacementGuideViewed,
    }
  }

  const publish = () => {
    const ackSnapshot = acks.getSnapshot()
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      previewBusy: ackSnapshot.acknowledgeBusy,
      downloadBusy: state.downloadBusy,
      actionError: ackSnapshot.acknowledgeError ?? state.actionError,
    }
    emit()
  }

  const dispatch = (action: HomeAction) => {
    state = reduce(state, action)
    publish()
  }

  const refreshViewModelFromAcks = () => {
    const workspace = state.workspace
    if (workspace == null) {
      publish()
      return
    }

    dispatch({
      type: "view_model_updated",
      viewModel: assembleViewModel(workspace, currentAcks(), state.feedback),
    })
  }

  acks.subscribe(() => {
    refreshViewModelFromAcks()
  })

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    dispatch({ type: "load_started", generation })

    try {
      const [feedbackResult] = await Promise.all([
        adapters.getFeedback(selectedLocationId),
        acks.load(selectedLocationId),
      ])

      if (generation !== state.loadGeneration) {
        return
      }

      const feedback = {
        total: feedbackResult.total,
        recent: feedbackResult.recent,
      }
      const viewModel = assembleViewModel(workspace, currentAcks(), feedback)

      dispatch({
        type: "load_succeeded",
        generation,
        feedback,
        viewModel,
      })
    } catch {
      dispatch({ type: "load_failed", generation })
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
    syncWorkspace: async (input) => {
      if (input.selectedLocationId == null) {
        dispatch({ type: "workspace_cleared" })
        acks.reset()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      if (locationChanged) {
        const emptyAcks: OperatorHomeChecklistAcks = {
          guestFormPreviewed: false,
          qrPlacementGuideViewed: false,
        }
        const viewModel = assembleViewModel(input, emptyAcks, null)
        dispatch({ type: "workspace_synced", workspace: input, viewModel })
        await loadForSelectedLocation()
        return
      }

      // Same Owned location: refresh shell-facing workspace fields only.
      dispatch({
        type: "workspace_fields_updated",
        workspace: input,
        viewModel: assembleViewModel(input, currentAcks(), state.feedback),
      })
    },
    retryLoad: () => loadForSelectedLocation(),
    previewGuestForm: () => {
      const viewModel = state.viewModel
      if (
        viewModel?.smartGuestLink == null ||
        acks.getSnapshot().acknowledgeBusy
      ) {
        return
      }

      adapters.openSmartGuestLink(viewModel.smartGuestLink)
      acks.acknowledge("guestFormPreviewed")
    },
    downloadQr: () => {
      const viewModel = state.viewModel
      if (viewModel == null || state.downloadBusy) {
        return
      }

      dispatch({ type: "download_busy", busy: true })
      dispatch({ type: "action_error", error: null })

      void adapters
        .downloadQr({
          locationId: viewModel.selectedLocationId,
          locationName: viewModel.selectedLocationName,
        })
        .then((result) => {
          if (!result.ok) {
            dispatch({ type: "action_error", error: result.error })
          }
        })
        .finally(() => {
          dispatch({ type: "download_busy", busy: false })
        })
    },
  }
}
