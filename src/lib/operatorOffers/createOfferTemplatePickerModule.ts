import { OFFER_TEMPLATE_PICKER_COPY } from "@/lib/operatorOffers/offerTemplatePickerPresentation"
import type { OfferTemplateSeedItem } from "@/lib/operatorOffers/offerTemplateSeed"

export type OfferTemplatePickerAdapters = {
  loadTemplates: () => Promise<OfferTemplateSeedItem[]>
}

export type OfferTemplatePickerCardViewModel = {
  id: string
  title: string
  summary: string
  suggestedBenefit?: string
  suggestedValidity?: string
  suggestedSource?: string
  offerTitlePlaceholder?: string
  startingDescription: string
  /** Preview omitted for MVP until a Preview Figma exists. */
  previewDisabled: true
  useTemplateEnabled: true
}

export type OfferTemplatePickerViewModel = {
  title: string
  subtitle: string
  searchPlaceholder: string
  searchQuery: string
  cards: OfferTemplatePickerCardViewModel[]
  showSearchMiss: boolean
}

export type OfferTemplatePickerSnapshot = {
  open: boolean
  loadStatus: "idle" | "loading" | "loaded" | "error"
  loadError: string | null
  viewModel: OfferTemplatePickerViewModel | null
}

export type OfferTemplatePickerModule = {
  getSnapshot: () => OfferTemplatePickerSnapshot
  subscribe: (listener: () => void) => () => void
  open: () => Promise<void>
  close: () => void
  retryLoad: () => Promise<void>
  setSearchQuery: (query: string) => void
}

type PickerState = {
  open: boolean
  loadStatus: OfferTemplatePickerSnapshot["loadStatus"]
  loadError: string | null
  searchQuery: string
  items: OfferTemplateSeedItem[] | null
  loadGeneration: number
}

function matchesSearch(item: OfferTemplateSeedItem, query: string): boolean {
  const q = query.trim().toLowerCase()
  if (q.length === 0) {
    return true
  }

  const haystack = [
    item.title,
    item.summary,
    item.suggestedBenefit,
    item.suggestedValidity,
    item.suggestedSource,
    item.offerTitlePlaceholder,
    item.startingDescription,
  ]
    .filter((value): value is string => value != null && value.length > 0)
    .join(" ")
    .toLowerCase()

  return haystack.includes(q)
}

function toCard(item: OfferTemplateSeedItem): OfferTemplatePickerCardViewModel {
  return {
    id: item.id,
    title: item.title,
    summary: item.summary,
    suggestedBenefit: item.suggestedBenefit,
    suggestedValidity: item.suggestedValidity,
    suggestedSource: item.suggestedSource,
    offerTitlePlaceholder: item.offerTitlePlaceholder,
    startingDescription: item.startingDescription,
    previewDisabled: true,
    useTemplateEnabled: true,
  }
}

function buildViewModel(
  items: OfferTemplateSeedItem[],
  searchQuery: string
): OfferTemplatePickerViewModel {
  const filtered = items.filter((item) => matchesSearch(item, searchQuery))
  return {
    title: OFFER_TEMPLATE_PICKER_COPY.title,
    subtitle: OFFER_TEMPLATE_PICKER_COPY.subtitle,
    searchPlaceholder: OFFER_TEMPLATE_PICKER_COPY.searchPlaceholder,
    searchQuery,
    cards: filtered.map(toCard),
    showSearchMiss: searchQuery.trim().length > 0 && filtered.length === 0,
  }
}

function projectSnapshot(state: PickerState): OfferTemplatePickerSnapshot {
  const viewModel =
    state.items != null ? buildViewModel(state.items, state.searchQuery) : null

  return {
    open: state.open,
    loadStatus: state.loadStatus,
    loadError: state.loadError,
    viewModel,
  }
}

export function createOfferTemplatePickerModule(
  adapters: OfferTemplatePickerAdapters
): OfferTemplatePickerModule {
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

  const getSnapshot = (): OfferTemplatePickerSnapshot => snapshot

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
          loadError: OFFER_TEMPLATE_PICKER_COPY.emptyError,
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
        loadError: OFFER_TEMPLATE_PICKER_COPY.loadError,
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
      // Keep the last loaded catalogue during Dialog exit animation.
      const stillLoading = state.loadStatus === "loading"
      setState({
        open: false,
        searchQuery: "",
        loadGeneration: state.loadGeneration + 1,
        ...(stillLoading
          ? {
              loadStatus: "idle" as const,
              loadError: null,
              items: null,
            }
          : {}),
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
