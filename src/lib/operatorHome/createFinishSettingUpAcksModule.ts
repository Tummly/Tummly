import type {
  ChecklistAcksResponse,
  UpdateChecklistAcksRequest,
} from "@/types/dashboard"
import type { OperatorHomeChecklistAcks } from "@/types/operatorHome"

export type FinishSettingUpAckField =
  | "guestFormPreviewed"
  | "qrPlacementGuideViewed"

export type FinishSettingUpAcksSnapshot = {
  loadStatus: "idle" | "loading" | "loaded" | "error"
  guestFormPreviewed: boolean
  qrPlacementGuideViewed: boolean
  acknowledgeBusy: boolean
  acknowledgeError: string | null
}

export type FinishSettingUpAcksAdapters = {
  getChecklistAcks: (locationId: number) => Promise<ChecklistAcksResponse>
  setChecklistAcks: (
    locationId: number,
    body: UpdateChecklistAcksRequest
  ) => Promise<ChecklistAcksResponse>
}

export type FinishSettingUpAcksModule = {
  getSnapshot: () => FinishSettingUpAcksSnapshot
  subscribe: (listener: () => void) => () => void
  load: (locationId: number) => Promise<void>
  acknowledge: (field: FinishSettingUpAckField) => void
  reset: () => void
}

const EMPTY_ACKS: OperatorHomeChecklistAcks = {
  guestFormPreviewed: false,
  qrPlacementGuideViewed: false,
}

const ACKNOWLEDGE_ERROR =
  "Could not save checklist progress. Please try again."

type AcksState = FinishSettingUpAcksSnapshot & {
  locationId: number | null
  loadGeneration: number
}

type AcksAction =
  | { type: "reset" }
  | { type: "load_started"; generation: number; locationId: number }
  | {
      type: "load_succeeded"
      generation: number
      guestFormPreviewed: boolean
      qrPlacementGuideViewed: boolean
    }
  | { type: "load_failed"; generation: number }
  | {
      type: "acknowledge_optimistic"
      field: FinishSettingUpAckField
    }
  | {
      type: "acknowledge_confirmed"
      guestFormPreviewed: boolean
      qrPlacementGuideViewed: boolean
    }
  | {
      type: "acknowledge_rolled_back"
      guestFormPreviewed: boolean
      qrPlacementGuideViewed: boolean
      error: string
    }
  | { type: "acknowledge_busy"; busy: boolean }

function reduce(state: AcksState, action: AcksAction): AcksState {
  switch (action.type) {
    case "reset":
      return {
        ...state,
        loadStatus: "idle",
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        acknowledgeBusy: false,
        acknowledgeError: null,
        locationId: null,
      }
    case "load_started":
      return {
        ...state,
        loadStatus: "loading",
        loadGeneration: action.generation,
        locationId: action.locationId,
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
        acknowledgeBusy: false,
        acknowledgeError: null,
      }
    case "load_succeeded":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "loaded",
        guestFormPreviewed: action.guestFormPreviewed,
        qrPlacementGuideViewed: action.qrPlacementGuideViewed,
      }
    case "load_failed":
      if (action.generation !== state.loadGeneration) {
        return state
      }
      return {
        ...state,
        loadStatus: "error",
        guestFormPreviewed: false,
        qrPlacementGuideViewed: false,
      }
    case "acknowledge_optimistic":
      return {
        ...state,
        [action.field]: true,
        acknowledgeError: null,
      }
    case "acknowledge_confirmed":
      return {
        ...state,
        guestFormPreviewed: action.guestFormPreviewed,
        qrPlacementGuideViewed: action.qrPlacementGuideViewed,
        acknowledgeBusy: false,
        acknowledgeError: null,
      }
    case "acknowledge_rolled_back":
      return {
        ...state,
        guestFormPreviewed: action.guestFormPreviewed,
        qrPlacementGuideViewed: action.qrPlacementGuideViewed,
        acknowledgeBusy: false,
        acknowledgeError: action.error,
      }
    case "acknowledge_busy":
      return { ...state, acknowledgeBusy: action.busy }
    default:
      return state
  }
}

