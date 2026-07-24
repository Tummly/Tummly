import {
  createFeedbackDetailsModule,
  type CorrectClassificationResponse,
  type FeedbackDetailsSnapshot,
} from "@/lib/operatorHome/createFeedbackDetailsModule"
import { createFinishSettingUpAcksModule } from "@/lib/operatorHome/createFinishSettingUpAcksModule"
import { buildOperatorHomeViewModel } from "@/lib/operatorHome/buildHomeViewModel"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import type {
  ChecklistAcksResponse,
  FeedbackDetailsResponse,
  FeedbackResponse,
  FeedbackSentiment,
  HomeLatestActivityItem,
  HomeLatestActivityResponse,
  HomePerformanceResponse,
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
  performanceLoadStatus: "idle" | "loading" | "loaded" | "error"
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
  getHomeLatestActivity: (
    locationId: number
  ) => Promise<HomeLatestActivityResponse>
  getHomePerformance: (
    locationId: number,
    from: string,
    to: string
  ) => Promise<HomePerformanceResponse>
  getHomePerformanceDateRange: () => HomePerformanceDateRange
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
  onPerformanceLoadError?: (message: string) => void
}

export type OperatorHomePageModule = {
  getSnapshot: () => OperatorHomePageSnapshot
  subscribe: (listener: () => void) => () => void
  connect: () => Promise<void>
  disconnect: () => Promise<void>
  syncWorkspace: (input: OperatorHomeWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Re-load Home using the current Home performance date range from adapters. */
  reloadForHomePerformanceDateRange: () => Promise<void>
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
  performanceLoadStatus: OperatorHomePageSnapshot["performanceLoadStatus"]
  workspace: OperatorHomeWorkspaceInput | null
  feedback: { total: number; recent: FeedbackResponse["recent"] } | null
  latestActivity: HomeLatestActivityItem[] | null
  feedbackSubmitted: number | null
  feedbackSubmittedPrevious: number | null
  guestsJoined: number | null
  guestsJoinedPrevious: number | null
  qrScans: number | null
  qrScansPrevious: number | null
  viewModel: OperatorHomeViewModel | null
  actionError: string | null
  loadGeneration: number
  performanceLoadGeneration: number
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
      latestActivity: HomeLatestActivityItem[]
      feedbackSubmitted: number | null
      feedbackSubmittedPrevious: number | null
      guestsJoined: number | null
      guestsJoinedPrevious: number | null
      qrScans: number | null
      qrScansPrevious: number | null
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "load_failed"; generation: number }
  | { type: "performance_load_started"; generation: number }
  | {
      type: "performance_load_succeeded"
      generation: number
      feedbackSubmitted: number
      feedbackSubmittedPrevious: number
      guestsJoined: number
      guestsJoinedPrevious: number
      qrScans: number
      qrScansPrevious: number
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "performance_load_failed"; generation: number }
  | {
      type: "view_model_updated"
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "activity_patched"
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      latestActivity: HomeLatestActivityItem[]
      viewModel: OperatorHomeViewModel | null
    }
  | {
      type: "feedback_patched"
      feedback: { total: number; recent: FeedbackResponse["recent"] }
      latestActivity: HomeLatestActivityItem[] | null
      viewModel: OperatorHomeViewModel | null
    }
  | { type: "action_error"; error: string | null }

function assembleViewModel(
  workspace: OperatorHomeWorkspaceInput,
  checklistAcks: OperatorHomeChecklistAcks,
  feedback: HomeState["feedback"],
  latestActivity: HomeState["latestActivity"],
  feedbackSubmitted: number | null,
  guestsJoined: number | null,
  feedbackSubmittedPrevious: number | null,
  guestsJoinedPrevious: number | null,
  qrScans: number | null,
  qrScansPrevious: number | null,
  dateRangeLabel: string
): OperatorHomeViewModel | null {
  if (workspace.selectedLocationId == null) {
    return null
  }

  return buildOperatorHomeViewModel({
    locations: workspace.locations,
    selectedLocationId: workspace.selectedLocationId,
    feedback,
    latestActivity,
    feedbackSubmitted,
    feedbackSubmittedPrevious,
    guestsJoined,
    guestsJoinedPrevious,
    qrScans,
    qrScansPrevious,
    dateRangeLabel,
    checklistAcks,
  })
}

function reduce(state: HomeState, action: HomeAction): HomeState {
  switch (action.type) {
    case "workspace_cleared":
      return {
        ...state,
        loadStatus: "idle",
        performanceLoadStatus: "idle",
        workspace: null,
        feedback: null,
        latestActivity: null,
        feedbackSubmitted: null,
        feedbackSubmittedPrevious: null,
        guestsJoined: null,
        guestsJoinedPrevious: null,
        qrScans: null,
        qrScansPrevious: null,
        viewModel: null,
        actionError: null,
      }
    case "workspace_synced":
      return {
        ...state,
        workspace: action.workspace,
        feedback: null,
        latestActivity: null,
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
        latestActivity: action.latestActivity,
        feedbackSubmitted: action.feedbackSubmitted,
        feedbackSubmittedPrevious: action.feedbackSubmittedPrevious,
        guestsJoined: action.guestsJoined,
        guestsJoinedPrevious: action.guestsJoinedPrevious,
        qrScans: action.qrScans,
        qrScansPrevious: action.qrScansPrevious,
        viewModel: action.viewModel,
      }
    case "load_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return { ...state, loadStatus: "error" }
    case "performance_load_started":
      return {
        ...state,
        performanceLoadStatus: "loading",
        performanceLoadGeneration: action.generation,
      }
    case "performance_load_succeeded":
      if (action.generation !== state.performanceLoadGeneration) {
        return state
      }
      return {
        ...state,
        performanceLoadStatus: "loaded",
        feedbackSubmitted: action.feedbackSubmitted,
        feedbackSubmittedPrevious: action.feedbackSubmittedPrevious,
        guestsJoined: action.guestsJoined,
        guestsJoinedPrevious: action.guestsJoinedPrevious,
        qrScans: action.qrScans,
        qrScansPrevious: action.qrScansPrevious,
        viewModel: action.viewModel,
      }
    case "performance_load_failed":
      if (action.generation !== state.performanceLoadGeneration) {
        return state
      }
      return { ...state, performanceLoadStatus: "error" }
    case "view_model_updated":
      return {
        ...state,
        viewModel: action.viewModel,
      }
    case "activity_patched":
      return {
        ...state,
        feedback: action.feedback,
        latestActivity: action.latestActivity,
        viewModel: action.viewModel,
      }
    case "feedback_patched":
      return {
        ...state,
        feedback: action.feedback,
        latestActivity: action.latestActivity ?? state.latestActivity,
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
    performanceLoadStatus: "idle",
    workspace: null,
    feedback: null,
    latestActivity: null,
    feedbackSubmitted: null,
    feedbackSubmittedPrevious: null,
    guestsJoined: null,
    guestsJoinedPrevious: null,
    qrScans: null,
    qrScansPrevious: null,
    viewModel: null,
    actionError: null,
    loadGeneration: 0,
    performanceLoadGeneration: 0,
  }

  let snapshot: OperatorHomePageSnapshot = {
    loadStatus: state.loadStatus,
    performanceLoadStatus: state.performanceLoadStatus,
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
      logoUploaded: ackSnapshot.logoUploaded,
    }
  }

  const publish = () => {
    const ackSnapshot = acks.getSnapshot()
    snapshot = {
      loadStatus: state.loadStatus,
      performanceLoadStatus: state.performanceLoadStatus,
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

  const currentDateRangeLabel = () =>
    labelForHomePerformanceDateRange(adapters.getHomePerformanceDateRange())

  const refreshViewModelFromAcks = () => {
    const workspace = state.workspace
    if (workspace == null) {
      publish()
      return
    }

    dispatch({
      type: "view_model_updated",
      viewModel: assembleViewModel(
        workspace,
        currentAcks(),
        state.feedback,
        state.latestActivity,
        state.feedbackSubmitted,
        state.guestsJoined,
        state.feedbackSubmittedPrevious,
        state.guestsJoinedPrevious,
        state.qrScans,
        state.qrScansPrevious,
        currentDateRangeLabel()
      ),
    })
  }

  acks.subscribe(() => {
    refreshViewModelFromAcks()
  })

  feedbackDetails.subscribe(() => {
    publish()
  })

  const fetchPerformanceForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.performanceLoadGeneration + 1
    dispatch({ type: "performance_load_started", generation })

    try {
      const performanceWindow = resolveHomePerformanceWindow(
        adapters.getHomePerformanceDateRange()
      )
      const performanceResult = await adapters.getHomePerformance(
        selectedLocationId,
        performanceWindow.from.toISOString(),
        performanceWindow.to.toISOString()
      )

      if (generation !== state.performanceLoadGeneration) {
        return
      }

      dispatch({
        type: "performance_load_succeeded",
        generation,
        feedbackSubmitted: performanceResult.feedbackSubmitted,
        feedbackSubmittedPrevious:
          performanceResult.feedbackSubmittedPrevious,
        guestsJoined: performanceResult.guestsJoined,
        guestsJoinedPrevious: performanceResult.guestsJoinedPrevious,
        qrScans: performanceResult.qrScans,
        qrScansPrevious: performanceResult.qrScansPrevious,
        viewModel: assembleViewModel(
          workspace,
          currentAcks(),
          state.feedback,
          state.latestActivity,
          performanceResult.feedbackSubmitted,
          performanceResult.guestsJoined,
          performanceResult.feedbackSubmittedPrevious,
          performanceResult.guestsJoinedPrevious,
          performanceResult.qrScans,
          performanceResult.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    } catch {
      if (generation !== state.performanceLoadGeneration) {
        return
      }
      dispatch({ type: "performance_load_failed", generation })
      adapters.onPerformanceLoadError?.(
        "Could not load performance stats. Please try again."
      )
    }
  }

  const refreshFeedbackInBackground = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (
      workspace == null
      || selectedLocationId == null
      || state.loadStatus !== "loaded"
    ) {
      return
    }

    try {
      const [feedbackResult, latestActivityResult] = await Promise.all([
        adapters.getFeedback(selectedLocationId),
        adapters.getHomeLatestActivity(selectedLocationId),
      ])
      const feedback = {
        total: feedbackResult.total,
        recent: feedbackResult.recent,
      }
      const latestActivity = latestActivityResult.items
      dispatch({
        type: "activity_patched",
        feedback,
        latestActivity,
        viewModel: assembleViewModel(
          workspace,
          currentAcks(),
          feedback,
          latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    } catch {
      // Keep Latest activity on the last good list when a quiet refresh fails.
    }
  }

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    dispatch({ type: "load_started", generation })

    let feedback: { total: number; recent: FeedbackResponse["recent"] }
    let latestActivity: HomeLatestActivityItem[]

    try {
      const [feedbackResult, latestActivityResult] = await Promise.all([
        adapters.getFeedback(selectedLocationId),
        adapters.getHomeLatestActivity(selectedLocationId),
      ])
      feedback = {
        total: feedbackResult.total,
        recent: feedbackResult.recent,
      }
      latestActivity = latestActivityResult.items
      await acks.load(selectedLocationId)
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      dispatch({ type: "load_failed", generation })
      return
    }

    if (generation !== state.loadGeneration) {
      return
    }

    dispatch({
      type: "load_succeeded",
      generation,
      feedback,
      latestActivity,
      feedbackSubmitted: state.feedbackSubmitted,
      feedbackSubmittedPrevious: state.feedbackSubmittedPrevious,
      guestsJoined: state.guestsJoined,
      guestsJoinedPrevious: state.guestsJoinedPrevious,
      qrScans: state.qrScans,
      qrScansPrevious: state.qrScansPrevious,
      viewModel: assembleViewModel(
        workspace,
        currentAcks(),
        feedback,
        latestActivity,
        state.feedbackSubmitted,
        state.guestsJoined,
        state.feedbackSubmittedPrevious,
        state.guestsJoinedPrevious,
        state.qrScans,
        state.qrScansPrevious,
        currentDateRangeLabel()
      ),
    })

    await fetchPerformanceForSelectedLocation()
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

    void fetchPerformanceForSelectedLocation()
    void refreshFeedbackInBackground()

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
          void fetchPerformanceForSelectedLocation()
          void refreshFeedbackInBackground()
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
          logoUploaded: false,
        }
        const viewModel = assembleViewModel(
          input,
          emptyAcks,
          null,
          null,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        )
        dispatch({ type: "workspace_synced", workspace: input, viewModel })
        await loadForSelectedLocation()
        return
      }

      // Same Owned location: refresh shell-facing workspace fields only.
      dispatch({
        type: "workspace_fields_updated",
        workspace: input,
        viewModel: assembleViewModel(
          input,
          currentAcks(),
          state.feedback,
          state.latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    },
    retryLoad: () => loadForSelectedLocation(),
    reloadForHomePerformanceDateRange: async () => {
      const workspace = state.workspace
      if (workspace != null) {
        dispatch({
          type: "view_model_updated",
          viewModel: assembleViewModel(
            workspace,
            currentAcks(),
            state.feedback,
            state.latestActivity,
            state.feedbackSubmitted,
            state.guestsJoined,
            state.feedbackSubmittedPrevious,
            state.guestsJoinedPrevious,
            state.qrScans,
            state.qrScansPrevious,
            currentDateRangeLabel()
          ),
        })
      }
      await fetchPerformanceForSelectedLocation()
    },
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
      const latestActivity =
        state.latestActivity?.map((item) =>
          item.kind === "feedback" && item.id === feedbackId
            ? {
                ...item,
                classificationStatus: "Succeeded" as const,
                sentiment: nextSentiment,
              }
            : item
        ) ?? null
      dispatch({
        type: "feedback_patched",
        feedback,
        latestActivity,
        viewModel: assembleViewModel(
          state.workspace,
          currentAcks(),
          feedback,
          latestActivity,
          state.feedbackSubmitted,
          state.guestsJoined,
          state.feedbackSubmittedPrevious,
          state.guestsJoinedPrevious,
          state.qrScans,
          state.qrScansPrevious,
          currentDateRangeLabel()
        ),
      })
    },
  }
}
