import { describe, expect, it } from "vitest"

import {
  ASSISTANT_ADD_CREDITS_LABEL,
  ASSISTANT_CHOOSE_PLAN_LABEL,
  ASSISTANT_COMPOSER_CIRCLE_CLASS,
  ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS,
  ASSISTANT_COMPOSER_SEND_ICON_CLASS,
  ASSISTANT_CREDITS_STUB_ALLOWANCE,
  ASSISTANT_CREDITS_STUB_REMAINING,
  ASSISTANT_CREDITS_STUB_REMAINING_LINE,
  ASSISTANT_UPDATE_PAYMENT_LABEL,
  ASSISTANT_VIEW_USAGE_LABEL,
  SHELL_AI_ADD_CREDITS_LABEL,
  SHELL_AI_CREDITS_TITLE,
  SHELL_AI_VIEW_USAGE_LABEL,
  assistantComposerBorderClass,
  assistantComposerFieldClass,
  assistantComposerMicActive,
  assistantComposerShellClass,
  assistantComposerTextareaClass,
  assistantCreditsAddCreditsHref,
  assistantCreditsDepleted,
  assistantCreditsRemainingLine,
  assistantCreditsRestorationHelper,
  assistantCreditsShowAddCredits,
  assistantCreditsShowViewUsage,
  assistantCreditsViewUsageHref,
  isAssistantAccountLocked,
  resolveAssistantAccountLockCause,
  resolveShellAiCreditsUsed,
  shellAiCreditsButtonLabel,
  shellAiCreditsFillRatio,
  shellAiCreditsLeftLine,
  shellAiCreditsUsedLine,
} from "./assistantCreditsPresentation"

