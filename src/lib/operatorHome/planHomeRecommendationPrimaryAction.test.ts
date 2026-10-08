import { describe, expect, it } from "vitest"

import { HOME_RECOMMENDATION_COPY } from "./homeRecommendationPresentation"
import { planHomeRecommendationPrimaryAction } from "./planHomeRecommendationPrimaryAction"

const locationId = 7

describe("planHomeRecommendationPrimaryAction", () => {
  it("starts recovery for review-open-feedback when a single feedback target exists", () => {
    expect(
      planHomeRecommendationPrimaryAction({
        recommendation: {
          type: "review-open-feedback",
          title: "Follow up on open feedback",
          action: { kind: "open-feedback", feedbackId: 42 },
        },
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/feedback?location=7&feedbackId=42&startRecovery=1",
    })
  })

  it("opens Needs attention Feedback when review-open-feedback has no single target", () => {
    expect(
      planHomeRecommendationPrimaryAction({
        recommendation: {
          type: "review-open-feedback",
          title: "Follow up on open feedback",
          action: { kind: "open-feedback", feedbackId: null },
        },
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/feedback?location=7",
      feedbackInbox: { tab: "needs-attention" },
    })
  })

  it("opens Needs attention Feedback when review-open-feedback has no action", () => {
    expect(
      planHomeRecommendationPrimaryAction({
        recommendation: {
          type: "review-open-feedback",
          title: "Follow up on open feedback",
        },
        mode: "multi",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/multi-dashboard/feedback?location=7",
      feedbackInbox: { tab: "needs-attention" },
    })
  })

  it("keeps guest and offer domain destinations unchanged", () => {
    expect(
      planHomeRecommendationPrimaryAction({
        recommendation: {
          type: "thank-or-follow-guest",
          action: { kind: "open-guest", locationGuestId: 9 },
        },
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/guests/9?location=7",
    })

    expect(
      planHomeRecommendationPrimaryAction({
        recommendation: {
          type: "promote-or-fix-offer",
          action: { kind: "open-offer", offerId: 3 },
        },
        mode: "single",
        locationId,
      })
    ).toEqual({
      kind: "navigate",
      path: "/single-dashboard/offers/3?location=7",
    })
  })

  it("uses resolve CTA copy for review-open-feedback labels", () => {
    expect(HOME_RECOMMENDATION_COPY.startRecovery).toBe("Start recovery")
    expect(HOME_RECOMMENDATION_COPY.startRecovery).not.toBe("View feedback")
  })
})
