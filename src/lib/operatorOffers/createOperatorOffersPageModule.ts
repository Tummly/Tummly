import { listCatalogOffers as listCatalogOffersApi } from "@/api/dashboardApi"
import { CREATE_EDIT_OFFER_DRAWER_COPY } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import type { CreateEditOfferDrawerMode } from "@/lib/operatorOffers/createEditOfferDrawerPresentation"
import {
  buildOffersPerformanceKpis,
  type OperatorOffersKpi,
} from "@/lib/operatorOffers/buildOffersPerformanceKpis"
import {
  createOfferTemplatePickerModule,
  type OfferTemplatePickerSnapshot,
} from "@/lib/operatorOffers/createOfferTemplatePickerModule"
import { mapOfferTemplateToCreateDraft } from "@/lib/operatorOffers/mapOfferTemplateToCreateDraft"
import {
  canConfirmCampaignCatalogOfferDetails,
  catalogOfferDetailToDraft,
  emptyCampaignCatalogOfferDetailsDraft,
  isDirtyBenefitOrValidity,
  shouldConfirmEditOfferSave,
  toCreateCatalogOfferRequestBody,
  type CampaignCatalogOfferDetailsDraft,
  type CreateCatalogOfferRequestBody,
} from "@/lib/operatorOffers/offerCatalogPresentation"
import { loadOfferTemplateSeed, getOfferTemplateById } from "@/lib/operatorOffers/offerTemplateSeed"
import {
  mapCatalogOfferListItemToTableRow,
  type OfferRowActionId,
  type OperatorOffersListTableRow,
} from "@/lib/operatorOffers/offerListPresentation"
import { offersFilterSheetSchema } from "@/lib/operatorOffers/offersFilterSheetSchema"
import { buildOffersListQueryParams } from "@/lib/operatorOffers/offersListQueryParams"
import {
  OFFERS_PAGE_COPY,
  OFFERS_PAGE_SIZE,
  OPERATOR_OFFERS_DEFAULT_SORT_ID,
  OPERATOR_OFFERS_LIST_VIEW_LABELS,
  OPERATOR_OFFERS_LIST_VIEW_ORDER,
  OPERATOR_OFFERS_SORT_LABELS,
  offersListEmptyCopy,
} from "@/lib/operatorOffers/offersPresentation"
import {
  offersListSearchMissLabel,
  resolveOffersListEmptyStateKind,
} from "@/lib/operatorOffers/resolveOffersListEmptyStateKind"
import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import { NEEDS_ATTENTION_EMPTY_COPY } from "@/lib/operatorHome/operatorHomeSectionPresentation"
import {
  chipCount,
  emptySelection,
  openSession,
  projectChips,
  removeAppliedChip,
  type FilterChip,
  type FilterSheetSession,
  type OperatorFilterSelection,
} from "@/lib/operatorFilterSheet"
import type {
  CatalogOfferDetail,
  CatalogOffersListQueryParams,
  CatalogOffersListResponse,
  OperatorOffersListEmptyStateKind,
  OperatorOffersListTab,
  OperatorOffersListViewId,
  OperatorOffersSortId,
} from "@/types/operatorCampaigns"

export const OFFERS_LOAD_ERROR_MESSAGE = OFFERS_PAGE_COPY.loadError

const DEFAULT_SEARCH_DEBOUNCE_MS = 300
const OFFERS_FILTER_SCHEMA = offersFilterSheetSchema()

export type OperatorOffersWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorOffersWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorOffersWorkspaceLocation[]
}

export type OperatorOffersListEmptyViewModel = {
  kind: OperatorOffersListEmptyStateKind
  title: string
  helper: string
  createOfferLabel?: string
  useTemplateLabel?: string
  viewAllOffersLabel?: string
  clearAllFiltersLabel?: string
}

