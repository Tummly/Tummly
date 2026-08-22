import { describe, expect, it } from "vitest"

import {
  HOME_RECOMMENDATION_COPY,
  isHomeRecommendationCampaignType,
  primaryCtaLabelForHomeRecommendation,
} from "./homeRecommendationPresentation"
import { RECOMMENDED_EMPTY_COPY } from "./operatorHomeSectionPresentation"

describe("homeRecommendationPresentation", () => {
  it("keeps Home chrome subtitle and honest empty copy", () => {
    expect(HOME_RECOMMENDATION_COPY.subtitle).toContain("guest activity")
    expect(HOME_RECOMMENDATION_COPY.emptyCopy).toBe(RECOMMENDED_EMPTY_COPY)
    expect(HOME_RECOMMENDATION_COPY.retry).toBe("Retry")
    expect(HOME_RECOMMENDATION_COPY.notNow).toBe("Not now")
  })

  it("detects campaign allow-list types for audience CTAs", () => {
    expect(isHomeRecommendationCampaignType("thank-recent-guests")).toBe(true)
    expect(isHomeRecommendationCampaignType("re-engage")).toBe(true)
    expect(isHomeRecommendationCampaignType("recovery-follow-up")).toBe(true)
    expect(isHomeRecommendationCampaignType("review-open-feedback")).toBe(false)
    expect(isHomeRecommendationCampaignType("none")).toBe(false)
  })

  it("labels campaign primary as Review campaign draft", () => {
    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "thank-recent-guests",
        title: "Thank recent guests",
      })
    ).toBe(HOME_RECOMMENDATION_COPY.reviewCampaignDraft)
  })

  it("labels Home-native primary CTAs from domain action", () => {
    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "review-open-feedback",
        action: { kind: "open-feedback", feedbackId: 12 },
      })
    ).toBe(HOME_RECOMMENDATION_COPY.viewFeedback)

    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "thank-or-follow-guest",
        action: { kind: "open-guest", locationGuestId: 9 },
      })
    ).toBe(HOME_RECOMMENDATION_COPY.viewGuestProfile)

    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "thank-or-follow-guest",
        action: { kind: "open-guest", locationGuestId: null },
      })
    ).toBe(HOME_RECOMMENDATION_COPY.viewGuests)

    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "promote-or-fix-offer",
        action: { kind: "open-offer", offerId: 3 },
      })
    ).toBe(HOME_RECOMMENDATION_COPY.editOffer)

    expect(
      primaryCtaLabelForHomeRecommendation({
        type: "promote-or-fix-offer",
        action: { kind: "open-offer", offerId: null },
      })
    ).toBe(HOME_RECOMMENDATION_COPY.createOffer)
  })
})
