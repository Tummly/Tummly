import { AxiosError, AxiosHeaders } from "axios"
import { describe, expect, it, vi } from "vitest"

import {
  GuestPreferencesSessionExpiredError,
  type GuestPreferenceRestaurant,
} from "@/api/publicGuestPreferencesApi"
import {
  brandInitial,
  commitBrandMute,
  commitLocationMute,
  createGuestPreferencesRestaurantsState,
  createRestaurantsLoader,
  firstBrandIds,
  guestPreferencesRestaurantsReducer,
  isBrandAllMuted,
  isGuestPreferencesSessionExpired,
  loadRestaurants,
  normalizeSearchQuery,
  resolveBrandLogo,
  type GuestPreferencesRestaurantsAction,
  type GuestPreferencesRestaurantsState,
} from "@/lib/guestPreferences/guestPreferencesRestaurantsState"

const restaurants: GuestPreferenceRestaurant[] = [
  {
    restaurantId: 1,
    name: "KFC",
    logoUrl: null,
    locations: [
      { locationId: 11, name: "Camden", muted: false },
      { locationId: 12, name: "Soho", muted: false },
      { locationId: 13, name: "Shoreditch", muted: true },
    ],
  },
  {
    restaurantId: 2,
    name: "Burger King",
    logoUrl: null,
    locations: [{ locationId: 21, name: "Leicester Square", muted: false }],
  },
]

function axiosErrorWithBody(data: unknown, status: number): AxiosError {
  return new AxiosError("Request failed", "ERR_BAD_REQUEST", undefined, undefined, {
    status,
    statusText: "",
    headers: {},
    config: { headers: new AxiosHeaders() },
    data,
  })
}

/** Applies actions in order, from the initial state. */
function reduceAll(
  actions: GuestPreferencesRestaurantsAction[],
  start = createGuestPreferencesRestaurantsState()
): GuestPreferencesRestaurantsState {
  return actions.reduce(guestPreferencesRestaurantsReducer, start)
}

function loadedState(list = restaurants): GuestPreferencesRestaurantsState {
  return reduceAll([
    { type: "load-started", requestId: 1 },
    { type: "load-succeeded", requestId: 1, restaurants: list },
  ])
}

/** A dispatch that keeps the latest state, so commit helpers can be tested directly. */
function recordingStore(initial: GuestPreferencesRestaurantsState) {
  let state = initial
  return {
    dispatch: (action: GuestPreferencesRestaurantsAction) => {
      state = guestPreferencesRestaurantsReducer(state, action)
    },
    getState: () => state,
    get state() {
      return state
    },
  }
}

describe("guestPreferencesRestaurantsReducer", () => {
  it("opens only the first brand once the list loads", () => {
    const state = loadedState()

    expect(state.status).toBe("ready")
    expect(state.openBrandIds).toEqual([1])
    expect(firstBrandIds(restaurants)).toEqual([1])
    expect(firstBrandIds([])).toEqual([])
  })

  it("ignores a response from an older request", () => {
    const state = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-started", requestId: 2 },
      { type: "load-succeeded", requestId: 2, restaurants: [restaurants[1]] },
      { type: "load-succeeded", requestId: 1, restaurants },
    ])

    expect(state.restaurants.map((r) => r.name)).toEqual(["Burger King"])
  })

  it("marks the list failed only for the newest request", () => {
    const failedLatest = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-failed", requestId: 1 },
    ])
    const failedStale = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-started", requestId: 2 },
      { type: "load-failed", requestId: 1 },
    ])

    expect(failedLatest.status).toBe("failed")
    expect(failedStale.status).toBe("loading")
  })

  it("keeps the search input value separate from the loaded list", () => {
    const state = reduceAll([{ type: "query-changed", query: "Cam" }])

    expect(state.query).toBe("Cam")
    expect(state.restaurants).toEqual([])
  })

  it("applies a location mute optimistically and clears the pending flag on success", () => {
    const state = reduceAll(
      [
        { type: "mute-started", change: { kind: "location", locationId: 11, muted: true } },
      ],
      loadedState()
    )

    expect(state.restaurants[0].locations[0]).toEqual({
      locationId: 11,
      name: "Camden",
      muted: true,
    })
    expect(state.pendingMute).not.toBeNull()

    const settled = reduceAll([{ type: "mute-succeeded" }], state)
    expect(settled.pendingMute).toBeNull()
    expect(settled.restaurants[0].locations[0].muted).toBe(true)
  })

  it("rolls a failed location mute back to the previous value", () => {
    const state = reduceAll(
      [
        { type: "mute-started", change: { kind: "location", locationId: 11, muted: true } },
        { type: "mute-failed" },
      ],
      loadedState()
    )

    expect(state.restaurants[0].locations[0].muted).toBe(false)
    expect(state.pendingMute).toBeNull()
  })

  it("applies a brand mute to every Location and rolls back only the ones it changed", () => {
    const muteAll = reduceAll(
      [
        { type: "mute-started", change: { kind: "brand", restaurantId: 1, muted: true } },
      ],
      loadedState()
    )

    expect(muteAll.restaurants[0].locations.map((l) => l.muted)).toEqual([
      true,
      true,
      true,
    ])
    expect(muteAll.restaurants[1].locations[0].muted).toBe(false)

    const rolledBack = reduceAll([{ type: "mute-failed" }], muteAll)
    expect(rolledBack.restaurants[0].locations.map((l) => l.muted)).toEqual([
      false,
      false,
      true,
    ])
  })

  it("ignores a second mute while one is in flight", () => {
    const first = reduceAll(
      [{ type: "mute-started", change: { kind: "location", locationId: 11, muted: true } }],
      loadedState()
    )
    const second = guestPreferencesRestaurantsReducer(first, {
      type: "mute-started",
      change: { kind: "location", locationId: 12, muted: true },
    })

    expect(second).toBe(first)
  })

  it("treats an empty brand as not all muted", () => {
    expect(isBrandAllMuted({ restaurantId: 9, name: "X", logoUrl: null, locations: [] })).toBe(false)
    expect(isBrandAllMuted(restaurants[0])).toBe(false)
    expect(
      isBrandAllMuted({
        ...restaurants[0],
        locations: restaurants[0].locations.map((l) => ({ ...l, muted: true })),
      })
    ).toBe(true)
  })
})

