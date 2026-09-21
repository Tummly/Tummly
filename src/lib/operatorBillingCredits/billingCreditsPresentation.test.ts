import { describe, expect, it } from "vitest"

import { formatQrPacksLabel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"

describe("formatQrPacksLabel", () => {
  it("formatQrPacksLabel ignores BA state and returns launch copy", () => {
    expect(formatQrPacksLabel("unused")).toBe(
      "One complimentary kit per Active Location"
    )
    expect(formatQrPacksLabel("used")).toBe(
      "One complimentary kit per Active Location"
    )
  })
})
