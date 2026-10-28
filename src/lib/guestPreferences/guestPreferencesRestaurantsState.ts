import { isAxiosError } from "axios"

import {
  fetchRestaurants,
  GuestPreferencesSessionExpiredError,
  type GuestPreferenceRestaurant,
} from "@/api/publicGuestPreferencesApi"
import {
  browserTimerScheduler,
  createDebouncedCall,
  type TimerScheduler,
} from "@/lib/guestPreferences/debouncedCall"

/** Delay between the last keystroke and the search request. */
export const RESTAURANT_SEARCH_DEBOUNCE_MS = 300

export type GuestPreferencesRestaurantsLoadStatus = "loading" | "ready" | "failed"

/** One mute change. A brand change applies to every Location under the brand. */
export type GuestPreferencesMuteChange =
  | { kind: "location"; locationId: number; muted: boolean }
  | { kind: "brand"; restaurantId: number; muted: boolean }

type PendingMute = {
  /** Each affected Location and its muted value before the optimistic change. */
  previous: { locationId: number; muted: boolean }[]
}

export type GuestPreferencesRestaurantsState = {
  /** Search input value. The list reloads from the debounced value, not this one. */
  query: string
  status: GuestPreferencesRestaurantsLoadStatus
  restaurants: GuestPreferenceRestaurant[]
  /** Id of the newest list request. Responses to older requests are ignored. */
  latestRequestId: number
  /** Brand ids (`restaurantId`) whose panel is open. */
  openBrandIds: number[]
  /** Non-null while a mute request is in flight. Only one runs at a time. */
  pendingMute: PendingMute | null
  /** Set once a request returns 401. The container then hands off to the entry view. */
  sessionExpired: boolean
}

export type GuestPreferencesRestaurantsAction =
  | { type: "query-changed"; query: string }
  | { type: "session-expired" }
  | { type: "load-started"; requestId: number }
  | {
      type: "load-succeeded"
      requestId: number
      restaurants: GuestPreferenceRestaurant[]
    }
  | { type: "load-failed"; requestId: number }
  | { type: "mute-started"; change: GuestPreferencesMuteChange }
  | { type: "mute-succeeded" }
  | { type: "mute-failed" }
  | { type: "brands-open-changed"; openBrandIds: number[] }

export type GuestPreferencesRestaurantsDispatch = (
  action: GuestPreferencesRestaurantsAction
) => void

export function createGuestPreferencesRestaurantsState(): GuestPreferencesRestaurantsState {
  return {
    query: "",
    status: "loading",
    restaurants: [],
    latestRequestId: 0,
    openBrandIds: [],
    pendingMute: null,
    sessionExpired: false,
  }
}

function targetsLocation(
  change: GuestPreferencesMuteChange,
  restaurant: GuestPreferenceRestaurant,
  locationId: number
): boolean {
  return change.kind === "brand"
    ? restaurant.restaurantId === change.restaurantId
    : locationId === change.locationId
}

function applyMuteChange(
  restaurants: GuestPreferenceRestaurant[],
  change: GuestPreferencesMuteChange
): GuestPreferenceRestaurant[] {
  return restaurants.map((restaurant) => ({
    ...restaurant,
    locations: restaurant.locations.map((location) =>
      targetsLocation(change, restaurant, location.locationId)
        ? { ...location, muted: change.muted }
        : location
    ),
  }))
}

function snapshotMuteStates(
  restaurants: GuestPreferenceRestaurant[],
  change: GuestPreferencesMuteChange
): { locationId: number; muted: boolean }[] {
  return restaurants.flatMap((restaurant) =>
    restaurant.locations
      .filter((location) =>
        targetsLocation(change, restaurant, location.locationId)
      )
      .map((location) => ({
        locationId: location.locationId,
        muted: location.muted,
      }))
  )
}

function restoreMuteStates(
  restaurants: GuestPreferenceRestaurant[],
  previous: PendingMute["previous"]
): GuestPreferenceRestaurant[] {
  return restaurants.map((restaurant) => ({
    ...restaurant,
    locations: restaurant.locations.map((location) => {
      const before = previous.find(
        (entry) => entry.locationId === location.locationId
      )
      return before === undefined
        ? location
        : { ...location, muted: before.muted }
    }),
  }))
}

export function guestPreferencesRestaurantsReducer(
  state: GuestPreferencesRestaurantsState,
  action: GuestPreferencesRestaurantsAction
): GuestPreferencesRestaurantsState {
  switch (action.type) {
    case "query-changed":
      return { ...state, query: action.query }
    case "session-expired":
      return { ...state, sessionExpired: true }
    case "load-started":
      return {
        ...state,
        status: "loading",
        latestRequestId: action.requestId,
      }
    case "load-succeeded": {
      if (action.requestId !== state.latestRequestId) {
        return state
      }
      return {
        ...state,
        status: "ready",
        restaurants: action.restaurants,
        openBrandIds: firstBrandIds(action.restaurants),
      }
    }
    case "load-failed":
      return action.requestId === state.latestRequestId
        ? { ...state, status: "failed" }
        : state
    case "mute-started": {
      if (state.pendingMute !== null) {
        return state
      }
      return {
        ...state,
        restaurants: applyMuteChange(state.restaurants, action.change),
        pendingMute: {
          previous: snapshotMuteStates(state.restaurants, action.change),
        },
      }
    }
    case "mute-succeeded":
      return { ...state, pendingMute: null }
    case "mute-failed":
      if (state.pendingMute === null) {
        return state
      }
      return {
        ...state,
        restaurants: restoreMuteStates(
          state.restaurants,
          state.pendingMute.previous
        ),
        pendingMute: null,
      }
    case "brands-open-changed":
      return { ...state, openBrandIds: action.openBrandIds }
  }
}

