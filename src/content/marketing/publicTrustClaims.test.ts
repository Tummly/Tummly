import { afterEach, describe, expect, it, vi } from "vitest"

import { PRODUCT_TRUST } from "@/content/marketing/productPage"
import {
  PUBLIC_TRUST_CLAIMS_APPROVED,
  gateTrustClaims,
  publicFooterTrustBadges,
  publicForGroupsTrustBadges,
  publicHomeTrustBadges,
  publicProductTrustCertifications,
  trustClaimsVisible,
} from "@/content/marketing/publicTrustClaims"

afterEach(() => {
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
})

function stubPreview(value: string | null) {
  vi.stubGlobal("localStorage", {
    getItem: (key: string) => (key === "tummly.trustPreview" ? value : null),
  })
}

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

describe("trustClaimsVisible release gate", () => {
  it("keeps the approval flag a literal false", () => {
    expect(PUBLIC_TRUST_CLAIMS_APPROVED).toBe(false)
  })

  it("is hidden in a production build even with the preview key", () => {
    vi.stubEnv("DEV", false)
    stubPreview("1")
    expect(trustClaimsVisible()).toBe(false)
  })

  it("is visible in dev only with the preview key set to 1", () => {
    vi.stubEnv("DEV", true)
    stubPreview("1")
    expect(trustClaimsVisible()).toBe(true)
    stubPreview("0")
    expect(trustClaimsVisible()).toBe(false)
    stubPreview(null)
    expect(trustClaimsVisible()).toBe(false)
  })

  it("is hidden when no localStorage exists", () => {
    vi.stubEnv("DEV", true)
    vi.stubGlobal("localStorage", undefined)
    expect(trustClaimsVisible()).toBe(false)
  })
})
