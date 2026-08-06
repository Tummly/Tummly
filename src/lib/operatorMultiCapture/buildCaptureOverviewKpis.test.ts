import { describe, expect, it } from "vitest"

import {
  buildCaptureOverviewKpis,
  type CaptureOverviewFacts,
} from "./buildCaptureOverviewKpis"

const emptyFacts: CaptureOverviewFacts = {
  activeLocations: 0,
  totalLocations: 0,
  activeQrPlacements: 0,
  qrScans: 0,
  qrScansPrevious: 0,
  feedbackSubmitted: 0,
  feedbackSubmittedPrevious: 0,
  marketingOptIns: 0,
  marketingOptInsPrevious: 0,
  offerClaims: 0,
  offerClaimsHasRealData: false,
}

describe("buildCaptureOverviewKpis", () => {
  it("builds six KPI cards with Figma rate secondaries", () => {
    const { kpis } = buildCaptureOverviewKpis({
      activeLocations: 2,
      totalLocations: 5,
      activeQrPlacements: 7,
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
      "active-locations",
      "active-qr-placements",
      "qr-scans",
      "feedback-submitted",
      "marketing-opt-ins",
      "offer-claims",
    ])
    expect(kpis[0]).toMatchObject({
      primaryText: "2",
      secondaryKind: "of-total",
      secondaryText: "of 5",
    })
    expect(kpis[1]).toMatchObject({
      primaryText: "7",
      secondaryKind: "dash",
      hasRealData: true,
    })
    expect(kpis[2]).toMatchObject({
      label: "Guest form opens",
      primaryText: "12",
      secondaryKind: "none",
      secondaryText: null,
    })
    expect(kpis[3]).toMatchObject({
      primaryText: "6",
      secondaryKind: "rate",
      secondaryText: "50% completion rate",
    })
    expect(kpis[4]).toMatchObject({
      primaryText: "3",
      secondaryKind: "rate",
      secondaryText: "50% of submissions",
    })
    expect(kpis[5]).toMatchObject({
      primaryText: "0",
      secondaryKind: "dash",
      hasRealData: false,
    })
  })

  it("dashes engagement rates when denominators are 0", () => {
    const { kpis } = buildCaptureOverviewKpis(emptyFacts)

    expect(kpis.find((kpi) => kpi.id === "feedback-submitted")).toMatchObject({
      secondaryKind: "dash",
      secondaryText: null,
    })
    expect(kpis.find((kpi) => kpi.id === "marketing-opt-ins")).toMatchObject({
      secondaryKind: "dash",
      secondaryText: null,
    })
  })

  it("shows offer-claims rate of submissions when the metric has real data", () => {
    const { kpis } = buildCaptureOverviewKpis({
      ...emptyFacts,
      qrScans: 10,
      feedbackSubmitted: 4,
      offerClaims: 1,
      offerClaimsHasRealData: true,
    })

    expect(kpis.find((kpi) => kpi.id === "offer-claims")).toMatchObject({
      primaryText: "1",
      secondaryKind: "rate",
      secondaryText: "25% of submissions",
      hasRealData: true,
    })
  })
})
