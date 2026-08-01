import { describe, expect, it, vi, type Mock } from "vitest"

import {
  createOperatorFeedbackPageModule,
  type OperatorFeedbackPageAdapters,
} from "@/lib/operatorFeedback/createOperatorFeedbackPageModule"
import { DEFAULT_HOME_PERFORMANCE_DATE_RANGE } from "@/lib/operatorHome/homePerformanceDateRange"
import type { FeedbackSummaryResponse } from "@/types/dashboard"

function summaryResponse(
  overrides: Partial<FeedbackSummaryResponse> = {}
): FeedbackSummaryResponse {
  return {
    success: true,
    total: 10,
    positive: 4,
    neutral: 2,
    negative: 3,
    totalPrevious: 8,
    positivePrevious: 3,
    neutralPrevious: 2,
    negativePrevious: 2,
    needsAttentionTotal: 2,
    ...overrides,
  }
}

function createAdapters(
  overrides: Partial<OperatorFeedbackPageAdapters> & {
    getFeedbackSummary?: Mock<OperatorFeedbackPageAdapters["getFeedbackSummary"]>
  } = {}
): OperatorFeedbackPageAdapters {
  return {
    getFeedbackSummary:
      overrides.getFeedbackSummary
      ?? vi.fn(async () => summaryResponse()),
    getFeedbackPageDateRange:
      overrides.getFeedbackPageDateRange
      ?? (() => DEFAULT_HOME_PERFORMANCE_DATE_RANGE),
    getNow: overrides.getNow ?? (() => new Date("2026-07-17T12:00:00.000Z")),
    scheduleReady: overrides.scheduleReady ?? (async () => undefined),
  }
}

describe("createOperatorFeedbackPageModule", () => {
  it("loads Feedback summary KPIs for the selected location and date range", async () => {
    const getFeedbackSummary = vi.fn(async () =>
      summaryResponse({
        total: 10,
        positive: 4,
        neutral: 2,
        negative: 3,
        needsAttentionTotal: 2,
      })
    )
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({ getFeedbackSummary })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 7,
      locations: [{ id: 7, locationName: "Camden Street" }],
    })

    const snapshot = pageModule.getSnapshot()
    expect(snapshot.loadStatus).toBe("loaded")
    expect(getFeedbackSummary).toHaveBeenCalledWith({
      locationId: 7,
      from: expect.any(String),
      to: expect.any(String),
    })
    expect(snapshot.viewModel).toMatchObject({
      locationId: 7,
      locationName: "Camden Street",
      dateRangeLabel: "Last 7 days",
      needsAttentionCount: 2,
      summary: { kind: "kpis" },
    })
    expect(snapshot.viewModel?.summary.kind === "kpis"
      ? snapshot.viewModel.summary.kpis.map((kpi) => kpi.id)
      : []).toEqual(["total", "positive", "neutral", "negative"])
  })

  it("reloads summary when the Feedback page date range changes", async () => {
    let range = DEFAULT_HOME_PERFORMANCE_DATE_RANGE
    const getFeedbackSummary = vi.fn(async () => summaryResponse())
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary,
        getFeedbackPageDateRange: () => range,
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 3,
      locations: [{ id: 3, locationName: "Main" }],
    })
    expect(getFeedbackSummary).toHaveBeenCalledTimes(1)

    range = { kind: "preset", presetId: "last30" }
    await pageModule.reloadForFeedbackPageDateRange()

    expect(getFeedbackSummary).toHaveBeenCalledTimes(2)
    expect(pageModule.getSnapshot().viewModel?.dateRangeLabel).toBe(
      "Last 30 days"
    )
  })

  it("shows empty summary when Total is 0", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({
            total: 0,
            positive: 0,
            neutral: 0,
            negative: 0,
            needsAttentionTotal: 0,
          })
        ),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })

    expect(pageModule.getSnapshot().viewModel?.summary).toEqual({
      kind: "empty",
    })
  })

  it("shows KPI zeros when Total > 0 and all unclassified", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({
            total: 4,
            positive: 0,
            neutral: 0,
            negative: 0,
            needsAttentionTotal: 0,
          })
        ),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })

    const summary = pageModule.getSnapshot().viewModel?.summary
    expect(summary?.kind).toBe("kpis")
    if (summary?.kind !== "kpis") {
      return
    }
    expect(summary.kpis.map((kpi) => kpi.value)).toEqual([4, 0, 0, 0])
  })

  it("Review needs attention switches inbox tab without changing N", async () => {
    const pageModule = createOperatorFeedbackPageModule(
      createAdapters({
        getFeedbackSummary: vi.fn(async () =>
          summaryResponse({ needsAttentionTotal: 5 })
        ),
      })
    )

    await pageModule.syncWorkspace({
      selectedLocationId: 1,
      locations: [{ id: 1, locationName: "Main" }],
    })

    expect(pageModule.getSnapshot().activeInboxTabId).toBe("all")
    expect(pageModule.getSnapshot().viewModel?.needsAttentionCount).toBe(5)

    pageModule.reviewNeedsAttention()

    const after = pageModule.getSnapshot()
    expect(after.activeInboxTabId).toBe("needs-attention")
    expect(after.viewModel?.needsAttentionCount).toBe(5)
    expect(after.scrollToInboxRequestId).toBe(1)
  })
})
