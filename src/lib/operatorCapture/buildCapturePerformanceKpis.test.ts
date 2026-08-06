import { describe, expect, it } from "vitest"

import {
  buildCapturePerformanceKpis,
  type CapturePerformanceFacts,
} from "./buildCapturePerformanceKpis"

const emptyFacts: CapturePerformanceFacts = {
  qrScans: 0,
  qrScansPrevious: 0,
  feedbackSubmitted: 0,
  feedbackSubmittedPrevious: 0,
  marketingOptIns: 0,
  marketingOptInsPrevious: 0,
  offerClaims: 0,
  offerClaimsHasRealData: false,
}

describe("buildCapturePerformanceKpis", () => {
  it("builds five KPI cards with Figma rate secondaries", () => {
    const { kpis } = buildCapturePerformanceKpis({
      qrScans: 12,
      qrScansPrevious: 10,
      feedbackSubmitted: 6,
      feedbackSubmittedPrevious: 4,
      marketingOptIns: 3,
      marketingOptInsPrevious: 2,
      offerClaims: 0,
      offerClaimsHasRealData: false,
    })

    expect(kpis.map((kpi) => kpi.id)).toEqual([
      "qr-scans",
      "form-starts",
      "feedback-submitted",
      "marketing-opt-ins",
      "offer-claims",
    ])
    expect(kpis[0]).toMatchObject({
      id: "qr-scans",
      label: "Guest form opens",
      primaryText: "12",
      secondaryText: null,
      hasRealData: true,
    })
    expect(kpis[1]).toMatchObject({
      id: "form-starts",
      label: "Form starts",
      primaryText: "50%",
      secondaryText: "50% of scans",
      hasRealData: true,
    })
    expect(kpis[2]).toMatchObject({
      primaryText: "6",
      secondaryText: "50% completion rate",
      hasRealData: true,
    })
    expect(kpis[3]).toMatchObject({
      primaryText: "3",
      secondaryText: "50% of submissions",
      hasRealData: true,
    })
    expect(kpis[4]).toMatchObject({
      primaryText: "0",
      secondaryText: null,
      hasRealData: false,
    })
  })

  it("shows Form starts as — when current scans are 0", () => {
    const { kpis } = buildCapturePerformanceKpis({
      ...emptyFacts,
      feedbackSubmitted: 2,
    })

    expect(kpis.find((kpi) => kpi.id === "form-starts")).toMatchObject({
      primaryText: "—",
      secondaryText: null,
      hasRealData: true,
    })
  })

  it("omits rate secondaries when the denominator is 0", () => {
    const { kpis } = buildCapturePerformanceKpis({
      ...emptyFacts,
      qrScans: 0,
      feedbackSubmitted: 0,
      marketingOptIns: 2,
    })

    expect(
      kpis.find((kpi) => kpi.id === "feedback-submitted")?.secondaryText
    ).toBeNull()
    expect(
      kpis.find((kpi) => kpi.id === "marketing-opt-ins")?.secondaryText
    ).toBeNull()
  })

  it("shows offer-claims rate of submissions when the metric has real data", () => {
    const { kpis } = buildCapturePerformanceKpis({
      ...emptyFacts,
      qrScans: 10,
      feedbackSubmitted: 4,
      offerClaims: 1,
      offerClaimsHasRealData: true,
    })

    expect(kpis.find((kpi) => kpi.id === "offer-claims")).toMatchObject({
      primaryText: "1",
      secondaryText: "25% of submissions",
      hasRealData: true,
    })
  })

  it("marks the window empty when scans and feedback are both zero", () => {
    expect(buildCapturePerformanceKpis(emptyFacts).isEmpty).toBe(true)
    expect(
      buildCapturePerformanceKpis({ ...emptyFacts, qrScans: 1 }).isEmpty
    ).toBe(false)
    expect(
      buildCapturePerformanceKpis({ ...emptyFacts, feedbackSubmitted: 1 })
        .isEmpty
    ).toBe(false)
  })
})
