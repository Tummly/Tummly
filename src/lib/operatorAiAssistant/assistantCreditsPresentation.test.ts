import { describe, expect, it } from "vitest"

import {
  ASSISTANT_ADD_CREDITS_LABEL,
  ASSISTANT_COMPOSER_CIRCLE_CLASS,
  ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS,
  ASSISTANT_COMPOSER_SEND_ICON_CLASS,
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

  it("puts rest and focus chrome on the outer composer shell, not the field", () => {
    expect(assistantComposerBorderClass(false)).toBe(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerBorderClass(true)).toBe(
      "border-ring ring-3 ring-ring/50"
    )
    expect(assistantComposerShellClass(false)).toContain(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerShellClass(true)).toContain("border-ring")
    expect(assistantComposerShellClass(true)).toContain("ring-3")
    expect(assistantComposerShellClass(true)).toContain("ring-ring/50")
    expect(assistantComposerShellClass(true)).not.toContain(
      "border-op-text-primary"
    )
    expect(assistantComposerFieldClass("mic")).toContain("border-0")
    expect(assistantComposerFieldClass("mic")).not.toContain(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerFieldClass("mic")).not.toContain("border-ring")
  })

  it("uses a shorter composer field below md and Figma 144px from md", () => {
    const className = assistantComposerFieldClass("mic")

    expect(className).toContain("min-h-[112px]")
    expect(className).toContain("p-4")
    expect(className).toContain("md:min-h-[144px]")
    expect(className).toContain("md:p-[21px]")
  })

  it("paints Send smaller than the mic circle", () => {
    expect(ASSISTANT_COMPOSER_CIRCLE_CLASS).toContain("size-10")
    expect(ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS).toContain("size-8")
    expect(ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS).not.toContain("size-10")
    expect(ASSISTANT_COMPOSER_SEND_ICON_CLASS).toBe("size-4")
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
