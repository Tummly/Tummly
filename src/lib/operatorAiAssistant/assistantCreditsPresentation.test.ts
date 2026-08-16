import { describe, expect, it } from "vitest"

import {
  ASSISTANT_ADD_CREDITS_LABEL,
  ASSISTANT_CREDITS_STUB_ALLOWANCE,
  ASSISTANT_CREDITS_STUB_REMAINING,
  ASSISTANT_CREDITS_STUB_REMAINING_LINE,
  ASSISTANT_VIEW_USAGE_LABEL,
  assistantComposerBorderClass,
  assistantComposerFieldClass,
  assistantComposerMicActive,
  assistantComposerShellClass,
  assistantComposerTextareaClass,
  assistantCreditsRemainingLine,
} from "./assistantCreditsPresentation"

describe("assistantCreditsPresentation", () => {
  it("formats the Figma remaining line from stub remaining and allowance", () => {
    expect(ASSISTANT_CREDITS_STUB_REMAINING).toBe(20)
    expect(ASSISTANT_CREDITS_STUB_ALLOWANCE).toBe(20)
    expect(assistantCreditsRemainingLine(20, 20)).toBe(
      "20 of 20 monthly AI actions remaining"
    )
    expect(ASSISTANT_CREDITS_STUB_REMAINING_LINE).toBe(
      "20 of 20 monthly AI actions remaining"
    )
    expect(ASSISTANT_VIEW_USAGE_LABEL).toBe("View usage")
    expect(ASSISTANT_ADD_CREDITS_LABEL).toBe("Add credits")
  })

  it("lifts the composer field fill while the mic is recording or transcribing", () => {
    expect(assistantComposerMicActive("mic")).toBe(false)
    expect(assistantComposerMicActive("tick_cancel")).toBe(true)
    expect(assistantComposerMicActive("loader")).toBe(true)
    expect(assistantComposerFieldClass("mic")).toContain(
      "bg-op-assistant-composer-background"
    )
    expect(assistantComposerFieldClass("mic")).not.toContain(
      "bg-op-assistant-composer-recording-background"
    )
    expect(assistantComposerFieldClass("tick_cancel")).toContain(
      "bg-op-assistant-composer-recording-background"
    )
    expect(assistantComposerFieldClass("loader")).toContain(
      "bg-op-assistant-composer-recording-background"
    )
  })

  it("puts rest and focus border on the outer composer shell, not the field", () => {
    expect(assistantComposerBorderClass(false)).toBe(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerBorderClass(true)).toBe("border-op-text-primary")
    expect(assistantComposerShellClass(false)).toContain(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerShellClass(true)).toContain("border-op-text-primary")
    expect(assistantComposerFieldClass("mic")).toContain("border-0")
    expect(assistantComposerFieldClass("mic")).not.toContain(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerFieldClass("mic")).not.toContain(
      "border-op-text-primary"
    )
  })

  it("uses primary typed text, muted placeholder text, and no inner focus chrome", () => {
    const className = assistantComposerTextareaClass()

    expect(className).toContain("text-op-text-primary")
    expect(className).toContain(
      "placeholder:text-[var(--op-color-gray-550)]"
    )
    expect(className).toContain("focus-visible:border-0")
    expect(className).toContain("focus-visible:ring-0")
  })
})