describe("commit helpers", () => {
  it("rethrows a failed location mute after rolling back", async () => {
    const store = recordingStore(loadedState())
    const failure = axiosErrorWithBody({ error: "Nope." }, 500)
    const setLocationMuted = vi.fn(async () => {
      throw failure
    })

    await expect(
      commitLocationMute(setLocationMuted, store, 11, true)
    ).rejects.toBe(failure)
    expect(store.state.restaurants[0].locations[0].muted).toBe(false)
    expect(store.state.pendingMute).toBeNull()
  })

  it("rethrows a failed brand mute and restores every Location it touched", async () => {
    const store = recordingStore(loadedState())
    const failure = axiosErrorWithBody({ error: "Nope." }, 500)

    await expect(
      commitBrandMute(
        async () => {
          throw failure
        },
        store,
        1,
        true
      )
    ).rejects.toBe(failure)
    expect(store.state.restaurants[0].locations.map((l) => l.muted)).toEqual([
      false,
      false,
      true,
    ])
  })
})

describe("loadRestaurants", () => {
  it("sends the trimmed query, or no q when the search is blank", async () => {
    const fetchList = vi.fn(async () => restaurants)

    await loadRestaurants(fetchList, "  Camden ", 1, () => undefined)
    await loadRestaurants(fetchList, "   ", 2, () => undefined)
    await loadRestaurants(fetchList, undefined, 3, () => undefined)

    expect(fetchList.mock.calls).toEqual([["Camden"], [undefined], [undefined]])
  })

  it("dispatches load-failed and rethrows when the request fails", async () => {
    const store = recordingStore(createGuestPreferencesRestaurantsState())
    const failure = new GuestPreferencesSessionExpiredError()

    await expect(
      loadRestaurants(async () => Promise.reject(failure), undefined, 1, store.dispatch)
    ).rejects.toBe(failure)
    expect(store.state.status).toBe("failed")
    expect(isGuestPreferencesSessionExpired(failure)).toBe(true)
  })
})

describe("createRestaurantsLoader", () => {
  it("flags a 401 as sessionExpired instead of rejecting", async () => {
    const store = recordingStore(createGuestPreferencesRestaurantsState())
    const loader = createRestaurantsLoader(store.dispatch, async () => {
      throw new GuestPreferencesSessionExpiredError()
    })

    await expect(loader.load(undefined)).resolves.toBeUndefined()
    expect(store.state.sessionExpired).toBe(true)
    expect(store.state.status).toBe("failed")
  })

  it("keeps only the newest response when an older one resolves last", async () => {
    let resolveFirst: (value: GuestPreferenceRestaurant[]) => void = () => undefined
    const fetchList = vi.fn<(q?: string) => Promise<GuestPreferenceRestaurant[]>>()
      .mockImplementationOnce(
        () => new Promise<GuestPreferenceRestaurant[]>((resolve) => {
          resolveFirst = resolve
        })
      )
      .mockImplementationOnce(async () => [restaurants[1]])
    const store = recordingStore(createGuestPreferencesRestaurantsState())
    const loader = createRestaurantsLoader(store.dispatch, fetchList)

    const first = loader.load("K")
    await loader.load("Burger")
    resolveFirst([restaurants[0]])
    await first

    expect(store.state.restaurants.map((r) => r.name)).toEqual(["Burger King"])
  })
})

