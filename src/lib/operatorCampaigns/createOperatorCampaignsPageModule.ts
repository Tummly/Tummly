import {
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_PAGE_SIZE,
  CAMPAIGNS_SUMMARY_MOCK_KPIS,
  OPERATOR_CAMPAIGNS_LIST_VIEW_LABELS,
  OPERATOR_CAMPAIGNS_LIST_VIEW_ORDER,
  campaignsListEmptyCopy,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  mapCampaignListItemToTableRow,
  type OperatorCampaignsListTableRow,
} from "@/lib/operatorCampaigns/campaignListPresentation"
import {
  labelForCampaignsOverviewDateRange,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"
import { buildCampaignRecommendationRequest } from "@/lib/operatorCampaigns/buildCampaignRecommendationRequest"
import {
  CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
  resolveCampaignMessagingUsage,
  type CampaignBillingBalancesPayload,
} from "@/lib/operatorCampaigns/campaignMessagingBalances"
import {
  messagingUsageViewModelFromFixture,
  type OperatorCampaignsMessagingUsageViewModel,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"
import {
  campaignsListSearchMissLabel,
  resolveCampaignsListEmptyStateKind,
} from "@/lib/operatorCampaigns/resolveCampaignsListEmptyStateKind"
import type {
  CampaignRecommendation,
  CampaignRecommendationRequest,
  CampaignRecommendationResponse,
  CampaignsListQueryParams,
  CampaignsListResponse,
  OperatorCampaignsListEmptyStateKind,
  OperatorCampaignsListTab,
  OperatorCampaignsListViewId,
} from "@/types/operatorCampaigns"

export { CAMPAIGNS_PAGE_COPY }

export type { OperatorCampaignsListTableRow }

export const CAMPAIGNS_LOAD_ERROR_MESSAGE =
  "Could not load campaigns. Please try again."

export const CAMPAIGNS_RECOMMENDATION_LOAD_ERROR_MESSAGE =
  CAMPAIGNS_PAGE_COPY.recommendationFailCopy

const DEFAULT_SEARCH_DEBOUNCE_MS = 300

export type OperatorCampaignsWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorCampaignsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorCampaignsWorkspaceLocation[]
}

export type OperatorCampaignsSummaryKpiId =
  | "marketing-eligible"
  | "campaigns-in-flight"
  | "messages-sent"
  | "campaign-attributed-redemptions"

export type OperatorCampaignsSummaryKpi = {
  id: OperatorCampaignsSummaryKpiId
  label: string
  description: string
  value: number
}

export type OperatorCampaignsPageAdapters = {
  loadCampaignsList: (
    params: CampaignsListQueryParams
  ) => Promise<CampaignsListResponse>
  loadMarketingEligible: (input: {
    locationId: number
    overviewDateRange: CampaignsOverviewDateRange
  }) => Promise<number>
  loadCampaignRecommendation: (input: {
    request: CampaignRecommendationRequest
  }) => Promise<CampaignRecommendationResponse>
  getCampaignsOverviewDateRange: () => CampaignsOverviewDateRange
  /**
   * Billing balances (+ plan) for Messaging usage (ticket 25).
   * Omitted → fixtures until Billing cutover.
   */
  loadMessagingBalances?: () => Promise<CampaignBillingBalancesPayload>
  /** Test seam — defaults to a short debounce. */
  debounceMs?: number
  /** Test seam — relative Updated labels on list rows. */
  getNow?: () => Date
}

export type OperatorCampaignsRecommendationStatus =
  | "idle"
  | "loading"
  | "ready"
  | "error"
  | "dismissed"

export type OperatorCampaignsRecommendationViewModel = {
  status: OperatorCampaignsRecommendationStatus
  /** Present when status is ready and type is not none. */
  recommendation: CampaignRecommendation | null
  /** True when status is ready and type is none (or weak-signal none). */
  isNone: boolean
  errorMessage: string | null
  errorRetryable: boolean
  showAudiencePanel: boolean
}

export type OperatorCampaignsListEmptyViewModel = {
  kind: OperatorCampaignsListEmptyStateKind
  title: string
  helper: string
  createCampaignLabel?: string
  useTemplateLabel?: string
  viewAllCampaignsLabel?: string
  clearAllFiltersLabel?: string
}

export type OperatorCampaignsListViewModel = {
  tabs: OperatorCampaignsListTab[]
  activeViewId: OperatorCampaignsListViewId
  searchQuery: string
  searchMissLabel: string | null
  /** True when All = 0 and there is no active query — hide tabs/toolbar. */
  showListChrome: boolean
  /** Figma table rows — empty when view has no matching campaigns. */
  rows: OperatorCampaignsListTableRow[]
  empty: OperatorCampaignsListEmptyViewModel | null
}

export type OperatorCampaignsSummaryViewModel = {
  title: string
  subtitle: string
  kpis: OperatorCampaignsSummaryKpi[]
}

export type OperatorCampaignsMessagingUsageSection = {
  status: "ready" | "load-failed"
  viewModel: OperatorCampaignsMessagingUsageViewModel | null
  errorMessage: string | null
}

export type OperatorCampaignsPageViewModel = {
  locationId: number
  locationName: string
  /** True when All campaigns count is 0 and no search/filters — Figma true-empty. */
  isTrueEmpty: boolean
  dateRangeLabel: string
  selectedDateRange: CampaignsOverviewDateRange
  header: {
    createCampaignLabel: string
    useTemplateLabel: string
  }
  summary: OperatorCampaignsSummaryViewModel
  /** Messaging usage — fixtures pre-cutover; live Billing balances after (ticket 25). */
  messagingUsage: OperatorCampaignsMessagingUsageSection
  /** AI Recommended next step — Campaigns-only (ticket 31). */
  recommendation: OperatorCampaignsRecommendationViewModel
  list: OperatorCampaignsListViewModel
}

export type OperatorCampaignsPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorCampaignsPageViewModel | null
  loadError: string | null
}

