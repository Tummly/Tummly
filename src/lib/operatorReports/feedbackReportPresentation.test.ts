import { describe, it, expect } from "vitest"
import {
  FEEDBACK_REPORT_PAGE_COPY,
  DATE_PRESET_LABELS,
  mockFeedbackReportData,
} from "./feedbackReportPresentation"

describe("feedbackReportPresentation", () => {
  it("exports complete copy constants for Feedback Report page", () => {
    expect(FEEDBACK_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(FEEDBACK_REPORT_PAGE_COPY.breadcrumbFeedbackReport).toBe(
      "Feedback report"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.title).toBe("Feedback report")
    expect(FEEDBACK_REPORT_PAGE_COPY.subtitle).toContain(
      "Read private guest feedback"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.emptyTitle).toBe("No feedback yet")
    expect(FEEDBACK_REPORT_PAGE_COPY.emptySubtitle).toContain(
      "Once guests submit private feedback"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.checkGuestForm).toBe("Check guest form")
    expect(FEEDBACK_REPORT_PAGE_COPY.feedbackOverTimeTitle).toBe(
      "Feedback over time"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.commonThemesTitle).toBe("Common themes")
    expect(FEEDBACK_REPORT_PAGE_COPY.viewSourceFeedback).toBe(
      "View source feedback"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.needsFollowUpTitle).toBe(
      "Needs follow-up"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.openFeedbackInbox).toBe(
      "Open feedback inbox"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceTitle).toBe(
      "Feedback by source"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.feedbackStatusTitle).toBe(
      "Feedback status"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.manageFeedback).toBe("Manage feedback")
  })

  it("exports date preset labels for all supported date filters", () => {
    expect(DATE_PRESET_LABELS["7d"]).toBe("Last 7 days")
    expect(DATE_PRESET_LABELS["30d"]).toBe("Last 30 days")
    expect(DATE_PRESET_LABELS["90d"]).toBe("Last 90 days")
    expect(DATE_PRESET_LABELS.month).toBe("This month")
    expect(DATE_PRESET_LABELS.ytd).toBe("Year to date")
  })

  it("provides structured mock data with 5 top KPIs, themes, follow-ups, sources and status KPIs", () => {
    expect(Object.keys(mockFeedbackReportData.kpis)).toHaveLength(5)
    expect(mockFeedbackReportData.kpis.feedbackReceived.label).toBe(
      "Feedback received"
    )
    expect(mockFeedbackReportData.kpis.contactableFeedback.label).toBe(
      "Contactable feedback"
    )
    expect(mockFeedbackReportData.kpis.followUpNeeded.label).toBe(
      "Follow-up needed"
    )
    expect(mockFeedbackReportData.kpis.followedUp.label).toBe("Followed up")
    expect(mockFeedbackReportData.kpis.resolved.label).toBe("Resolved")

    expect(mockFeedbackReportData.themes).toHaveLength(3)
    expect(mockFeedbackReportData.themes[0].theme).toContain("delivery packaging")

    expect(mockFeedbackReportData.followUpList.length).toBeGreaterThanOrEqual(5)
    expect(mockFeedbackReportData.followUpList[0].guest).toBe("Sarah")

    expect(mockFeedbackReportData.sourcesList).toHaveLength(4)
    expect(mockFeedbackReportData.sourcesList[0].source).toBe("Delivery insert")

    expect(Object.keys(mockFeedbackReportData.statusKpis)).toHaveLength(5)
    expect(mockFeedbackReportData.statusKpis.newFeedback.label).toBe("New")
    expect(mockFeedbackReportData.statusKpis.reviewed.label).toBe("Reviewed")
  })
})
