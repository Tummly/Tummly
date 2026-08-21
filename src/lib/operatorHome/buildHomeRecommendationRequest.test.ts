import { describe, expect, it } from "vitest"

import { buildHomeRecommendationRequest } from "./buildHomeRecommendationRequest"

describe("buildHomeRecommendationRequest", () => {
  it("builds a preset window body with resolved from/to ISO strings", () => {
    const now = new Date(2026, 7, 21, 15, 30, 0)
    expect(
      buildHomeRecommendationRequest({
        locationId: 42,
        performanceDateRange: { kind: "preset", presetId: "last7" },
        now,
      })
    ).toEqual({
      locationId: 42,
      overviewDatePreset: "last7",
      from: new Date(2026, 7, 15).toISOString(),
      to: now.toISOString(),
      refresh: false,
    })
  })

  it("builds a custom window body and sets refresh when requested", () => {
    expect(
      buildHomeRecommendationRequest({
        locationId: 9,
        performanceDateRange: {
          kind: "custom",
          startDate: "2026-07-12",
          endDate: "2026-07-18",
        },
        refresh: true,
        now: new Date(2026, 7, 21, 12, 0, 0),
      })
    ).toEqual({
      locationId: 9,
      overviewDatePreset: "custom",
      from: new Date(2026, 6, 12).toISOString(),
      to: new Date(2026, 6, 19).toISOString(),
      refresh: true,
    })
  })
})