export type OperatorOffersListViewModel = {
  tabs: OperatorOffersListTab[]
  activeViewId: OperatorOffersListViewId
  searchQuery: string
  searchMissLabel: string | null
  /** True when All = 0 and there is no active query — hide tabs/toolbar. */
  showListChrome: boolean
  rows: OperatorOffersListTableRow[]
  empty: OperatorOffersListEmptyViewModel | null
  sortId: OperatorOffersSortId
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

export type OperatorOffersPendingLifecycleAction = {
  offerId: number
  offerTitle: string
  actionId: Extract<
    OfferRowActionId,
    "pause" | "resume" | "duplicate" | "archive"
  >
  title: string
  description: string
}

export type OperatorOffersPerformanceView = {
  selectedRange: HomePerformanceDateRange
  dateRangeLabel: string
  kpis: OperatorOffersKpi[]
}

export type OperatorOffersNeedsAttentionView = {
  title: string
  subtitle: string
  emptyCopy: string
  isEmpty: boolean
}

export type OperatorOffersPageViewModel = {
  locationId: number
  locationName: string
  /** True when All offers count is 0 and no search/filters — Figma true-empty. */
  isTrueEmpty: boolean
  header: {
    createOfferLabel: string
    openStaffRedeemLabel: string
    viewRedemptionLogLabel: string
  }
  performance: OperatorOffersPerformanceView
  needsAttention: OperatorOffersNeedsAttentionView
  list: OperatorOffersListViewModel
  filtersSession: FilterSheetSession | null
  filtersBusy: boolean
  /** Gated lifecycle confirm chrome — writes deferred (ticket 32). */
  pendingLifecycleAction: OperatorOffersPendingLifecycleAction | null
}

export type OperatorOffersCreateOfferDrawerViewModel = {
  open: boolean
  mode: CreateEditOfferDrawerMode
  locationSubtitle: string
  draft: CampaignCatalogOfferDetailsDraft
  status: "idle" | "saving" | "error"
  error: string | null
  canConfirm: boolean
  /** Always false once catalog update API is live (ticket 31). */
  saveGated: boolean
}

export type OperatorOffersPendingEditOfferSave = {
  title: string
  description: string
}

export type OperatorOffersPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorOffersPageViewModel | null
  loadError: string | null
  createOfferDrawer: OperatorOffersCreateOfferDrawerViewModel | null
  /** Offer templates picker — Create offer opens this before the drawer (ticket 19). */
  offerTemplatePicker: OfferTemplatePickerSnapshot
  pendingEditOfferSave: OperatorOffersPendingEditOfferSave | null
  /** Always null for Create success — stay on list (ticket 09/18). List refresh lands with list module. */
  pendingNavigation: null
}

