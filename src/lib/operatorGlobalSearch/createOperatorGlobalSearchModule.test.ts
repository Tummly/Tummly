import { describe, expect, it, vi } from "vitest"

import {
  EMPTY_AI_SUGGESTIONS,
  createOperatorGlobalSearchModule,
  isGlobalSearchOpenShortcut,
  shortcutModifierLabel,
  type OperatorGlobalSearchAdapters,
} from "./createOperatorGlobalSearchModule"

function makeAdapters(
  overrides: Partial<OperatorGlobalSearchAdapters> = {}
): OperatorGlobalSearchAdapters {
  return {
    handoffSuggestionToAssistant: vi.fn(),
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
})