/** A brand with Locations where every Location is muted. */
export function isBrandAllMuted(restaurant: GuestPreferenceRestaurant): boolean {
  return (
    restaurant.locations.length > 0 &&
    restaurant.locations.every((location) => location.muted)
  )
}

/** Only the first brand starts open. */
export function firstBrandIds(restaurants: GuestPreferenceRestaurant[]): number[] {
  return restaurants.length > 0 ? [restaurants[0].restaurantId] : []
}

/** Blank or whitespace-only search means no `q`, which loads every restaurant. */
export function normalizeSearchQuery(query: string | undefined): string | undefined {
  const trimmed = query?.trim() ?? ""
  return trimmed === "" ? undefined : trimmed
}

/** First letter of the brand name, for the logo fallback. */
export function brandInitial(name: string): string {
  return name.trim().charAt(0).toUpperCase()
}

/** A 404 with `{ error: "not-found" }`. The list is stale and must be refetched. */
export function isNotFoundError(error: unknown): boolean {
  if (!isAxiosError(error) || error.response?.status !== 404) {
    return false
  }
  const data: unknown = error.response.data
  return (
    typeof data === "object" &&
    data !== null &&
    "error" in data &&
    data.error === "not-found"
  )
}

export function isGuestPreferencesSessionExpired(error: unknown): boolean {
  return error instanceof GuestPreferencesSessionExpiredError
}

/**
 * Fetches the list for `query` as the newest request. The state ignores the
 * response if a newer request has started. Rethrows failures so the caller
 * can route a 401.
 */
export async function loadRestaurants(
  fetchList: (q?: string) => Promise<GuestPreferenceRestaurant[]>,
  query: string | undefined,
  requestId: number,
  dispatch: GuestPreferencesRestaurantsDispatch
): Promise<void> {
  dispatch({ type: "load-started", requestId })
  try {
    const restaurants = await fetchList(normalizeSearchQuery(query))
    dispatch({ type: "load-succeeded", requestId, restaurants })
  } catch (error) {
    dispatch({ type: "load-failed", requestId })
    throw error
  }
}

/**
 * Applies the change optimistically, then sends it. A failure rolls the
 * change back and rethrows. A success confirms it.
 */
async function runMuteChange(
  dispatch: GuestPreferencesRestaurantsDispatch,
  change: GuestPreferencesMuteChange,
  send: () => Promise<void>
): Promise<void> {
  dispatch({ type: "mute-started", change })
  try {
    await send()
    dispatch({ type: "mute-succeeded" })
  } catch (error) {
    dispatch({ type: "mute-failed" })
    throw error
  }
}

export function commitLocationMute(
  setLocationMuted: (locationId: number, muted: boolean) => Promise<void>,
  dispatch: GuestPreferencesRestaurantsDispatch,
  locationId: number,
  muted: boolean
): Promise<void> {
  return runMuteChange(dispatch, { kind: "location", locationId, muted }, () =>
    setLocationMuted(locationId, muted)
  )
}

export function commitBrandMute(
  setRestaurantMuted: (restaurantId: number, muted: boolean) => Promise<void>,
  dispatch: GuestPreferencesRestaurantsDispatch,
  restaurantId: number,
  muted: boolean
): Promise<void> {
  return runMuteChange(dispatch, { kind: "brand", restaurantId, muted }, () =>
    setRestaurantMuted(restaurantId, muted)
  )
}

export type GuestPreferencesRestaurantsLoader = {
  /** Loads now. Never rejects: a 401 sets `sessionExpired`, other failures set `failed`. */
  load: (query: string | undefined) => Promise<void>
  /** Loads once the search input has been idle for the debounce delay. */
  search: (query: string) => void
  cancelSearch: () => void
}

/**
 * Owns the request sequence and the debounced search. Each `load` takes a new
 * id, so a slow response to an older query cannot overwrite a newer list.
 */
export function createRestaurantsLoader(
  dispatch: GuestPreferencesRestaurantsDispatch,
  fetchList: (q?: string) => Promise<GuestPreferenceRestaurant[]> = fetchRestaurants,
  debounceMs: number = RESTAURANT_SEARCH_DEBOUNCE_MS,
  scheduler: TimerScheduler = browserTimerScheduler
): GuestPreferencesRestaurantsLoader {
  let latestRequestId = 0

  async function load(query: string | undefined): Promise<void> {
    latestRequestId += 1
    try {
      await loadRestaurants(fetchList, query, latestRequestId, dispatch)
    } catch (error) {
      if (isGuestPreferencesSessionExpired(error)) {
        dispatch({ type: "session-expired" })
      }
    }
  }

  const debounced = createDebouncedCall(
    (query: string) => void load(query),
    debounceMs,
    scheduler
  )

  return {
    load,
    search: (query) => debounced.call(query),
    cancelSearch: () => debounced.cancel(),
  }
}
