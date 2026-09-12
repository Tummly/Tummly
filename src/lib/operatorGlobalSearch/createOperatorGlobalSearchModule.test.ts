import { describe, expect, it, vi } from "vitest"

import {
  EMPTY_AI_SUGGESTIONS,
  createOperatorGlobalSearchModule,
  isGlobalSearchOpenShortcut,
  shortcutModifierLabel,
  type OperatorGlobalSearchAdapters,
  type OperatorGlobalSearchEntityHit,
} from "./createOperatorGlobalSearchModule"

function makeGuestHit(
  overrides: Partial<OperatorGlobalSearchEntityHit> = {}
): OperatorGlobalSearchEntityHit {
  return {
    id: "12",
    title: "Mohamed",
    subtitle: "a@example.com",
    status: "Eligible — Email",
    locationId: 1,
    initials: "M",
    ...overrides,
  }
}

function makeFeedbackHit(
  overrides: Partial<OperatorGlobalSearchEntityHit> = {}
): OperatorGlobalSearchEntityHit {
  return {
    id: "34",
    title: "Sam",
    subtitle: "Food was cold",
    status: "New",
    locationId: 1,
    initials: "S",
    ...overrides,
  }
}

function makeCampaignHit(
  overrides: Partial<OperatorGlobalSearchEntityHit> = {}
): OperatorGlobalSearchEntityHit {
  return {
    id: "55",
    title: "Weekend brunch",
    subtitle: "Email",
    status: "Draft",
    locationId: 1,
    initials: "WB",
    ...overrides,
  }
}

function makeOfferHit(
  overrides: Partial<OperatorGlobalSearchEntityHit> = {}
): OperatorGlobalSearchEntityHit {
  return {
    id: "66",
    title: "Monday Lunch Deal",
    subtitle: "Camden",
    status: "Active",
    locationId: 1,
    initials: "ML",
    ...overrides,
  }
}

function makeAdapters(
  overrides: Partial<OperatorGlobalSearchAdapters> = {}
): OperatorGlobalSearchAdapters {
  return {
    handoffSuggestionToAssistant: vi.fn(),
    searchHits: vi.fn(async () => ({
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    })),
    navigateToGuestProfile: vi.fn(),
    navigateToFeedbackDetail: vi.fn(),
    navigateToCampaignDetail: vi.fn(),
    navigateToOfferDetails: vi.fn(),
    navigateToCapturePlacementDetail: vi.fn(),
    getLocationId: () => 1,
    getAuthorisedLocationCount: () => 1,
    debounceMs: 0,
    ...overrides,
  }
}

const emptyHits = {
  guestHits: [] as OperatorGlobalSearchEntityHit[],
  feedbackHits: [] as OperatorGlobalSearchEntityHit[],
  campaignHits: [] as OperatorGlobalSearchEntityHit[],
  offerHits: [] as OperatorGlobalSearchEntityHit[],
  qrCodeHits: [] as OperatorGlobalSearchEntityHit[],
}

describe("isGlobalSearchOpenShortcut", () => {
  it("matches ⌘K on Apple and Ctrl+K elsewhere", () => {
    expect(
      isGlobalSearchOpenShortcut({
        key: "k",
        metaKey: true,
        ctrlKey: false,
        isApplePlatform: true,
      })
    ).toBe(true)
    expect(
      isGlobalSearchOpenShortcut({
        key: "k",
        metaKey: false,
        ctrlKey: true,
        isApplePlatform: false,
      })
    ).toBe(true)
    expect(
      isGlobalSearchOpenShortcut({
        key: "k",
        metaKey: true,
        ctrlKey: false,
        isApplePlatform: false,
      })
    ).toBe(false)
    expect(
      isGlobalSearchOpenShortcut({
        key: "k",
        metaKey: false,
        ctrlKey: true,
        isApplePlatform: true,
      })
    ).toBe(false)
  })
})

describe("shortcutModifierLabel", () => {
  it("shows ⌘ on Apple and Ctrl elsewhere", () => {
    expect(shortcutModifierLabel(true)).toBe("⌘")
    expect(shortcutModifierLabel(false)).toBe("Ctrl")
  })
})

