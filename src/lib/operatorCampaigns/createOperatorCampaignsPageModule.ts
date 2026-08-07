import { CAMPAIGNS_PAGE_COPY } from "@/lib/operatorCampaigns/campaignsPresentation"

export { CAMPAIGNS_PAGE_COPY }

export const CAMPAIGNS_LOAD_ERROR_MESSAGE =
  "Could not load campaigns. Please try again."

export type OperatorCampaignsWorkspaceLocation = {
  id: number
  locationName: string
}

export type OperatorCampaignsWorkspaceInput = {
  selectedLocationId: number | null
  locations: readonly OperatorCampaignsWorkspaceLocation[]
}

export type OperatorCampaignsOverviewResult = {
  /** All-view campaign count for the Owned location (Draft rows later). */
  totalCount: number
}

export type OperatorCampaignsPageAdapters = {
  loadOverview: (input: {
    locationId: number
  }) => Promise<OperatorCampaignsOverviewResult>
}

export type OperatorCampaignsListEmptyViewModel = {
  title: string
  helper: string
  createCampaignLabel: string
  useTemplateLabel: string
}

export type OperatorCampaignsPageViewModel = {
  locationId: number
  locationName: string
  /** True when All campaigns count is 0 — Figma true-empty overview. */
  isTrueEmpty: boolean
  header: {
    createCampaignLabel: string
    useTemplateLabel: string
  }
  listEmpty: OperatorCampaignsListEmptyViewModel | null
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
}

type CampaignsState = {
  loadStatus: OperatorCampaignsPageSnapshot["loadStatus"]
  workspace: OperatorCampaignsWorkspaceInput | null
  viewModel: OperatorCampaignsPageViewModel | null
  loadError: string | null
  loadGeneration: number
}

function assembleViewModel(
  workspace: OperatorCampaignsWorkspaceInput,
  totalCount: number
): OperatorCampaignsPageViewModel | null {
  const locationId = workspace.selectedLocationId
  if (locationId == null) {
    return null
  }

  const locationName =
    workspace.locations.find((location) => location.id === locationId)
      ?.locationName ?? ""
  const isTrueEmpty = totalCount === 0

  return {
    locationId,
    locationName,
    isTrueEmpty,
    header: {
      createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
      useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
    },
    listEmpty: isTrueEmpty
      ? {
          title: CAMPAIGNS_PAGE_COPY.trueEmptyTitle,
          helper: CAMPAIGNS_PAGE_COPY.trueEmptyHelper,
          createCampaignLabel: CAMPAIGNS_PAGE_COPY.createCampaign,
          useTemplateLabel: CAMPAIGNS_PAGE_COPY.useTemplate,
        }
      : null,
  }
}

export function createOperatorCampaignsPageModule(
  adapters: OperatorCampaignsPageAdapters
): OperatorCampaignsPageModule {
  let state: CampaignsState = {
    loadStatus: "idle",
    workspace: null,
    viewModel: null,
    loadError: null,
    loadGeneration: 0,
  }

  let snapshot: OperatorCampaignsPageSnapshot = {
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

  const loadForSelectedLocation = async () => {
    const workspace = state.workspace
    const selectedLocationId = workspace?.selectedLocationId
    if (workspace == null || selectedLocationId == null) {
      return
    }

    const generation = state.loadGeneration + 1
    state = {
      ...state,
      loadStatus: "loading",
      loadError: null,
      loadGeneration: generation,
    }
    publish()

    try {
      const overview = await adapters.loadOverview({
        locationId: selectedLocationId,
      })
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        viewModel: assembleViewModel(workspace, overview.totalCount),
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
      }
      publish()
    }
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
        state = {
          loadStatus: "idle",
          workspace: null,
          viewModel: null,
          loadError: null,
          loadGeneration: state.loadGeneration + 1,
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
  }
}
