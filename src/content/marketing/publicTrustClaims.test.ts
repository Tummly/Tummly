import { describe, expect, it } from "vitest"

import {
  publicFooterTrustBadges,
  publicHomeTrustBadges,
} from "@/content/marketing/publicTrustClaims"

// ENG01 / E3: no scheme or proof is shown publicly until the Director approves
// it with a working proof link (replaced by the 15-indicator model, ENG10.3).
describe("public trust claims", () => {
  it("withholds Home trust badges", () => {
    expect(publicHomeTrustBadges()).toEqual([])
  })

  it("withholds the footer trust badge grid", () => {
    expect(publicFooterTrustBadges()).toEqual([])
  })
})
