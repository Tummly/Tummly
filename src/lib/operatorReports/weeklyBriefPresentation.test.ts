import { describe, expect, it } from "vitest"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  formatWeeklyBriefDataSources,
  formatWeeklyBriefGeneratedAt,
  mockWeeklyBriefData,
  planWeeklyBriefFeedbackFollowUpCta,
  shouldShowWeeklyBriefFeedbackSummary,
  shouldShowWeeklyBriefWhatChanged,
} from "@/lib/operatorReports/weeklyBriefPresentation"

describe("weeklyBriefPresentation", () => {
  it("exports expected page copy constants", () => {
    expect(WEEKLY_BRIEF_PAGE_COPY.pageTitle).toBe("Weekly Brief")
    expect(WEEKLY_BRIEF_PAGE_COPY.emptyTitle).toBe("No weekly brief yet")
    expect(WEEKLY_BRIEF_PAGE_COPY.downloadPdf).toBe("Download PDF")
    expect(WEEKLY_BRIEF_PAGE_COPY.markAsReviewed).toBe("Mark as reviewed")
    expect(WEEKLY_BRIEF_PAGE_COPY.periodLabel).toBe("Period")
    expect(WEEKLY_BRIEF_PAGE_COPY.executiveSummaryTitle).toBe(
      "Executive summary"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.whatChangedTitle).toBe("What changed")
    expect(WEEKLY_BRIEF_PAGE_COPY.feedbackSummaryTitle).toBe(
      "Feedback summary"
    )
    expect(WEEKLY_BRIEF_PAGE_COPY.reviewFollowUpQueue).toBe(
      "Review follow-up queue"
    )
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
})
