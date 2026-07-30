import {
  buildCaptureArchiveList,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  duplicateDigitalGuestLinkName,
  type CaptureArchiveFilters,
  type CaptureArchiveListResult,
  type CaptureArchiveSortId,
} from "@/lib/operatorCapture/buildCaptureArchive"
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
  getArchivedCapturePlacements: () => Promise<CaptureArchivedPlacementsResponse>
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
  archivedFacts: CaptureArchivedPlacementItem[] | null
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
  if (state.archivedFacts == null && state.loadStatus === "idle") {
    return null
  }
  const facts = state.archivedFacts ?? []
  const list = buildCaptureArchiveList({
    facts,
    searchQuery: state.searchQuery,
    filters: state.filters,
    sort: state.sort,
    nowMs,
    showLocationFilter: state.showLocationFilter,
  })
  const archiverOptions = [
    ...new Set(
      facts
        .map((f) => f.archivedByDisplayName?.trim())
        .filter((name): name is string => name != null && name !== "")
    ),
  ].sort((a, b) => a.localeCompare(b))

  return {
    ...list,
    searchQuery: state.searchQuery,
    filters: state.filters,
    sort: state.sort,
    returnPath: state.returnPath,
    showLocationFilter: state.showLocationFilter,
    archiverOptions,
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
 * Capture Archive module — load, list interaction, Restore confirm, archive-row
 * commands. Publishes only to its own subscribers (no live Capture relay).
 */
export function createCaptureArchiveModule(
  adapters: CaptureArchiveAdapters
): CaptureArchiveModule {
  const nowMs = adapters.nowMs ?? (() => Date.now())

  let state: ArchiveState = {
    loadStatus: "idle",
    restoreConfirmIsOpen: false,
    restoreConfirmDetails: null,
    archivedFacts: null,
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

  const publish = () => {
    snapshot = toSnapshot(state, nowMs())
    for (const listener of listeners) {
      listener()
    }
  }

  const loadArchivedFacts = async (
    generation: number,
    options?: { clearFactsOnError?: boolean }
  ) => {
    try {
      const response = await adapters.getArchivedCapturePlacements()
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "loaded",
        archivedFacts: response.placements,
      }
      publish()
    } catch {
      if (generation !== state.loadGeneration) {
        return
      }
      state = {
        ...state,
        loadStatus: "error",
        ...(options?.clearFactsOnError === false
          ? {}
          : { archivedFacts: [] as CaptureArchivedPlacementItem[] }),
      }
      publish()
      adapters.onArchiveLoadError?.(ARCHIVE_LOAD_ERROR_MESSAGE)
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
    async enter(input) {
      const generation = ++state.loadGeneration
      const preselected = input.preselectedLocationId
      state = {
        ...state,
        loadStatus: "loading",
        returnPath: input.returnPath,
        showLocationFilter: input.showLocationFilter,
        locations: input.locations,
        searchQuery: "",
        sort: "recently-archived",
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds:
            input.showLocationFilter && preselected != null ? [preselected] : [],
        },
        createPrefill: null,
        ...clearRestoreConfirmState(),
      }
      publish()
      await loadArchivedFacts(generation)
    },
    async reload() {
      const generation = ++state.loadGeneration
      state = {
        ...state,
        loadStatus: "loading",
      }
      publish()
      await loadArchivedFacts(generation, { clearFactsOnError: false })
    },
    setSearchQuery(query) {
      state = {
        ...state,
        searchQuery: query,
      }
      publish()
    },
    setFilters(filters) {
      state = {
        ...state,
        filters,
      }
      publish()
    },
    setSort(sort) {
      state = {
        ...state,
        sort,
      }
      publish()
    },
    clearSearchAndFilters() {
      state = {
        ...state,
        searchQuery: "",
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds: [],
        },
      }
      publish()
    },
    requestRestore(qrCodeId) {
      const fact = state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId)
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

      const prior = state.archivedFacts?.find(
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

      const nextArchived =
        state.archivedFacts?.filter(
          (item) => item.qrCodeId !== details.qrCodeId
        ) ?? []

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

      state = {
        ...state,
        archivedFacts: nextArchived,
        ...clearRestoreConfirmState(),
      }
      publish()
      return {
        outcome: "restored",
        toastMessage: details.successToastMessage,
        restoredFact,
        locationId: details.locationId,
      }
    },
    requestDuplicateAsNew(qrCodeId) {
      const fact = state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId)
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
        state.archivedFacts?.find((item) => item.qrCodeId === qrCodeId) ?? null
      )
    },
  }
}
