import { OFFERS_REDEMPTION_LOG_COPY } from "@/lib/operatorOffers/offersRedemptionLogPresentation"

export const OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE =
  OFFERS_REDEMPTION_LOG_COPY.loadError

export type OperatorOffersRedemptionLogWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorOffersRedemptionLogWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorOffersRedemptionLogWorkspaceLocation[]
}

export type OperatorOffersRedemptionLogRow = {
  id: string
  dateTimeText: string
  guestName: string
  passReferenceText: string
  locationName: string
  staffMemberText: string
  outcomeText: string
  reasonText: string
  offerVersionText: string
  offerTitle: string
}

export type OperatorOffersRedemptionLogViewModel = {
  locationId: number
  locationName: string
  title: string
  subtitle: string
  backLabel: string
  columns: typeof OFFERS_REDEMPTION_LOG_COPY.columns
  rows: readonly OperatorOffersRedemptionLogRow[]
  empty: {
    title: string
    helper: string
    retryLabel: string
  }
}

export type OperatorOffersRedemptionLogSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  viewModel: OperatorOffersRedemptionLogViewModel | null
  loadError: string | null
}

export type OperatorOffersRedemptionLogAdapters = {
  /** Optional until the location-wide redemption log API ships. */
  listRedemptions?: (
    locationId: number
  ) => Promise<readonly OperatorOffersRedemptionLogRow[]>
}

export type OperatorOffersRedemptionLogModule = {
  getSnapshot: () => OperatorOffersRedemptionLogSnapshot
  subscribe: (listener: () => void) => () => void
  syncWorkspace: (
    input: OperatorOffersRedemptionLogWorkspaceInput
  ) => Promise<void>
  retryLoad: () => Promise<void>
}

type ModuleState = {
  loadStatus: OperatorOffersRedemptionLogSnapshot["loadStatus"]
  workspace: OperatorOffersRedemptionLogWorkspaceInput | null
  viewModel: OperatorOffersRedemptionLogViewModel | null
  loadError: string | null
}

function assembleViewModel(
  location: OperatorOffersRedemptionLogWorkspaceLocation,
  rows: readonly OperatorOffersRedemptionLogRow[]
): OperatorOffersRedemptionLogViewModel {
  const copy = OFFERS_REDEMPTION_LOG_COPY
  return {
    locationId: location.id,
    locationName: location.locationName,
    title: copy.title,
    subtitle: copy.subtitle,
    backLabel: copy.backToOffers,
    columns: copy.columns,
    rows,
    empty: {
      title: copy.emptyTitle,
      helper: copy.emptyHelper,
      retryLabel: copy.retry,
    },
  }
}

/**
 * Location-wide redemption log page module — chrome + honest empty until API.
 */
export function createOperatorOffersRedemptionLogModule(
  adapters: OperatorOffersRedemptionLogAdapters = {}
): OperatorOffersRedemptionLogModule {
  let state: ModuleState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
  }

  let snapshot: OperatorOffersRedemptionLogSnapshot = {
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

  const loadForWorkspace = async (
    input: OperatorOffersRedemptionLogWorkspaceInput
  ) => {
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
        loadError: OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE,
      }
      publish()
      return
    }

    try {
      const rows =
        adapters.listRedemptions == null
          ? []
          : await adapters.listRedemptions(location.id)

      state = {
        ...state,
        loadStatus: "loaded",
        viewModel: assembleViewModel(location, rows),
        loadError: null,
      }
      publish()
    } catch {
      state = {
        ...state,
        loadStatus: "error",
        viewModel: null,
        loadError: OFFERS_REDEMPTION_LOG_LOAD_ERROR_MESSAGE,
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
  }
}
