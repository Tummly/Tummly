import { describe, expect, it } from "vitest"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  formatWeeklyBriefDataSources,
  formatWeeklyBriefGeneratedAt,
  mapWeeklyBriefRecommendedActionFact,
  mapWeeklyBriefSuggestedCampaign,
  mockWeeklyBriefData,
  planWeeklyBriefFeedbackFollowUpCta,
  planWeeklyBriefRecommendedActionCta,
  planWeeklyBriefSuggestedCampaignCta,
  shouldShowWeeklyBriefFeedbackSummary,
  shouldShowWeeklyBriefRecommendedActions,
  shouldShowWeeklyBriefSuggestedCampaign,
  shouldShowWeeklyBriefWhatChanged,
  weeklyBriefMarkAsReviewedLabel,
} from "@/lib/operatorReports/weeklyBriefPresentation"

describe("weeklyBriefPresentation", () => {
  it("exports expected page copy constants", () => {
    expect(WEEKLY_BRIEF_PAGE_COPY.pageTitle).toBe("Weekly Brief")
    expect(WEEKLY_BRIEF_PAGE_COPY.emptyTitle).toBe("No weekly brief yet")
    expect(WEEKLY_BRIEF_PAGE_COPY.downloadPdf).toBe("Download PDF")
    expect(WEEKLY_BRIEF_PAGE_COPY.markAsReviewed).toBe("Mark as reviewed")
    expect(WEEKLY_BRIEF_PAGE_COPY.reviewed).toBe("Reviewed")
    expect(WEEKLY_BRIEF_PAGE_COPY.periodLabel).toBe("Period")
    expect(WEEKLY_BRIEF_PAGE_COPY.executiveSummaryTitle).toBe(
      "Executive summary"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.whatChangedTitle).toBe("What changed")
    expect(WEEKLY_BRIEF_PAGE_COPY.feedbackSummaryTitle).toBe(
      "Feedback summary"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.recommendedActionsTitle).toBe(
      "Recommended actions"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.suggestedCampaignTitle).toBe(
      "Suggested campaign"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.reviewFollowUpQueue).toBe(
      "Review follow-up queue"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.openFollowUpQueue).toBe(
      "Open follow-up queue"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.reviewCampaign).toBe("Review campaign")
  })

  it("contains populated mock dataset for all 6 sections", () => {
    expect(mockWeeklyBriefData.period).toBe("6–12 July")
    expect(mockWeeklyBriefData.changes.length).toBe(5)
    expect(mockWeeklyBriefData.recommendedActions.length).toBe(3)
    expect(mockWeeklyBriefData.suggestedCampaign.title).toBe("Quiet-day boost")
  })

  it("formats generatedAtUtc as en-GB day month, HH:mm", () => {
    expect(formatWeeklyBriefGeneratedAt("2026-07-13T07:30:00.000Z")).toMatch(
      /^\d{1,2} July, \d{2}:\d{2}$/
    )
  })

  it("joins data source labels with commas", () => {
    expect(
      formatWeeklyBriefDataSources(["Capture", "Feedback", "Offers"])
    ).toBe("Capture, Feedback, Offers")
  })

  it("switches Mark as reviewed label when durable reviewedAtUtc is set", () => {
    expect(weeklyBriefMarkAsReviewedLabel(null)).toBe("Mark as reviewed")
    expect(weeklyBriefMarkAsReviewedLabel("")).toBe("Mark as reviewed")
    expect(weeklyBriefMarkAsReviewedLabel("2026-08-19T09:15:00Z")).toBe(
      "Reviewed"
    )
  })

  it("shows What changed only when rows exist", () => {
    expect(shouldShowWeeklyBriefWhatChanged([])).toBe(false)
    expect(shouldShowWeeklyBriefWhatChanged(null)).toBe(false)
    expect(
      shouldShowWeeklyBriefWhatChanged([
        {
          area: "QR scans",
          change: "+12%",
          meaning: "More guests are engaging with your QR placements.",
        },
      ])
    ).toBe(true)
  })

  it("shows Feedback summary only when facts exist", () => {
    expect(shouldShowWeeklyBriefFeedbackSummary(null)).toBe(false)
    expect(
      shouldShowWeeklyBriefFeedbackSummary({
        text: "   ",
        subtitle: "Based on private feedback submitted between 6–12 July.",
        needsAttentionCount: 0,
      })
    ).toBe(false)
    expect(
      shouldShowWeeklyBriefFeedbackSummary({
        text: "42 private feedback messages this week.",
        subtitle: "Based on private feedback submitted between 6–12 July.",
        needsAttentionCount: 6,
      })
    ).toBe(true)
  })

  it("plans Review follow-up queue to Feedback needs-attention", () => {
    expect(
      planWeeklyBriefFeedbackFollowUpCta({ mode: "single", locationId: 42 })
    ).toEqual({
      path: "/single-dashboard/feedback?location=42",
      feedbackInbox: { tab: "needs-attention" },
    })
    expect(
      planWeeklyBriefFeedbackFollowUpCta({ mode: "multi", locationId: 7 })
    ).toEqual({
      path: "/multi-dashboard/feedback?location=7",
      feedbackInbox: { tab: "needs-attention" },
    })
  })

  it("maps recommended-action facts to card copy", () => {
    expect(
      mapWeeklyBriefRecommendedActionFact({
        kind: "feedback-needs-attention",
        count: 6,
        target: "feedback-needs-attention",
      })
    ).toEqual({
      id: "feedback-needs-attention",
      title: "Follow up with 6 guests",
      subtitle:
        "These guests shared contact details and may need a response.",
      cta: "Open follow-up queue",
      target: "feedback-needs-attention",
    })

    expect(
      mapWeeklyBriefRecommendedActionFact({
        kind: "feedback-needs-attention",
        count: 6,
        target: "feedback-needs-attention",
        title: "Follow up with six guests this week",
        subtitle: "AI enriched subtitle for needs attention.",
      })
    ).toEqual({
      id: "feedback-needs-attention",
      title: "Follow up with six guests this week",
      subtitle: "AI enriched subtitle for needs attention.",
      cta: "Open follow-up queue",
      target: "feedback-needs-attention",
    })

    expect(
      mapWeeklyBriefRecommendedActionFact({
        kind: "repeated-invalid",
        count: 4,
        target: "redemption-log",
      })
    ).toEqual({
      id: "repeated-invalid",
      title: "Repeated invalid attempts",
      subtitle:
        "4 attempts this period were already-used or expired offers.",
      cta: "View redemption log",
      target: "redemption-log",
    })

    expect(
      mapWeeklyBriefRecommendedActionFact({
        kind: "low-redemption",
        offerId: 9,
        offerTitle: "Quiet-day treat",
        claims: 10,
        redemptions: 2,
        rate: 0.2,
        target: "offers",
      })
    ).toEqual({
      id: "low-redemption",
      title: "High claims, lower redemptions",
      subtitle:
        "The Quiet-day treat offer had 10 claims and 2 redemptions.",
      cta: "Review offer",
      target: "offers",
    })
  })

  it("hides Recommended actions when the fact list is empty", () => {
    expect(shouldShowWeeklyBriefRecommendedActions([])).toBe(false)
    expect(shouldShowWeeklyBriefRecommendedActions(null)).toBe(false)
    expect(
      shouldShowWeeklyBriefRecommendedActions([
        {
          kind: "feedback-needs-attention",
          count: 1,
          target: "feedback-needs-attention",
        },
      ])
    ).toBe(true)
  })

  it("plans recommended-action CTA targets", () => {
    expect(
      planWeeklyBriefRecommendedActionCta({
        mode: "single",
        locationId: 42,
        target: "feedback-needs-attention",
      })
    ).toEqual({
      path: "/single-dashboard/feedback?location=42",
      feedbackInbox: { tab: "needs-attention" },
    })
    expect(
      planWeeklyBriefRecommendedActionCta({
        mode: "single",
        locationId: 42,
        target: "redemption-log",
      })
    ).toEqual({
      path: "/single-dashboard/offers/redemption-log?location=42",
    })
    expect(
      planWeeklyBriefRecommendedActionCta({
        mode: "multi",
        locationId: 7,
        target: "offers",
      })
    ).toEqual({
      path: "/multi-dashboard/offers?location=7",
    })
  })

  it("shows Suggested campaign only when a draft is present", () => {
    expect(shouldShowWeeklyBriefSuggestedCampaign(null)).toBe(false)
    expect(
      shouldShowWeeklyBriefSuggestedCampaign({
        campaignId: 41,
        name: "Quiet-day boost",
        audienceKey: "all-eligible-guests",
      })
    ).toBe(true)
  })

  it("maps suggested campaign and plans Review campaign CTA", () => {
    expect(
      mapWeeklyBriefSuggestedCampaign({
        campaignId: 41,
        name: "Quiet-day boost",
        audienceKey: "all-eligible-guests",
      })
    ).toEqual({
      status: "Draft",
      title: "Quiet-day boost",
      subtitle: "All eligible guests",
      cta: "Review campaign",
      campaignId: 41,
    })

    expect(
      planWeeklyBriefSuggestedCampaignCta({
        mode: "single",
        locationId: 42,
        campaignId: 41,
      })
    ).toEqual({
      path: "/single-dashboard/campaigns?location=42",
      campaigns: {
        continueEditingCampaignId: 41,
        continueEditingStep: "review",
      },
    })
  })
})
