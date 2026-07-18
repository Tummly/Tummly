import {
  createFeedbackDetailsModule,
  type CorrectClassificationResponse,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import { createFinishSettingUpAcksModule } from "@/lib/operatorHome/createFinishSettingUpAcksModule"
import { buildOperatorHomeViewModel } from "@/lib/operatorHome/buildHomeViewModel"
import type {
  ChecklistAcksResponse,
  FeedbackDetailsResponse,
  FeedbackResponse,
  FeedbackSentiment,
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

export type CopySmartGuestLinkResult = "copied" | "failed" | "noop"

export type OperatorHomePageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorHomeViewModel | null
  previewBusy: boolean
  actionError: string | null
  feedbackDetails: FeedbackDetailsSnapshot
}

export type ClassificationTerminalSignal = {
  feedbackId: number
  locationId: number
}

export type FeedbackHomeRealtimeHandlers = {
  onClassificationTerminal: (signal: ClassificationTerminalSignal) => void
  onReconnected: () => void
}

export type FeedbackHomeRealtimeSession = {
  stop: () => Promise<void>
}

export type OperatorHomePageAdapters = {
  getFeedback: (locationId: number) => Promise<FeedbackResponse>
  getFeedbackDetails: (feedbackId: number) => Promise<FeedbackDetailsResponse>
  correctClassification: (
    feedbackId: number,
    sentiment: FeedbackSentiment
  ) => Promise<CorrectClassificationResponse>
  getChecklistAcks: (locationId: number) => Promise<ChecklistAcksResponse>
  setChecklistAcks: (
    locationId: number,
    body: UpdateChecklistAcksRequest
  ) => Promise<ChecklistAcksResponse>
  copyText: (
    text: string
  ) => Promise<{ ok: true } | { ok: false; error: string }>
  openSmartGuestLink: (url: string) => void
  connectRealtime: (
    handlers: FeedbackHomeRealtimeHandlers
  ) => Promise<FeedbackHomeRealtimeSession>
}

export type OperatorHomePageModule = {
  getSnapshot: () => OperatorHomePageSnapshot
  subscribe: (listener: () => void) => () => void
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  syncWorkspace: (input: OperatorHomeWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  previewGuestForm: () => void
  copySmartGuestLink: () => Promise<CopySmartGuestLinkResult>
  openFeedbackDetails: (feedbackId: number) => Promise<void>
  closeFeedbackDetails: () => void
  retryFeedbackDetails: () => Promise<void>
  startClassificationCorrection: () => void
  setClassificationDraftSentiment: (sentiment: FeedbackSentiment) => void
  cancelClassificationCorrection: () => void
  saveClassificationCorrection: () => Promise<void>
}

type HomeState = {
  loadStatus: OperatorHomePageSnapshot["loadStatus"]
  workspace: OperatorHomeWorkspaceInput | null
  feedback: { total: number; recent: FeedbackResponse["recent"] } | null
  viewModel: OperatorHomeViewModel | null
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
  | {
      type: "feedback_patched"
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      viewModel: OperatorHomeViewModel | null
    }
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
    case "feedback_patched":
      return {
        ...state,
        feedback: action.feedback,
        viewModel: action.viewModel,
      }
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
  const feedbackDetails = createFeedbackDetailsModule({
    getFeedbackDetails: adapters.getFeedbackDetails,
    correctClassification: adapters.correctClassification,
  })

  let state: HomeState = {
    loadStatus: "idle",
    workspace: null,
    feedback: null,
    viewModel: null,
    actionError: null,
    loadGeneration: 0,
  }

  let snapshot: OperatorHomePageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    previewBusy: false,
    actionError: null,
    feedbackDetails: feedbackDetails.getSnapshot(),
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
      actionError: ackSnapshot.acknowledgeError ?? state.actionError,
      feedbackDetails: feedbackDetails.getSnapshot(),
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

  feedbackDetails.subscribe(() => {
    publish()
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

  const refreshOpenFeedbackDetails = () => {
    const details = feedbackDetails.getSnapshot()
    // Do not kick the operator out of an in-progress correction (Cancel/Save exit).
    if (
      details.isOpen
      && details.feedbackId != null
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }

  const handleClassificationTerminal = (
    signal: ClassificationTerminalSignal
  ) => {
    const selectedLocationId = state.workspace?.selectedLocationId
    if (
      selectedLocationId == null
      || signal.locationId !== selectedLocationId
    ) {
      return
    }

    void loadForSelectedLocation()

    const details = feedbackDetails.getSnapshot()
    if (
      details.isOpen
      && details.feedbackId === signal.feedbackId
      && !details.correction.isEditing
    ) {
      void feedbackDetails.retry()
    }
  }

  let realtimeSession: FeedbackHomeRealtimeSession | null = null
  let connectingRealtime = false

  const ensureRealtime = async () => {
    if (realtimeSession != null || connectingRealtime) {
      return
    }

    connectingRealtime = true
    try {
      realtimeSession = await adapters.connectRealtime({
        onClassificationTerminal: handleClassificationTerminal,
        onReconnected: () => {
          void loadForSelectedLocation()
          refreshOpenFeedbackDetails()
        },
      })
    } finally {
      connectingRealtime = false
    }
  }

  const disconnect = async () => {
    const session = realtimeSession
    realtimeSession = null
    if (session != null) {
      await session.stop()
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
    connect: () => ensureRealtime(),
    disconnect,
    syncWorkspace: async (input) => {
      if (input.selectedLocationId == null) {
        dispatch({ type: "workspace_cleared" })
        acks.reset()
        feedbackDetails.reset()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      if (locationChanged) {
        feedbackDetails.reset()
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
    copySmartGuestLink: async () => {
      const link = state.viewModel?.smartGuestLink
      if (link == null) {
        return "noop"
      }

      dispatch({ type: "action_error", error: null })
      const result = await adapters.copyText(link)
      if (!result.ok) {
        dispatch({ type: "action_error", error: result.error })
        return "failed"
      }

      return "copied"
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
    saveClassificationCorrection: async () => {
      const before = feedbackDetails.getSnapshot()
      const feedbackId = before.feedbackId
      await feedbackDetails.saveCorrection()
      const after = feedbackDetails.getSnapshot()
      const nextSentiment = after.details?.sentiment
      if (
        feedbackId == null
        || nextSentiment == null
        || after.correction.isEditing
        || state.feedback == null
        || state.workspace == null
      ) {
        return
      }

      const recent = state.feedback.recent.map((item) =>
        item.id === feedbackId
          ? {
              ...item,
              classificationStatus: "Succeeded" as const,
              sentiment: nextSentiment,
            }
          : item
      )
      const feedback = { total: state.feedback.total, recent }
      dispatch({
        type: "feedback_patched",
        feedback,
        viewModel: assembleViewModel(
          state.workspace,
          currentAcks(),
          feedback
        ),
      })
    },
  }
}