describe("fix round 1: pending mute and reloads", () => {
  it("keeps a pending mute when a search response lands before the PUT settles", () => {
    const midFlight = reduceAll(
      [
        { type: "mute-started", change: { kind: "location", locationId: 11, muted: true } },
        { type: "load-started", requestId: 2 },
        // The server answered the search before the PUT applied, so it shows the old value.
        { type: "load-succeeded", requestId: 2, restaurants },
      ],
      loadedState()
    )
    expect(midFlight.restaurants[0].locations[0].muted).toBe(true)

    const settled = reduceAll([{ type: "mute-succeeded" }], midFlight)
    expect(settled.restaurants[0].locations[0].muted).toBe(true)
  })

  it("keeps the brands the guest opened when a reload returns them", () => {
    const state = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-succeeded", requestId: 1, restaurants },
      { type: "brands-open-changed", openBrandIds: [2] },
      { type: "load-started", requestId: 2 },
      { type: "load-succeeded", requestId: 2, restaurants },
    ])

    expect(state.openBrandIds).toEqual([2])
  })

  it("opens the first brand when none of the open brands is in the new list", () => {
    const state = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-succeeded", requestId: 1, restaurants },
      { type: "brands-open-changed", openBrandIds: [2] },
      { type: "load-started", requestId: 2 },
      { type: "load-succeeded", requestId: 2, restaurants: [restaurants[0]] },
    ])

    expect(state.openBrandIds).toEqual([1])
  })

  it("sends nothing for a second mute while one is in flight", async () => {
    const store = recordingStore(loadedState())
    let finishFirst: () => void = () => undefined
    const firstSend = commitLocationMute(
      () =>
        new Promise<void>((resolve) => {
          finishFirst = resolve
        }),
      store,
      11,
      true
    )
    const secondLocation = vi.fn(async () => undefined)
    const secondBrand = vi.fn(async () => undefined)

    await commitLocationMute(secondLocation, store, 12, true)
    await commitBrandMute(secondBrand, store, 2, true)

    expect(secondLocation).not.toHaveBeenCalled()
    expect(secondBrand).not.toHaveBeenCalled()
    finishFirst()
    await firstSend
  })

  it("reload refetches the latest typed query, not the one from the last load", async () => {
    const store = recordingStore(createGuestPreferencesRestaurantsState())
    const fetchList = vi.fn(async () => restaurants)
    const loader = createRestaurantsLoader(store.dispatch, fetchList)

    await loader.load(undefined)
    loader.search("KFC")
    await loader.reload()
    loader.cancelSearch()

    expect(fetchList).toHaveBeenLastCalledWith("KFC")
  })

  it("reload with nothing typed fetches the full list", async () => {
    const store = recordingStore(createGuestPreferencesRestaurantsState())
    const fetchList = vi.fn(async () => restaurants)
    const loader = createRestaurantsLoader(store.dispatch, fetchList)

    await loader.reload()

    expect(fetchList).toHaveBeenLastCalledWith(undefined)
  })

  it("picks the logo image, or the letter tile when there is no usable logo", () => {
    const url = "https://cdn.example.com/kfc.png"
    const next = "https://cdn.example.com/kfc-2.png"

    expect(resolveBrandLogo(url, null)).toEqual({ kind: "image", src: url })
    expect(resolveBrandLogo(null, null)).toEqual({ kind: "fallback" })
    expect(resolveBrandLogo("", null)).toEqual({ kind: "fallback" })
    expect(resolveBrandLogo(url, url)).toEqual({ kind: "fallback" })
    expect(resolveBrandLogo(next, url)).toEqual({ kind: "image", src: next })
  })
})

describe("request error helpers", () => {
  it("normalizes blank search text to no query", () => {
    expect(normalizeSearchQuery("  ")).toBeUndefined()
    expect(normalizeSearchQuery(undefined)).toBeUndefined()
    expect(normalizeSearchQuery(" KFC ")).toBe("KFC")
  })

  it("takes the first letter of a brand name for the logo fallback", () => {
    expect(brandInitial("burger king")).toBe("B")
    expect(brandInitial("  Pizza Hut")).toBe("P")
    expect(brandInitial("")).toBe("")
  })
})
