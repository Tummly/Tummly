import { describe, expect, it } from "vitest"

import {
  RECOVERY_WIZARD_PAGE_TITLE,
  formatRecoveryLastSavedLabel,
} from "./recoveryWizardChromePresentation"

describe("recoveryWizardChromePresentation", () => {
  it("keeps the mid-flow page title as Start recovery", () => {
    expect(RECOVERY_WIZARD_PAGE_TITLE).toBe("Start recovery")
  })

  it("formats Last saved like Figma footer copy", () => {
    // Fixed instant — afternoon so AM/PM is unambiguous.
    const at = new Date("2026-08-14T14:18:00")

    expect(formatRecoveryLastSavedLabel(at)).toBe(
      "Last saved 14 August 2026 at 2:18 PM."
    )
  })

  it("uses AM for morning times", () => {
    const at = new Date("2026-01-05T09:05:00")

    expect(formatRecoveryLastSavedLabel(at)).toBe(
      "Last saved 5 January 2026 at 9:05 AM."
    )
  })
})
