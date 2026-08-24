import { describe, expect, it } from "vitest"

import { buildOfferRecommendationRequest } from "@/lib/operatorOffers/offerRecommendationContract"

describe("buildOfferRecommendationRequest", () => {
  it("sends locationId and optional refresh only — not Overview KPI range", () => {
    expect(
      buildOfferRecommendationRequest({ locationId: 42 })
    ).toEqual({
      locationId: 42,
      refresh: false,
    })
    expect(
      buildOfferRecommendationRequest({ locationId: 42, refresh: true })
    ).toEqual({
      locationId: 42,
      refresh: true,
    })
    expect(
      Object.keys(buildOfferRecommendationRequest({ locationId: 7 }))
    ).toEqual(["locationId", "refresh"])
  })
})
