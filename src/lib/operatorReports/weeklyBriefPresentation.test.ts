import { describe, expect, it } from "vitest"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  formatWeeklyBriefDataSources,
  formatWeeklyBriefGeneratedAt,
  mockWeeklyBriefData,
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
})
