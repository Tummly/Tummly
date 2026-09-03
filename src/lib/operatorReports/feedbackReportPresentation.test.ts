import { describe, it, expect } from "vitest"
import {
  FEEDBACK_REPORT_PAGE_COPY,
  buildFeedbackReportViewModel,
} from "./feedbackReportPresentation"
import type { ReportsFeedbackResponse } from "@/types/operatorReports"

function readyFeedback(): Extract<
  ReportsFeedbackResponse,
  { lifetimeEmpty: false }
> {
  const metric = (value: number, valuePrevious: number) => ({
    value,
    valuePrevious,
  })
  return {
    success: true,
    lifetimeEmpty: false,
    kpis: {
      feedbackReceived: metric(8, 1),
      marketingOptIns: metric(6, 1),
      followUpNeeded: metric(6, 0),
      resolved: metric(1, 0),
    },
    status: {
      new: metric(1, 0),
      inProgress: metric(6, 0),
      followUpNeeded: metric(6, 0),
      resolved: metric(1, 0),
    },
    needsAttention: [
      {
        feedbackId: 42,
        submittedAt: "2026-07-13T16:00:00.000Z",
        guestName: "Needs Guest 4",
        source: "Counter card",
        commentPreview: "Needs attention 4",
        workflowStatus: "In progress",
      },
    ],
    bySource: [
      {
        qrCodeId: 9,
        source: "Counter card",
        feedback: 8,
        marketingOptIns: 6,
        followUpNeeded: 6,
      },
    ],
  }
}

describe("feedbackReportPresentation", () => {
  it("exports copy for Feedback report chrome and empty state", () => {
    expect(FEEDBACK_REPORT_PAGE_COPY.breadcrumbFeedbackReport).toBe(
      "Feedback report"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.title).toBe("Feedback report")
    expect(FEEDBACK_REPORT_PAGE_COPY.emptyTitle).toBe("No feedback yet")
    expect(FEEDBACK_REPORT_PAGE_COPY.needsFollowUpTitle).toBe(
      "Needs follow-up"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.feedbackBySourceTitle).toBe(
      "Feedback by source"
    )
    expect(FEEDBACK_REPORT_PAGE_COPY.feedbackStatusTitle).toBe(
      "Feedback status"
    )
  })

  it("builds live view model with four top KPIs and no themes", () => {
    const view = buildFeedbackReportViewModel(readyFeedback())
    expect(view.topKpis).toHaveLength(4)
    expect(view.topKpis[0]?.label).toBe("Feedback received")
    expect(view.topKpis[1]?.label).toBe("Marketing opt-ins")
    expect(view.topKpis[0]?.value).toBe("8")
    expect(view.statusKpis).toHaveLength(4)
    expect(view.statusKpis[0]?.label).toBe("New")
    expect(view.statusKpis[1]?.label).toBe("In progress")
    expect(view.followUpList[0]?.feedbackId).toBe(42)
    expect(view.sourcesList[0]?.marketingOptIns).toBe(6)
  })
})
