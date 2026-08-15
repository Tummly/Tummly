import { describe, expect, it } from "vitest"

import {
  ASSISTANT_WAIT_ICON_CLASS,
  ASSISTANT_WAIT_PHRASE_MS,
  ASSISTANT_WAIT_PHRASES,
  ASSISTANT_WAIT_TEXT_CLASS,
  assistantWaitPhraseAt,
} from "./assistantWaitPresentation"

describe("assistantWaitPresentation", () => {
  it("starts on Working and cycles Claude-style wait phrases", () => {
    expect(ASSISTANT_WAIT_PHRASES[0]).toBe("Working…")
    expect(assistantWaitPhraseAt(0, false)).toBe("Working…")
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS - 1, false)).toBe(
      "Working…"
    )
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS, false)).toBe(
      "Thinking…"
    )
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS * 2, false)).toBe(
      "Analyzing…"
    )
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS * 3, false)).toBe(
      "Looking up…"
    )
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS * 4, false)).toBe(
      "Working…"
    )
  })

  it("keeps Working when motion is reduced", () => {
    expect(assistantWaitPhraseAt(ASSISTANT_WAIT_PHRASE_MS * 3, true)).toBe(
      "Working…"
    )
  })

  it("spins the AI icon and shimmers the wait line", () => {
    expect(ASSISTANT_WAIT_ICON_CLASS).toContain("animate-spin")
    expect(ASSISTANT_WAIT_TEXT_CLASS).toContain("assistant-wait-shimmer")
  })
})
