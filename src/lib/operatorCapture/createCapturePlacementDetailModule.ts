import {
  buildPlacementDetailDrawer,
  PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH,
  type PlacementDetailDrawerView,
} from "@/lib/operatorCapture/buildPlacementDetailDrawer"
import type {
  CapturePlacementItem,
  CaptureQrCodeStatus,
} from "@/types/dashboard"

export type PlacementDetailFact = Omit<CapturePlacementItem, "status"> & {
  status: CaptureQrCodeStatus
}

export type PlacementDetailDrawerSnapshot = {
  isOpen: boolean
  selectedQrCodeId: number | null
  details: PlacementDetailDrawerView | null
}

export type CapturePlacementDetailOpenContext = {
  isOpen: boolean
  qrCodeId: number | null
  fact: PlacementDetailFact | null
  locationId: number | null
  descriptionDraft: string
}

export type CapturePlacementDetailAdapters = {
  nowMs?: () => number
}

export type OpenFromLiveInput = {
  fact: PlacementDetailFact
  locationName: string
  locationCapturePaused: boolean
}

export type OpenFromArchiveInput = {
  fact: PlacementDetailFact
  locationId: number
  locationName: string
}

export type PatchFactInput = {
  fact: PlacementDetailFact
  locationName: string
  locationCapturePaused: boolean
  descriptionDraft?: string
  locationId?: number | null
}

export type CapturePlacementDetailModule = {
  getSnapshot: () => PlacementDetailDrawerSnapshot
  subscribe: (listener: () => void) => () => void
  getOpenContext: () => CapturePlacementDetailOpenContext
  openFromLive: (input: OpenFromLiveInput) => "opened"
  openFromArchive: (input: OpenFromArchiveInput) => "opened"
  close: () => void
  reset: () => void
  setDescriptionDraft: (value: string) => void
  patchFact: (input: PatchFactInput) => void
}

type DetailState = {
  isOpen: boolean
  selectedQrCodeId: number | null
  descriptionDraft: string
  fact: PlacementDetailFact | null
  locationId: number | null
  locationName: string
  locationCapturePaused: boolean
}

function closedSnapshot(): PlacementDetailDrawerSnapshot {
  return {
    isOpen: false,
    selectedQrCodeId: null,
    details: null,
  }
}

function clearDetailState(): DetailState {
  return {
    isOpen: false,
    selectedQrCodeId: null,
    descriptionDraft: "",
    fact: null,
    locationId: null,
    locationName: "",
    locationCapturePaused: false,
  }
}

function toSnapshot(
  state: DetailState,
  nowMs: number
): PlacementDetailDrawerSnapshot {
  if (!state.isOpen || state.selectedQrCodeId == null || state.fact == null) {
    return {
      isOpen: state.isOpen,
      selectedQrCodeId: state.selectedQrCodeId,
      details: null,
    }
  }

  return {
    isOpen: true,
    selectedQrCodeId: state.selectedQrCodeId,
    details: buildPlacementDetailDrawer({
      fact: state.fact,
      locationName: state.locationName,
      descriptionDraft: state.descriptionDraft,
      locationCapturePaused: state.locationCapturePaused,
      nowMs,
    }),
  }
}

/**
 * Capture Placement Detail module — open/close, draft, and drawer projection.
 * Publishes only to its own subscribers (no live Capture relay).
 */
export function createCapturePlacementDetailModule(
  adapters: CapturePlacementDetailAdapters = {}
): CapturePlacementDetailModule {
  const nowMs = adapters.nowMs ?? (() => Date.now())

  let state: DetailState = clearDetailState()
  let snapshot = closedSnapshot()
  const listeners = new Set<() => void>()

  const publish = () => {
    snapshot = toSnapshot(state, nowMs())
    for (const listener of listeners) {
      listener()
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
    getOpenContext: () => ({
      isOpen: state.isOpen,
      qrCodeId: state.selectedQrCodeId,
      fact: state.fact,
      locationId: state.locationId,
      descriptionDraft: state.descriptionDraft,
    }),
    openFromLive(input) {
      state = {
        isOpen: true,
        selectedQrCodeId: input.fact.qrCodeId,
        descriptionDraft: input.fact.internalDescription ?? "",
        fact: input.fact,
        locationId: null,
        locationName: input.locationName,
        locationCapturePaused: input.locationCapturePaused,
      }
      publish()
      return "opened"
    },
    openFromArchive(input) {
      state = {
        isOpen: true,
        selectedQrCodeId: input.fact.qrCodeId,
        descriptionDraft: input.fact.internalDescription ?? "",
        fact: input.fact,
        locationId: input.locationId,
        locationName: input.locationName,
        locationCapturePaused: false,
      }
      publish()
      return "opened"
    },
    close() {
      if (!state.isOpen) {
        return
      }
      state = clearDetailState()
      publish()
    },
    reset() {
      state = clearDetailState()
      publish()
    },
    setDescriptionDraft(value) {
      if (!state.isOpen) {
        return
      }
      state = {
        ...state,
        descriptionDraft: value.slice(
          0,
          PLACEMENT_INTERNAL_DESCRIPTION_MAX_LENGTH
        ),
      }
      publish()
    },
    patchFact(input) {
      if (!state.isOpen || state.selectedQrCodeId !== input.fact.qrCodeId) {
        return
      }
      state = {
        ...state,
        fact: input.fact,
        locationName: input.locationName,
        locationCapturePaused: input.locationCapturePaused,
        descriptionDraft:
          input.descriptionDraft
          ?? state.descriptionDraft,
        locationId:
          input.locationId !== undefined
            ? input.locationId
            : state.locationId,
      }
      publish()
    },
  }
}
