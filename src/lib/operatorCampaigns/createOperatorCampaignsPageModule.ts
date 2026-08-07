import {
  CAMPAIGNS_PAGE_COPY,
  CAMPAIGNS_SUMMARY_MOCK_KPIS,
} from "@/lib/operatorCampaigns/campaignsPresentation"
import {
  labelForCampaignsOverviewDateRange,
  type CampaignsOverviewDateRange,
} from "@/lib/operatorCampaigns/campaignsOverviewDateRange"

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
  loadOverview: (input: {
    locationId: number
  }) => Promise<OperatorCampaignsOverviewResult>
  loadMarketingEligible: (input: {
    locationId: number
    overviewDateRange: CampaignsOverviewDateRange
  }) => Promise<number>
  getCampaignsOverviewDateRange: () => CampaignsOverviewDateRange
}

export type OperatorCampaignsListEmptyViewModel = {
  title: string
  helper: string
  createCampaignLabel: string
  useTemplateLabel: string
}

export type OperatorCampaignsSummaryViewModel = {
  title: string
  subtitle: string
  kpis: OperatorCampaignsSummaryKpi[]
}

export type OperatorCampaignsPageViewModel = {
  locationId: number
  locationName: string
  /** True when All campaigns count is 0 — Figma true-empty overview. */
  isTrueEmpty: boolean
  dateRangeLabel: string
  selectedDateRange: CampaignsOverviewDateRange
  header: {
    createCampaignLabel: string
    useTemplateLabel: string
  }
  summary: OperatorCampaignsSummaryViewModel
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
  /** Refetch Marketing eligible only after the visit store date window changes. */
  reloadForOverviewDateRange: () => Promise<void>
}

type CampaignsState = {
  loadStatus: OperatorCampaignsPageSnapshot["loadStatus"]
  workspace: OperatorCampaignsWorkspaceInput | null
  viewModel: OperatorCampaignsPageViewModel | null
  loadError: string | null
  loadGeneration: number
  marketingEligibleGeneration: number
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

function assembleViewModel(
  workspace: OperatorCampaignsWorkspaceInput,
  totalCount: number,
  marketingEligible: number,
  overviewDateRange: CampaignsOverviewDateRange
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
    marketingEligibleGeneration: 0,
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
    const marketingEligibleGeneration = state.marketingEligibleGeneration + 1
    state = {
      ...state,
      loadStatus: "loading",
      loadError: null,
      loadGeneration: generation,
      marketingEligibleGeneration,
    }
    publish()

    const overviewDateRange = adapters.getCampaignsOverviewDateRange()

    try {
      const [overview, marketingEligible] = await Promise.all([
        adapters.loadOverview({ locationId: selectedLocationId }),
        adapters.loadMarketingEligible({
          locationId: selectedLocationId,
          overviewDateRange,
        }),
      ])
      if (
        generation !== state.loadGeneration
        || marketingEligibleGeneration !== state.marketingEligibleGeneration
      ) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        loadError: null,
        viewModel: assembleViewModel(
          workspace,
          overview.totalCount,
          marketingEligible,
          overviewDateRange
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
      }
      publish()
    }
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
        },
      }
      publish()
    } catch {
      // Keep prior Marketing eligible KPI; date chrome reads the visit store.
      return
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
          marketingEligibleGeneration: state.marketingEligibleGeneration + 1,
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
    reloadForOverviewDateRange: () => reloadMarketingEligibleOnly(),
  }
}