export type OperatorCampaignsPageModule = {
  getSnapshot: () => OperatorCampaignsPageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorCampaignsWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  /** Refetch Marketing eligible and recommendation after the visit store date window changes. */
  reloadForOverviewDateRange: () => Promise<void>
  /** Explicit recommendation retry / refresh (bypasses server cache). */
  retryRecommendation: () => Promise<void>
  /** Retry Messaging usage after live balances load-failed (ticket 25). */
  retryMessagingUsage: () => Promise<void>
  /** Session hide only — does not write server dismiss/cache. */
  dismissRecommendation: () => void
  openRecommendationAudience: () => void
  closeRecommendationAudience: () => void
  setListView: (viewId: OperatorCampaignsListViewId) => Promise<void>
  setSearchQuery: (query: string) => void
  clearSearchAndFilters: () => Promise<void>
  viewAllCampaigns: () => Promise<void>
}

type CampaignsState = {
  loadStatus: OperatorCampaignsPageSnapshot["loadStatus"]
  workspace: OperatorCampaignsWorkspaceInput | null
  viewModel: OperatorCampaignsPageViewModel | null
  loadError: string | null
  loadGeneration: number
  listLoadGeneration: number
  marketingEligibleGeneration: number
  recommendationGeneration: number
  activeViewId: OperatorCampaignsListViewId
  searchQuery: string
  lastListResponse: CampaignsListResponse | null
  recommendation: OperatorCampaignsRecommendationViewModel
}

function idleRecommendation(): OperatorCampaignsRecommendationViewModel {
  return {
    status: "idle",
    recommendation: null,
    isNone: false,
    errorMessage: null,
    errorRetryable: false,
    showAudiencePanel: false,
  }
}

function mapRecommendationResponse(
  response: CampaignRecommendationResponse
): OperatorCampaignsRecommendationViewModel {
  if (!response.success || response.recommendation == null) {
    return {
      status: "error",
      recommendation: null,
      isNone: false,
      errorMessage:
        response.message ?? CAMPAIGNS_RECOMMENDATION_LOAD_ERROR_MESSAGE,
      errorRetryable: response.retryable !== false,
      showAudiencePanel: false,
    }
  }

  const isNone = response.recommendation.type === "none"
  return {
    status: "ready",
    recommendation: isNone ? null : response.recommendation,
    isNone,
    errorMessage: null,
    errorRetryable: false,
    showAudiencePanel: false,
  }
}

function buildSummaryKpis(
  marketingEligible: number
): OperatorCampaignsSummaryKpi[] {
  return [
    {
      id: "marketing-eligible",
      label: CAMPAIGNS_PAGE_COPY.marketingEligibleLabel,
      description: CAMPAIGNS_PAGE_COPY.marketingEligibleDescription,
      value: marketingEligible,
    },
    ...CAMPAIGNS_SUMMARY_MOCK_KPIS,
  ]
}

