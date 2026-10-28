import { isValidElement, type ReactElement, type ReactNode } from "react"
import { renderToStaticMarkup } from "react-dom/server"
import { describe, expect, it, vi } from "vitest"

import type { GuestPreferenceRestaurant } from "@/api/publicGuestPreferencesApi"
import {
  GuestPreferencesRestaurantsContent,
  type GuestPreferencesRestaurantsContentProps,
} from "@/components/guest-preferences/GuestPreferencesRestaurants"
import { GUEST_PREFERENCES_COPY } from "@/content/guestPreferences/guestPreferencesCopy"
import {
  commitBrandMute,
  commitLocationMute,
  createGuestPreferencesRestaurantsState,
  createRestaurantsLoader,
  guestPreferencesRestaurantsReducer,
  isBrandAllMuted,
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
    logoUrl: "https://cdn.example.com/burger-king.png",
    locations: [{ locationId: 21, name: "Leicester Square", muted: false }],
  },
]

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

/** Keeps the latest state so a test can render after the commit helpers run. */
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

const noop = () => undefined

function contentProps(
  state: GuestPreferencesRestaurantsState,
  overrides: Partial<GuestPreferencesRestaurantsContentProps> = {}
): GuestPreferencesRestaurantsContentProps {
  return {
    state,
    onBack: noop,
    onQueryChange: noop,
    onRetry: noop,
    onOpenBrandsChange: noop,
    onMuteLocation: noop,
    onMuteBrand: noop,
    ...overrides,
  }
}

function render(
  state: GuestPreferencesRestaurantsState,
  overrides: Partial<GuestPreferencesRestaurantsContentProps> = {}
): string {
  return renderToStaticMarkup(
    <GuestPreferencesRestaurantsContent {...contentProps(state, overrides)} />
  )
}

/** The markup of one location row, from its name up to the row's close tag. */
function locationRowHtml(html: string, locationName: string): string {
  const start = html.indexOf(`>${locationName}</span>`)
  if (start < 0) {
    throw new Error(`No row for ${locationName}`)
  }
  return html.slice(start, html.indexOf("</li>", start))
}

/** Every element in the tree whose predicate matches. Walks children only, so function components are not called. */
type ElementProps = { children?: ReactNode; onClick?: () => void }

function collectElements(
  node: ReactNode,
  predicate: (element: ReactElement<ElementProps>) => boolean
): ReactElement<ElementProps>[] {
  if (Array.isArray(node)) {
    return node.flatMap((child) => collectElements(child, predicate))
  }
  if (!isValidElement<ElementProps>(node)) {
    return []
  }
  const own = predicate(node) ? [node] : []
  return [...own, ...collectElements(node.props.children, predicate)]
}

