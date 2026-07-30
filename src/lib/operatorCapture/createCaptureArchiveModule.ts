import {
  buildCaptureArchiveList,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  duplicateDigitalGuestLinkName,
  type CaptureArchiveFilters,
  type CaptureArchiveListResult,
  type CaptureArchiveSortId,
} from "@/lib/operatorCapture/buildCaptureArchive"
import {
  buildCaptureArchiveListQueryParams,
  CAPTURE_ARCHIVE_PAGE_SIZE,
  type CaptureArchiveListQueryParams,
} from "@/lib/operatorCapture/captureArchiveListQueryParams"
import {
  buildRestoreConfirm,
  type RestoreConfirmView,
} from "@/lib/operatorCapture/buildRestoreConfirm"
import type {
  CaptureArchivedPlacementItem,
  CaptureArchivedPlacementsResponse,
  CaptureDigitalGuestLinkChannel,
  CapturePlacementStatus,
  CaptureQrCodeStatus,
} from "@/types/dashboard"

export type CaptureArchiveLocationOption = {
  id: number
  locationName: string
}

export type CreateDigitalGuestLinkPrefill = {
  linkName: string
  channel: CaptureDigitalGuestLinkChannel
  status: CapturePlacementStatus
  locationId: number
}

export type OperatorCaptureArchiveView = CaptureArchiveListResult & {
  searchQuery: string
  filters: CaptureArchiveFilters
  sort: CaptureArchiveSortId
  returnPath: string | null
  showLocationFilter: boolean
  archiverOptions: readonly string[]
  locationOptions: readonly { id: number; label: string }[]
  createPrefill: CreateDigitalGuestLinkPrefill | null
}

export type RestoreConfirmSnapshot = {
  isOpen: boolean
  details: RestoreConfirmView | null
}

export type CaptureArchiveSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  restoreConfirm: RestoreConfirmSnapshot
  archive: OperatorCaptureArchiveView | null
}

/** Fact shape returned after a successful restore for page-module orchestration. */
export type RestoredArchivePlacementFact = {
  qrCodeId: number
  qrType: CaptureArchivedPlacementItem["qrType"]
  status: Extract<CaptureQrCodeStatus, "Paused">
  linkName: string | null | undefined
  channel: CaptureArchivedPlacementItem["channel"]
  internalDescription: string | null | undefined
  qrLinkUrl: string
  qrScans: number
  feedbackSubmitted: number
  marketingOptIns: 0
  offerClaims: 0
  lastScanAt: string | null
}

export type ConfirmRestoreResult =
  | {
      outcome: "restored"
      toastMessage: string
      restoredFact: RestoredArchivePlacementFact
      locationId: number
    }
  | "conflict"
  | "failed"
  | "noop"

export type CaptureArchiveAdapters = {
  getArchivedCapturePlacements: (
    params: CaptureArchiveListQueryParams
  ) => Promise<CaptureArchivedPlacementsResponse>
  restoreCapturePlacement: (
    locationId: number,
    qrCodeId: number
  ) => Promise<
    | { ok: true; qrCodeId: number; status: "Paused"; qrLinkUrl: string }
    | { ok: false; reason: "conflict" | "failed"; message: string }
  >
  onArchiveLoadError?: (message: string) => void
  onPlacementActionError?: (message: string) => void
  nowMs?: () => number
  /** Search debounce; defaults to 300ms (Guests-aligned). */
  debounceMs?: number
}

