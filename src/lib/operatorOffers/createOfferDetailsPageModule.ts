import {
  buildOfferDetailsDefinitionFields,
  buildOfferDetailsHeaderMenuItems,
  buildOfferDetailsMetaRows,
  buildOfferDetailsOverviewKpis,
  DEFAULT_OFFER_DETAILS_DATE_RANGE,
  labelForOfferDetailsDateRange,
  OFFER_DETAILS_COPY,
  OFFER_DETAILS_TAB_IDS,
  OFFER_DETAILS_TAB_LABELS,
  offerDetailsHeaderActionConfirmCopy,
  offerDetailsStatusLabel,
  tabEmptyPlaceholderCopy,
  type OfferDetailsDateRange,
  type OfferDetailsHeaderActionId,
  type OfferDetailsHeaderMenuItem,
  type OfferDetailsKpi,
  type OfferDetailsLabeledValue,
  type OfferDetailsOverviewMetrics,
  type OfferDetailsTabId,
} from "@/lib/operatorOffers/offerDetailsPresentation"
import type { CatalogOfferDetail } from "@/types/operatorCampaigns"

export const OFFER_DETAILS_LOAD_ERROR_MESSAGE = OFFER_DETAILS_COPY.loadError

export type OfferDetailsWorkspaceLocation = {
  id: number
  locationName: string
}

export type OfferDetailsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OfferDetailsWorkspaceLocation[]
  offerId: number | null
}

export type OfferDetailsPendingHeaderAction = {
  actionId: OfferDetailsHeaderActionId
  title: string
  description: string
}

export type OfferDetailsTabChrome = {
  id: OfferDetailsTabId
  label: string
}

export type OfferDetailsOverviewViewModel = {
  dateRange: OfferDetailsDateRange
  dateRangeLabel: string
  kpis: readonly OfferDetailsKpi[]
  definitionTitle: string
  definitionFields: readonly OfferDetailsLabeledValue[]
  recommendation: {
    title: string
    subtitle: string
    emptyTitle: string
    emptyHelper: string
  }
}

export type OfferDetailsViewModel = {
  offerId: number
  locationId: number
  locationName: string
  title: string
  subtitle: string
  status: CatalogOfferDetail["status"]
  statusLabel: string
  breadcrumbOffersLabel: string
  editOfferLabel: string
  openStaffRedeemLabel: string
  moreActionsAriaLabel: string
  headerMenuItems: readonly OfferDetailsHeaderMenuItem[]
  pendingHeaderAction: OfferDetailsPendingHeaderAction | null
  metaRows: readonly OfferDetailsLabeledValue[]
  tabs: readonly OfferDetailsTabChrome[]
  activeTabId: OfferDetailsTabId
  overview: OfferDetailsOverviewViewModel
  activeTabEmptyPlaceholder: string
}

export type OfferDetailsSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OfferDetailsViewModel | null
  loadError: string | null
}

export type OfferDetailsAdapters = {
  getOffer: (offerId: number) => Promise<CatalogOfferDetail>
  /** Optional until offer metrics API ships — zeros respond to date range when absent. */
  getOfferMetrics?: (
    offerId: number,
    range: OfferDetailsDateRange
  ) => Promise<OfferDetailsOverviewMetrics>
}

export type OfferDetailsPageModule = {
  getSnapshot: () => OfferDetailsSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OfferDetailsWorkspaceInput) => Promise<void>
  retryLoad: () => Promise<void>
  setActiveTab: (tabId: OfferDetailsTabId) => void
  setOverviewDateRange: (range: OfferDetailsDateRange) => Promise<void>
  requestHeaderAction: (actionId: OfferDetailsHeaderActionId) => void
  /** Clears pending confirm — does not call lifecycle write APIs (ticket 32). */
  confirmPendingHeaderAction: () => void
  cancelPendingHeaderAction: () => void
}

type ModuleState = {
  loadStatus: OfferDetailsSnapshot["loadStatus"]
  workspace: OfferDetailsWorkspaceInput | null
  offer: CatalogOfferDetail | null
  locationName: string | null
  viewModel: OfferDetailsViewModel | null
  loadError: string | null
  activeTabId: OfferDetailsTabId
  overviewDateRange: OfferDetailsDateRange
  overviewMetrics: OfferDetailsOverviewMetrics
  pendingHeaderAction: OfferDetailsPendingHeaderAction | null
  loadGeneration: number
}

function emptyMetrics(): OfferDetailsOverviewMetrics {
  return {
    claims: 0,
    redemptions: 0,
    expiredUnused: 0,
    failedAttempts: 0,
  }
}