describe("GuestPreferencesRestaurants", () => {
  it("renders brands collapsed except first", () => {
    const html = render(loadedState())

    expect(html).toContain("KFC")
    expect(html).toContain("Burger King")
    // The first brand's Locations are in the tree. The second brand's are not.
    expect(html).toContain("Camden")
    expect(html).not.toContain("Leicester Square")
    expect(html.match(/aria-expanded="true"/g)).toHaveLength(1)
    expect(html.match(/aria-expanded="false"/g)).toHaveLength(1)
    expect(html).toContain(GUEST_PREFERENCES_COPY.brandLocationCount(3))
    expect(html).toContain(GUEST_PREFERENCES_COPY.brandLocationCount(1))
  })

  it("Mute calls setLocationMuted(id, true) and row shows Muted", async () => {
    const store = recordingStore(loadedState())
    const setLocationMuted = vi.fn(async () => undefined)

    await commitLocationMute(setLocationMuted, store, 11, true)
    const row = locationRowHtml(render(store.state), "Camden")

    expect(setLocationMuted).toHaveBeenCalledWith(11, true)
    expect(row).toContain(`>${GUEST_PREFERENCES_COPY.locationMuted}</span>`)
    expect(row).toContain(`aria-label="${GUEST_PREFERENCES_COPY.unmuteLabel} Camden"`)
  })

  it("Mute all brand locations calls setRestaurantMuted", async () => {
    const store = recordingStore(loadedState())
    const setRestaurantMuted = vi.fn(async () => undefined)
    const kfc = store.state.restaurants[0]

    // The button label reads "Mute all" while any Location is unmuted.
    expect(render(store.state)).toContain(GUEST_PREFERENCES_COPY.muteAllBrandLocationsLabel)

    await commitBrandMute(
      setRestaurantMuted,
      store,
      kfc.restaurantId,
      !isBrandAllMuted(kfc)
    )
    const html = render(store.state)

    expect(setRestaurantMuted).toHaveBeenCalledWith(1, true)
    expect(html).toContain(GUEST_PREFERENCES_COPY.unmuteAllBrandLocationsLabel)
    expect(html).not.toContain(GUEST_PREFERENCES_COPY.muteAllBrandLocationsLabel)
  })

  it("search debounces and calls fetchRestaurants with q", () => {
    vi.useFakeTimers()
    try {
      const fetchList = vi.fn(async () => restaurants)
      const store = recordingStore(createGuestPreferencesRestaurantsState())
      const loader = createRestaurantsLoader(store.dispatch, fetchList)

      loader.search("K")
      loader.search("KF")
      loader.search("KFC")
      vi.advanceTimersByTime(299)
      expect(fetchList).not.toHaveBeenCalled()

      vi.advanceTimersByTime(1)
      expect(fetchList).toHaveBeenCalledTimes(1)
      expect(fetchList).toHaveBeenCalledWith("KFC")
      loader.cancelSearch()
    } finally {
      vi.useRealTimers()
    }
  })

  it("Back returns to overview", () => {
    const onBack = vi.fn()
    const tree = GuestPreferencesRestaurantsContent(
      contentProps(loadedState(), { onBack })
    )
    const backButtons = collectElements(
      tree,
      (element) =>
        typeof element.props.onClick === "function" &&
        Array.isArray(element.props.children) &&
        element.props.children.includes(GUEST_PREFERENCES_COPY.backLabel)
    )

    expect(backButtons).toHaveLength(1)
    backButtons[0].props.onClick?.()
    expect(onBack).toHaveBeenCalledTimes(1)
  })

  it("shows the empty copy when the list is empty and ready", () => {
    const html = render(
      reduceAll([
        { type: "load-started", requestId: 1 },
        { type: "load-succeeded", requestId: 1, restaurants: [] },
      ])
    )

    expect(html).toContain(GUEST_PREFERENCES_COPY.restaurantsEmptyLabel)
  })

  it("says no restaurants match when a search finds none", () => {
    const state = reduceAll([
      { type: "query-changed", query: "zzz" },
      { type: "load-started", requestId: 1 },
      { type: "load-succeeded", requestId: 1, restaurants: [] },
    ])
    const html = render(state)

    expect(html).toContain(GUEST_PREFERENCES_COPY.restaurantsNoMatchLabel)
    expect(html).not.toContain(GUEST_PREFERENCES_COPY.restaurantsEmptyLabel)
  })

  it("keeps the empty copy for an empty list with no query", () => {
    const state = reduceAll([
      { type: "load-started", requestId: 1 },
      { type: "load-succeeded", requestId: 1, restaurants: [] },
    ])
    const html = render(state)

    expect(html).toContain(GUEST_PREFERENCES_COPY.restaurantsEmptyLabel)
    expect(html).not.toContain(GUEST_PREFERENCES_COPY.restaurantsNoMatchLabel)
  })

  it("shows a neutral tile with the first letter when the brand has no logo", () => {
    const html = render(loadedState())

    expect(html).toContain(">K</span>")
    expect(html).toContain(`src="${restaurants[1].logoUrl}"`)
    expect(html.match(/<img /g)).toHaveLength(1)
  })
})
