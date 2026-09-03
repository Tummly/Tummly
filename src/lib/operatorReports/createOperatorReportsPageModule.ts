import { isAccountLockedBillingStatus } from "@/lib/operatorHome/lockAlertPresentation"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  buildReportsOverviewViewModel,
  REPORTS_HUB_LOAD_ERROR_MESSAGE,
  type ReportsOverviewViewModel,
} from "@/lib/operatorReports/reportsOverviewPresentation"
import type {
  ReportsKpiLoadStatus,
  ReportsOverviewResponse,
  ReportsSurface,
} from "@/types/operatorReports"

export type OperatorReportsWorkspaceLocation = {
  id: number
  locationName: string
  address: string
}

export type OperatorReportsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorReportsWorkspaceLocation[]
  billingStatus: string
  /** Chargeback overlay — omit / false keeps Export enabled. */
  chargebackRestricted?: boolean
}

export type OperatorReportsPageSnapshot = {
  activeSurface: ReportsSurface
  hubLoadStatus: ReportsKpiLoadStatus
  hubOverview: ReportsOverviewViewModel | null
  hubLoadError: string | null
  exportAllowed: boolean
  dateRange: HomePerformanceDateRange
  dateRangeLabel: string
  exportDialogOpen: boolean
  selectedLocationId: number | null
  selectedLocationName: string | null
}

export type OperatorReportsPageAdapters = {
  getOverview: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<ReportsOverviewResponse>
  getReportsDateRange: () => HomePerformanceDateRange
  /** Seams for later child report tickets — unused in ticket 11 hub wire. */
  getCapture?: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<unknown>
  getFeedback?: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<unknown>
  getOffers?: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<unknown>
  getCampaigns?: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<unknown>
  getWeeklyBrief?: (locationId: number) => Promise<unknown>
  generateWeeklyBrief?: (locationId: number) => Promise<unknown>
  downloadExport?: (input: {
    locationId: number
    from: string
    to: string
    kind: string
  }) => Promise<unknown>
}

export type OperatorReportsPageModule = {
  getSnapshot: () => OperatorReportsPageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorReportsWorkspaceInput) => Promise<void>
  setActiveSurface: (surface: ReportsSurface) => void
  reloadForReportsDateRange: () => Promise<void>
  retryHubLoad: () => Promise<void>
  openExportDialog: () => void
  closeExportDialog: () => void
}

type ModuleState = {
  activeSurface: ReportsSurface
  hubLoadStatus: ReportsKpiLoadStatus
  hubOverview: ReportsOverviewViewModel | null
  hubLoadError: string | null
  exportAllowed: boolean
  exportDialogOpen: boolean
  workspace: OperatorReportsWorkspaceInput | null
  hubLoadGeneration: number
}

function resolveExportAllowed(input: OperatorReportsWorkspaceInput): boolean {
  if (isAccountLockedBillingStatus(input.billingStatus)) {
    return false
  }
  if (input.chargebackRestricted === true) {
    return false
  }
  return true
}

function selectedLocationName(
  workspace: OperatorReportsWorkspaceInput | null
): string | null {
  if (workspace?.selectedLocationId == null) {
    return null
  }
  return (
    workspace.locations.find((row) => row.id === workspace.selectedLocationId)
      ?.locationName ?? null
  )
}

/**
 * Layout-scoped Reports page module — hub overview load + shared date range
 * + export-allowed from shell billing lock. Child KPI surfaces land later.
 */
export function createOperatorReportsPageModule(
  adapters: OperatorReportsPageAdapters
): OperatorReportsPageModule {
  let state: ModuleState = {
    activeSurface: "hub",
    hubLoadStatus: "idle",
    hubOverview: null,
    hubLoadError: null,
    exportAllowed: true,
    exportDialogOpen: false,
    workspace: null,
    hubLoadGeneration: 0,
  }

  let snapshot: OperatorReportsPageSnapshot = projectSnapshot()

  const listeners = new Set<() => void>()

  function projectSnapshot(): OperatorReportsPageSnapshot {
    const dateRange = adapters.getReportsDateRange()
    return {
      activeSurface: state.activeSurface,
      hubLoadStatus: state.hubLoadStatus,
      hubOverview: state.hubOverview,
      hubLoadError: state.hubLoadError,
      exportAllowed: state.exportAllowed,
      dateRange,
      dateRangeLabel: labelForHomePerformanceDateRange(dateRange),
      exportDialogOpen: state.exportDialogOpen,
      selectedLocationId: state.workspace?.selectedLocationId ?? null,
      selectedLocationName: selectedLocationName(state.workspace),
    }
  }

  const publish = () => {
    snapshot = projectSnapshot()
    for (const listener of listeners) {
      listener()
    }
  }

  const loadHub = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      state = {
        ...state,
        hubLoadStatus: "idle",
        hubOverview: null,
        hubLoadError: null,
      }
      publish()
      return
    }

    const generation = state.hubLoadGeneration + 1
    state = {
      ...state,
      hubLoadStatus: "loading",
      hubLoadGeneration: generation,
      hubLoadError: null,
    }
    publish()

    try {
      const window = resolveHomePerformanceWindow(
        adapters.getReportsDateRange()
      )
      const response = await adapters.getOverview({
        locationId,
        from: window.from.toISOString(),
        to: window.to.toISOString(),
      })

      if (generation !== state.hubLoadGeneration) {
        return
      }

      if (!response.success) {
        state = {
          ...state,
          hubLoadStatus: "error",
          hubOverview: null,
          hubLoadError:
            response.message?.trim() || REPORTS_HUB_LOAD_ERROR_MESSAGE,
        }
        publish()
        return
      }

      if (response.lifetimeEmpty) {
        state = {
          ...state,
          hubLoadStatus: "lifetimeEmpty",
          hubOverview: null,
          hubLoadError: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        hubLoadStatus: "ready",
        hubOverview: buildReportsOverviewViewModel(response),
        hubLoadError: null,
      }
      publish()
    } catch {
      if (generation !== state.hubLoadGeneration) {
        return
      }
      state = {
        ...state,
        hubLoadStatus: "error",
        hubOverview: null,
        hubLoadError: REPORTS_HUB_LOAD_ERROR_MESSAGE,
      }
      publish()
    }
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
      const locationChanged =
        state.workspace?.selectedLocationId !== input.selectedLocationId
      state = {
        ...state,
        workspace: input,
        exportAllowed: resolveExportAllowed(input),
        ...(locationChanged
          ? { exportDialogOpen: false }
          : {}),
      }
      publish()

      if (state.activeSurface === "hub") {
        await loadHub()
      }
    },
    setActiveSurface(surface) {
      const changed = state.activeSurface !== surface
      state = { ...state, activeSurface: surface }
      publish()
      if (surface === "hub" && changed) {
        void loadHub()
      }
    },
    async reloadForReportsDateRange() {
      publish()
      if (state.activeSurface === "hub") {
        await loadHub()
      }
    },
    async retryHubLoad() {
      await loadHub()
    },
    openExportDialog() {
      if (!state.exportAllowed) {
        return
      }
      state = { ...state, exportDialogOpen: true }
      publish()
    },
    closeExportDialog() {
      state = { ...state, exportDialogOpen: false }
      publish()
    },
  }
}
