import { describe, expect, it } from "vitest"

import {
  OPERATOR_WIZARD_SHELL_MODULE,
  RECOVERY_WIZARD_PAGE_TITLE,
  formatRecoveryLastSavedLabel,
} from "./recoveryWizardChromePresentation"
import {
  GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
  GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
} from "./guestResponseChooserPresentation"

describe("recoveryWizardChromePresentation", () => {
  it("keeps the mid-flow page title as Start recovery", () => {
    expect(RECOVERY_WIZARD_PAGE_TITLE).toBe("Start recovery")
  })

  it("wires Recovery chrome to the shared Operator wizard shell module", () => {
    expect(OPERATOR_WIZARD_SHELL_MODULE).toBe(
      "@/components/dashboard/operator/OperatorWizardShell"
    )
    expect(OPERATOR_WIZARD_SHELL_MODULE).not.toContain("/Feedback/")
  })

  it("owns preparing overlay copy for Recovery callers (shell has no domain defaults)", () => {
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_TITLE.length).toBeGreaterThan(0)
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION.length).toBeGreaterThan(0)
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
