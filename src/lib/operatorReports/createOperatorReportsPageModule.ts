import { isAccountLockedBillingStatus } from "@/lib/operatorHome/lockAlertPresentation"
import {
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import {
  buildReportsCaptureViewModel,
  REPORTS_CAPTURE_LOAD_ERROR_MESSAGE,
  type CaptureReportViewModel,
} from "@/lib/operatorReports/captureReportPresentation"
import {
  buildFeedbackReportViewModel,
  FEEDBACK_REPORT_PAGE_COPY,
  type FeedbackReportViewModel,
} from "@/lib/operatorReports/feedbackReportPresentation"
import {
  buildOffersReportViewModel,
  OFFERS_REPORT_PAGE_COPY,
  type OffersReportData,
} from "@/lib/operatorReports/offersReportPresentation"
import {
  buildCampaignsReportViewModel,
  CAMPAIGNS_REPORT_PAGE_COPY,
  type CampaignsReportViewModel,
} from "@/lib/operatorReports/campaignsReportPresentation"
import {
  buildReportsOverviewViewModel,
  REPORTS_HUB_LOAD_ERROR_MESSAGE,
  type ReportsOverviewViewModel,
} from "@/lib/operatorReports/reportsOverviewPresentation"
import {
  buildReportsWeeklyBriefHubSecondary,
  REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
} from "@/lib/operatorReports/reportsWeeklyBriefPresentation"
import type {
  WeeklyBriefBody,
  WeeklyBriefFeedbackSummary,
  WeeklyBriefGenerateResponse,
  WeeklyBriefGetResponse,
  WeeklyBriefMarkReviewedResponse,
  WeeklyBriefWhatChangedRow,
} from "@/types/operatorHome"
import type {
  ReportsCampaignsResponse,
  ReportsCaptureResponse,
  ReportsFeedbackResponse,
  ReportsKpiLoadStatus,
  ReportsOffersResponse,
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

export type OperatorReportsWeeklyBriefStatus =
  | "empty"
  | "loading"
  | "ready"
  | "error"

export type OperatorReportsWeeklyBriefMeta = {
  period: string
  dataSources: string[]
  confidence: string
  generatedAtUtc: string
}

export type OperatorReportsWeeklyBriefViewModel = {
  status: OperatorReportsWeeklyBriefStatus
  week: string | null
  body: WeeklyBriefBody | null
  headline: string | null
  secondary: string | null
  meta: OperatorReportsWeeklyBriefMeta | null
  executiveSummary: string | null
  /** Ready What changed rows; empty → hide section. */
  whatChanged: WeeklyBriefWhatChangedRow[]
  /** Ready Feedback summary facts; null → hide section. */
  feedbackSummary: WeeklyBriefFeedbackSummary | null
  /** Durable mark-reviewed timestamp from the ready envelope; null until marked. */
  reviewedAtUtc: string | null
  reviewedByUserId: number | null
  markReviewedBusy: boolean
  errorMessage: string | null
  errorRetryable: boolean
  generateBusy: boolean
}

export type OperatorReportsPageSnapshot = {
  activeSurface: ReportsSurface
  hubLoadStatus: ReportsKpiLoadStatus
  hubOverview: ReportsOverviewViewModel | null
  hubLoadError: string | null
  weeklyBrief: OperatorReportsWeeklyBriefViewModel
  captureLoadStatus: ReportsKpiLoadStatus
  captureReport: CaptureReportViewModel | null
  captureLoadError: string | null
  feedbackLoadStatus: ReportsKpiLoadStatus
  feedbackReport: FeedbackReportViewModel | null
  feedbackLoadError: string | null
  offersLoadStatus: ReportsKpiLoadStatus
  offersReport: OffersReportData | null
  offersLoadError: string | null
  campaignsLoadStatus: ReportsKpiLoadStatus
  campaignsReport: CampaignsReportViewModel | null
  campaignsLoadError: string | null
  exportAllowed: boolean
  /**
   * Mark as reviewed is an annotation write — allowed under Soft lock.
   * Always true while workspace is present (gate is Area reports View on the API).
   */
  markAsReviewedAllowed: boolean
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
  getWeeklyBrief: (locationId: number) => Promise<WeeklyBriefGetResponse>
  generateWeeklyBrief: (
    locationId: number
  ) => Promise<WeeklyBriefGenerateResponse>
  markWeeklyBriefReviewed: (
    locationId: number,
    week?: string | null
  ) => Promise<WeeklyBriefMarkReviewedResponse>
  getCapture: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<ReportsCaptureResponse>
  getFeedback: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<ReportsFeedbackResponse>
  getOffers: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<ReportsOffersResponse>
  getCampaigns: (input: {
    locationId: number
    from: string
    to: string
  }) => Promise<ReportsCampaignsResponse>
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
  /** GET again; generate again only if still missing (lock 10). */
  retryWeeklyBrief: () => Promise<void>
  /**
   * Header Generate brief: navigate-only when ready; otherwise POST then ready.
   * Returns true when the UI should navigate to the weekly-brief page.
   */
  ensureWeeklyBriefReady: () => Promise<boolean>
  /** Page empty CTA: POST generate in place, then show body. */
  generateWeeklyBriefInPlace: () => Promise<void>
  /**
   * Persist Mark as reviewed on the ready location+week row; updates snapshot from
   * the ready envelope. Soft lock does not block (annotation).
   */
  markWeeklyBriefAsReviewed: () => Promise<boolean>
  retryCaptureLoad: () => Promise<void>
  retryFeedbackLoad: () => Promise<void>
  retryOffersLoad: () => Promise<void>
  retryCampaignsLoad: () => Promise<void>
  openExportDialog: () => void
  closeExportDialog: () => void
}

type ModuleState = {
  activeSurface: ReportsSurface
  hubLoadStatus: ReportsKpiLoadStatus
  hubOverview: ReportsOverviewViewModel | null
  hubLoadError: string | null
  weeklyBrief: OperatorReportsWeeklyBriefViewModel
  captureLoadStatus: ReportsKpiLoadStatus
  captureReport: CaptureReportViewModel | null
  captureLoadError: string | null
  feedbackLoadStatus: ReportsKpiLoadStatus
  feedbackReport: FeedbackReportViewModel | null
  feedbackLoadError: string | null
  offersLoadStatus: ReportsKpiLoadStatus
  offersReport: OffersReportData | null
  offersLoadError: string | null
  campaignsLoadStatus: ReportsKpiLoadStatus
  campaignsReport: CampaignsReportViewModel | null
  campaignsLoadError: string | null
  exportAllowed: boolean
  exportDialogOpen: boolean
  workspace: OperatorReportsWorkspaceInput | null
  hubLoadGeneration: number
  weeklyBriefGeneration: number
  captureLoadGeneration: number
  feedbackLoadGeneration: number
  offersLoadGeneration: number
  campaignsLoadGeneration: number
}

function emptyWeeklyBrief(
  overrides: Partial<OperatorReportsWeeklyBriefViewModel> = {}
): OperatorReportsWeeklyBriefViewModel {
  return {
    status: "empty",
    week: null,
    body: null,
    headline: null,
    secondary: null,
    meta: null,
    executiveSummary: null,
    whatChanged: [],
    feedbackSummary: null,
    reviewedAtUtc: null,
    reviewedByUserId: null,
    markReviewedBusy: false,
    errorMessage: null,
    errorRetryable: false,
    generateBusy: false,
    ...overrides,
  }
}

function mapReadyWeeklyBrief(
  response: Extract<WeeklyBriefGetResponse, { ready: true }>
): OperatorReportsWeeklyBriefViewModel {
  return {
    status: "ready",
    week: response.week,
    body: response.body,
    headline: response.body.headline,
    secondary: buildReportsWeeklyBriefHubSecondary(
      response.body,
      response.metrics
    ),
    meta: {
      period: response.meta.period,
      dataSources: response.meta.dataSources,
      confidence: response.meta.confidence,
      generatedAtUtc: response.generatedAtUtc,
    },
    executiveSummary: response.executiveSummary,
    whatChanged: response.whatChanged ?? [],
    feedbackSummary: response.feedbackSummary ?? null,
    reviewedAtUtc: response.reviewedAtUtc ?? null,
    reviewedByUserId: response.reviewedByUserId ?? null,
    markReviewedBusy: false,
    errorMessage: null,
    errorRetryable: false,
    generateBusy: false,
  }
}

function weeklyBriefErrorFrom(
  message: string | null | undefined,
  retryable: boolean
): OperatorReportsWeeklyBriefViewModel {
  return emptyWeeklyBrief({
    status: "error",
    errorMessage:
      message?.trim() || REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE,
    errorRetryable: retryable,
  })
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
 * Layout-scoped Reports page module — hub overview + Capture + Feedback + Offers +
 * Campaigns KPI loads + Weekly Brief slice + shared date range + export-allowed
 * from shell lock.
 */
export function createOperatorReportsPageModule(
  adapters: OperatorReportsPageAdapters
): OperatorReportsPageModule {
  let state: ModuleState = {
    activeSurface: "hub",
    hubLoadStatus: "idle",
    hubOverview: null,
    hubLoadError: null,
    weeklyBrief: emptyWeeklyBrief(),
    captureLoadStatus: "idle",
    captureReport: null,
    captureLoadError: null,
    feedbackLoadStatus: "idle",
    feedbackReport: null,
    feedbackLoadError: null,
    offersLoadStatus: "idle",
    offersReport: null,
    offersLoadError: null,
    campaignsLoadStatus: "idle",
    campaignsReport: null,
    campaignsLoadError: null,
    exportAllowed: true,
    exportDialogOpen: false,
    workspace: null,
    hubLoadGeneration: 0,
    weeklyBriefGeneration: 0,
    captureLoadGeneration: 0,
    feedbackLoadGeneration: 0,
    offersLoadGeneration: 0,
    campaignsLoadGeneration: 0,
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
      weeklyBrief: state.weeklyBrief,
      captureLoadStatus: state.captureLoadStatus,
      captureReport: state.captureReport,
      captureLoadError: state.captureLoadError,
      feedbackLoadStatus: state.feedbackLoadStatus,
      feedbackReport: state.feedbackReport,
      feedbackLoadError: state.feedbackLoadError,
      offersLoadStatus: state.offersLoadStatus,
      offersReport: state.offersReport,
      offersLoadError: state.offersLoadError,
      campaignsLoadStatus: state.campaignsLoadStatus,
      campaignsReport: state.campaignsReport,
      campaignsLoadError: state.campaignsLoadError,
      exportAllowed: state.exportAllowed,
      markAsReviewedAllowed: state.workspace != null,
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

  const patchWeeklyBrief = (
    next: OperatorReportsWeeklyBriefViewModel
  ) => {
    state = { ...state, weeklyBrief: next }
    publish()
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

  /**
   * GET-only load — no auto POST on hub / weekly-brief enter (lock 10).
   */
  const loadWeeklyBriefGetOnly = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      patchWeeklyBrief(emptyWeeklyBrief())
      return
    }

    const generation = state.weeklyBriefGeneration + 1
    state = { ...state, weeklyBriefGeneration: generation }

    if (state.weeklyBrief.status !== "ready") {
      patchWeeklyBrief(
        emptyWeeklyBrief({
          status: "loading",
        })
      )
    }

    try {
      const response = await adapters.getWeeklyBrief(locationId)
      if (generation !== state.weeklyBriefGeneration) {
        return
      }

      if (response.success && response.ready) {
        patchWeeklyBrief(mapReadyWeeklyBrief(response))
        return
      }

      if (response.success && !response.ready) {
        patchWeeklyBrief(
          emptyWeeklyBrief({
            status: "empty",
            week: response.week,
          })
        )
        return
      }

      patchWeeklyBrief(
        weeklyBriefErrorFrom(REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE, true)
      )
    } catch {
      if (generation !== state.weeklyBriefGeneration) {
        return
      }
      patchWeeklyBrief(
        weeklyBriefErrorFrom(REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE, true)
      )
    }
  }

  /**
   * Retry / generate path: GET; if still missing, POST then map ready.
   */
  const runWeeklyBriefGetThenGenerateIfMissing = async (options?: {
    showLoadingImmediately?: boolean
    markGenerateBusy?: boolean
  }) => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      return false
    }

    const generation = state.weeklyBriefGeneration + 1
    state = { ...state, weeklyBriefGeneration: generation }

    if (options?.showLoadingImmediately === true) {
      patchWeeklyBrief(
        emptyWeeklyBrief({
          status: "loading",
          generateBusy: options.markGenerateBusy === true,
        })
      )
    } else if (options?.markGenerateBusy === true) {
      patchWeeklyBrief({
        ...state.weeklyBrief,
        generateBusy: true,
      })
    }

    try {
      const first = await adapters.getWeeklyBrief(locationId)
      if (generation !== state.weeklyBriefGeneration) {
        return false
      }

      if (first.success && first.ready) {
        patchWeeklyBrief(mapReadyWeeklyBrief(first))
        return true
      }

      if (options?.markGenerateBusy !== true) {
        patchWeeklyBrief(
          emptyWeeklyBrief({
            status: "loading",
          })
        )
      }

      const generated = await adapters.generateWeeklyBrief(locationId)
      if (generation !== state.weeklyBriefGeneration) {
        return false
      }

      if (!generated.success) {
        patchWeeklyBrief(
          weeklyBriefErrorFrom(
            generated.message,
            generated.retryable !== false
          )
        )
        return false
      }

      patchWeeklyBrief(mapReadyWeeklyBrief(generated))
      return true
    } catch {
      if (generation !== state.weeklyBriefGeneration) {
        return false
      }
      patchWeeklyBrief(
        weeklyBriefErrorFrom(REPORTS_WEEKLY_BRIEF_LOAD_ERROR_MESSAGE, true)
      )
      return false
    }
  }

  const loadHubAndBrief = async () => {
    await Promise.all([loadHub(), loadWeeklyBriefGetOnly()])
  }

  const loadCapture = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      state = {
        ...state,
        captureLoadStatus: "idle",
        captureReport: null,
        captureLoadError: null,
      }
      publish()
      return
    }

    const generation = state.captureLoadGeneration + 1
    state = {
      ...state,
      captureLoadStatus: "loading",
      captureLoadGeneration: generation,
      captureLoadError: null,
    }
    publish()

    try {
      const window = resolveHomePerformanceWindow(
        adapters.getReportsDateRange()
      )
      const response = await adapters.getCapture({
        locationId,
        from: window.from.toISOString(),
        to: window.to.toISOString(),
      })

      if (generation !== state.captureLoadGeneration) {
        return
      }

      if (!response.success) {
        state = {
          ...state,
          captureLoadStatus: "error",
          captureReport: null,
          captureLoadError:
            response.message?.trim() || REPORTS_CAPTURE_LOAD_ERROR_MESSAGE,
        }
        publish()
        return
      }

      if (response.lifetimeEmpty) {
        state = {
          ...state,
          captureLoadStatus: "lifetimeEmpty",
          captureReport: null,
          captureLoadError: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        captureLoadStatus: "ready",
        captureReport: buildReportsCaptureViewModel(response),
        captureLoadError: null,
      }
      publish()
    } catch {
      if (generation !== state.captureLoadGeneration) {
        return
      }
      state = {
        ...state,
        captureLoadStatus: "error",
        captureReport: null,
        captureLoadError: REPORTS_CAPTURE_LOAD_ERROR_MESSAGE,
      }
      publish()
    }
  }

  const loadFeedback = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      state = {
        ...state,
        feedbackLoadStatus: "idle",
        feedbackReport: null,
        feedbackLoadError: null,
      }
      publish()
      return
    }

    const generation = state.feedbackLoadGeneration + 1
    state = {
      ...state,
      feedbackLoadStatus: "loading",
      feedbackLoadGeneration: generation,
      feedbackLoadError: null,
    }
    publish()

    try {
      const window = resolveHomePerformanceWindow(
        adapters.getReportsDateRange()
      )
      const response = await adapters.getFeedback({
        locationId,
        from: window.from.toISOString(),
        to: window.to.toISOString(),
      })

      if (generation !== state.feedbackLoadGeneration) {
        return
      }

      if (!response.success) {
        state = {
          ...state,
          feedbackLoadStatus: "error",
          feedbackReport: null,
          feedbackLoadError:
            response.message?.trim() || FEEDBACK_REPORT_PAGE_COPY.loadError,
        }
        publish()
        return
      }

      if (response.lifetimeEmpty) {
        state = {
          ...state,
          feedbackLoadStatus: "lifetimeEmpty",
          feedbackReport: null,
          feedbackLoadError: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        feedbackLoadStatus: "ready",
        feedbackReport: buildFeedbackReportViewModel(response),
        feedbackLoadError: null,
      }
      publish()
    } catch {
      if (generation !== state.feedbackLoadGeneration) {
        return
      }
      state = {
        ...state,
        feedbackLoadStatus: "error",
        feedbackReport: null,
        feedbackLoadError: FEEDBACK_REPORT_PAGE_COPY.loadError,
      }
      publish()
    }
  }

  const loadOffers = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      state = {
        ...state,
        offersLoadStatus: "idle",
        offersReport: null,
        offersLoadError: null,
      }
      publish()
      return
    }

    const generation = state.offersLoadGeneration + 1
    state = {
      ...state,
      offersLoadStatus: "loading",
      offersLoadGeneration: generation,
      offersLoadError: null,
    }
    publish()

    try {
      const window = resolveHomePerformanceWindow(
        adapters.getReportsDateRange()
      )
      const response = await adapters.getOffers({
        locationId,
        from: window.from.toISOString(),
        to: window.to.toISOString(),
      })

      if (generation !== state.offersLoadGeneration) {
        return
      }

      if (!response.success) {
        state = {
          ...state,
          offersLoadStatus: "error",
          offersReport: null,
          offersLoadError:
            response.message?.trim() || OFFERS_REPORT_PAGE_COPY.loadError,
        }
        publish()
        return
      }

      if (response.lifetimeEmpty) {
        state = {
          ...state,
          offersLoadStatus: "lifetimeEmpty",
          offersReport: null,
          offersLoadError: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        offersLoadStatus: "ready",
        offersReport: buildOffersReportViewModel(response),
        offersLoadError: null,
      }
      publish()
    } catch {
      if (generation !== state.offersLoadGeneration) {
        return
      }
      state = {
        ...state,
        offersLoadStatus: "error",
        offersReport: null,
        offersLoadError: OFFERS_REPORT_PAGE_COPY.loadError,
      }
      publish()
    }
  }

  const loadCampaigns = async () => {
    const workspace = state.workspace
    const locationId = workspace?.selectedLocationId
    if (workspace == null || locationId == null) {
      state = {
        ...state,
        campaignsLoadStatus: "idle",
        campaignsReport: null,
        campaignsLoadError: null,
      }
      publish()
      return
    }

    const generation = state.campaignsLoadGeneration + 1
    state = {
      ...state,
      campaignsLoadStatus: "loading",
      campaignsLoadGeneration: generation,
      campaignsLoadError: null,
    }
    publish()

    try {
      const window = resolveHomePerformanceWindow(
        adapters.getReportsDateRange()
      )
      const response = await adapters.getCampaigns({
        locationId,
        from: window.from.toISOString(),
        to: window.to.toISOString(),
      })

      if (generation !== state.campaignsLoadGeneration) {
        return
      }

      if (!response.success) {
        state = {
          ...state,
          campaignsLoadStatus: "error",
          campaignsReport: null,
          campaignsLoadError:
            response.message?.trim() || CAMPAIGNS_REPORT_PAGE_COPY.loadError,
        }
        publish()
        return
      }

      if (response.lifetimeEmpty) {
        state = {
          ...state,
          campaignsLoadStatus: "lifetimeEmpty",
          campaignsReport: null,
          campaignsLoadError: null,
        }
        publish()
        return
      }

      state = {
        ...state,
        campaignsLoadStatus: "ready",
        campaignsReport: buildCampaignsReportViewModel(response),
        campaignsLoadError: null,
      }
      publish()
    } catch {
      if (generation !== state.campaignsLoadGeneration) {
        return
      }
      state = {
        ...state,
        campaignsLoadStatus: "error",
        campaignsReport: null,
        campaignsLoadError: CAMPAIGNS_REPORT_PAGE_COPY.loadError,
      }
      publish()
    }
  }

  const loadForActiveSurface = async () => {
    if (state.activeSurface === "hub") {
      await loadHubAndBrief()
      return
    }
    if (state.activeSurface === "weekly-brief") {
      await loadWeeklyBriefGetOnly()
      return
    }
    if (state.activeSurface === "capture") {
      await loadCapture()
      return
    }
    if (state.activeSurface === "feedback") {
      await loadFeedback()
      return
    }
    if (state.activeSurface === "offers") {
      await loadOffers()
      return
    }
    if (state.activeSurface === "campaigns") {
      await loadCampaigns()
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
          ? {
              exportDialogOpen: false,
              weeklyBrief: emptyWeeklyBrief(),
              weeklyBriefGeneration: state.weeklyBriefGeneration + 1,
            }
          : {}),
      }
      publish()

      await loadForActiveSurface()
    },
    setActiveSurface(surface) {
      const changed = state.activeSurface !== surface
      state = { ...state, activeSurface: surface }
      publish()
      if (!changed) {
        return
      }
      if (surface === "hub") {
        void loadHubAndBrief()
        return
      }
      if (surface === "weekly-brief") {
        void loadWeeklyBriefGetOnly()
        return
      }
      if (surface === "capture") {
        void loadCapture()
        return
      }
      if (surface === "feedback") {
        void loadFeedback()
        return
      }
      if (surface === "offers") {
        void loadOffers()
        return
      }
      if (surface === "campaigns") {
        void loadCampaigns()
      }
    },
    async reloadForReportsDateRange() {
      publish()
      if (state.activeSurface === "hub") {
        await loadHub()
        return
      }
      if (state.activeSurface === "capture") {
        await loadCapture()
        return
      }
      if (state.activeSurface === "feedback") {
        await loadFeedback()
        return
      }
      if (state.activeSurface === "offers") {
        await loadOffers()
        return
      }
      if (state.activeSurface === "campaigns") {
        await loadCampaigns()
      }
    },
    async retryHubLoad() {
      await loadHub()
    },
    async retryWeeklyBrief() {
      await runWeeklyBriefGetThenGenerateIfMissing({
        showLoadingImmediately: true,
      })
    },
    async ensureWeeklyBriefReady() {
      if (state.weeklyBrief.status === "ready") {
        return true
      }
      return runWeeklyBriefGetThenGenerateIfMissing({
        showLoadingImmediately: true,
        markGenerateBusy: true,
      })
    },
    async generateWeeklyBriefInPlace() {
      await runWeeklyBriefGetThenGenerateIfMissing({
        showLoadingImmediately: true,
        markGenerateBusy: true,
      })
    },
    async markWeeklyBriefAsReviewed() {
      const workspace = state.workspace
      const locationId = workspace?.selectedLocationId
      if (
        workspace == null
        || locationId == null
        || state.weeklyBrief.status !== "ready"
      ) {
        return false
      }

      patchWeeklyBrief({
        ...state.weeklyBrief,
        markReviewedBusy: true,
      })

      try {
        const response = await adapters.markWeeklyBriefReviewed(
          locationId,
          state.weeklyBrief.week
        )
        if (response.success && response.ready) {
          patchWeeklyBrief(mapReadyWeeklyBrief(response))
          return true
        }
        patchWeeklyBrief({
          ...state.weeklyBrief,
          markReviewedBusy: false,
        })
        return false
      } catch {
        patchWeeklyBrief({
          ...state.weeklyBrief,
          markReviewedBusy: false,
        })
        return false
      }
    },
    async retryCaptureLoad() {
      await loadCapture()
    },
    async retryFeedbackLoad() {
      await loadFeedback()
    },
    async retryOffersLoad() {
      await loadOffers()
    },
    async retryCampaignsLoad() {
      await loadCampaigns()
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
