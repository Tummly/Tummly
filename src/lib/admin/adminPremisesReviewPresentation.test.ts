import { describe, expect, it } from "vitest"

import {
  canSubmitPremisesReviewReason,
  describePremisesMatch,
  formatMatchAddress,
} from "@/lib/admin/adminPremisesReviewPresentation"
import type { AdminPremisesReviewMatch } from "@/types/adminPremisesReview"

const base: AdminPremisesReviewMatch = {
  locationId: 7,
  restaurantId: 3,
  workspaceName: "Their Kitchen",
  locationName: "Their Main",
  address: "1 High Street",
  city: "Leeds",
  postcode: "LS1 1AA",
  lifecycleStatus: "active",
  sameWorkspace: false,
  premisesKeyMatch: true,
  fsaMatch: false,
  priorPilot: false,
  priorStarterKit: false,
}

describe("canSubmitPremisesReviewReason", () => {
  it("requires a non-blank reason", () => {
    expect(canSubmitPremisesReviewReason("")).toBe(false)
    expect(canSubmitPremisesReviewReason("   ")).toBe(false)
    expect(canSubmitPremisesReviewReason("Confirmed by phone.")).toBe(true)
  })

  it("rejects a reason over 500 characters", () => {
    expect(canSubmitPremisesReviewReason("a".repeat(500))).toBe(true)
    expect(canSubmitPremisesReviewReason("a".repeat(501))).toBe(false)
  })
})

describe("describePremisesMatch", () => {
  it("tags an other-Workspace premises match", () => {
    expect(describePremisesMatch(base)).toEqual([
      "Other Workspace",
      "Same premises",
    ])
  })

  it("adds FSA and prior entitlement tags in a fixed order", () => {
    expect(
      describePremisesMatch({
        ...base,
        sameWorkspace: true,
        premisesKeyMatch: false,
        fsaMatch: true,
        priorPilot: true,
        priorStarterKit: true,
      })
    ).toEqual([
      "Same Workspace",
      "Same FSA id",
      "Prior Pilot",
      "Prior Starter Kit",
    ])
  })
})

describe("formatMatchAddress", () => {
  it("joins the address parts", () => {
    expect(formatMatchAddress(base)).toBe("1 High Street, Leeds, LS1 1AA")
  })

  it("explains a Location that is gone", () => {
    expect(
      formatMatchAddress({ ...base, address: null, city: null, postcode: null })
    ).toBe("Address no longer on file")
  })
})
