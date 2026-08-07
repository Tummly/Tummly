import { CAMPAIGN_TEMPLATE_PICKER_COPY } from "@/lib/operatorCampaigns/campaignTemplatePickerPresentation"
import type { CampaignTemplateListItem } from "@/types/operatorCampaigns"

export type CampaignTemplatePickerAdapters = {
  loadTemplates: () => Promise<CampaignTemplateListItem[]>
}

export type CampaignTemplatePickerCardViewModel = {
  id: string
  version: number
  title: string
  description: string
  goalLabel: string
  audienceLabel: string
  channelLabel: string
  offerLabel: string
  /** Preview is shown but disabled until Figma overlay URL (ticket 13). */
  previewDisabled: true
  /** Use template opens the wizard at Audience (ticket 28). */
  useTemplateEnabled: true
}

export type CampaignTemplatePickerViewModel = {
  title: string
  subtitle: string
  searchPlaceholder: string
  searchQuery: string
  cards: CampaignTemplatePickerCardViewModel[]
  showSearchMiss: boolean
}

export type CampaignTemplatePickerSnapshot = {
  open: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  viewModel: CampaignTemplatePickerViewModel | null
}

export type CampaignTemplatePickerModule = {
  getSnapshot: () => CampaignTemplatePickerSnapshot
  subscribe: (listener: () => void) => () => void
  open: () => Promise<void>
  close: () => void
  retryLoad: () => Promise<void>
  setSearchQuery: (query: string) => void
}

type PickerState = {
  open: boolean
  loadStatus: CampaignTemplatePickerSnapshot["loadStatus"]
  loadError: string | null
  searchQuery: string
  items: CampaignTemplateListItem[] | null
  loadGeneration: number
}

function matchesSearch(
  item: CampaignTemplateListItem,
  query: string
): boolean {
  const q = query.trim().toLowerCase()
  if (q.length === 0) {
    return true
  }

  const haystack = [
    item.title,
    item.description,
    item.goalLabel,
    item.audienceLabel,
    item.channelLabel,
    item.offerLabel,
  ]
    .join(" ")
    .toLowerCase()

  return haystack.includes(q)
}

function toCard(
  item: CampaignTemplateListItem
): CampaignTemplatePickerCardViewModel {
  return {
    id: item.id,
    version: item.version,
    title: item.title,
    description: item.description,
    goalLabel: item.goalLabel,
    audienceLabel: item.audienceLabel,
    channelLabel: item.channelLabel,
    offerLabel: item.offerLabel,
    previewDisabled: true,
    useTemplateEnabled: true,
  }
}

function buildViewModel(
  items: CampaignTemplateListItem[],
  searchQuery: string
): CampaignTemplatePickerViewModel {
  const filtered = items.filter((item) => matchesSearch(item, searchQuery))
  return {
    title: CAMPAIGN_TEMPLATE_PICKER_COPY.title,
    subtitle: CAMPAIGN_TEMPLATE_PICKER_COPY.subtitle,
    searchPlaceholder: CAMPAIGN_TEMPLATE_PICKER_COPY.searchPlaceholder,
    searchQuery,
    cards: filtered.map(toCard),
    showSearchMiss: searchQuery.trim().length > 0 && filtered.length === 0,
  }
}

function projectSnapshot(state: PickerState): CampaignTemplatePickerSnapshot {
  const viewModel =
    state.items != null
      ? buildViewModel(state.items, state.searchQuery)
      : null

  return {
    open: state.open,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    viewModel,
  }
}

export function createCampaignTemplatePickerModule(
  adapters: CampaignTemplatePickerAdapters
): CampaignTemplatePickerModule {
  let state: PickerState = {
    open: false,
    loadStatus: "idle",
    loadError: null,
    searchQuery: "",
    items: null,
    loadGeneration: 0,
  }

  let snapshot = projectSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const setState = (patch: Partial<PickerState>) => {
    state = { ...state, ...patch }
    snapshot = projectSnapshot(state)
    emit()
  }

  const getSnapshot = (): CampaignTemplatePickerSnapshot => snapshot

  const loadCatalogue = async () => {
    const generation = state.loadGeneration + 1
    setState({
      loadGeneration: generation,
      loadStatus: "loading",
      loadError: null,
    })

    try {
      const items = await adapters.loadTemplates()
      if (generation !== state.loadGeneration) {
        return
      }

      if (items.length === 0) {
        setState({
          loadStatus: "error",
          loadError: CAMPAIGN_TEMPLATE_PICKER_COPY.emptyError,
          items: null,
        })
        return
      }

      setState({
        loadStatus: "loaded",
        loadError: null,
        items,
      })
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }

      setState({
        loadStatus: "error",
        loadError: CAMPAIGN_TEMPLATE_PICKER_COPY.loadError,
        items: null,
      })
    }
  }

  return {
    getSnapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    open: async () => {
      setState({ open: true, searchQuery: "" })
      await loadCatalogue()
    },
    close: () => {
      setState({
        open: false,
        searchQuery: "",
        loadStatus: "idle",
        loadError: null,
        items: null,
        loadGeneration: state.loadGeneration + 1,
      })
    },
    retryLoad: async () => {
      if (!state.open) {
        return
      }
      await loadCatalogue()
    },
    setSearchQuery: (query) => {
      setState({ searchQuery: query })
    },
  }
}
