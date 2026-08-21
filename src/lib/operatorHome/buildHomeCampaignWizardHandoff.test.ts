import { describe, expect, it } from "vitest"

import { buildHomeCampaignWizardHandoff } from "@/lib/operatorHome/buildHomeCampaignWizardHandoff"
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

describe("buildHomeCampaignWizardHandoff", () => {
  it("builds openFromRecommendation input for campaign types with draftPrefill", () => {
    expect(
      buildHomeCampaignWizardHandoff({
        locationId: 12,
        locationName: "Main",
        locationAddress: "1 High Street",
        recommendation: campaignRecommendation,
      })
    ).toEqual({
      locationId: 12,
      locationName: "Main",
      locationAddress: "1 High Street",
      draftPrefill: campaignRecommendation.draftPrefill,
    })
  })

  it("returns null for Home-native types", () => {
    expect(
      buildHomeCampaignWizardHandoff({
        locationId: 12,
        locationName: "Main",
        recommendation: {
          type: "review-open-feedback",
          title: "Review open feedback",
        },
      })
    ).toBeNull()
  })

  it("returns null when campaign type lacks draftPrefill", () => {
    expect(
      buildHomeCampaignWizardHandoff({
        locationId: 12,
        locationName: "Main",
        recommendation: {
          type: "thank-recent-guests",
          title: "Thank recent guests",
        },
      })
    ).toBeNull()
  })
})
