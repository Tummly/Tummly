import { describe, expect, it } from "vitest"

import {
  OPERATOR_WIZARD_SHELL_BODY_CLASS,
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

  it("uses surface-secondary body fill and card border under the close header", () => {
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("bg-op-surface-secondary")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("border-op-card-border")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).toContain("rounded-t-[20px]")
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).not.toContain(
      "bg-op-background-primary"
    )
    expect(OPERATOR_WIZARD_SHELL_BODY_CLASS).not.toContain(
      "bg-[var(--op-color-gray-980)]"
    )
  })
})
