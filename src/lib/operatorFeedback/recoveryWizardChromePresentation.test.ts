import { readFileSync } from "node:fs"
import { resolve } from "node:path"

import { describe, expect, it } from "vitest"

import {
  RECOVERY_WIZARD_PAGE_TITLE,
  formatRecoveryLastSavedLabel,
} from "./recoveryWizardChromePresentation"
import {
  GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION,
  GUEST_RESPONSE_PREPARING_OVERLAY_TITLE,
} from "./guestResponseChooserPresentation"

const respondToGuestWizardSource = readFileSync(
  resolve(
    process.cwd(),
    "src/components/dashboard/operator/Feedback/RespondToGuestWizard.tsx"
  ),
  "utf8"
)

describe("recoveryWizardChromePresentation", () => {
  it("keeps the mid-flow page title as Start recovery", () => {
    expect(RECOVERY_WIZARD_PAGE_TITLE).toBe("Start recovery")
  })

  it("wires Respond to guest to OperatorWizardShell with caller preparing copy", () => {
    expect(respondToGuestWizardSource).toContain(
      'from "@/components/dashboard/operator/OperatorWizardShell"'
    )
    expect(respondToGuestWizardSource).toContain("<OperatorWizardShell")
    expect(respondToGuestWizardSource).not.toContain("RecoveryWizardShell")
    expect(respondToGuestWizardSource).toContain(
      "GUEST_RESPONSE_PREPARING_OVERLAY_TITLE"
    )
    expect(respondToGuestWizardSource).toContain(
      "GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION"
    )
  })

  it("owns preparing overlay copy for Recovery callers (shell has no domain defaults)", () => {
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_TITLE).toBe(
      "Preparing response draft…"
    )
    expect(GUEST_RESPONSE_PREPARING_OVERLAY_DESCRIPTION.length).toBeGreaterThan(0)
  })

  it("re-exports Last saved formatter from shared Operator chrome", () => {
    const at = new Date("2026-08-14T14:18:00")

    expect(formatRecoveryLastSavedLabel(at)).toBe(
      "Last saved 14 August 2026 at 2:18 PM."
    )
  })
})
