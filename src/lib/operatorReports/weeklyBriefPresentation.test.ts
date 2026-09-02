import { describe, expect, it } from "vitest"
import {
  WEEKLY_BRIEF_PAGE_COPY,
  mockWeeklyBriefData,
} from "@/lib/operatorReports/weeklyBriefPresentation"

describe("weeklyBriefPresentation", () => {
  it("exports expected page copy constants", () => {
    expect(WEEKLY_BRIEF_PAGE_COPY.pageTitle).toBe("Weekly Brief")
    expect(WEEKLY_BRIEF_PAGE_COPY.emptyTitle).toBe("No weekly brief yet")
    expect(WEEKLY_BRIEF_PAGE_COPY.downloadPdf).toBe("Download PDF")
    expect(WEEKLY_BRIEF_PAGE_COPY.markAsReviewed).toBe("Mark as reviewed")
  })

  it("contains populated mock dataset for all 6 sections", () => {
    expect(mockWeeklyBriefData.period).toBe("6–12 July")
    expect(mockWeeklyBriefData.changes.length).toBe(5)
    expect(mockWeeklyBriefData.recommendedActions.length).toBe(3)
    expect(mockWeeklyBriefData.suggestedCampaign.title).toBe("Quiet-day boost")
  })
})
