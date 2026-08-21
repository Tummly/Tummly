import { describe, expect, it } from "vitest"

import { homeCampaignRecommendationDraftPrefill } from "@/lib/operatorHome/buildHomeCampaignWizardHandoff"
import type { HomeRecommendation } from "@/types/operatorHome"

const campaignRecommendation: HomeRecommendation = {
  type: "recovery-follow-up",
  title: "Follow up on recovery guests",
  draftPrefill: {
    goalId: "follow-up-completed-recovery",
    audienceKey: "completed-recovery-follow-up",
    channel: "email",
    offerStance: "no-offer",
    campaignName: "Recovery follow-up",
    messageSubject: "We want to make this right",
    messageBody: "Thanks for your feedback.",
  },
}

describe("homeCampaignRecommendationDraftPrefill", () => {
  it("returns draftPrefill for campaign types", () => {
    expect(
      homeCampaignRecommendationDraftPrefill(campaignRecommendation)
    ).toEqual(campaignRecommendation.draftPrefill)
  })

  it("returns null for Home-native types", () => {
    expect(
      homeCampaignRecommendationDraftPrefill({
        type: "review-open-feedback",
        title: "Review open feedback",
      })
    ).toBeNull()
  })

  it("returns null when campaign type lacks draftPrefill", () => {
    expect(
      homeCampaignRecommendationDraftPrefill({
        type: "thank-recent-guests",
        title: "Thank recent guests",
      })
    ).toBeNull()
  })
})