export type CaptureArchiveModule = {
  getSnapshot: () => CaptureArchiveSnapshot
  subscribe: (listener: () => void) => () => void
  enter: (input: {
    returnPath: string
    preselectedLocationId?: number | null
    showLocationFilter: boolean
    locations: readonly CaptureArchiveLocationOption[]
  }) => Promise<void>
  reload: () => Promise<void>
  setSearchQuery: (query: string) => void
  setFilters: (filters: CaptureArchiveFilters) => void
  setSort: (sort: CaptureArchiveSortId) => void
  setPage: (page: number) => void
  goToPreviousPage: () => void
  goToNextPage: () => void
  clearSearchAndFilters: () => void
  requestRestore: (qrCodeId: number) => "opened" | "noop"
  cancelRestoreConfirm: () => void
  confirmRestore: () => Promise<ConfirmRestoreResult>
  requestDuplicateAsNew: (qrCodeId: number) => "opened" | "noop"
  clearCreatePrefill: () => void
  getArchivedPlacement: (
    qrCodeId: number
  ) => CaptureArchivedPlacementItem | null
}

type ArchiveState = {
  loadStatus: CaptureArchiveSnapshot["loadStatus"]
  restoreConfirmIsOpen: boolean
  restoreConfirmDetails: RestoreConfirmView | null
  pagePlacements: CaptureArchivedPlacementItem[] | null
  totalCount: number
  page: number
  pageSize: number
  archiverOptions: readonly string[]
  searchQuery: string
  filters: CaptureArchiveFilters
  sort: CaptureArchiveSortId
  returnPath: string | null
  showLocationFilter: boolean
  locations: readonly CaptureArchiveLocationOption[]
  createPrefill: CreateDigitalGuestLinkPrefill | null
  loadGeneration: number
}

const ARCHIVE_LOAD_ERROR_MESSAGE =
  "Could not load archived placements. Please try again."

const RESTORE_ACTION_ERROR_MESSAGE =
  "Could not restore QR code. Please try again."

const DEFAULT_SEARCH_DEBOUNCE_MS = 300

function closedRestoreConfirm(): RestoreConfirmSnapshot {
  return {
    isOpen: false,
    details: null,
  }
}

function clearRestoreConfirmState() {
  return {
    restoreConfirmIsOpen: false as const,
    restoreConfirmDetails: null,
  }
}

function buildArchiveView(
  state: ArchiveState,
  nowMs: number
): OperatorCaptureArchiveView | null {
  if (state.pagePlacements == null && state.loadStatus === "idle") {
    return null
  }
  const placements = state.pagePlacements ?? []
  const list = buildCaptureArchiveList({
    placements,
    totalCount: state.totalCount,
    page: state.page,
    pageSize: state.pageSize,
    searchQuery: state.searchQuery,
    filters: state.filters,
    nowMs,
    showLocationFilter: state.showLocationFilter,
  })

  return {
    ...list,
    searchQuery: state.searchQuery,
    filters: state.filters,
    sort: state.sort,
    returnPath: state.returnPath,
    showLocationFilter: state.showLocationFilter,
    archiverOptions: state.archiverOptions,
    locationOptions: state.locations.map((l) => ({
      id: l.id,
      label: l.locationName,
    })),
    createPrefill: state.createPrefill,
  }
}

function toSnapshot(
  state: ArchiveState,
  nowMs: number
): CaptureArchiveSnapshot {
  const restoreConfirm =
    !state.restoreConfirmIsOpen || state.restoreConfirmDetails == null
      ? closedRestoreConfirm()
      : {
          isOpen: true,
          details: state.restoreConfirmDetails,
        }

  return {
    loadStatus: state.loadStatus,
    restoreConfirm,
    archive: buildArchiveView(state, nowMs),
  }
}

/**
 * Capture Archive module — refetch-driven list consumer of the server-paged
 * Capture Archive list module (ADR-0024). Publishes only to its own subscribers
 * (no live Capture relay).
 */
