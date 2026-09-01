import {
  describe,
  expect,
  it,
} from "vitest"

import {
  formatEntitlementUsage,
  normalizePlanEntitlementLimit,
  teamMemberCapReachedMessage,
} from "@/lib/planEntitlements/planEntitlementsPresentation"

describe("planEntitlementsPresentation", () => {
  it("formats usage when available", () => {
    expect(
      formatEntitlementUsage({
        cap: 3,
        current: 2,
        atCap: false,
        available: true,
      })
    ).toBe("2 of 3 used")
  })

  it("returns empty usage when unavailable", () => {
    expect(
      formatEntitlementUsage(
        normalizePlanEntitlementLimit({ available: false })
      )
    ).toBe("")
  })

  it("builds team cap message from limit", () => {
    expect(
      teamMemberCapReachedMessage({
        cap: 3,
        current: 3,
        atCap: true,
        available: true,
      })
    ).toContain("3 team users")
  })
})
