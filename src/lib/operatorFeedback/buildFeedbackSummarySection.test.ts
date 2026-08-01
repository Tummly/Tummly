import { describe, expect, it } from "vitest"

import {
  buildFeedbackSummarySection,
  feedbackSharePercent,
  formatAbsoluteCountDelta,
  formatSharePointDelta,
} from "@/lib/operatorFeedback/buildFeedbackSummarySection"
import type { FeedbackSummaryResponse } from "@/types/dashboard"

function summary(
  overrides: Partial<FeedbackSummaryResponse> = {}
): FeedbackSummaryResponse {
  return {
    success: true,
    total: 0,
    positive: 0,
    neutral: 0,
    negative: 0,
    totalPrevious: 0,
    positivePrevious: 0,
    neutralPrevious: 0,
    negativePrevious: 0,
    needsAttentionTotal: 0,
    ...overrides,
  }
}

describe("buildFeedbackSummarySection", () => {
  it("returns empty section when Total is 0", () => {
    expect(buildFeedbackSummarySection(summary({ total: 0 }))).toEqual({
      kind: "empty",
    })
  })

  it("returns KPI strip with zeros when Total > 0 and all unclassified", () => {
    const section = buildFeedbackSummarySection(
      summary({
        total: 5,
        positive: 0,
        neutral: 0,
        negative: 0,
        totalPrevious: 4,
      })
    )
    expect(section.kind).toBe("kpis")
    if (section.kind !== "kpis") {
      return
    }
    expect(section.kpis.map((kpi) => kpi.value)).toEqual([5, 0, 0, 0])
    expect(section.kpis[1]?.shareLabel).toBe("0% of feedback")
  })

  it("uses half-up share percents of Total", () => {
    expect(feedbackSharePercent(1, 3)).toBe(33)
    expect(feedbackSharePercent(2, 3)).toBe(67)
  })

  it("hides comparison when previous Total is 0", () => {
    expect(formatAbsoluteCountDelta(5, 0, 0)).toBeNull()
    expect(formatSharePointDelta(2, 5, 0, 0)).toBeNull()
  })

  it("formats absolute and share-point PoP helpers", () => {
    expect(formatAbsoluteCountDelta(10, 8, 8)).toBe("+2 vs previous period")
    expect(formatAbsoluteCountDelta(3, 5, 8)).toBe("-2 vs previous period")
    // Current 40% (4/10), previous 37.5% (3/8) → +3pp half-up
    expect(formatSharePointDelta(4, 10, 3, 8)).toBe("+3pp vs previous period")
  })
})