describe("assistantCreditsPresentation", () => {
  it("formats the remaining line as monthly AI credits", () => {
    expect(ASSISTANT_CREDITS_STUB_REMAINING).toBe(20)
    expect(ASSISTANT_CREDITS_STUB_ALLOWANCE).toBe(20)
    expect(assistantCreditsRemainingLine(20, 20)).toBe(
      "20 of 20 monthly AI credits remaining"
    )
    expect(assistantCreditsRemainingLine(0, 20)).toBe(
      "0 of 20 monthly AI credits remaining"
    )
    expect(ASSISTANT_CREDITS_STUB_REMAINING_LINE).toBe(
      "20 of 20 monthly AI credits remaining"
    )
    expect(ASSISTANT_VIEW_USAGE_LABEL).toBe("View usage")
    expect(ASSISTANT_ADD_CREDITS_LABEL).toBe("Add credits")
  })

  it("formats shell AI credits button and popover copy", () => {
    expect(shellAiCreditsButtonLabel(100)).toBe("100 AI credits")
    expect(shellAiCreditsButtonLabel(0)).toBe("0 AI credits")
    expect(shellAiCreditsUsedLine(0, 100)).toBe("0 of 100 AI credits used")
    expect(shellAiCreditsLeftLine(100)).toBe("100 AI credits left")
    expect(shellAiCreditsFillRatio(100, 0)).toBe(0)
    expect(shellAiCreditsFillRatio(70, 30)).toBe(0.3)
    expect(resolveShellAiCreditsUsed({ remaining: 80, allowance: 100 })).toBe(
      20
    )
    expect(
      resolveShellAiCreditsUsed({
        remaining: 80,
        allowance: 100,
        usedThisCycle: 12,
      })
    ).toBe(12)
    expect(SHELL_AI_CREDITS_TITLE).toBe("AI credit usage")
    expect(SHELL_AI_VIEW_USAGE_LABEL).toBe("View AI usage")
    expect(SHELL_AI_ADD_CREDITS_LABEL).toBe("Add AI credits")
  })

  it("treats remaining at or below 0 as depleted", () => {
    expect(assistantCreditsDepleted(1)).toBe(false)
    expect(assistantCreditsDepleted(0)).toBe(true)
    expect(assistantCreditsDepleted(-1)).toBe(true)
  })

  it("shows View usage for View and Manage; Add credits only for Manage writers", () => {
    expect(assistantCreditsShowViewUsage("view")).toBe(true)
    expect(assistantCreditsShowViewUsage("manage")).toBe(true)
    expect(assistantCreditsShowViewUsage("none")).toBe(false)

    expect(
      assistantCreditsShowAddCredits({
        accessLevel: "view",
        permissionRole: "Admin",
      })
    ).toBe(false)
    expect(
      assistantCreditsShowAddCredits({
        accessLevel: "manage",
        permissionRole: "Owner",
      })
    ).toBe(true)
    expect(
      assistantCreditsShowAddCredits({
        accessLevel: "manage",
        permissionRole: "Billing Admin",
      })
    ).toBe(true)
    expect(
      assistantCreditsShowAddCredits({
        accessLevel: "manage",
        permissionRole: "Admin",
      })
    ).toBe(true)
    expect(
      assistantCreditsShowAddCredits({
        accessLevel: "manage",
        permissionRole: "",
      })
    ).toBe(true)
  })

  it("builds View usage and Add credits landing hrefs", () => {
    expect(assistantCreditsViewUsageHref("multi", 7)).toBe(
      "/multi-dashboard/settings/billing-credits?location=7&tab=credits-usage"
    )
    expect(assistantCreditsAddCreditsHref("single", 42)).toBe(
      "/single-dashboard/settings/billing-credits/manage-plan?location=42&section=credit-top-ups&channel=ai"
    )
  })

  it("resolves Soft lock and Dormant causes and restoration helpers", () => {
    expect(isAssistantAccountLocked("Active")).toBe(false)
    expect(isAssistantAccountLocked("Soft lock")).toBe(true)
    expect(isAssistantAccountLocked("Dormant")).toBe(true)

    expect(
      resolveAssistantAccountLockCause({
        billingStatus: "Soft lock",
        isPilot: true,
      })
    ).toBe("unpaid-pilot")
    expect(
      resolveAssistantAccountLockCause({
        billingStatus: "Dormant",
        isPilot: false,
      })
    ).toBe("dunning")

    expect(
      assistantCreditsRestorationHelper({
        lockCause: "unpaid-pilot",
        accessLevel: "manage",
        permissionRole: "Owner",
        mode: "multi",
        locationId: 7,
      })
    ).toEqual({
      label: ASSISTANT_CHOOSE_PLAN_LABEL,
      href: "/multi-dashboard/settings/billing-credits/manage-plan?location=7",
    })

    expect(
      assistantCreditsRestorationHelper({
        lockCause: "unpaid-pilot",
        accessLevel: "manage",
        permissionRole: "Billing Admin",
        mode: "multi",
        locationId: 7,
      })
    ).toBeNull()

    expect(
      assistantCreditsRestorationHelper({
        lockCause: "dunning",
        accessLevel: "manage",
        permissionRole: "Billing Admin",
        mode: "single",
        locationId: 42,
      })
    ).toEqual({
      label: ASSISTANT_UPDATE_PAYMENT_LABEL,
      href: "/single-dashboard/settings/billing-credits?location=42&tab=payment-invoices",
    })

    expect(
      assistantCreditsRestorationHelper({
        lockCause: "dunning",
        accessLevel: "view",
        permissionRole: "Admin",
        mode: "single",
        locationId: 42,
      })
    ).toBeNull()
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
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerShellClass(true)).toContain(
      "border-op-assistant-composer-border"
    )
    expect(assistantComposerShellClass(true)).not.toContain("ring-3")
    expect(assistantComposerShellClass(true)).not.toContain("ring-ring/50")
    expect(assistantComposerShellClass(true)).not.toContain("border-ring")
    expect(assistantComposerShellClass(true)).not.toContain(
      "border-op-text-primary"
    )
    expect(assistantComposerFieldClass("mic")).toContain("border-0")
    expect(assistantComposerFieldClass("mic")).not.toContain(
      "border-op-assistant-composer-border"
    )
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
