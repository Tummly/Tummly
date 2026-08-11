import {
  buildOffersPerformanceKpis,
  type OperatorOffersKpi,
} from "@/lib/operatorOffers/buildOffersPerformanceKpis"
import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"
import {
  DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
  labelForHomePerformanceDateRange,
  type HomePerformanceDateRange,
} from "@/lib/operatorHome/homePerformanceDateRange"
import { NEEDS_ATTENTION_EMPTY_COPY } from "@/lib/operatorHome/operatorHomeSectionPresentation"

export const OFFERS_LOAD_ERROR_MESSAGE = OFFERS_PAGE_COPY.loadError

export type OperatorOffersWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorOffersWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorOffersWorkspaceLocation[]
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
  header: {
    createOfferLabel: string
    openStaffRedeemLabel: string
    viewRedemptionLogLabel: string
  }
  performance: OperatorOffersPerformanceView
  needsAttention: OperatorOffersNeedsAttentionView
}

export type OperatorOffersPageSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorOffersPageViewModel | null
  loadError: string | null
}

export type OperatorOffersPageModule = {
  getSnapshot: () => OperatorOffersPageSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (input: OperatorOffersWorkspaceInput) => Promise<void>
  setPerformanceDateRange: (range: HomePerformanceDateRange) => void
}

type OffersState = {
  loadStatus: OperatorOffersPageSnapshot["loadStatus"]
  workspace: OperatorOffersWorkspaceInput | null
  viewModel: OperatorOffersPageViewModel | null
  loadError: string | null
  performanceDateRange: HomePerformanceDateRange
  /** Snapshot Active-offers count — independent of the date window. */
  activeOffersCount: number
  /** Window event counts — stay zero until metrics wiring. */
  windowCounts: {
    offersIssued: number
    claims: number
    redemptions: number
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
  dateRange: HomePerformanceDateRange,
  activeOffersCount: number,
  windowCounts: OffersState["windowCounts"]
): OperatorOffersPageViewModel {
  return {
    locationId: location.id,
    locationName: location.locationName,
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
  }
}

/**
 * Operator Offers page module — subscribe/snapshot/syncWorkspace + Performance date.
 * List and metrics APIs land in later tickets.
 */
export function createOperatorOffersPageModule(): OperatorOffersPageModule {
  let state: OffersState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
    performanceDateRange: DEFAULT_HOME_PERFORMANCE_DATE_RANGE,
    activeOffersCount: 0,
    windowCounts: {
      offersIssued: 0,
      claims: 0,
      redemptions: 0,
    },
  }

  let snapshot: OperatorOffersPageSnapshot = {
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
      state = {
        ...state,
        workspace: input,
        loadStatus: "loading",
        loadError: null,
      }
      publish()

      if (input.selectedLocationId == null) {
        state = {
          ...state,
          loadStatus: "idle",
          viewModel: null,
          loadError: null,
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
          viewModel: null,
          loadError: OFFERS_LOAD_ERROR_MESSAGE,
        }
        publish()
        return
      }

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: assembleViewModel(
          location,
          state.performanceDateRange,
          state.activeOffersCount,
          state.windowCounts
        ),
        loadError: null,
      }
      publish()
    },
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
  }
}
