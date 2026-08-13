import { resolveInitialLocationId } from "@/lib/operatorHome/buildHomeViewModel"
import { parseOperatorProfile } from "@/lib/operatorHome/parseOperatorProfile"
import type { LocationItem, LocationsResponse } from "@/types/dashboard"

export type OperatorWorkspaceMode = "single" | "multi"

export type OperatorWorkspaceSnapshot = {
  status: "idle" | "loading" | "loaded" | "error"
  mode: OperatorWorkspaceMode
  locations: LocationItem[]
  selectedLocationId: number | null
  restaurantName: string
  operatorDisplayName: string
  activationExpiresAt: string | null
  selfRole: string | null
  locationSwitcherInteractive: boolean
}

export type OperatorWorkspaceAdapters = {
  getLocations: () => Promise<LocationsResponse>
  fetchCurrentUser: () => Promise<unknown>
  getPersistedLocationId: () => number | null
  persistSelectedLocation: (locationId: number) => void
}

export type OperatorWorkspaceSession = {
  getSnapshot: () => OperatorWorkspaceSnapshot
  subscribe: (listener: () => void) => () => void
  load: (input?: { queryLocationId?: number | null }) => Promise<void>
  retry: () => Promise<void>
  selectLocation: (locationId: number) => void
  preferLocationFromQuery: (queryLocationId: number | null) => void
}

type WorkspaceState = OperatorWorkspaceSnapshot & {
  lastQueryLocationId: number | null
}

type WorkspaceAction =
  | { type: "load_started" }
  | {
      type: "load_succeeded"
      locations: LocationItem[]
      selectedLocationId: number | null
      restaurantName: string
      operatorDisplayName: string
      activationExpiresAt: string | null
      selfRole: string | null
      queryLocationId: number | null
    }
  | { type: "load_failed" }
  | { type: "select_location"; locationId: number }
  | { type: "remember_query_location"; queryLocationId: number | null }

function isOwnedLocationId(
  locations: LocationItem[],
  locationId: number | null
): locationId is number {
  return (
    locationId != null &&
    locations.some((location) => location.id === locationId)
  )
}

function reduce(
  state: WorkspaceState,
  action: WorkspaceAction
): WorkspaceState {
  switch (action.type) {
    case "load_started":
      return { ...state, status: "loading" }
    case "load_succeeded":
      return {
        ...state,
        status: "loaded",
        locations: action.locations,
        selectedLocationId: action.selectedLocationId,
        restaurantName: action.restaurantName,
        operatorDisplayName: action.operatorDisplayName,
        activationExpiresAt: action.activationExpiresAt,
        selfRole: action.selfRole,
        lastQueryLocationId: action.queryLocationId,
      }
    case "load_failed":
      return { ...state, status: "error" }
    case "select_location":
      return { ...state, selectedLocationId: action.locationId }
    case "remember_query_location":
      return { ...state, lastQueryLocationId: action.queryLocationId }
    default:
      return state
  }
}

export function createOperatorWorkspaceSession(
  config: { mode: OperatorWorkspaceMode },
  adapters: OperatorWorkspaceAdapters
): OperatorWorkspaceSession {
  let state: WorkspaceState = {
    status: "idle",
    mode: config.mode,
    locations: [],
    selectedLocationId: null,
    restaurantName: "",
    operatorDisplayName: "Operator",
    activationExpiresAt: null,
    selfRole: null,
    locationSwitcherInteractive: config.mode === "multi",
    lastQueryLocationId: null,
  }

  let snapshot: OperatorWorkspaceSnapshot = {
    status: state.status,
    mode: state.mode,
    locations: state.locations,
    selectedLocationId: state.selectedLocationId,
    restaurantName: state.restaurantName,
    operatorDisplayName: state.operatorDisplayName,
    activationExpiresAt: state.activationExpiresAt,
    selfRole: state.selfRole,
    locationSwitcherInteractive: state.locationSwitcherInteractive,
  }

  const listeners = new Set<() => void>()

  const emit = () => {
    for (const listener of listeners) {
      listener()
    }
  }

  const publish = () => {
    snapshot = {
      status: state.status,
      mode: state.mode,
      locations: state.locations,
      selectedLocationId: state.selectedLocationId,
      restaurantName: state.restaurantName,
      operatorDisplayName: state.operatorDisplayName,
      activationExpiresAt: state.activationExpiresAt,
      selfRole: state.selfRole,
      locationSwitcherInteractive: state.locationSwitcherInteractive,
    }
    emit()
  }

  const dispatch = (action: WorkspaceAction) => {
    state = reduce(state, action)
    publish()
  }

  const commitSelection = (locationId: number) => {
    adapters.persistSelectedLocation(locationId)
    dispatch({ type: "select_location", locationId })
  }

  const load = async (input?: { queryLocationId?: number | null }) => {
    const queryLocationId = input?.queryLocationId ?? null
    dispatch({ type: "load_started" })

    try {
      const [locationsResult, meResult] = await Promise.all([
        adapters.getLocations(),
        adapters.fetchCurrentUser(),
      ])

      const profile = parseOperatorProfile(meResult)
      // Invalid/foreign query ids are ignored so persistence can still win.
      const preferredId = isOwnedLocationId(
        locationsResult.locations,
        queryLocationId
      )
        ? queryLocationId
        : adapters.getPersistedLocationId()
      const selectedLocationId = resolveInitialLocationId(
        locationsResult.locations,
        preferredId
      )

      if (selectedLocationId != null) {
        adapters.persistSelectedLocation(selectedLocationId)
      }

      dispatch({
        type: "load_succeeded",
        locations: locationsResult.locations,
        selectedLocationId,
        restaurantName: locationsResult.restaurantName?.trim() ?? "",
        operatorDisplayName: profile?.fullName ?? "Operator",
        activationExpiresAt: profile?.activationExpiresAt ?? null,
        selfRole: profile?.selfRole ?? null,
        queryLocationId,
      })
    } catch {
      dispatch({ type: "load_failed" })
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
    load,
    retry: () => load({ queryLocationId: state.lastQueryLocationId }),
    selectLocation: (locationId) => {
      if (config.mode !== "multi") {
        return
      }
      if (state.status !== "loaded") {
        return
      }
      if (!state.locations.some((location) => location.id === locationId)) {
        return
      }
      if (state.selectedLocationId === locationId) {
        return
      }
      commitSelection(locationId)
    },
    preferLocationFromQuery: (queryLocationId) => {
      dispatch({ type: "remember_query_location", queryLocationId })
      if (state.status !== "loaded" || queryLocationId == null) {
        return
      }
      if (!isOwnedLocationId(state.locations, queryLocationId)) {
        return
      }
      if (state.selectedLocationId === queryLocationId) {
        return
      }
      commitSelection(queryLocationId)
    },
  }
}
