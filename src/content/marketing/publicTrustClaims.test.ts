import { afterEach, describe, expect, it, vi } from "vitest"

import {
  PUBLIC_TRUST_CLAIMS_APPROVED,
  trustClaimsVisible,
  visibleTrustIndicators,
} from "@/content/marketing/publicTrustClaims"
import type { PublicTrustIndicator } from "@/lib/marketing/publicTrustIndicators"

afterEach(() => {
  vi.unstubAllEnvs()
  vi.unstubAllGlobals()
})

function stubPreview(value: string | null) {
  vi.stubGlobal("localStorage", {
    getItem: (key: string) => (key === "tummly.trustPreview" ? value : null),
  })
}

const ONE: PublicTrustIndicator = {
  itemNumber: 1,
  title: "ICO data protection fee",
  status: "Verified",
  statusLabel: "Verified",
  iconKey: "ico-data",
  imageUrl: null,
  altText: "",
  tooltip: "We pay the data protection fee.",
  cta: null,
}

// ENG01 / E3 and CONTRACT §9: public trust indicators render only through the
// release gate, and only when the published request is ready with items.
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

describe("visibleTrustIndicators", () => {
  it("returns the items when the gate is open and the request is ready", () => {
    vi.stubEnv("DEV", true)
    stubPreview("1")
    expect(visibleTrustIndicators({ state: "ready", items: [ONE] })).toEqual([ONE])
  })

  it("returns nothing while loading or after an error", () => {
    vi.stubEnv("DEV", true)
    stubPreview("1")
    expect(visibleTrustIndicators({ state: "loading", items: [ONE] })).toEqual([])
    expect(visibleTrustIndicators({ state: "error", items: [ONE] })).toEqual([])
  })

  it("returns nothing while the gate is closed", () => {
    vi.stubEnv("DEV", true)
    stubPreview(null)
    expect(visibleTrustIndicators({ state: "ready", items: [ONE] })).toEqual([])
  })

  it("returns nothing in a production build, even with the preview key", () => {
    vi.stubEnv("DEV", false)
    stubPreview("1")
    expect(visibleTrustIndicators({ state: "ready", items: [ONE] })).toEqual([])
  })
})
