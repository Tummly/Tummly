import { buildOperatorGuestsViewModel } from "@/lib/operatorGuests/buildGuestsViewModel"
import {
  OPERATOR_GUEST_FIXTURES,
  OPERATOR_GUEST_FIXTURES_REFERENCE_MS,
} from "@/lib/operatorGuests/guestFixtures"
import { OPERATOR_GUEST_DEFAULT_SORT_ID } from "@/lib/operatorGuests/guestsPresentation"
import {
  computeVisibleSelectionState,
  formatGuestSelectionLabel,
  sortedSelectionIds,
  toggleAllVisibleInSelection,
  toggleGuestInSelection,
} from "@/lib/operatorGuests/guestSelection"
import type {
  OperatorGuestFixture,
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsViewModel,
} from "@/types/operatorGuests"

export type OperatorGuestsPageSnapshot = {
  viewModel: OperatorGuestsViewModel
  searchQuery: string
  sortId: OperatorGuestSortId
  selectedGuestIds: readonly string[]
  selectedCount: number
  bulkSelectionLabel: string | null
  isAllVisibleSelected: boolean
  isSomeVisibleSelected: boolean
  isGuestSelected: (guestId: string) => boolean
}

export type OperatorGuestsPageModule = {
  subscribe: (listener: () => void) => () => void
  getSnapshot: () => OperatorGuestsPageSnapshot
  setActiveSmartGroupId: (id: OperatorGuestSmartGroupId) => void
  setSearchQuery: (query: string) => void
  setSortId: (id: OperatorGuestSortId) => void
  setPage: (page: number) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  toggleGuestSelection: (guestId: string) => void
  toggleSelectAllVisibleRows: () => void
  clearSelection: () => void
  clearSearchAndFilters: () => void
}

type ModuleState = {
  activeSmartGroupId: OperatorGuestSmartGroupId
  searchQuery: string
  sortId: OperatorGuestSortId
  page: number
}

function buildViewModel(
  guests: readonly OperatorGuestFixture[],
  state: ModuleState,
  nowMs: number,
  pageOverride?: number
): OperatorGuestsViewModel {
  return buildOperatorGuestsViewModel({
    guests,
    activeSmartGroupId: state.activeSmartGroupId,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    page: pageOverride ?? state.page,
    nowMs,
  })
}

function resolveEffectivePage(
  guests: readonly OperatorGuestFixture[],
  state: ModuleState,
  nowMs: number
): { page: number; viewModel: OperatorGuestsViewModel } {
  const viewModel = buildViewModel(guests, state, nowMs)
  const maxPage = Math.max(
    1,
    Math.ceil(viewModel.totalFilteredCount / viewModel.pageSize)
  )
  const effectivePage = Math.min(Math.max(1, state.page), maxPage)

  if (effectivePage === state.page) {
    return { page: effectivePage, viewModel }
  }

  return {
    page: effectivePage,
    viewModel: buildViewModel(guests, state, nowMs, effectivePage),
  }
}

function buildSnapshot(
  guests: readonly OperatorGuestFixture[],
  state: ModuleState,
  selectedGuestIds: ReadonlySet<string>,
  nowMs: number
): OperatorGuestsPageSnapshot {
  const { viewModel } = resolveEffectivePage(guests, state, nowMs)
  const visibleGuestIds = viewModel.tableRows.map((row) => row.id)
  const visibleSelection = computeVisibleSelectionState(
    selectedGuestIds,
    visibleGuestIds
  )
  const selectedCount = selectedGuestIds.size

  return {
    viewModel,
    searchQuery: state.searchQuery,
    sortId: state.sortId,
    selectedGuestIds: sortedSelectionIds(selectedGuestIds),
    selectedCount,
    bulkSelectionLabel: formatGuestSelectionLabel(selectedCount),
    isAllVisibleSelected: visibleSelection.isAllVisibleSelected,
    isSomeVisibleSelected: visibleSelection.isSomeVisibleSelected,
    isGuestSelected(guestId: string) {
      return selectedGuestIds.has(guestId)
    },
  }
}

function selectionSetsEqual(
  left: ReadonlySet<string>,
  right: ReadonlySet<string>
): boolean {
  if (left.size !== right.size) {
    return false
  }

  for (const id of left) {
    if (!right.has(id)) {
      return false
    }
  }

  return true
}

export function createOperatorGuestsPageModule(options?: {
  nowMs?: number
  guests?: readonly OperatorGuestFixture[]
}): OperatorGuestsPageModule {
  const nowMs = options?.nowMs ?? OPERATOR_GUEST_FIXTURES_REFERENCE_MS
  const guests = options?.guests ?? OPERATOR_GUEST_FIXTURES
  let state: ModuleState = {
    activeSmartGroupId: "all-guests",
    searchQuery: "",
    sortId: OPERATOR_GUEST_DEFAULT_SORT_ID,
    page: 1,
  }
  let selectedGuestIds = new Set<string>()
  let snapshot = buildSnapshot(guests, state, selectedGuestIds, nowMs)
  const listeners = new Set<() => void>()

  function publish(): void {
    const resolved = resolveEffectivePage(guests, state, nowMs)
    state = { ...state, page: resolved.page }
    snapshot = buildSnapshot(guests, state, selectedGuestIds, nowMs)
    for (const listener of listeners) {
      listener()
    }
  }

  return {
    subscribe(listener) {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    getSnapshot() {
      return snapshot
    },
    setActiveSmartGroupId(id) {
      if (state.activeSmartGroupId === id) {
        return
      }
      state = { ...state, activeSmartGroupId: id, page: 1 }
      publish()
    },
    setSearchQuery(query) {
      if (state.searchQuery === query) {
        return
      }
      state = { ...state, searchQuery: query, page: 1 }
      publish()
    },
    setSortId(id) {
      if (state.sortId === id) {
        return
      }
      state = { ...state, sortId: id, page: 1 }
      publish()
    },
    setPage(page) {
      if (page < 1 || state.page === page) {
        return
      }
      state = { ...state, page }
      publish()
    },
    goToPreviousPage() {
      if (state.page <= 1) {
        return
      }
      state = { ...state, page: state.page - 1 }
      publish()
    },
    goToNextPage() {
      const { viewModel } = resolveEffectivePage(guests, state, nowMs)
      const maxPage = Math.max(
        1,
        Math.ceil(viewModel.totalFilteredCount / viewModel.pageSize)
      )
      if (state.page >= maxPage) {
        return
      }
      state = { ...state, page: state.page + 1 }
      publish()
    },
    toggleGuestSelection(guestId) {
      const nextSelectedGuestIds = toggleGuestInSelection(
        selectedGuestIds,
        guestId
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    toggleSelectAllVisibleRows() {
      const visibleGuestIds = snapshot.viewModel.tableRows.map((row) => row.id)
      const nextSelectedGuestIds = toggleAllVisibleInSelection(
        selectedGuestIds,
        visibleGuestIds
      )

      if (selectionSetsEqual(selectedGuestIds, nextSelectedGuestIds)) {
        return
      }

      selectedGuestIds = nextSelectedGuestIds
      publish()
    },
    clearSelection() {
      if (selectedGuestIds.size === 0) {
        return
      }

      selectedGuestIds = new Set()
      publish()
    },
    clearSearchAndFilters() {
      if (
        state.searchQuery === "" &&
        state.activeSmartGroupId === "all-guests" &&
        state.page === 1
      ) {
        return
      }

      state = {
        ...state,
        searchQuery: "",
        activeSmartGroupId: "all-guests",
        page: 1,
      }
      publish()
    },
  }
}
