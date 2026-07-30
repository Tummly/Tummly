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
  it("builds five KPI cards with Home-style PoP secondaries", () => {
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
      trendPercent: 20,
      hasRealData: true,
    })
    expect(kpis[1]).toMatchObject({
      id: "form-starts",
      label: "Form starts",
      primaryText: "50%",
      // current 50%, previous 40% → +25%
      trendPercent: 25,
      hasRealData: true,
    })
    expect(kpis[2]).toMatchObject({
      primaryText: "6",
      trendPercent: 50,
      hasRealData: true,
    })
    expect(kpis[3]).toMatchObject({
      primaryText: "3",
      trendPercent: 50,
      hasRealData: true,
    })
    expect(kpis[4]).toMatchObject({
      primaryText: "0",
      trendPercent: null,
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
      trendPercent: null,
      hasRealData: true,
    })
  })

  it("nulls Form starts PoP when either period rate is undefined", () => {
    const previousUndefined = buildCapturePerformanceKpis({
      ...emptyFacts,
      qrScans: 10,
      feedbackSubmitted: 5,
      qrScansPrevious: 0,
      feedbackSubmittedPrevious: 2,
    })
    expect(
      previousUndefined.kpis.find((kpi) => kpi.id === "form-starts")
        ?.trendPercent
    ).toBeNull()

    const currentUndefined = buildCapturePerformanceKpis({
      ...emptyFacts,
      qrScans: 0,
      feedbackSubmitted: 0,
      qrScansPrevious: 10,
      feedbackSubmittedPrevious: 5,
    })
    expect(
      currentUndefined.kpis.find((kpi) => kpi.id === "form-starts")
        ?.trendPercent
    ).toBeNull()
  })

  it("treats zero previous count as +100% when current has activity", () => {
    const { kpis } = buildCapturePerformanceKpis({
      ...emptyFacts,
      qrScans: 4,
      qrScansPrevious: 0,
    })

    expect(kpis.find((kpi) => kpi.id === "qr-scans")?.trendPercent).toBe(100)
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
