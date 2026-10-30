import { describe, expect, it } from "vitest"

import { PRODUCT_TRUST } from "@/content/marketing/productPage"
import {
  gateTrustClaims,
  publicFooterTrustBadges,
  publicForGroupsTrustBadges,
  publicHomeTrustBadges,
  publicProductTrustCertifications,
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

  it("withholds For Groups trust badges", () => {
    expect(publicForGroupsTrustBadges()).toEqual([])
  })

  it("withholds the Product Trust section certifications", () => {
    expect(publicProductTrustCertifications()).toEqual([])
  })

  it("returns the full list once the claims are approved", () => {
    expect(gateTrustClaims(true, PRODUCT_TRUST.certifications)).toBe(
      PRODUCT_TRUST.certifications,
    )
    expect(gateTrustClaims(false, PRODUCT_TRUST.certifications)).toEqual([])
  })
})