function mapTabs(
  tabCounts: CampaignsListResponse["tabCounts"]
): OperatorCampaignsListTab[] {
  const counts: Record<OperatorCampaignsListViewId, number> = {
    all: tabCounts.all,
    "needs-attention": tabCounts.needsAttention,
    drafts: tabCounts.drafts,
    "in-flight": tabCounts.inFlight,
    sent: tabCounts.sent,
  }

  return OPERATOR_CAMPAIGNS_LIST_VIEW_ORDER.map((id) => ({
    id,
    label: OPERATOR_CAMPAIGNS_LIST_VIEW_LABELS[id],
    count: counts[id],
    showCount: id !== "all",
  }))
}

function buildListEmpty(
  kind: OperatorCampaignsListEmptyStateKind,
  activeViewId: OperatorCampaignsListViewId
): OperatorCampaignsListEmptyViewModel {
  const copy = campaignsListEmptyCopy({ kind, activeViewId })
  if (kind === "true-empty") {
    return {
      kind,
      title: copy.title,
      helper: copy.helper,
      createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
      useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
    }
  }

  if (kind === "filter-search") {
    return {
      kind,
      title: copy.title,
      helper: copy.helper,
      viewAllCampaignsLabel: CAMPAIGNS_PAGE_COPY.viewAllCampaigns,
      clearAllFiltersLabel: CAMPAIGNS_PAGE_COPY.clearAllFilters,
    }
  }

  return {
    kind,
    title: copy.title,
    helper: copy.helper,
  }
}

function buildListViewModel(input: {
  response: CampaignsListResponse
  activeViewId: OperatorCampaignsListViewId
  searchQuery: string
  nowMs: number
}): OperatorCampaignsListViewModel {
  const hasActiveQuery = input.searchQuery.trim().length > 0
  const allCount = input.response.tabCounts.all
  const emptyKind = resolveCampaignsListEmptyStateKind({
    allCount,
    filteredTotalCount: input.response.totalCount,
    hasActiveQuery,
  })
  const isTrueEmpty = emptyKind === "true-empty"

  return {
    tabs: mapTabs(input.response.tabCounts),
    activeViewId: input.activeViewId,
    searchQuery: input.searchQuery,
    searchMissLabel: campaignsListSearchMissLabel(input.searchQuery),
    showListChrome: !isTrueEmpty,
    rows: input.response.items.map((item) =>
      mapCampaignListItemToTableRow(item, input.nowMs)
    ),
    empty:
      emptyKind == null
        ? null
        : buildListEmpty(emptyKind, input.activeViewId),
  }
}

function assembleViewModel(
  workspace: OperatorCampaignsWorkspaceInput,
  listResponse: CampaignsListResponse,
  marketingEligible: number,
  overviewDateRange: CampaignsOverviewDateRange,
  activeViewId: OperatorCampaignsListViewId,
  searchQuery: string,
  nowMs: number,
  recommendation: OperatorCampaignsRecommendationViewModel,
  messagingUsage: OperatorCampaignsMessagingUsageSection
): OperatorCampaignsPageViewModel | null {
  const locationId = workspace.selectedLocationId
  if (locationId == null) {
    return null
  }

  const locationName =
    workspace.locations.find((location) => location.id === locationId)
      ?.locationName ?? ""
  const list = buildListViewModel({
    response: listResponse,
    activeViewId,
    searchQuery,
    nowMs,
  })

  return {
    locationId,
    locationName,
    isTrueEmpty: list.empty?.kind === "true-empty",
    dateRangeLabel: labelForCampaignsOverviewDateRange(overviewDateRange),
    selectedDateRange: overviewDateRange,
    header: {
      createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
      useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
    },
    summary: {
      title: CAMPAIGNS_PAGE_COPY.summaryTitle,
      subtitle: CAMPAIGNS_PAGE_COPY.summarySubtitle,
      kpis: buildSummaryKpis(marketingEligible),
    },
    messagingUsage,
    recommendation,
    list,
  }
}

