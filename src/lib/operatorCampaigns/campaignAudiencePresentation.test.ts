import { describe, expect, it } from "vitest"

import { formatExcludedReasonLabel } from "./campaignAudiencePresentation"

describe("formatExcludedReasonLabel", () => {
  it("maps handoff exclusion codes to operator labels", () => {
    expect(formatExcludedReasonLabel("not-granted")).toBe("Not granted")
    expect(formatExcludedReasonLabel("withdrawn")).toBe("Withdrawn")
    expect(formatExcludedReasonLabel("suppressed")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("invalid-contact")).toBe("Invalid contact")
  })

  it("maps legacy wire codes to the same handoff labels", () => {
    expect(formatExcludedReasonLabel("opt-out")).toBe("Not granted")
    expect(formatExcludedReasonLabel("suppression")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("channel")).toBe("Suppressed")
    expect(formatExcludedReasonLabel("channel-disabled")).toBe("Suppressed")
  })
})