function toSnapshot(state: AcksState): FinishSettingUpAcksSnapshot {
  return {
    loadStatus: state.loadStatus,
    guestFormPreviewed: state.guestFormPreviewed,
    qrPlacementGuideViewed: state.qrPlacementGuideViewed,
    acknowledgeBusy: state.acknowledgeBusy,
    acknowledgeError: state.acknowledgeError,
  }
}

export function createInMemoryFinishSettingUpAcksAdapters(
  initial: Record<number, OperatorHomeChecklistAcks> = {}
): FinishSettingUpAcksAdapters {
  const store = new Map<number, OperatorHomeChecklistAcks>(
    Object.entries(initial).map(([locationId, acks]) => [
      Number(locationId),
      { ...acks },
    ])
  )

  return {
    getChecklistAcks: async (locationId) => {
      const acks = store.get(locationId) ?? EMPTY_ACKS
      return {
        success: true,
        locationId,
        guestFormPreviewed: acks.guestFormPreviewed,
        qrPlacementGuideViewed: acks.qrPlacementGuideViewed,
        guestFormPreviewedAt: acks.guestFormPreviewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
        qrPlacementGuideViewedAt: acks.qrPlacementGuideViewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
      }
    },
    setChecklistAcks: async (locationId, body) => {
      const previous = store.get(locationId) ?? { ...EMPTY_ACKS }
      const next: OperatorHomeChecklistAcks = {
        guestFormPreviewed:
          body.guestFormPreviewed ?? previous.guestFormPreviewed,
        qrPlacementGuideViewed:
          body.qrPlacementGuideViewed ?? previous.qrPlacementGuideViewed,
      }
      store.set(locationId, next)
      return {
        success: true,
        locationId,
        guestFormPreviewed: next.guestFormPreviewed,
        qrPlacementGuideViewed: next.qrPlacementGuideViewed,
        guestFormPreviewedAt: next.guestFormPreviewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
        qrPlacementGuideViewedAt: next.qrPlacementGuideViewed
          ? "2026-07-14T12:00:00.000Z"
          : null,
      }
    },
  }
}

export function createFinishSettingUpAcksModule(
  adapters: FinishSettingUpAcksAdapters
): FinishSettingUpAcksModule {
  let state: AcksState = {
    loadStatus: "idle",
    guestFormPreviewed: false,
    qrPlacementGuideViewed: false,
    acknowledgeBusy: false,
    acknowledgeError: null,
    locationId: null,
    loadGeneration: 0,
  }

  let snapshot = toSnapshot(state)
  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = toSnapshot(state)
    emit()
  }

  const dispatch = (action: AcksAction) => {
    state = reduce(state, action)
    publish()
  }

  return {
    getSnapshot: () => snapshot,
    subscribe: (listener) => {
      listeners.add(listener)
      return () => {
        listeners.delete(listener)
      }
    },
    load: async (locationId) => {
      const generation = state.loadGeneration + 1
      dispatch({ type: "load_started", generation, locationId })

      try {
        const result = await adapters.getChecklistAcks(locationId)
        dispatch({
          type: "load_succeeded",
          generation,
          guestFormPreviewed: result.guestFormPreviewed,
          qrPlacementGuideViewed: result.qrPlacementGuideViewed,
        })
      } catch {
        dispatch({ type: "load_failed", generation })
      }
    },
    acknowledge: (field) => {
      if (state.locationId == null || state.acknowledgeBusy) {
        return
      }
      if (state[field]) {
        return
      }

      const previous = {
        guestFormPreviewed: state.guestFormPreviewed,
        qrPlacementGuideViewed: state.qrPlacementGuideViewed,
      }
      const locationId = state.locationId

      dispatch({ type: "acknowledge_optimistic", field })
      dispatch({ type: "acknowledge_busy", busy: true })

      void adapters
        .setChecklistAcks(locationId, { [field]: true })
        .then((result) => {
          dispatch({
            type: "acknowledge_confirmed",
            guestFormPreviewed: result.guestFormPreviewed,
            qrPlacementGuideViewed: result.qrPlacementGuideViewed,
          })
        })
        .catch(() => {
          dispatch({
            type: "acknowledge_rolled_back",
            guestFormPreviewed: previous.guestFormPreviewed,
            qrPlacementGuideViewed: previous.qrPlacementGuideViewed,
            error: ACKNOWLEDGE_ERROR,
          })
        })
    },
    reset: () => {
      dispatch({ type: "reset" })
    },
  }
}