async function resolveMessagingUsageSection(
  loadMessagingBalances:
    | (() => Promise<CampaignBillingBalancesPayload>)
    | undefined
): Promise<OperatorCampaignsMessagingUsageSection> {
  if (loadMessagingBalances == null) {
    const resolved = resolveCampaignMessagingUsage({ cutover: "fixtures" })
    if (resolved.status !== "ready") {
      return {
        status: "load-failed",
        viewModel: null,
        errorMessage: CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
      }
    }
    return {
      status: "ready",
      viewModel: resolved.viewModel,
      errorMessage: null,
    }
  }

  try {
    const balances = await loadMessagingBalances()
    const resolved = resolveCampaignMessagingUsage({
      cutover: "live",
      balances,
    })
    if (resolved.status !== "ready") {
      return {
        status: "load-failed",
        viewModel: null,
        errorMessage:
          resolved.status === "load-failed"
            ? resolved.errorMessage
            : CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
      }
    }
    return {
      status: "ready",
      viewModel: resolved.viewModel,
      errorMessage: null,
    }
  } catch {
    const resolved = resolveCampaignMessagingUsage({
      cutover: "live",
      failed: true,
    })
    return {
      status: "load-failed",
      viewModel: null,
      errorMessage:
        resolved.status === "load-failed"
          ? resolved.errorMessage
          : CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
    }
  }
}

function withRecommendation(
  viewModel: OperatorCampaignsPageViewModel | null,
  recommendation: OperatorCampaignsRecommendationViewModel
): OperatorCampaignsPageViewModel | null {
  if (viewModel == null) {
    return null
  }
  return { ...viewModel, recommendation }
}

