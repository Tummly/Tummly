import { describe, expect, it, vi } from "vitest"

import {
  EMPTY_AI_SUGGESTIONS,
  createOperatorGlobalSearchModule,
  isGlobalSearchOpenShortcut,
  shortcutModifierLabel,
  type OperatorGlobalSearchAdapters,
  type OperatorGlobalSearchGuestHit,
} from "./createOperatorGlobalSearchModule"

function makeGuestHit(
  overrides: Partial<OperatorGlobalSearchGuestHit> = {}
): OperatorGlobalSearchGuestHit {
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

function makeAdapters(
  overrides: Partial<OperatorGlobalSearchAdapters> = {}
): OperatorGlobalSearchAdapters {
  return {
    handoffSuggestionToAssistant: vi.fn(),
    searchGuests: vi.fn(async () => ({ hits: [] })),
    navigateToGuestProfile: vi.fn(),
    getLocationId: () => 1,
    debounceMs: 0,
    ...overrides,
  }
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
    })
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("keeps getSnapshot identity until publish", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())
    const before = module.getSnapshot()
    expect(module.getSnapshot()).toBe(before)
    module.open()
    expect(module.getSnapshot()).not.toBe(before)
    expect(module.getSnapshot()).toBe(module.getSnapshot())
  })

  it("opens and closes the overlay; Esc closes when open", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())

    module.open()
    expect(module.getSnapshot().open).toBe(true)

    module.close()
    expect(module.getSnapshot().open).toBe(false)

    module.open()
    module.dismissFromEscape()
    expect(module.getSnapshot().open).toBe(false)
  })

  it("opens from the Global Search shortcut when closed", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())

    const handled = module.handleShortcutKeydown({
      key: "k",
      metaKey: true,
      ctrlKey: false,
      isApplePlatform: true,
    })

    expect(handled).toBe(true)
    expect(module.getSnapshot().open).toBe(true)
  })

  it("does not toggle closed on a second open shortcut while open", () => {
    const module = createOperatorGlobalSearchModule(makeAdapters())
    module.open()

    const handled = module.handleShortcutKeydown({
      key: "k",
      metaKey: false,
      ctrlKey: true,
      isApplePlatform: false,
    })

    expect(handled).toBe(false)
    expect(module.getSnapshot().open).toBe(true)
  })

  it("selects an AI suggestion: closes Search, fills Assistant composer, does not send", () => {
    const handoffSuggestionToAssistant = vi.fn()
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ handoffSuggestionToAssistant })
    )
    module.open()
    module.setQuery("partial")

    const first = module.getSnapshot().emptySuggestions[0]
    expect(first).toBeDefined()

    module.selectSuggestion(first!.id)

    expect(module.getSnapshot()).toMatchObject({
      open: false,
      query: "",
      guestHits: [],
      hitsPending: false,
    })
    expect(handoffSuggestionToAssistant).toHaveBeenCalledTimes(1)
    expect(handoffSuggestionToAssistant).toHaveBeenCalledWith(first!.prompt)
  })

  it("hands off the prompt to Assistant without sending (Send stays Assistant-gated)", () => {
    const handoffSuggestionToAssistant = vi.fn()
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ handoffSuggestionToAssistant })
    )
    module.open()

    const suggestion = module.getSnapshot().emptySuggestions[1]!
    module.selectSuggestion(suggestion.id)

    expect(handoffSuggestionToAssistant).toHaveBeenCalledWith(suggestion.prompt)
    expect(module.getSnapshot().open).toBe(false)
  })

  it("ignores unknown suggestion ids without closing", () => {
    const handoffSuggestionToAssistant = vi.fn()
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ handoffSuggestionToAssistant })
    )
    module.open()

    module.selectSuggestion("missing-id")

    expect(module.getSnapshot().open).toBe(true)
    expect(handoffSuggestionToAssistant).not.toHaveBeenCalled()
  })

  it("keeps Search available when handoff throws (Search failure must not break shell)", () => {
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

  it("updates query immediately and debounces guest search without blocking typing", async () => {
    vi.useFakeTimers()
    const searchGuests = vi.fn(async () => ({
      hits: [makeGuestHit()],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchGuests, debounceMs: 300 })
    )
    module.open()

    module.setQuery("mo")
    expect(module.getSnapshot().query).toBe("mo")
    expect(searchGuests).not.toHaveBeenCalled()
    expect(module.getSnapshot().hitsPending).toBe(false)

    await vi.advanceTimersByTimeAsync(300)
    await vi.waitFor(() => {
      expect(searchGuests).toHaveBeenCalledWith(
        expect.objectContaining({ q: "mo", locationId: 1 })
      )
      expect(module.getSnapshot().guestHits).toEqual([makeGuestHit()])
      expect(module.getSnapshot().hitsPending).toBe(false)
    })
    vi.useRealTimers()
  })

  it("ignores stale guest search results when a newer query wins", async () => {
    vi.useFakeTimers()
    let resolveSlow!: (value: { hits: OperatorGlobalSearchGuestHit[] }) => void
    const slow = new Promise<{ hits: OperatorGlobalSearchGuestHit[] }>(
      (resolve) => {
        resolveSlow = resolve
      }
    )
    const searchGuests = vi
      .fn()
      .mockImplementationOnce(() => slow)
      .mockResolvedValueOnce({
        hits: [makeGuestHit({ id: "99", title: "Morgan" })],
      })

    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchGuests, debounceMs: 0 })
    )
    module.open()

    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    expect(module.getSnapshot().hitsPending).toBe(true)

    module.setQuery("mor")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(searchGuests).toHaveBeenCalledTimes(2)
    })
    await vi.waitFor(() => {
      expect(module.getSnapshot().guestHits[0]?.title).toBe("Morgan")
    })

    resolveSlow({ hits: [makeGuestHit({ id: "12", title: "Mohamed" })] })
    await Promise.resolve()

    expect(module.getSnapshot().guestHits[0]?.title).toBe("Morgan")
    expect(module.getSnapshot().hitsPending).toBe(false)
    vi.useRealTimers()
  })

  it("clears guest hits when query drops below two characters", async () => {
    vi.useFakeTimers()
    const searchGuests = vi.fn(async () => ({
      hits: [makeGuestHit()],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({ searchGuests, debounceMs: 0 })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    await vi.waitFor(() => {
      expect(module.getSnapshot().guestHits).toHaveLength(1)
    })

    module.setQuery("m")
    expect(module.getSnapshot().query).toBe("m")
    expect(module.getSnapshot().guestHits).toEqual([])
    expect(module.getSnapshot().hitsPending).toBe(false)
    vi.useRealTimers()
  })

  it("selectGuestHit closes Search and navigates to Guest profile", async () => {
    vi.useFakeTimers()
    const navigateToGuestProfile = vi.fn()
    const searchGuests = vi.fn(async () => ({
      hits: [makeGuestHit({ id: "42", locationId: 7 })],
    }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchGuests,
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
    })
    expect(navigateToGuestProfile).toHaveBeenCalledWith(42, 7)
    vi.useRealTimers()
  })

  it("does not search when overlay is closed or location is missing", async () => {
    vi.useFakeTimers()
    const searchGuests = vi.fn(async () => ({ hits: [] }))
    const module = createOperatorGlobalSearchModule(
      makeAdapters({
        searchGuests,
        getLocationId: () => null,
        debounceMs: 0,
      })
    )
    module.open()
    module.setQuery("mo")
    await vi.advanceTimersByTimeAsync(0)
    expect(searchGuests).not.toHaveBeenCalled()
    expect(module.getSnapshot().guestHits).toEqual([])
    vi.useRealTimers()
  })
})
