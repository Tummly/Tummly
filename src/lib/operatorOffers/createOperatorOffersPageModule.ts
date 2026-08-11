import { OFFERS_PAGE_COPY } from "@/lib/operatorOffers/offersPresentation"

export const OFFERS_LOAD_ERROR_MESSAGE = OFFERS_PAGE_COPY.loadError

export type OperatorOffersWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorOffersWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorOffersWorkspaceLocation[]
}

export type OperatorOffersPageViewModel = {
  locationId: number
  locationName: string
  header: {
    createOfferLabel: string
    openStaffRedeemLabel: string
    viewRedemptionLogLabel: string
  }
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
}

type OffersState = {
  loadStatus: OperatorOffersPageSnapshot["loadStatus"]
  workspace: OperatorOffersWorkspaceInput | null
  viewModel: OperatorOffersPageViewModel | null
  loadError: string | null
}

function assembleViewModel(
  location: OperatorOffersWorkspaceLocation
): OperatorOffersPageViewModel {
  return {
    locationId: location.id,
    locationName: location.locationName,
    header: {
      createOfferLabel: OFFERS_PAGE_COPY.createOffer,
      openStaffRedeemLabel: OFFERS_PAGE_COPY.openStaffRedeem,
      viewRedemptionLogLabel: OFFERS_PAGE_COPY.viewRedemptionLog,
    },
  }
}

/**
 * Operator Offers page module shell — subscribe/snapshot/syncWorkspace.
 * List, Performance, and Needs attention children land in later tickets.
 */
export function createOperatorOffersPageModule(): OperatorOffersPageModule {
  let state: OffersState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
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
        viewModel: assembleViewModel(location),
        loadError: null,
      }
      publish()
    },
  }
}