export function createOperatorCampaignsPageModule(
  adapters: OperatorCampaignsPageAdapters
): OperatorCampaignsPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS
  const getNow = adapters.getNow ?? (() => new Date())

  let state: CampaignsState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
    loadGeneration: 0,
    listLoadGeneration: 0,
    marketingEligibleGeneration: 0,
    recommendationGeneration: 0,
    activeViewId: "all",
    searchQuery: "",
    lastListResponse: null,
    recommendation: idleRecommendation(),
  }

  let snapshot: OperatorCampaignsPageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    loadError: state.loadError,
  }

  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null
  /** Session-only Not now — survives recommendation reloads until location change. */
  let recommendationDismissedForSession = false

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      loadError: state.loadError,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const buildListParams = (locationId: number): CampaignsListQueryParams => ({
    locationId,
    view: state.activeViewId,
    q: state.searchQuery.trim() || undefined,
    page: 1,
    pageSize: CAMPAIGNS_PAGE_SIZE,
  })

  const patchRecommendation = (
    recommendation: OperatorCampaignsRecommendationViewModel
  ) => {
    state = {
      ...state,
      recommendation,
      viewModel: withRecommendation(state.viewModel, recommendation),
    }
    publish()
  }

  const loadRecommendation = async (options?: { refresh?: boolean }) => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }
    if (recommendationDismissedForSession) {
      return
    }

    const generation = state.recommendationGeneration + 1
    const loadingRecommendation: OperatorCampaignsRecommendationViewModel = {
      ...idleRecommendation(),
      status: "loading",
    }
    state = {
      ...state,
      recommendationGeneration: generation,
      recommendation: loadingRecommendation,
      viewModel: withRecommendation(state.viewModel, loadingRecommendation),
    }
    publish()

    const overviewDateRange = adapters.getCampaignsOverviewDateRange()
    const request = buildCampaignRecommendationRequest({
      locationId: selectedLocationId,
      overviewDateRange,
      refresh: options?.refresh === true,
      now: getNow(),
    })

    try {
      const response = await adapters.loadCampaignRecommendation({ request })
      if (generation !== state.recommendationGeneration) {
        return
      }
      patchRecommendation(mapRecommendationResponse(response))
    } catch {
      if (generation !== state.recommendationGeneration) {
        return
      }
      patchRecommendation({
        status: "error",
        recommendation: null,
        isNone: false,
        errorMessage: CAMPAIGNS_RECOMMENDATION_LOAD_ERROR_MESSAGE,
        errorRetryable: true,
        showAudiencePanel: false,
      })
    }
  }

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    clearSearchDebounce()
    const generation = state.loadGeneration + 1
    const listLoadGeneration = state.listLoadGeneration + 1
    const marketingEligibleGeneration = state.marketingEligibleGeneration + 1
    const recommendationGeneration = state.recommendationGeneration + 1
    const loadingRecommendation: OperatorCampaignsRecommendationViewModel =
      recommendationDismissedForSession
        ? { ...idleRecommendation(), status: "dismissed" }
        : { ...idleRecommendation(), status: "loading" }
    state = {
      ...state,
      loadStatus: "loading",
      loadError: null,
      loadGeneration: generation,
      listLoadGeneration,
      marketingEligibleGeneration,
      recommendationGeneration,
      recommendation: loadingRecommendation,
    }
    publish()

    const overviewDateRange = adapters.getCampaignsOverviewDateRange()

    try {
      const [listResponse, marketingEligible, messagingUsage] =
        await Promise.all([
          adapters.loadCampaignsList(buildListParams(selectedLocationId)),
          adapters.loadMarketingEligible({
            locationId: selectedLocationId,
            overviewDateRange,
          }),
          resolveMessagingUsageSection(adapters.loadMessagingBalances),
        ])
      if (
        generation !== state.loadGeneration
        || listLoadGeneration !== state.listLoadGeneration
        || marketingEligibleGeneration !== state.marketingEligibleGeneration
      ) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        lastListResponse: listResponse,
        viewModel: assembleViewModel(
          workspace,
          listResponse,
          marketingEligible,
          overviewDateRange,
          state.activeViewId,
          state.searchQuery,
          getNow().getTime(),
          state.recommendation,
          messagingUsage
        ),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        loadError: CAMPAIGNS_LOAD_ERROR_MESSAGE,
        viewModel: null,
        lastListResponse: null,
        recommendation: idleRecommendation(),
      }
      publish()
      return
    }

    if (recommendationGeneration !== state.recommendationGeneration) {
      return
    }
    if (recommendationDismissedForSession) {
      return
    }

    const request = buildCampaignRecommendationRequest({
      locationId: selectedLocationId,
      overviewDateRange,
      now: getNow(),
    })

    try {
      const response = await adapters.loadCampaignRecommendation({ request })
      if (recommendationGeneration !== state.recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation(mapRecommendationResponse(response))
    } catch {
      if (recommendationGeneration !== state.recommendationGeneration) {
        return
      }
      if (recommendationDismissedForSession) {
        return
      }
      patchRecommendation({
        status: "error",
        recommendation: null,
        isNone: false,
        errorMessage: CAMPAIGNS_RECOMMENDATION_LOAD_ERROR_MESSAGE,
        errorRetryable: true,
        showAudiencePanel: false,
      })
    }
  }

  const fetchList = async (options?: { quiet?: boolean }) => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.listLoadGeneration + 1
    state = {
      ...state,
      listLoadGeneration: generation,
      loadStatus:
        options?.quiet === true && state.viewModel != null
          ? state.loadStatus
          : "loading",
    }
    if (options?.quiet !== true) {
      publish()
    }

    try {
      const listResponse = await adapters.loadCampaignsList(
        buildListParams(selectedLocationId)
      )
      if (generation !== state.listLoadGeneration) {
        return
      }

      const marketingEligible =
        state.viewModel?.summary.kpis.find(
          (kpi) => kpi.id === "marketing-eligible"
        )?.value ?? 0
      const overviewDateRange = adapters.getCampaignsOverviewDateRange()
      const messagingUsage =
        state.viewModel?.messagingUsage
        ?? {
          status: "ready" as const,
          viewModel: messagingUsageViewModelFromFixture(),
          errorMessage: null,
        }

      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        lastListResponse: listResponse,
        viewModel: assembleViewModel(
          workspace,
          listResponse,
          marketingEligible,
          overviewDateRange,
          state.activeViewId,
          state.searchQuery,
          getNow().getTime(),
          state.recommendation,
          messagingUsage
        ),
      }
      publish()
    } catch {
      if (generation !== state.listLoadGeneration) {
        return
      }
      if (options?.quiet !== true) {
        state = {
          ...state,
          loadStatus: "error",
          loadError: CAMPAIGNS_LOAD_ERROR_MESSAGE,
        }
        publish()
      }
    }
  }

  const scheduleListFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchList({ quiet: true })
    }, debounceMs)
  }

  const reloadMarketingEligibleOnly = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    const currentViewModel = state.viewModel
    if (
      workspace == null
      || selectedLocationId == null
      || currentViewModel == null
    ) {
      return
    }

    const marketingEligibleGeneration = state.marketingEligibleGeneration + 1
    state = {
      ...state,
      marketingEligibleGeneration,
    }

    const overviewDateRange = adapters.getCampaignsOverviewDateRange()

    try {
      const marketingEligible = await adapters.loadMarketingEligible({
        locationId: selectedLocationId,
        overviewDateRange,
      })
      if (marketingEligibleGeneration !== state.marketingEligibleGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        viewModel: {
          ...currentViewModel,
          dateRangeLabel: labelForCampaignsOverviewDateRange(overviewDateRange),
          selectedDateRange: overviewDateRange,
          summary: {
            ...currentViewModel.summary,
            kpis: buildSummaryKpis(marketingEligible),
          },
          recommendation: state.recommendation,
        },
      }
      publish()
    } catch {
      // Keep prior Marketing eligible KPI; date chrome reads the visit store.
    }

    // Window change invalidates recommendation cache key — reload without refresh.
    await loadRecommendation()
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
        clearSearchDebounce()
        recommendationDismissedForSession = false
        state = {
          loadStatus: "idle",
          workspace: null,
          viewModel: null,
          loadError: null,
          loadGeneration: state.loadGeneration + 1,
          listLoadGeneration: state.listLoadGeneration + 1,
          marketingEligibleGeneration: state.marketingEligibleGeneration + 1,
          recommendationGeneration: state.recommendationGeneration + 1,
          activeViewId: "all",
          searchQuery: "",
          lastListResponse: null,
          recommendation: idleRecommendation(),
        }
        publish()
        return
      }

      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId

      state = {
        ...state,
        workspace: input,
      }

      if (locationChanged || state.viewModel == null) {
        recommendationDismissedForSession = false
        state = {
          ...state,
          activeViewId: "all",
          searchQuery: "",
        }
        await loadForSelectedLocation()
        return
      }

      // Same Owned location: refresh shell-facing location name only.
      if (state.viewModel != null) {
        const locationName =
          input.locations.find(
            (location) => location.id === input.selectedLocationId
          )?.locationName ?? state.viewModel.locationName
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            locationName,
          },
        }
        publish()
      }
    },
    retryLoad: () => loadForSelectedLocation(),
    reloadForOverviewDateRange: () => reloadMarketingEligibleOnly(),
    retryRecommendation: () => loadRecommendation({ refresh: true }),
    retryMessagingUsage: async () => {
      const workspace = state.workspace
      const currentViewModel = state.viewModel
      if (workspace == null || currentViewModel == null) {
        return
      }
      const messagingUsage = await resolveMessagingUsageSection(
        adapters.loadMessagingBalances
      )
      state = {
        ...state,
        viewModel: {
          ...currentViewModel,
          messagingUsage,
        },
      }
      publish()
    },
    dismissRecommendation: () => {
      recommendationDismissedForSession = true
      patchRecommendation({
        ...idleRecommendation(),
        status: "dismissed",
      })
    },
    openRecommendationAudience: () => {
      if (state.recommendation.status !== "ready"
        || state.recommendation.recommendation == null) {
        return
      }
      patchRecommendation({
        ...state.recommendation,
        showAudiencePanel: true,
      })
    },
    closeRecommendationAudience: () => {
      if (!state.recommendation.showAudiencePanel) {
        return
      }
      patchRecommendation({
        ...state.recommendation,
        showAudiencePanel: false,
      })
    },
    setListView: async (viewId) => {
      if (state.activeViewId === viewId) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        activeViewId: viewId,
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            list: {
              ...state.viewModel.list,
              activeViewId: viewId,
            },
          },
        }
        publish()
      }
      await fetchList()
    },
    setSearchQuery: (query) => {
      state = {
        ...state,
        searchQuery: query,
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            list: {
              ...state.viewModel.list,
              searchQuery: query,
              searchMissLabel: campaignsListSearchMissLabel(query),
            },
          },
        }
        publish()
      }
      scheduleListFetch()
    },
    clearSearchAndFilters: async () => {
      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
      }
      await fetchList()
    },
    viewAllCampaigns: async () => {
      clearSearchDebounce()
      state = {
        ...state,
        activeViewId: "all",
        searchQuery: "",
      }
      await fetchList()
    },
  }
}
