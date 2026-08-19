import { describe, expect, it } from "vitest"

import {
  OPERATOR_WIZARD_SELECTABLE_CARD_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS,
  OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS,
  OPERATOR_WIZARD_PREPARING_OVERLAY_BODY_CLASS,
  OPERATOR_WIZARD_PREPARING_OVERLAY_CLASS,
  OPERATOR_WIZARD_SHELL_BODY_CLASS,
  OPERATOR_WIZARD_SHELL_DIALOG_CLASS,
  OPERATOR_WIZARD_SHELL_FOOTER_CLASS,
  OPERATOR_WIZARD_SHELL_HEADER_CLASS,
  OPERATOR_WIZARD_SHELL_INSET_CLASS,
  OPERATOR_WIZARD_SHELL_SURFACE_CLASS,
  OPERATOR_WIZARD_SUCCESS_BODY_GAP_CLASS,
  OPERATOR_WIZARD_SUCCESS_COLUMN_CLASS,
  OPERATOR_WIZARD_SUCCESS_DESCRIPTION_CLASS,
  formatOperatorWizardLastSavedLabel,
} from "./operatorWizardChromePresentation"

describe("operatorWizardChromePresentation", () => {
  it("formats Last saved like Figma footer copy", () => {
    const at = new Date("2026-08-14T14:18:00")

    expect(formatOperatorWizardLastSavedLabel(at)).toBe(
      "Last saved 14 August 2026 at 2:18 PM."
    )
  })

  it("uses AM for morning times", () => {
    const at = new Date("2026-01-05T09:05:00")

    expect(formatOperatorWizardLastSavedLabel(at)).toBe(
      "Last saved 5 January 2026 at 9:05 AM."
    )
  })

  it("uses shell-chrome fill and divider borders on header, body, and footer", () => {
    expect(OPERATOR_WIZARD_SHELL_SURFACE_CLASS).toBe("bg-op-shell-chrome")
    expect(OPERATOR_WIZARD_SHELL_DIALOG_CLASS).toContain("bg-op-shell-chrome")
    expect(OPERATOR_WIZARD_SHELL_HEADER_CLASS).toContain("bg-op-shell-chrome")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("bg-op-shell-chrome")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("border-op-divider")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("rounded-t-[20px]")
    expect(OPERATOR_WIZARD_SHELL_FOOTER_CLASS).toContain("bg-op-shell-chrome")
    expect(OPERATOR_WIZARD_SHELL_FOOTER_CLASS).toContain("border-op-divider")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).not.toContain(
      "bg-op-surface-secondary"
    )
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).not.toContain(
      "border-op-card-border"
    )
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).not.toContain(
      "bg-op-background-primary"
    )
  })

  it("uses divider idle border and gray-60 / gray-1000 fill on selectable cards", () => {
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS).toContain(
      "border-op-divider"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS).toContain(
      "bg-op-color-gray-60"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS).toContain(
      "hover:border-[var(--op-color-gray-550)]"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS).toContain(
      "border-[var(--op-color-gray-550)]"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS).not.toContain(
      "dark:border-op-text-primary"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS).toContain(
      "hover:border-op-divider"
    )
    expect(OPERATOR_WIZARD_SELECTABLE_CARD_CLASS).toContain(
      OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS
    )
  })

  it("centers a 600px column on the Success step", () => {
    expect(OPERATOR_WIZARD_SHELL_INSET_CLASS).toContain("min-[1728px]:px-[200px]")
    expect(OPERATOR_WIZARD_SUCCESS_COLUMN_CLASS).toContain("max-w-[600px]")
    expect(OPERATOR_WIZARD_SUCCESS_COLUMN_CLASS).toContain("w-full")
    expect(OPERATOR_WIZARD_SUCCESS_DESCRIPTION_CLASS).toContain("max-w-[425px]")
    expect(OPERATOR_WIZARD_SUCCESS_BODY_GAP_CLASS).toContain("mt-[52px]")
  })

  it("uses gray-60 / gray-1000 fill and 42px bottom padding on Preparing overlay", () => {
    expect(OPERATOR_WIZARD_PREPARING_OVERLAY_CLASS).toContain("bg-op-color-gray-60")
    expect(OPERATOR_WIZARD_PREPARING_OVERLAY_CLASS).toContain(
      "dark:bg-[var(--op-color-gray-1000)]"
    )
    expect(OPERATOR_WIZARD_PREPARING_OVERLAY_CLASS).not.toContain(
      "bg-op-surface-secondary"
    )
    expect(OPERATOR_WIZARD_PREPARING_OVERLAY_BODY_CLASS).toContain("pb-[42px]")
    expect(OPERATOR_WIZARD_PREPARING_OVERLAY_BODY_CLASS).toContain("pt-[22px]")
  })
})
