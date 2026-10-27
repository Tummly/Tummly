import {
  describe,
  expect,
  it,
} from "vitest"

import {
  activeQrCapReachedMessage,
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
    ).toBe(
      "Your current plan’s user allowance has been reached (3 team users). Upgrade your plan to add more users."
    )
  })

  it("words the active QR cap as a Workspace limit", () => {
    expect(
      activeQrCapReachedMessage({
        cap: 25,
        current: 25,
        atCap: true,
        available: true,
      })
    ).toBe(
      "Your plan includes up to 25 active QR placements across your Workspace. Pause one to activate another."
    )
  })
})