export function createCaptureArchiveModule(
  adapters: CaptureArchiveAdapters
): CaptureArchiveModule {
  const nowMs = adapters.nowMs ?? (() => Date.now())
  const debounceMs = adapters.debounceMs ?? DEFAULT_SEARCH_DEBOUNCE_MS

  let state: ArchiveState = {
    loadStatus: "idle",
    restoreConfirmIsOpen: false,
    restoreConfirmDetails: null,
    pagePlacements: null,
    totalCount: 0,
    page: 1,
    pageSize: CAPTURE_ARCHIVE_PAGE_SIZE,
    archiverOptions: [],
    searchQuery: "",
    filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
    sort: "recently-archived",
    returnPath: null,
    showLocationFilter: false,
    locations: [],
    createPrefill: null,
    loadGeneration: 0,
  }

  let snapshot = toSnapshot(state, nowMs())
  const listeners = new Set<() => void>()
  let searchDebounceTimer: ReturnType<typeof setTimeout> | null = null

  const publish = () => {
    snapshot = toSnapshot(state, nowMs())
    for (const listener of listeners) {
      listener()
    }
  }

  const clearSearchDebounce = () => {
    if (searchDebounceTimer != null) {
      clearTimeout(searchDebounceTimer)
      searchDebounceTimer = null
    }
  }

  const fetchList = async (options?: {
    clearPlacementsOnError?: boolean
    quiet?: boolean
  }) => {
    const generation = state.loadGeneration + 1
    const isQuiet = options?.quiet === true && state.pagePlacements != null

    state = {
      ...state,
      loadStatus: isQuiet ? state.loadStatus : "loading",
      loadGeneration: generation,
    }
    publish()

    try {
      const response = await adapters.getArchivedCapturePlacements(
        buildCaptureArchiveListQueryParams({
          q: state.searchQuery,
          filters: state.filters,
          sort: state.sort,
          page: state.page,
          pageSize: state.pageSize,
          now: new Date(nowMs()),
        })
      )
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        pagePlacements: response.placements,
        totalCount: response.totalCount,
        page: response.page,
        pageSize: response.pageSize || CAPTURE_ARCHIVE_PAGE_SIZE,
        archiverOptions: response.archiverOptions ?? [],
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        ...(options?.clearPlacementsOnError === false
          ? {}
          : {
              pagePlacements: [] as CaptureArchivedPlacementItem[],
              totalCount: 0,
              archiverOptions: [] as string[],
            }),
      }
      publish()
      adapters.onArchiveLoadError?.(ARCHIVE_LOAD_ERROR_MESSAGE)
    }
  }

  const scheduleSearchFetch = () => {
    clearSearchDebounce()
    searchDebounceTimer = setTimeout(() => {
      searchDebounceTimer = null
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    }, debounceMs)
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    async enter(input) {
      clearSearchDebounce()
      const preselected = input.preselectedLocationId
      state = {
        ...state,
        returnPath: input.returnPath,
        showLocationFilter: input.showLocationFilter,
        locations: input.locations,
        searchQuery: "",
        sort: "recently-archived",
        page: 1,
        pageSize: CAPTURE_ARCHIVE_PAGE_SIZE,
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds:
            input.showLocationFilter && preselected != null ? [preselected] : [],
        },
        createPrefill: null,
        ...clearRestoreConfirmState(),
      }
      await fetchList()
    },
    async reload() {
      clearSearchDebounce()
      await fetchList({ clearPlacementsOnError: false })
    },
    setSearchQuery(query) {
      if (state.searchQuery === query) {
        return
      }
      state = {
        ...state,
        searchQuery: query,
        page: 1,
      }
      publish()
      scheduleSearchFetch()
    },
    setFilters(filters) {
      clearSearchDebounce()
      state = {
        ...state,
        filters,
        page: 1,
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    setSort(sort) {
      if (state.sort === sort) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        sort,
        page: 1,
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    setPage(page) {
      if (page < 1 || state.page === page) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        page,
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    goToPreviousPage() {
      if (state.page <= 1) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        page: state.page - 1,
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    goToNextPage() {
      const maxPage = Math.max(
        1,
        Math.ceil(state.totalCount / state.pageSize)
      )
      if (state.page >= maxPage) {
        return
      }
      clearSearchDebounce()
      state = {
        ...state,
        page: state.page + 1,
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    clearSearchAndFilters() {
      clearSearchDebounce()
      state = {
        ...state,
        searchQuery: "",
        page: 1,
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds: [],
        },
      }
      void fetchList({ quiet: true, clearPlacementsOnError: false })
    },
    requestRestore(qrCodeId) {
      const fact = state.pagePlacements?.find((item) => item.qrCodeId === qrCodeId)
      if (fact == null || !fact.canRestore) {
        return "noop"
      }
      state = {
        ...state,
        restoreConfirmIsOpen: true,
        restoreConfirmDetails: buildRestoreConfirm(fact),
      }
      publish()
      return "opened"
    },
    cancelRestoreConfirm() {
      state = {
        ...state,
        ...clearRestoreConfirmState(),
      }
      publish()
    },
    async confirmRestore() {
      const details = state.restoreConfirmDetails
      if (!state.restoreConfirmIsOpen || details == null) {
        return "noop"
      }

      const prior = state.pagePlacements?.find(
        (item) => item.qrCodeId === details.qrCodeId
      )

      const result = await adapters.restoreCapturePlacement(
        details.locationId,
        details.qrCodeId
      )
      if (!result.ok) {
        if (result.reason === "conflict") {
          adapters.onPlacementActionError?.(result.message)
          return "conflict"
        }
        adapters.onPlacementActionError?.(
          result.message || RESTORE_ACTION_ERROR_MESSAGE
        )
        return "failed"
      }

      const restoredFact: RestoredArchivePlacementFact = {
        qrCodeId: result.qrCodeId,
        qrType: prior?.qrType ?? "CounterCard",
        status: "Paused",
        linkName: prior?.linkName ?? null,
        channel: prior?.channel ?? null,
        internalDescription: prior?.internalDescription ?? null,
        qrLinkUrl: result.qrLinkUrl,
        qrScans: prior?.qrScans ?? 0,
        feedbackSubmitted: prior?.feedbackSubmitted ?? 0,
        marketingOptIns: 0,
        offerClaims: 0,
        lastScanAt: prior?.lastScanAt ?? null,
      }

      const nextPlacements =
        state.pagePlacements?.filter(
          (item) => item.qrCodeId !== details.qrCodeId
        ) ?? []
      const nextTotalCount = Math.max(0, state.totalCount - 1)
      const maxPage = Math.max(1, Math.ceil(nextTotalCount / state.pageSize))
      const nextPage = Math.min(state.page, maxPage)

      state = {
        ...state,
        pagePlacements: nextPlacements,
        totalCount: nextTotalCount,
        page: nextPage,
        ...clearRestoreConfirmState(),
      }
      publish()

      // If Restore emptied this page but other pages still have rows, refetch.
      if (nextPlacements.length === 0 && nextTotalCount > 0) {
        void fetchList({ quiet: true, clearPlacementsOnError: false })
      }

      return {
        outcome: "restored",
        toastMessage: details.successToastMessage,
        restoredFact,
        locationId: details.locationId,
      }
    },
    requestDuplicateAsNew(qrCodeId) {
      const fact = state.pagePlacements?.find((item) => item.qrCodeId === qrCodeId)
      if (
        fact == null
        || fact.qrType !== "DigitalGuestLink"
        || fact.channel == null
      ) {
        return "noop"
      }
      state = {
        ...state,
        createPrefill: {
          linkName: duplicateDigitalGuestLinkName(fact.linkName ?? ""),
          channel: fact.channel,
          status: "Active",
          locationId: fact.locationId,
        },
      }
      publish()
      return "opened"
    },
    clearCreatePrefill() {
      state = {
        ...state,
        createPrefill: null,
      }
      publish()
    },
    getArchivedPlacement(qrCodeId) {
      return (
        state.pagePlacements?.find((item) => item.qrCodeId === qrCodeId) ?? null
      )
    },
  }
}
