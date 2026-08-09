import {
  CAMPAIGNS_HELP_ARTICLE_SLUG,
  CAMPAIGNS_MESSAGING_USAGE_ANCHOR_ID,
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_PAGE_SIZE,
  CAMPAIGNS_SUMMARY_MOCK_KPIS,
  OPERATOR_CAMPAIGNS_DEFAULT_SORT_ID,
  OPERATOR_CAMPAIGNS_LIST_VIEW_LABELS,
  OPERATOR_CAMPAIGNS_LIST_VIEW_ORDER,
  OPERATOR_CAMPAIGNS_SORT_LABELS,
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
  campaignsFilterSheetSchema,
  campaignsFilterSheetSchemaForWorkspace,
} from "@/lib/operatorCampaigns/campaignsFilterSheetSchema"
import { buildCampaignsListQueryParams } from "@/lib/operatorCampaigns/campaignsListQueryParams"
import {
  campaignsListSearchMissLabel,
  resolveCampaignsListEmptyStateKind,
} from "@/lib/operatorCampaigns/resolveCampaignsListEmptyStateKind"
import { helpCentreArticleUrl } from "@/config/support"
import {
  chipCount,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
  type SchemaOption,
} from "@/lib/operatorFilterSheet"
import type {
  CampaignRecommendation,
  CampaignRecommendationRequest,
  CampaignRecommendationResponse,
  CampaignsCreatedByOption,
  CampaignsListQueryParams,
  CampaignsListResponse,
  OperatorCampaignsListEmptyStateKind,
  OperatorCampaignsListTab,
  OperatorCampaignsListViewId,
  OperatorCampaignsSortId,
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
  sortId: OperatorCampaignsSortId
  sortLabel: string
  filterChips: FilterChip[]
  filterChipCount: number
  currentPage: number
  pageSize: number
  totalCount: number
  pageRangeLabel: string
  canGoPrevious: boolean
  canGoNext: boolean
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
    messagingUsageAnchorId: string
    campaignHelpUrl: string
  }
  summary: OperatorCampaignsSummaryViewModel
  /** Messaging usage — fixtures pre-cutover; live Billing balances after (ticket 25). */
  messagingUsage: OperatorCampaignsMessagingUsageSection
  /** AI Recommended next step — Campaigns-only (ticket 31). */
  recommendation: OperatorCampaignsRecommendationViewModel
  list: OperatorCampaignsListViewModel
  filterSchemaLocations: SchemaOption[]
  filterSchemaCreatedBy: SchemaOption[]
  showLocationFilter: boolean
  filtersSession: FilterSheetSession | null
  filtersBusy: boolean
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
  setSortId: (id: OperatorCampaignsSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
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
  sortId: OperatorCampaignsSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  filtersBusy: boolean
  createdByCatalog: CampaignsCreatedByOption[]
  lastListResponse: CampaignsListResponse | null
  recommendation: OperatorCampaignsRecommendationViewModel
}

const CAMPAIGNS_FILTER_SCHEMA = campaignsFilterSheetSchema()

function emptyCampaignsFilters(): OperatorFilterSelection {
  return emptySelection(CAMPAIGNS_FILTER_SCHEMA)
}

function formatCampaignsPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount === 0) {
    return "Showing 0 of 0 campaigns"
  }
  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} campaigns`
}

function hasActiveFilters(filters: OperatorFilterSelection): boolean {
  return (
    JSON.stringify(filters) !== JSON.stringify(emptyCampaignsFilters())
  )
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
  sortId: OperatorCampaignsSortId
  page: number
  appliedFilters: OperatorFilterSelection
  createdByCatalog: CampaignsCreatedByOption[]
  workspace: OperatorCampaignsWorkspaceInput
  nowMs: number
}): OperatorCampaignsListViewModel {
  const hasActiveQuery =
    input.searchQuery.trim().length > 0
    || hasActiveFilters(input.appliedFilters)
  const allCount = input.response.tabCounts.all
  const emptyKind = resolveCampaignsListEmptyStateKind({
    allCount,
    filteredTotalCount: input.response.totalCount,
    hasActiveQuery,
  })
  const isTrueEmpty = emptyKind === "true-empty"
  const totalCount = input.response.totalCount
  const pageSize = input.response.pageSize || CAMPAIGNS_PAGE_SIZE
  const maxPage = Math.max(1, Math.ceil(totalCount / pageSize))
  const filterSchema = resolveFilterSchema({
    workspace: input.workspace,
    createdByCatalog: input.createdByCatalog,
  })
  const filterChips = projectChips(filterSchema, input.appliedFilters)

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
    sortId: input.sortId,
    sortLabel: OPERATOR_CAMPAIGNS_SORT_LABELS[input.sortId],
    filterChips,
    filterChipCount: chipCount(filterSchema, input.appliedFilters),
    currentPage: input.page,
    pageSize,
    totalCount,
    pageRangeLabel: formatCampaignsPageRangeLabel(
      input.page,
      pageSize,
      totalCount
    ),
    canGoPrevious: input.page > 1,
    canGoNext: input.page < maxPage && totalCount > 0,
  }
}

function resolveFilterSchema(input: {
  workspace: OperatorCampaignsWorkspaceInput
  createdByCatalog: CampaignsCreatedByOption[]
}) {
  const locations = input.workspace.locations.map((location) => ({
    id: String(location.id),
    label: location.locationName,
  }))
  const createdBy = input.createdByCatalog.map((option) => ({
    id: String(option.id),
    label: option.label,
  }))
  return campaignsFilterSheetSchemaForWorkspace({
    locations,
    createdBy,
    showLocationFilter: locations.length > 1,
  })
}

function assembleViewModel(
  workspace: OperatorCampaignsWorkspaceInput,
  listResponse: CampaignsListResponse,
  marketingEligible: number,
  overviewDateRange: CampaignsOverviewDateRange,
  activeViewId: OperatorCampaignsListViewId,
  searchQuery: string,
  sortId: OperatorCampaignsSortId,
  page: number,
  appliedFilters: OperatorFilterSelection,
  createdByCatalog: CampaignsCreatedByOption[],
  filtersSession: FilterSheetSession | null,
  filtersBusy: boolean,
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
    sortId,
    page,
    appliedFilters,
    createdByCatalog,
    workspace,
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
      messagingUsageAnchorId: CAMPAIGNS_MESSAGING_USAGE_ANCHOR_ID,
      campaignHelpUrl: helpCentreArticleUrl(CAMPAIGNS_HELP_ARTICLE_SLUG),
    },
    summary: {
      title: CAMPAIGNS_PAGE_COPY.summaryTitle,
      subtitle: CAMPAIGNS_PAGE_COPY.summarySubtitle,
      kpis: buildSummaryKpis(marketingEligible),
    },
    messagingUsage,
    recommendation,
    list,
    filterSchemaLocations: workspace.locations.map((location) => ({
      id: String(location.id),
      label: location.locationName,
    })),
    filterSchemaCreatedBy: createdByCatalog.map((option) => ({
      id: String(option.id),
      label: option.label,
    })),
    showLocationFilter: workspace.locations.length > 1,
    filtersSession,
    filtersBusy,
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
    sortId: OPERATOR_CAMPAIGNS_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptyCampaignsFilters(),
    filtersSession: null,
    filtersBusy: false,
    createdByCatalog: [],
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

  const buildListParams = (locationId: number): CampaignsListQueryParams =>
    buildCampaignsListQueryParams({
      locationId,
      view: state.activeViewId,
      q: state.searchQuery,
      sort: state.sortId,
      page: state.page,
      pageSize: CAMPAIGNS_PAGE_SIZE,
      filters: state.appliedFilters,
      now: getNow(),
    })

  const catalogFromResponse = (
    response: CampaignsListResponse
  ): CampaignsCreatedByOption[] => response.filterCatalog?.createdBy ?? []

  const assembleFromState = (
    workspace: OperatorCampaignsWorkspaceInput,
    listResponse: CampaignsListResponse,
    marketingEligible: number,
    overviewDateRange: CampaignsOverviewDateRange,
    messagingUsage: OperatorCampaignsMessagingUsageSection
  ) =>
    assembleViewModel(
      workspace,
      listResponse,
      marketingEligible,
      overviewDateRange,
      state.activeViewId,
      state.searchQuery,
      state.sortId,
      state.page,
      state.appliedFilters,
      catalogFromResponse(listResponse).length > 0
        ? catalogFromResponse(listResponse)
        : state.createdByCatalog,
      state.filtersSession,
      state.filtersBusy,
      getNow().getTime(),
      state.recommendation,
      messagingUsage
    )

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
        createdByCatalog: catalogFromResponse(listResponse),
        viewModel: assembleFromState(
          workspace,
          listResponse,
          marketingEligible,
          overviewDateRange,
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
        createdByCatalog: catalogFromResponse(listResponse),
        viewModel: assembleFromState(
          workspace,
          listResponse,
          marketingEligible,
          overviewDateRange,
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
          sortId: OPERATOR_CAMPAIGNS_DEFAULT_SORT_ID,
          page: 1,
          appliedFilters: emptyCampaignsFilters(),
          filtersSession: null,
          filtersBusy: false,
          createdByCatalog: [],
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
          sortId: OPERATOR_CAMPAIGNS_DEFAULT_SORT_ID,
          page: 1,
          appliedFilters: emptyCampaignsFilters(),
          filtersSession: null,
          createdByCatalog: [],
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
        page: 1,
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            list: {
              ...state.viewModel.list,
              activeViewId: viewId,
              currentPage: 1,
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
        page: 1,
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
              currentPage: 1,
            },
          },
        }
        publish()
      }
      scheduleListFetch()
    },
    setSortId: (id) => {
      if (state.sortId === id) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        sortId: id,
        page: 1,
      }
      void fetchList({ quiet: true })
    },
    goToPreviousPage: () => {
      if (state.page <= 1) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        page: state.page - 1,
      }
      void fetchList({ quiet: true })
    },
    goToNextPage: () => {
      const totalCount = state.lastListResponse?.totalCount ?? 0
      const maxPage = Math.max(1, Math.ceil(totalCount / CAMPAIGNS_PAGE_SIZE))
      if (state.page >= maxPage) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        page: state.page + 1,
      }
      void fetchList({ quiet: true })
    },
    openFilters: () => {
      state = {
        ...state,
        filtersBusy: false,
        filtersSession: openSession(state.appliedFilters),
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            filtersSession: state.filtersSession,
            filtersBusy: false,
          },
        }
      }
      publish()
    },
    closeFilters: () => {
      if (state.filtersSession == null) {
        return
      }
      state = {
        ...state,
        filtersSession: null,
        filtersBusy: false,
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            filtersSession: null,
            filtersBusy: false,
          },
        }
      }
      publish()
    },
    setFiltersSession: (session) => {
      state = {
        ...state,
        filtersSession: session,
      }
      if (state.viewModel != null) {
        state = {
          ...state,
          viewModel: {
            ...state.viewModel,
            filtersSession: session,
          },
        }
      }
      publish()
    },
    applyFilters: (filters) => {
      clearSearchDebounce()
      state = {
        ...state,
        appliedFilters: filters,
        filtersSession:
          state.filtersSession != null ? openSession(filters) : null,
        page: 1,
      }
      void fetchList({ quiet: true })
    },
    removeFilterChip: (chip) => {
      const workspace = state.workspace
      if (workspace == null) {
        return
      }
      clearSearchDebounce()
      const filterSchema = resolveFilterSchema({
        workspace,
        createdByCatalog: state.createdByCatalog,
      })
      state = {
        ...state,
        appliedFilters: removeAppliedChip(
          filterSchema,
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      void fetchList({ quiet: true })
    },
    clearSearchAndFilters: async () => {
      const filtersEmpty = !hasActiveFilters(state.appliedFilters)
      if (
        state.searchQuery === ""
        && state.page === 1
        && filtersEmpty
      ) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
        page: 1,
        appliedFilters: emptyCampaignsFilters(),
        filtersSession:
          state.filtersSession != null
            ? openSession(emptyCampaignsFilters())
            : null,
      }
      await fetchList()
    },
    viewAllCampaigns: async () => {
      clearSearchDebounce()
      state = {
        ...state,
        activeViewId: "all",
        searchQuery: "",
        page: 1,
        appliedFilters: emptyCampaignsFilters(),
      }
      await fetchList()
    },
  }
}