describe("createOperatorGlobalSearchModule", () => {
  it("starts closed with contextual empty AI suggestions", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      emptySuggestions: [...EMPTY_AI_SUGGESTIONS],
      hitsPending: false,
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
      locationScope: "current",
      canWidenLocationScope: false,
      showWidenFromNoResults: false,
      showNoResults: false,
    })
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("keeps getSnapshot identity until publish", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())
    const before = module.getSnapshot()
    expect(module.getSnapshot()).toBe(before)

    module.open()
    const afterOpen = module.getSnapshot()
    expect(afterOpen).not.toBe(before)
    expect(module.getSnapshot()).toBe(afterOpen)
  })

  it("opens and closes Search, clearing query on close", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())
    module.open()
    expect(module.getSnapshot().open).toBe(true)

    module.setQuery("mo")
    expect(module.getSnapshot().query).toBe("mo")

    module.close()
    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
      hitsPending: false,
      locationScope: "current",
      showNoResults: false,
    })
  })

  it("dismissFromEscape closes Search", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())
    module.open()
    module.dismissFromEscape()
    expect(module.getSnapshot().open).toBe(false)
  })

  it("handleShortcutKeydown opens when closed and ignores when already open", () => {
    const module = createOperatorGlobalSearchModule(
      makeAdapters(),
      { isApplePlatform: () => false }
    )
    expect(
      module.handleShortcutKeydown({
        key: "k",
        metaKey: false,
        ctrlKey: true,
        isApplePlatform: false,
      })
    ).toBe(true)
    expect(module.getSnapshot().open).toBe(true)
    expect(
      module.handleShortcutKeydown({
        key: "k",
        metaKey: false,
        ctrlKey: true,
        isApplePlatform: false,
      })
    ).toBe(false)
  })

  it("selectSuggestion closes Search and hands prompt to Assistant", () => {
    const handoffSuggestionToAssistant = vi.fn()
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ handoffSuggestionToAssistant })
    )
    module.open()
    const suggestion = module.getSnapshot().emptySuggestions[0]!
    module.selectSuggestion(suggestion.id)

    expect(module.getSnapshot().open).toBe(false)
    expect(handoffSuggestionToAssistant).toHaveBeenCalledWith(suggestion.prompt)
  })

  it("keeps Search usable when Assistant handoff throws", () => {
    const handoffSuggestionToAssistant = vi.fn(() => {
      throw new Error("assistant unavailable")
    })
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ handoffSuggestionToAssistant })
    )
    module.open()
    const suggestion = module.getSnapshot().emptySuggestions[0]!

    expect(() => module.selectSuggestion(suggestion.id)).not.toThrow()
    expect(module.getSnapshot().open).toBe(false)
    expect(module.getSnapshot().query).toBe("")

    module.open()
    expect(module.getSnapshot().open).toBe(true)
  })

  it("updates query immediately and debounces search without blocking typing", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({
      guestHits: [makeGuestHit()],
      feedbackHits: [makeFeedbackHit()],
      campaignHits: [makeCampaignHit()],
      offerHits: [makeOfferHit()],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchHits, debounceMs: 300 })
    )
    module.open()

    module.setQuery("mo")
    expect(module.getSnapshot().query).toBe("mo")
    expect(searchHits).not.toHaveBeenCalled()
    expect(module.getSnapshot().hitsPending).toBe(false)

    await vi.advanceTimersByTimeAsync(300)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenCalledWith(
        expect.objectContaining({ q: "mo", locationId: 1, scope: "current" })
      )
      expect(module.getSnapshot().guestHits).toEqual([makeGuestHit()])
      expect(module.getSnapshot().feedbackHits).toEqual([makeFeedbackHit()])
      expect(module.getSnapshot().campaignHits).toEqual([makeCampaignHit()])
      expect(module.getSnapshot().offerHits).toEqual([makeOfferHit()])
      expect(module.getSnapshot().hitsPending).toBe(false)
    })
    vi.useRealTimers()
  })

  it("ignores stale search results when a newer query wins", async () => {
    vi.useFakeTimers()
    let resolveSlow!: (value: {
      guestHits: OperatorGlobalSearchEntityHit[]
      feedbackHits: OperatorGlobalSearchEntityHit[]
      campaignHits: OperatorGlobalSearchEntityHit[]
      offerHits: OperatorGlobalSearchEntityHit[]
      qrCodeHits: OperatorGlobalSearchEntityHit[]
    }) => void
    const slow = new Promise<{
      guestHits: OperatorGlobalSearchEntityHit[]
      feedbackHits: OperatorGlobalSearchEntityHit[]
      campaignHits: OperatorGlobalSearchEntityHit[]
      offerHits: OperatorGlobalSearchEntityHit[]
      qrCodeHits: OperatorGlobalSearchEntityHit[]
    }>((resolve) => {
      resolveSlow = resolve
    })
    const searchHits = vi
      .fn()
      .mockImplementationOnce(() => slow)
      .mockResolvedValueOnce({
        guestHits: [makeGuestHit({ id: "99", title: "Morgan" })],
        feedbackHits: [],
        campaignHits: [makeCampaignHit({ id: "88", title: "Morgan offer" })],
        offerHits: [],
      qrCodeHits: [],
      })

    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchHits, debounceMs: 0 })
    )
    module.open()

    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    expect(module.getSnapshot().hitsPending).toBe(true)

    module.setQuery("mor")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenCalledTimes(2)
    })
    await vi.waitFor(() => {
      expect(module.getSnapshot().guestHits[0]?.title).toBe("Morgan")
      expect(module.getSnapshot().campaignHits[0]?.title).toBe("Morgan offer")
    })

    resolveSlow({
      guestHits: [makeGuestHit({ id: "12", title: "Mohamed" })],
      feedbackHits: [],
      campaignHits: [makeCampaignHit({ id: "55", title: "Mohamed brunch" })],
      offerHits: [],
      qrCodeHits: [],
    })
    await Promise.resolve()

    expect(module.getSnapshot().guestHits[0]?.title).toBe("Morgan")
    expect(module.getSnapshot().campaignHits[0]?.title).toBe("Morgan offer")
    expect(module.getSnapshot().hitsPending).toBe(false)
    vi.useRealTimers()
  })

  it("clears hits when query drops below two characters", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({
      guestHits: [makeGuestHit()],
      feedbackHits: [makeFeedbackHit()],
      campaignHits: [makeCampaignHit()],
      offerHits: [makeOfferHit()],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchHits, debounceMs: 0 })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().guestHits).toHaveLength(1)
      expect(module.getSnapshot().feedbackHits).toHaveLength(1)
      expect(module.getSnapshot().campaignHits).toHaveLength(1)
      expect(module.getSnapshot().offerHits).toHaveLength(1)
    })

    module.setQuery("m")
    expect(module.getSnapshot().query).toBe("m")
    expect(module.getSnapshot().guestHits).toEqual([])
    expect(module.getSnapshot().feedbackHits).toEqual([])
    expect(module.getSnapshot().campaignHits).toEqual([])
    expect(module.getSnapshot().offerHits).toEqual([])
    expect(module.getSnapshot().qrCodeHits).toEqual([])
    expect(module.getSnapshot().hitsPending).toBe(false)
    vi.useRealTimers()
  })

  it("selectGuestHit closes Search and navigates to Guest profile", async () => {
    vi.useFakeTimers()
    const navigateToGuestProfile = vi.fn()
    const searchHits = vi.fn(async () => ({
      guestHits: [makeGuestHit({ id: "42", locationId: 7 })],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        navigateToGuestProfile,
        getLocationId: () => 7,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().guestHits).toHaveLength(1)
    })

    module.selectGuestHit("42")

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    })
    expect(navigateToGuestProfile).toHaveBeenCalledWith(42, 7)
    vi.useRealTimers()
  })

  it("selectFeedbackHit closes Search and navigates with location + feedbackId", async () => {
    vi.useFakeTimers()
    const navigateToFeedbackDetail = vi.fn()
    const searchHits = vi.fn(async () => ({
      guestHits: [],
      feedbackHits: [makeFeedbackHit({ id: "88", locationId: 9 })],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        navigateToFeedbackDetail,
        getLocationId: () => 9,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("cold")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().feedbackHits).toHaveLength(1)
    })

    module.selectFeedbackHit("88")

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    })
    expect(navigateToFeedbackDetail).toHaveBeenCalledWith(88, 9)
    vi.useRealTimers()
  })

  it("selectCampaignHit closes Search and navigates to Campaign detail", async () => {
    vi.useFakeTimers()
    const navigateToCampaignDetail = vi.fn()
    const searchHits = vi.fn(async () => ({
      guestHits: [],
      feedbackHits: [],
      campaignHits: [makeCampaignHit({ id: "77", locationId: 9 })],
      offerHits: [],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        navigateToCampaignDetail,
        getLocationId: () => 9,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("br")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().campaignHits).toHaveLength(1)
    })

    module.selectCampaignHit("77")

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    })
    expect(navigateToCampaignDetail).toHaveBeenCalledWith(77, 9)
    vi.useRealTimers()
  })

  it("selectOfferHit closes Search and navigates to Offer details", async () => {
    vi.useFakeTimers()
    const navigateToOfferDetails = vi.fn()
    const searchHits = vi.fn(async () => ({
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [makeOfferHit({ id: "88", locationId: 9 })],
      qrCodeHits: [],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        navigateToOfferDetails,
        getLocationId: () => 9,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("lu")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().offerHits).toHaveLength(1)
    })

    module.selectOfferHit("88")

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [],
    })
    expect(navigateToOfferDetails).toHaveBeenCalledWith(88, 9)
    vi.useRealTimers()
  })

  it("selectQrCodeHit closes Search and navigates to Capture Placement Detail open query", async () => {
    vi.useFakeTimers()
    const navigateToCapturePlacementDetail = vi.fn()
    const searchHits = vi.fn(async () => ({
      guestHits: [],
      feedbackHits: [],
      campaignHits: [],
      offerHits: [],
      qrCodeHits: [
        {
          id: "55",
          title: "Table tent",
          subtitle: null,
          status: "Active",
          locationId: 3,
          initials: "TT",
        },
      ],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        navigateToCapturePlacementDetail,
        getLocationId: () => 3,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("ta")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().qrCodeHits).toHaveLength(1)
    })

    module.selectQrCodeHit("55")

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      qrCodeHits: [],
    })
    expect(navigateToCapturePlacementDetail).toHaveBeenCalledWith(55, 3)
    vi.useRealTimers()
  })

  it("does not search when overlay is closed or location is missing", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({ ...emptyHits }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getLocationId: () => null,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    expect(searchHits).not.toHaveBeenCalled()
    expect(module.getSnapshot().guestHits).toEqual([])
    expect(module.getSnapshot().feedbackHits).toEqual([])
    expect(module.getSnapshot().campaignHits).toEqual([])
    expect(module.getSnapshot().offerHits).toEqual([])
    vi.useRealTimers()
  })

  it("defaults locationScope to current Owned location", () => {
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ getAuthorisedLocationCount: () => 3 })
    )
    expect(module.getSnapshot().locationScope).toBe("current")
    expect(module.getSnapshot().canWidenLocationScope).toBe(true)
  })

  it("shows widen control when authorised for more than one location", () => {
    const multi = createOperatorGlobalSearchModule(
      makeAdapters({ getAuthorisedLocationCount: () => 2 })
    )
    expect(multi.getSnapshot().canWidenLocationScope).toBe(true)

    const single = createOperatorGlobalSearchModule(
      makeAdapters({ getAuthorisedLocationCount: () => 1 })
    )
    expect(single.getSnapshot().canWidenLocationScope).toBe(false)
  })

  it("widenToAllLocations re-runs search with all-scope", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({ ...emptyHits }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getAuthorisedLocationCount: () => 2,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenCalledWith(
        expect.objectContaining({ scope: "current" })
      )
    })

    module.widenToAllLocations()
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().locationScope).toBe("all")
      expect(searchHits).toHaveBeenCalledWith(
        expect.objectContaining({ q: "mo", locationId: 1, scope: "all" })
      )
    })
    vi.useRealTimers()
  })

  it("setLocationScope toggles between current and all and re-searches", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({ ...emptyHits }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getAuthorisedLocationCount: () => 2,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => expect(searchHits).toHaveBeenCalled())

    module.setLocationScope("all")
    expect(module.getSnapshot().locationScope).toBe("all")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenCalledWith(
        expect.objectContaining({ scope: "all" })
      )
    })

    module.setLocationScope("current")
    expect(module.getSnapshot().locationScope).toBe("current")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenLastCalledWith(
        expect.objectContaining({ scope: "current" })
      )
    })
    vi.useRealTimers()
  })

  it("notifyOwnedLocationChanged resets to current scope and refreshes search", async () => {
    vi.useFakeTimers()
    let locationId = 1
    const searchHits = vi.fn(async () => ({
      ...emptyHits,
      guestHits: [makeGuestHit({ locationId })],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getLocationId: () => locationId,
        getAuthorisedLocationCount: () => 2,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => expect(searchHits).toHaveBeenCalled())
    module.widenToAllLocations()
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().locationScope).toBe("all")
    })

    locationId = 7
    module.notifyOwnedLocationChanged()

    expect(module.getSnapshot().locationScope).toBe("current")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchHits).toHaveBeenLastCalledWith(
        expect.objectContaining({
          q: "mo",
          locationId: 7,
          scope: "current",
        })
      )
    })
    vi.useRealTimers()
  })

  it("offers widen from no-results when multi-location and still on current scope", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({ ...emptyHits }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getAuthorisedLocationCount: () => 2,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("zz")
    expect(module.getSnapshot().showWidenFromNoResults).toBe(false)

    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().hitsPending).toBe(false)
      expect(module.getSnapshot().showWidenFromNoResults).toBe(true)
    })

    module.widenToAllLocations()
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().locationScope).toBe("all")
      expect(module.getSnapshot().showWidenFromNoResults).toBe(false)
    })
    vi.useRealTimers()
  })

  it("hides widen from no-results for single-location operators", async () => {
    vi.useFakeTimers()
    const searchHits = vi.fn(async () => ({ ...emptyHits }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchHits,
        getAuthorisedLocationCount: () => 1,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("zz")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().hitsPending).toBe(false)
    })
    expect(module.getSnapshot().showWidenFromNoResults).toBe(false)
    expect(module.getSnapshot().canWidenLocationScope).toBe(false)
    vi.useRealTimers()
  })
})
