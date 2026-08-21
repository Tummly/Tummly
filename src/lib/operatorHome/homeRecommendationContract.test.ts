import { describe, expect, it } from "vitest"

import {
  HOME_RECOMMENDATION_CACHE_TTL_MINUTES,
  HOME_RECOMMENDATION_TYPES,
  buildHomeRecommendationCacheKey,
  isHomeRecommendationType,
} from "./homeRecommendationContract"

describe("homeRecommendationContract allow-list", () => {
  it("includes grilling-locked types and none only", () => {
    expect([...HOME_RECOMMENDATION_TYPES]).toEqual([
      "review-open-feedback",
      "thank-or-follow-guest",
      "promote-or-fix-offer",
      "thank-recent-guests",
      "re-engage",
      "recovery-follow-up",
      "none",
    ])
  })

  it("accepts allow-listed types and rejects Reports or setup types", () => {
    expect(isHomeRecommendationType("review-open-feedback")).toBe(true)
    expect(isHomeRecommendationType("none")).toBe(true)
    expect(isHomeRecommendationType("weekly-brief-ready")).toBe(false)
    expect(isHomeRecommendationType("setup-checklist")).toBe(false)
    expect(isHomeRecommendationType("quiet-time")).toBe(false)
  })
})

describe("homeRecommendationContract cache key", () => {
  it("uses a 30-minute TTL matching Campaigns recommendation", () => {
    expect(HOME_RECOMMENDATION_CACHE_TTL_MINUTES).toBe(30)
  })

  it("keys named presets by operator, location, and preset only", () => {
    expect(
      buildHomeRecommendationCacheKey({
        operatorUserId: 7,
        locationId: 42,
        overviewDatePreset: "last7",
        from: new Date("2026-08-14T00:00:00.000Z"),
        to: new Date("2026-08-21T12:00:00.000Z"),
      })
    ).toBe("home-recommendation:7:42:last7")
  })

  it("keys custom windows by UTC calendar day bounds", () => {
    expect(
      buildHomeRecommendationCacheKey({
        operatorUserId: 7,
        locationId: 42,
        overviewDatePreset: "custom",
        from: new Date("2026-07-12T10:15:00.000Z"),
        to: new Date("2026-07-18T22:45:00.000Z"),
      })
    ).toBe("home-recommendation:7:42:custom:2026-07-12:2026-07-18")
  })
})
