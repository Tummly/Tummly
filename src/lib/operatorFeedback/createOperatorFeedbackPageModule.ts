import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import { formatRelativeTime } from "@/lib/operatorHome/relativeTime"
import { buildFeedbackSummarySection } from "@/lib/operatorFeedback/buildFeedbackSummarySection"
import type { FeedbackSummaryResponse } from "@/types/dashboard"
import type {
  OperatorFeedbackInboxTabId,
  OperatorFeedbackPageViewModel,
} from "@/types/operatorFeedback"

export type OperatorFeedbackWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorFeedbackWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorFeedbackWorkspaceLocation[]
}

export type OperatorFeedbackPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorFeedbackPageViewModel | null
  /** Inbox tab target; Review needs attention switches to needs-attention. */
  activeInboxTabId: OperatorFeedbackInboxTabId
  /** Bumps when empty-state Change period should open the header date control. */
  openDateRangeRequestId: number
  /** Bumps when Review needs attention should scroll to the inbox shell. */
  scrollToInboxRequestId: number
}

export type OperatorFeedbackPageAdapters = {
  getFeedbackSummary: (params: {
    locationId: number
    from: string
    to: string
  }) => Promise<FeedbackSummaryResponse>
  getFeedbackPageDateRange: () => HomePerformanceDateRange
  getNow?: () => Date
  scheduleReady?: () => Promise<void>
}

export type OperatorFeedbackPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorFeedbackPageSnapshot
  syncWorkspace: (input: OperatorFeedbackWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  reloadForFeedbackPageDateRange: () => Promise<void>
  reviewNeedsAttention: () => void
  requestOpenDateRange: () => void
  setActiveInboxTabId: (id: OperatorFeedbackInboxTabId) => void
}

type ModuleState = {
  loadStatus: OperatorFeedbackPageSnapshot["loadStatus"]
  viewModel: OperatorFeedbackPageViewModel | null
  workspace: OperatorFeedbackWorkspaceInput | null
  activeInboxTabId: OperatorFeedbackInboxTabId
  openDateRangeRequestId: number
  scrollToInboxRequestId: number
  lastLoadedAtIso: string | null
  loadGeneration: number
  summaryLoadGeneration: number
}

const FEEDBACK_LOAD_ERROR_MESSAGE =
  "Could not load Feedback. Please try again."

function resolveLocationName(
  input: OperatorFeedbackWorkspaceInput,
  locationId: number
): string {
  return (
    input.locations.find((location) => location.id === locationId)
      ?.locationName ?? ""
  )
}

/**
 * Operator Feedback page module — adapters in, snapshot out.
 * Owns visit-scoped summary load for the selected Owned location.
 */
export function createOperatorFeedbackPageModule(
  adapters: OperatorFeedbackPageAdapters
): OperatorFeedbackPageModule {
  const scheduleReady = adapters.scheduleReady ?? (() => Promise.resolve())
  const getNow = adapters.getNow ?? (() => new Date())

  let state: ModuleState = {
    loadStatus: "idle",
    viewModel: null,
    workspace: null,
    activeInboxTabId: "all",
    openDateRangeRequestId: 0,
    scrollToInboxRequestId: 0,
    lastLoadedAtIso: null,
    loadGeneration: 0,
    summaryLoadGeneration: 0,
  }

  let snapshot: OperatorFeedbackPageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    activeInboxTabId: state.activeInboxTabId,
    openDateRangeRequestId: state.openDateRangeRequestId,
    scrollToInboxRequestId: state.scrollToInboxRequestId,
  }
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      activeInboxTabId: state.activeInboxTabId,
      openDateRangeRequestId: state.openDateRangeRequestId,
      scrollToInboxRequestId: state.scrollToInboxRequestId,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const buildViewModel = (
    input: OperatorFeedbackWorkspaceInput,
    locationId: number,
    summary: FeedbackSummaryResponse,
    loadedAtIso: string
  ): OperatorFeedbackPageViewModel => {
    const dateRange = adapters.getFeedbackPageDateRange()
    const now = getNow()
    return {
      locationId,
      locationName: resolveLocationName(input, locationId),
      dateRangeLabel: labelForHomePerformanceDateRange(dateRange),
      updatedRelativeLabel: formatRelativeTime(loadedAtIso, now.getTime()),
      needsAttentionCount: summary.needsAttentionTotal,
      summary: buildFeedbackSummarySection(summary),
    }
  }

  const fetchSummary = async (options: {
    locationId: number
    workspace: OperatorFeedbackWorkspaceInput
    isInitialLoad: boolean
  }): Promise<void> => {
    const generation = ++state.summaryLoadGeneration
    state = {
      ...state,
      loadStatus: "loading",
      ...(options.isInitialLoad ? { viewModel: null } : {}),
    }
    publish()

    await scheduleReady()

    const performanceWindow = resolveHomePerformanceWindow(
      adapters.getFeedbackPageDateRange(),
      getNow()
    )
    const from = performanceWindow.from.toISOString()
    const to = performanceWindow.to.toISOString()

    const settled = await adapters
      .getFeedbackSummary({
        locationId: options.locationId,
        from,
        to,
      })
      .then((response) => ({ ok: true as const, response }))
      .catch(() => ({ ok: false as const }))

    if (generation !== state.summaryLoadGeneration) {
      return
    }

    if (!settled.ok) {
      state = {
        ...state,
        loadStatus: "error",
        ...(options.isInitialLoad ? { viewModel: null } : {}),
        workspace: options.workspace,
      }
      publish()
      return
    }

    const loadedAtIso = getNow().toISOString()
    state = {
      ...state,
      loadStatus: "loaded",
      viewModel: buildViewModel(
        options.workspace,
        options.locationId,
        settled.response,
        loadedAtIso
      ),
      lastLoadedAtIso: loadedAtIso,
      workspace: options.workspace,
    }
    publish()
  }

  const loadForWorkspace = async (
    input: OperatorFeedbackWorkspaceInput
  ): Promise<void> => {
    const generation = ++state.loadGeneration
    state = {
      ...state,
      loadStatus: "loading",
      viewModel: null,
      workspace: input,
      activeInboxTabId: "all",
    }
    publish()

    if (input.selectedLocationId == null) {
      if (generation !== state.loadGeneration) {
        return
      }
      state.summaryLoadGeneration += 1
      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: null,
        workspace: input,
      }
      publish()
      return
    }

    if (generation !== state.loadGeneration) {
      return
    }

    await fetchSummary({
      locationId: input.selectedLocationId,
      workspace: input,
      isInitialLoad: true,
    })
  }

  return {
    getSnapshot() {
      return snapshot
    },
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    async syncWorkspace(input) {
      await loadForWorkspace(input)
    },
    async retryLoad() {
      if (state.workspace == null) {
        return
      }
      await loadForWorkspace(state.workspace)
    },
    async reloadForFeedbackPageDateRange() {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      if (workspace == null || locationId == null) {
        return
      }
      await fetchSummary({
        locationId,
        workspace,
        isInitialLoad: false,
      })
    },
    reviewNeedsAttention() {
      state = {
        ...state,
        activeInboxTabId: "needs-attention",
        scrollToInboxRequestId: state.scrollToInboxRequestId + 1,
      }
      publish()
    },
    requestOpenDateRange() {
      state = {
        ...state,
        openDateRangeRequestId: state.openDateRangeRequestId + 1,
      }
      publish()
    },
    setActiveInboxTabId(id) {
      state = {
        ...state,
        activeInboxTabId: id,
      }
      publish()
    },
  }
}

export { FEEDBACK_LOAD_ERROR_MESSAGE }