function assembleViewModel(state: ModuleState): OfferDetailsViewModel | null {
  if (state.offer == null || state.locationName == null) {
    return null
  }

  const offer = state.offer
  return {
    offerId: offer.id,
    locationId: offer.locationId,
    locationName: state.locationName,
    title: offer.title,
    subtitle: offer.description.trim(),
    status: offer.status,
    statusLabel: offerDetailsStatusLabel(offer.status),
    breadcrumbOffersLabel: OFFER_DETAILS_COPY.breadcrumbOffers,
    editOfferLabel: OFFER_DETAILS_COPY.editOffer,
    openStaffRedeemLabel: OFFER_DETAILS_COPY.openStaffRedeem,
    moreActionsAriaLabel: OFFER_DETAILS_COPY.moreActionsAriaLabel,
    headerMenuItems: buildOfferDetailsHeaderMenuItems(offer.status),
    pendingHeaderAction: state.pendingHeaderAction,
    metaRows: buildOfferDetailsMetaRows({
      locationName: state.locationName,
      createdAt: offer.createdAt,
    }),
    tabs: OFFER_DETAILS_TAB_IDS.map((id) => ({
      id,
      label: OFFER_DETAILS_TAB_LABELS[id],
    })),
    activeTabId: state.activeTabId,
    overview: {
      dateRange: state.overviewDateRange,
      dateRangeLabel: labelForOfferDetailsDateRange(state.overviewDateRange),
      kpis: buildOfferDetailsOverviewKpis(state.overviewMetrics),
      definitionTitle: OFFER_DETAILS_COPY.definitionTitle,
      definitionFields: buildOfferDetailsDefinitionFields({
        offer,
        locationName: state.locationName,
      }),
      recommendation: {
        title: OFFER_DETAILS_COPY.recommendedTitle,
        subtitle: OFFER_DETAILS_COPY.recommendedSubtitle,
        emptyTitle: OFFER_DETAILS_COPY.recommendedEmptyTitle,
        emptyHelper: OFFER_DETAILS_COPY.recommendedEmptyHelper,
      },
    },
    activeTabEmptyPlaceholder: tabEmptyPlaceholderCopy(state.activeTabId),
  }
}

/**
 * Offer Details page module — get-by-id chrome, Overview KPIs (zeros until metrics),
 * gated header lifecycle confirms (ticket 23).
 */
export function createOfferDetailsPageModule(
  adapters: OfferDetailsAdapters
): OfferDetailsPageModule {
  let state: ModuleState = {
    loadStatus: "idle",
    workspace: null,
    offer: null,
    locationName: null,
    viewModel: null,
    loadError: null,
    activeTabId: "overview",
    overviewDateRange: DEFAULT_OFFER_DETAILS_DATE_RANGE,
    overviewMetrics: emptyMetrics(),
    pendingHeaderAction: null,
    loadGeneration: 0,
  }

  let snapshot: OfferDetailsSnapshot = {
    loadStatus: state.loadStatus,
    viewModel: state.viewModel,
    loadError: state.loadError,
  }

  const listeners = new Set<() => void>()

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

  const patchPending = (
    pending: OfferDetailsPendingHeaderAction | null
  ) => {
    state = {
      ...state,
      pendingHeaderAction: pending,
      viewModel:
        state.viewModel == null
          ? null
          : {
              ...state.viewModel,
              pendingHeaderAction: pending,
            },
    }
    publish()
  }

  const loadMetrics = async (
    offerId: number,
    range: OfferDetailsDateRange
  ): Promise<OfferDetailsOverviewMetrics> => {
    if (adapters.getOfferMetrics == null) {
      return emptyMetrics()
    }
    return adapters.getOfferMetrics(offerId, range)
  }

  const loadForWorkspace = async (input: OfferDetailsWorkspaceInput) => {
    const generation = state.loadGeneration + 1
    state = {
      ...state,
      workspace: input,
      loadGeneration: generation,
      loadStatus: "loading",
      loadError: null,
      pendingHeaderAction: null,
    }
    publish()

    if (input.selectedLocationId == null || input.offerId == null) {
      state = {
        ...state,
        loadStatus: "idle",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: null,
        overviewMetrics: emptyMetrics(),
      }
      publish()
      return
    }

    const location = input.locations.find(
      (entry) => entry.id === input.selectedLocationId
    )
    if (location == null) {
      state = {
        ...state,
        loadStatus: "error",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: OFFER_DETAILS_LOAD_ERROR_MESSAGE,
        overviewMetrics: emptyMetrics(),
      }
      publish()
      return
    }

    try {
      const offer = await adapters.getOffer(input.offerId)
      if (generation !== state.loadGeneration) {
        return
      }

      const metrics = await loadMetrics(offer.id, state.overviewDateRange)
      if (generation !== state.loadGeneration) {
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        offer,
        locationName: location.locationName,
        overviewMetrics: metrics,
        loadError: null,
        pendingHeaderAction: null,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        offer: null,
        locationName: null,
        viewModel: null,
        loadError: OFFER_DETAILS_LOAD_ERROR_MESSAGE,
        overviewMetrics: emptyMetrics(),
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
      await loadForWorkspace(input)
    },
    async retryLoad() {
      if (state.workspace == null) {
        return
      }
      await loadForWorkspace(state.workspace)
    },
    setActiveTab(tabId) {
      if (state.viewModel == null) {
        return
      }
      state = {
        ...state,
        activeTabId: tabId,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    },
    async setOverviewDateRange(range) {
      if (state.viewModel == null || state.offer == null) {
        return
      }
      const offerId = state.offer.id
      state = {
        ...state,
        overviewDateRange: range,
      }
      const metrics = await loadMetrics(offerId, range)
      if (state.offer == null) {
        return
      }
      state = {
        ...state,
        overviewMetrics: metrics,
      }
      state = {
        ...state,
        viewModel: assembleViewModel(state),
      }
      publish()
    },
    requestHeaderAction(actionId) {
      if (state.viewModel == null) {
        return
      }
      // Rename opens Edit drawer in the page — no state-change confirm (ticket 10).
      if (actionId === "rename") {
        return
      }
      const allowed = state.viewModel.headerMenuItems.some(
        (item) => item.id === actionId
      )
      if (!allowed) {
        return
      }
      const copy = offerDetailsHeaderActionConfirmCopy(actionId)
      patchPending({
        actionId,
        title: copy.title,
        description: copy.description,
      })
    },
    confirmPendingHeaderAction() {
      // Ticket 32 owns live lifecycle writes — chrome only here.
      patchPending(null)
    },
    cancelPendingHeaderAction() {
      patchPending(null)
    },
  }
}