export type OperatorOffersPageAdapters = {
  listCatalogOffers: (
    params: CatalogOffersListQueryParams
  ) => Promise<CatalogOffersListResponse>
  debounceMs?: number
  createOffer?: (
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  updateOffer?: (
    offerId: number,
    body: CreateCatalogOfferRequestBody
  ) => Promise<CatalogOfferDetail>
  getOffer?: (offerId: number) => Promise<CatalogOfferDetail>
}

export type OperatorOffersPageModule = {
  getSnapshot: () => OperatorOffersPageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorOffersWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setPerformanceDateRange: (range: HomePerformanceDateRange) => void
  setListView: (viewId: OperatorOffersListViewId) => Promise<void>
  setSearchQuery: (query: string) => void
  setSortId: (id: OperatorOffersSortId) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  openFilters: () => void
  closeFilters: () => void
  setFiltersSession: (session: FilterSheetSession) => void
  applyFilters: (filters: OperatorFilterSelection) => void
  removeFilterChip: (chip: FilterChip) => void
  clearSearchAndFilters: () => Promise<void>
  viewAllOffers: () => Promise<void>
  /**
   * Row ⋮ — View navigates from the page (Details route). Edit opens the shared drawer (ticket 31).
   * Pause/Resume/Duplicate/Archive open confirm chrome only (writes: ticket 32).
   */
  requestRowAction: (offerId: number, actionId: OfferRowActionId) => void
  /** Clears pending confirm — does not call lifecycle write APIs (ticket 32). */
  confirmPendingLifecycleAction: () => void
  cancelPendingLifecycleAction: () => void
  /** Opens the Offer templates picker (not the Create drawer). */
  openCreateOffer: () => Promise<void>
  /** Closes the picker and soft-fills Create Offer — no catalog POST until Save. */
  useOfferTemplate: (templateId: string) => void
  closeOfferTemplatePicker: () => void
  setOfferTemplateSearchQuery: (query: string) => void
  retryOfferTemplateLoad: () => Promise<void>
  /** Opens Create drawer blank — prefer openCreateOffer / useOfferTemplate for Create. */
  openCreateOfferDrawer: () => void
  openEditOfferDrawer: (offerId: number) => Promise<void>
  closeCreateOfferDrawer: () => void
  patchCreateOfferDraft: (
    patch: Partial<CampaignCatalogOfferDetailsDraft>
  ) => void
  confirmCreateOffer: () => Promise<void>
  confirmPendingEditOfferSave: () => Promise<void>
  cancelPendingEditOfferSave: () => void
}

type OffersState = {
  loadStatus: OperatorOffersPageSnapshot["loadStatus"]
  workspace: OperatorOffersWorkspaceInput | null
  viewModel: OperatorOffersPageViewModel | null
  loadError: string | null
  loadGeneration: number
  listLoadGeneration: number
  activeViewId: OperatorOffersListViewId
  searchQuery: string
  sortId: OperatorOffersSortId
  page: number
  appliedFilters: OperatorFilterSelection
  filtersSession: FilterSheetSession | null
  filtersBusy: boolean
  lastListResponse: CatalogOffersListResponse | null
  pendingLifecycleAction: OperatorOffersPendingLifecycleAction | null
  performanceDateRange: HomePerformanceDateRange
  /** Snapshot Active-offers count — independent of the date window. */
  activeOffersCount: number
  /** Window event counts — stay zero until metrics wiring. */
  windowCounts: {
    offersIssued: number
    claims: number
    redemptions: number
  }
  createOfferDrawerOpen: boolean
  createOfferDrawerMode: CreateEditOfferDrawerMode
  createOfferDraft: CampaignCatalogOfferDetailsDraft
  createOfferStatus: OperatorOffersCreateOfferDrawerViewModel["status"]
  createOfferError: string | null
  /** Tracks which Edit hydrate is in flight so late responses can be ignored. */
  editHydrateOfferId: number | null
  /** Persists while Edit drawer is open — target for update API. */
  editOfferId: number | null
  editBaselineDraft: CampaignCatalogOfferDetailsDraft | null
  editIssueCount: number
  pendingEditOfferSave: OperatorOffersPendingEditOfferSave | null
}

function emptyOffersFilters(): OperatorFilterSelection {
  return emptySelection(OFFERS_FILTER_SCHEMA)
}

function formatOffersPageRangeLabel(
  page: number,
  pageSize: number,
  totalCount: number
): string {
  if (totalCount === 0) {
    return "Showing 0 of 0 offers"
  }
  const start = (page - 1) * pageSize + 1
  const end = Math.min(page * pageSize, totalCount)
  return `Showing ${start}–${end} of ${totalCount} offers`
}

function hasActiveFilters(filters: OperatorFilterSelection): boolean {
  return JSON.stringify(filters) !== JSON.stringify(emptyOffersFilters())
}

function mapTabs(
  tabCounts: CatalogOffersListResponse["tabCounts"]
): OperatorOffersListTab[] {
  const counts: Record<OperatorOffersListViewId, number> = {
    all: tabCounts.all,
    "needs-attention": tabCounts.needsAttention,
    drafts: tabCounts.drafts,
    "in-flight": tabCounts.inFlight,
    sent: tabCounts.sent,
  }

  return OPERATOR_OFFERS_LIST_VIEW_ORDER.map((id) => ({
    id,
    label: OPERATOR_OFFERS_LIST_VIEW_LABELS[id],
    count: counts[id],
    showCount: id !== "all",
  }))
}

function buildListEmpty(
  kind: OperatorOffersListEmptyStateKind,
  activeViewId: OperatorOffersListViewId
): OperatorOffersListEmptyViewModel {
  const copy = offersListEmptyCopy({ kind, activeViewId })
  if (kind === "true-empty") {
    return {
      kind,
      title: copy.title,
      helper: copy.helper,
      createOfferLabel: OFFERS_PAGE_COPY.createOffer,
      useTemplateLabel: OFFERS_PAGE_COPY.useTemplate,
    }
  }

  if (kind === "filter-search") {
    return {
      kind,
      title: copy.title,
      helper: copy.helper,
      viewAllOffersLabel: OFFERS_PAGE_COPY.viewAllOffers,
      clearAllFiltersLabel: OFFERS_PAGE_COPY.clearAllFilters,
    }
  }

  return {
    kind,
    title: copy.title,
    helper: copy.helper,
  }
}

function buildListViewModel(input: {
  response: CatalogOffersListResponse
  activeViewId: OperatorOffersListViewId
  searchQuery: string
  sortId: OperatorOffersSortId
  page: number
  appliedFilters: OperatorFilterSelection
}): OperatorOffersListViewModel {
  const hasActiveQuery =
    input.searchQuery.trim().length > 0
    || hasActiveFilters(input.appliedFilters)
  const allCount = input.response.tabCounts.all
  const emptyKind = resolveOffersListEmptyStateKind({
    allCount,
    filteredTotalCount: input.response.totalCount,
    hasActiveQuery,
  })
  const isTrueEmpty = emptyKind === "true-empty"
  const totalCount = input.response.totalCount
  const pageSize = input.response.pageSize || OFFERS_PAGE_SIZE
  const maxPage = Math.max(1, Math.ceil(totalCount / pageSize))
  const filterChips = projectChips(OFFERS_FILTER_SCHEMA, input.appliedFilters)

  return {
    tabs: mapTabs(input.response.tabCounts),
    activeViewId: input.activeViewId,
    searchQuery: input.searchQuery,
    searchMissLabel: offersListSearchMissLabel(input.searchQuery),
    showListChrome: !isTrueEmpty,
    rows: input.response.items.map((item) =>
      mapCatalogOfferListItemToTableRow(item)
    ),
    empty:
      emptyKind == null ? null : buildListEmpty(emptyKind, input.activeViewId),
    sortId: input.sortId,
    sortLabel: OPERATOR_OFFERS_SORT_LABELS[input.sortId],
    filterChips,
    filterChipCount: chipCount(OFFERS_FILTER_SCHEMA, input.appliedFilters),
    currentPage: input.page,
    pageSize,
    totalCount,
    pageRangeLabel: formatOffersPageRangeLabel(
      input.page,
      pageSize,
      totalCount
    ),
    canGoPrevious: input.page > 1,
    canGoNext: input.page < maxPage && totalCount > 0,
  }
}

function assemblePerformance(
  dateRange: HomePerformanceDateRange,
  activeOffersCount: number,
  windowCounts: OffersState["windowCounts"]
): OperatorOffersPerformanceView {
  return {
    selectedRange: dateRange,
    dateRangeLabel: labelForHomePerformanceDateRange(dateRange),
    kpis: buildOffersPerformanceKpis({
      activeOffers: activeOffersCount,
      offersIssued: windowCounts.offersIssued,
      claims: windowCounts.claims,
      redemptions: windowCounts.redemptions,
    }),
  }
}

function assembleNeedsAttention(): OperatorOffersNeedsAttentionView {
  return {
    title: OFFERS_PAGE_COPY.needsAttentionTitle,
    subtitle: OFFERS_PAGE_COPY.needsAttentionSubtitle,
    emptyCopy: NEEDS_ATTENTION_EMPTY_COPY,
    isEmpty: true,
  }
}

function assembleViewModel(
  location: OperatorOffersWorkspaceLocation,
  listResponse: CatalogOffersListResponse,
  activeViewId: OperatorOffersListViewId,
  searchQuery: string,
  sortId: OperatorOffersSortId,
  page: number,
  appliedFilters: OperatorFilterSelection,
  filtersSession: FilterSheetSession | null,
  filtersBusy: boolean,
  pendingLifecycleAction: OperatorOffersPendingLifecycleAction | null,
  dateRange: HomePerformanceDateRange,
  activeOffersCount: number,
  windowCounts: OffersState["windowCounts"]
): OperatorOffersPageViewModel {
  const list = buildListViewModel({
    response: listResponse,
    activeViewId,
    searchQuery,
    sortId,
    page,
    appliedFilters,
  })

  return {
    locationId: location.id,
    locationName: location.locationName,
    isTrueEmpty: list.empty?.kind === "true-empty",
    header: {
      createOfferLabel: OFFERS_PAGE_COPY.createOffer,
      openStaffRedeemLabel: OFFERS_PAGE_COPY.openStaffRedeem,
      viewRedemptionLogLabel: OFFERS_PAGE_COPY.viewRedemptionLog,
    },
    performance: assemblePerformance(
      dateRange,
      activeOffersCount,
      windowCounts
    ),
    needsAttention: assembleNeedsAttention(),
    list,
    filtersSession,
    filtersBusy,
    pendingLifecycleAction,
  }
}

function lifecycleConfirmCopy(
  actionId: OperatorOffersPendingLifecycleAction["actionId"]
): { title: string; description: string } {
  switch (actionId) {
    case "pause":
      return {
        title: OFFERS_PAGE_COPY.pauseConfirmTitle,
        description: OFFERS_PAGE_COPY.pauseConfirmDescription,
      }
    case "resume":
      return {
        title: OFFERS_PAGE_COPY.resumeConfirmTitle,
        description: OFFERS_PAGE_COPY.resumeConfirmDescription,
      }
    case "archive":
      return {
        title: OFFERS_PAGE_COPY.archiveConfirmTitle,
        description: OFFERS_PAGE_COPY.archiveConfirmDescription,
      }
    case "duplicate":
      return {
        title: OFFERS_PAGE_COPY.duplicateConfirmTitle,
        description: OFFERS_PAGE_COPY.duplicateConfirmDescription,
      }
  }
}

function buildCreateOfferDrawer(
  state: OffersState
): OperatorOffersCreateOfferDrawerViewModel | null {
  if (!state.createOfferDrawerOpen || state.viewModel == null) {
    return null
  }

  return {
    open: true,
    mode: state.createOfferDrawerMode,
    locationSubtitle: state.viewModel.locationName,
    draft: state.createOfferDraft,
    status: state.createOfferStatus,
    error: state.createOfferError,
    saveGated: false,
    canConfirm: canConfirmCampaignCatalogOfferDetails(state.createOfferDraft),
  }
}

/**
 * Operator Offers page module — Performance strip, Needs attention shell, list chrome,
 * and Create/Edit Offer drawer.
 * Metrics loaders and lifecycle writes stay deferred to later tickets.
 */
export function createOperatorOffersPageModule(
  adapters: OperatorOffersPageAdapters = {
    listCatalogOffers: listCatalogOffersApi,
  }
): OperatorOffersPageModule {
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS

  const offerTemplatePicker = createOfferTemplatePickerModule({
    loadTemplates: loadOfferTemplateSeed,
  })

  let state: OffersState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
    loadGeneration: 0,
    listLoadGeneration: 0,
    activeViewId: "all",
    searchQuery: "",
    sortId: OPERATOR_OFFERS_DEFAULT_SORT_ID,
    page: 1,
    appliedFilters: emptyOffersFilters(),
    filtersSession: null,
    filtersBusy: false,
    lastListResponse: null,
    pendingLifecycleAction: null,
    performanceDateRange: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
    activeOffersCount: 0,
    windowCounts: {
      offersIssued: 0,
      claims: 0,
      redemptions: 0,
    },
    createOfferDrawerOpen: false,
    createOfferDrawerMode: "create",
    createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
    createOfferStatus: "idle",
    createOfferError: null,
    editHydrateOfferId: null,
    editOfferId: null,
    editBaselineDraft: null,
    editIssueCount: 0,
    pendingEditOfferSave: null,
  }

  let snapshot: OperatorOffersPageSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    loadError: state.loadError,
    createOfferDrawer: null,
    offerTemplatePicker: offerTemplatePicker.getSnapshot(),
    pendingEditOfferSave: null,
    pendingNavigation: null,
  }

  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

  const publish = () => {
    snapshot = {
      loadStatus: state.loadStatus,
      viewModel: state.viewModel,
      loadError: state.loadError,
      createOfferDrawer: buildCreateOfferDrawer(state),
      offerTemplatePicker: offerTemplatePicker.getSnapshot(),
      pendingEditOfferSave: state.pendingEditOfferSave,
      pendingNavigation: null,
    }
    for (const listener of listeners) {
      listener()
    }
  }

  offerTemplatePicker.subscribe(() => {
    publish()
  })

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const buildListParams = (locationId: number): CatalogOffersListQueryParams =>
    buildOffersListQueryParams({
      locationId,
      view: state.activeViewId,
      q: state.searchQuery,
      sort: state.sortId,
      page: state.page,
      pageSize: OFFERS_PAGE_SIZE,
      filters: state.appliedFilters,
    })

  const assembleFromState = (
    location: OperatorOffersWorkspaceLocation,
    listResponse: CatalogOffersListResponse
  ) =>
    assembleViewModel(
      location,
      listResponse,
      state.activeViewId,
      state.searchQuery,
      state.sortId,
      state.page,
      state.appliedFilters,
      state.filtersSession,
      state.filtersBusy,
      state.pendingLifecycleAction,
      state.performanceDateRange,
      state.activeOffersCount,
      state.windowCounts
    )

  const resolveSelectedLocation = (
    workspace: OperatorOffersWorkspaceInput
  ): OperatorOffersWorkspaceLocation | null => {
    if (workspace.selectedLocationId == null) {
      return null
    }
    return (
      workspace.locations.find(
        (entry) => entry.id === workspace.selectedLocationId
      ) ?? null
    )
  }

  const fetchList = async (options?: { quiet?: boolean }) => {
    const workspace = state.workspace
    const location = workspace != null ? resolveSelectedLocation(workspace) : null
    if (workspace == null || location == null) {
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
      const listResponse = await adapters.listCatalogOffers(
        buildListParams(location.id)
      )
      if (generation !== state.listLoadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        lastListResponse: listResponse,
        viewModel: assembleFromState(location, listResponse),
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
          loadError: OFFERS_LOAD_ERROR_MESSAGE,
          viewModel: null,
          lastListResponse: null,
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

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    if (workspace == null) {
      return
    }

    clearSearchDebounce()
    const generation = state.loadGeneration + 1
    const listLoadGeneration = state.listLoadGeneration + 1
    state = {
      ...state,
      loadStatus: "loading",
      loadError: null,
      loadGeneration: generation,
      listLoadGeneration,
      pendingLifecycleAction: null,
    }
    publish()

    if (workspace.selectedLocationId == null) {
      state = {
        ...state,
        loadStatus: "idle",
        viewModel: null,
        loadError: null,
        lastListResponse: null,
      }
      publish()
      return
    }

    const location = resolveSelectedLocation(workspace)
    if (location == null) {
      state = {
        ...state,
        loadStatus: "error",
        viewModel: null,
        loadError: OFFERS_LOAD_ERROR_MESSAGE,
        lastListResponse: null,
      }
      publish()
      return
    }

    try {
      const listResponse = await adapters.listCatalogOffers(
        buildListParams(location.id)
      )
      if (
        generation !== state.loadGeneration
        || listLoadGeneration !== state.listLoadGeneration
      ) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        lastListResponse: listResponse,
        viewModel: assembleFromState(location, listResponse),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        loadError: OFFERS_LOAD_ERROR_MESSAGE,
        viewModel: null,
        lastListResponse: null,
      }
      publish()
    }
  }

  const patchPendingLifecycle = (
    pending: OperatorOffersPendingLifecycleAction | null
  ) => {
    state = {
      ...state,
      pendingLifecycleAction: pending,
      viewModel:
        state.viewModel == null
          ? null
          : {
              ...state.viewModel,
              pendingLifecycleAction: pending,
            },
    }
    publish()
  }

  async function executeUpdateOffer() {
    if (
      !state.createOfferDrawerOpen
      || state.createOfferDrawerMode !== "edit"
      || state.viewModel == null
      || adapters.updateOffer == null
      || state.editOfferId == null
      || state.createOfferStatus === "saving"
    ) {
      return
    }

    const body = toCreateCatalogOfferRequestBody({
      locationId: state.viewModel.locationId,
      draft: state.createOfferDraft,
    })
    if (body == null) {
      return
    }

    const offerId = state.editOfferId
    state = {
      ...state,
      createOfferStatus: "saving",
      createOfferError: null,
    }
    publish()

    try {
      await adapters.updateOffer(offerId, body)
      state = {
        ...state,
        createOfferDrawerOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
        editOfferId: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
      }
      publish()
      await fetchList({ quiet: true })
    } catch {
      state = {
        ...state,
        createOfferStatus: "error",
        createOfferError: CREATE_EDIT_OFFER_DRAWER_COPY.updateOfferError,
      }
      publish()
    }
  }

  const openEditOfferDrawer = async (offerId: number) => {
    if (state.viewModel == null || adapters.getOffer == null) {
      return
    }
    state = {
      ...state,
      createOfferDrawerOpen: true,
      createOfferDrawerMode: "edit",
      createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
      createOfferStatus: "idle",
      createOfferError: null,
      editHydrateOfferId: offerId,
      editOfferId: offerId,
      editBaselineDraft: null,
      editIssueCount: 0,
      pendingEditOfferSave: null,
    }
    publish()

    try {
      const offer = await adapters.getOffer(offerId)
      if (
        !state.createOfferDrawerOpen
        || state.createOfferDrawerMode !== "edit"
        || state.editHydrateOfferId !== offerId
      ) {
        return
      }
      const draft = catalogOfferDetailToDraft(offer)
      state = {
        ...state,
        createOfferDraft: draft,
        createOfferError: null,
        editHydrateOfferId: null,
        editBaselineDraft: draft,
        editIssueCount: offer.issueCount,
      }
      publish()
    } catch {
      if (
        !state.createOfferDrawerOpen
        || state.createOfferDrawerMode !== "edit"
        || state.editHydrateOfferId !== offerId
      ) {
        return
      }
      state = {
        ...state,
        createOfferError: CREATE_EDIT_OFFER_DRAWER_COPY.editLoadError,
        editHydrateOfferId: null,
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
      const previousLocationId = state.workspace?.selectedLocationId ?? null
      const locationChanged = previousLocationId !== input.selectedLocationId
      state = {
        ...state,
        workspace: input,
        createOfferDrawerOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
        editHydrateOfferId: null,
        editOfferId: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
        ...(locationChanged
          ? {
              activeViewId: "all" as const,
              searchQuery: "",
              sortId: OPERATOR_OFFERS_DEFAULT_SORT_ID,
              page: 1,
              appliedFilters: emptyOffersFilters(),
              filtersSession: null,
              filtersBusy: false,
              pendingLifecycleAction: null,
            }
          : {}),
      }
      offerTemplatePicker.close()
      await loadForSelectedLocation()
    },
    retryLoad: () => loadForSelectedLocation(),
    setPerformanceDateRange(range) {
      if (state.viewModel == null) {
        state = {
          ...state,
          performanceDateRange: range,
        }
        return
      }

      state = {
        ...state,
        performanceDateRange: range,
        viewModel: {
          ...state.viewModel,
          performance: assemblePerformance(
            range,
            state.activeOffersCount,
            state.windowCounts
          ),
        },
      }
      publish()
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
              searchMissLabel: offersListSearchMissLabel(query),
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
      const maxPage = Math.max(1, Math.ceil(totalCount / OFFERS_PAGE_SIZE))
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
      clearSearchDebounce()
      state = {
        ...state,
        appliedFilters: removeAppliedChip(
          OFFERS_FILTER_SCHEMA,
          state.appliedFilters,
          chip
        ),
        page: 1,
      }
      void fetchList({ quiet: true })
    },
    clearSearchAndFilters: async () => {
      const filtersEmpty = !hasActiveFilters(state.appliedFilters)
      if (state.searchQuery === "" && state.page === 1 && filtersEmpty) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
        page: 1,
        appliedFilters: emptyOffersFilters(),
        filtersSession:
          state.filtersSession != null
            ? openSession(emptyOffersFilters())
            : null,
      }
      await fetchList()
    },
    viewAllOffers: async () => {
      clearSearchDebounce()
      state = {
        ...state,
        activeViewId: "all",
        searchQuery: "",
        page: 1,
        appliedFilters: emptyOffersFilters(),
      }
      await fetchList()
    },
    requestRowAction: (offerId, actionId) => {
      if (actionId === "view") {
        return
      }
      if (actionId === "edit") {
        void openEditOfferDrawer(offerId)
        return
      }
      const row = state.viewModel?.list.rows.find((entry) => entry.id === offerId)
      if (row == null) {
        return
      }
      const copy = lifecycleConfirmCopy(actionId)
      patchPendingLifecycle({
        offerId,
        offerTitle: row.title,
        actionId,
        title: copy.title,
        description: copy.description,
      })
    },
    confirmPendingLifecycleAction: () => {
      // Ticket 32 owns live lifecycle writes — chrome only here.
      patchPendingLifecycle(null)
    },
    cancelPendingLifecycleAction: () => {
      patchPendingLifecycle(null)
    },
    async openCreateOffer() {
      if (state.viewModel == null) {
        return
      }
      await offerTemplatePicker.open()
    },
    useOfferTemplate(templateId) {
      if (state.viewModel == null) {
        return
      }
      const template = getOfferTemplateById(templateId)
      if (template == null) {
        return
      }

      const draft = mapOfferTemplateToCreateDraft(
        template,
        state.viewModel.locationName
      )

      offerTemplatePicker.close()
      state = {
        ...state,
        createOfferDrawerOpen: true,
        createOfferDrawerMode: "create",
        createOfferDraft: draft,
        createOfferStatus: "idle",
        createOfferError: null,
        editHydrateOfferId: null,
      }
      publish()
    },
    closeOfferTemplatePicker() {
      offerTemplatePicker.close()
    },
    setOfferTemplateSearchQuery(query) {
      offerTemplatePicker.setSearchQuery(query)
    },
    async retryOfferTemplateLoad() {
      await offerTemplatePicker.retryLoad()
    },
    openCreateOfferDrawer() {
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        createOfferDrawerOpen: true,
        createOfferDrawerMode: "create",
        createOfferDraft: emptyCampaignCatalogOfferDetailsDraft(),
        createOfferStatus: "idle",
        createOfferError: null,
        editHydrateOfferId: null,
        editOfferId: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
      }
      publish()
    },
    openEditOfferDrawer,
    closeCreateOfferDrawer() {
      state = {
        ...state,
        createOfferDrawerOpen: false,
        createOfferStatus: "idle",
        createOfferError: null,
        editHydrateOfferId: null,
        editOfferId: null,
        editBaselineDraft: null,
        editIssueCount: 0,
        pendingEditOfferSave: null,
      }
      publish()
    },
    patchCreateOfferDraft(patch) {
      if (!state.createOfferDrawerOpen) {
        return
      }
      state = {
        ...state,
        createOfferDraft: { ...state.createOfferDraft, ...patch },
        createOfferError: null,
      }
      publish()
    },
    async confirmCreateOffer() {
      if (
        !state.createOfferDrawerOpen
        || state.viewModel == null
        || state.createOfferStatus === "saving"
        || state.pendingEditOfferSave != null
      ) {
        return
      }

      if (state.createOfferDrawerMode === "edit") {
        if (
          adapters.updateOffer == null
          || state.editOfferId == null
          || !canConfirmCampaignCatalogOfferDetails(state.createOfferDraft)
        ) {
          return
        }

        const dirtyBenefitOrValidity =
          state.editBaselineDraft != null
          && isDirtyBenefitOrValidity(
            state.editBaselineDraft,
            state.createOfferDraft
          )
        if (
          shouldConfirmEditOfferSave({
            issueCount: state.editIssueCount,
            dirtyBenefitOrValidity,
          })
        ) {
          state = {
            ...state,
            pendingEditOfferSave: {
              title: CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmTitle,
              description:
                CREATE_EDIT_OFFER_DRAWER_COPY.editSaveConfirmDescription,
            },
          }
          publish()
          return
        }

        await executeUpdateOffer()
        return
      }

      if (adapters.createOffer == null) {
        return
      }

      const body = toCreateCatalogOfferRequestBody({
        locationId: state.viewModel.locationId,
        draft: state.createOfferDraft,
      })
      if (body == null) {
        return
      }

      state = {
        ...state,
        createOfferStatus: "saving",
        createOfferError: null,
      }
      publish()

      try {
        await adapters.createOffer(body)
        state = {
          ...state,
          createOfferDrawerOpen: false,
          createOfferStatus: "idle",
          createOfferError: null,
        }
        publish()
        await fetchList({ quiet: true })
      } catch {
        state = {
          ...state,
          createOfferStatus: "error",
          createOfferError: CREATE_EDIT_OFFER_DRAWER_COPY.createOfferError,
        }
        publish()
      }
    },
    async confirmPendingEditOfferSave() {
      if (state.pendingEditOfferSave == null) {
        return
      }
      state = {
        ...state,
        pendingEditOfferSave: null,
      }
      publish()
      await executeUpdateOffer()
    },
    cancelPendingEditOfferSave() {
      if (state.pendingEditOfferSave == null) {
        return
      }
      state = {
        ...state,
        pendingEditOfferSave: null,
      }
      publish()
    },
  }
}
