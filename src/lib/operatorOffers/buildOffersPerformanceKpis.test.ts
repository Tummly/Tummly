import { describe, expect, it } from "vitest"

import {
  buildOffersPerformanceKpis,
  formatClaimToRedemptionRate,
} from "./buildOffersPerformanceKpis"

describe("formatClaimToRedemptionRate", () => {
  it("returns an em dash when claims are 0", () => {
    expect(formatClaimToRedemptionRate(0, 0)).toBe("—")
    expect(formatClaimToRedemptionRate(0, 5)).toBe("—")
  })

  it("returns a rounded percentage when claims are positive", () => {
    expect(formatClaimToRedemptionRate(10, 5)).toBe("50%")
    expect(formatClaimToRedemptionRate(3, 1)).toBe("33%")
    expect(formatClaimToRedemptionRate(4, 4)).toBe("100%")
  })
})

describe("buildOffersPerformanceKpis", () => {
  it("shows active offers against plan cap when provided", () => {
    const kpis = buildOffersPerformanceKpis({
      activeOffers: 2,
      activeOffersCap: 3,
      offersIssued: 0,
      claims: 0,
      redemptions: 0,
    })

    expect(kpis[0]?.primaryText).toBe("2 of 3")
    expect(kpis[0]?.helperText).toContain("plan limit")
  })

  it("builds five KPI cells with honest zeros and fixed helpers", () => {
    const kpis = buildOffersPerformanceKpis({
      activeOffers: 0,
      offersIssued: 0,
      claims: 0,
      redemptions: 0,
    })

    expect(kpis.map((kpi) => kpi.id)).toEqual([
      "active-offers",
      "offers-issued",
      "claims",
      "redemptions",
      "claim-to-redemption-rate",
    ])
    expect(kpis[0]).toEqual({
      id: "active-offers",
      label: "Active offers",
      primaryText: "0",
      helperText:
        "Offers currently available for valid issuance or redemption.",
    })
    expect(kpis[1]).toEqual({
      id: "offers-issued",
      label: "Offers issued",
      primaryText: "0",
      helperText: "Guest-specific passes issued during the selected period.",
    })
    expect(kpis[2]).toEqual({
      id: "claims",
      label: "Claims",
      primaryText: "0",
      helperText:
        "Issued offers activated or opened by guests during this period.",
    })
    expect(kpis[3]).toEqual({
      id: "redemptions",
      label: "Redemptions",
      primaryText: "0",
      helperText:
        "Successful staff-confirmed redemptions during this period.",
    })
    expect(kpis[4]).toEqual({
      id: "claim-to-redemption-rate",
      label: "Claim-to-redemption rate",
      primaryText: "—",
      helperText: "Share of claims in this period that staff redeemed.",
    })
  })

  it("formats window counts and rate from facts; Active offers is a snapshot count", () => {
    const kpis = buildOffersPerformanceKpis({
      activeOffers: 12,
      offersIssued: 40,
      claims: 20,
      redemptions: 5,
    })

    expect(kpis[0].primaryText).toBe("12")
    expect(kpis[1].primaryText).toBe("40")
    expect(kpis[2].primaryText).toBe("20")
    expect(kpis[3].primaryText).toBe("5")
    expect(kpis[4].primaryText).toBe("25%")
    expect(kpis[4].helperText).toBe(
      "Share of claims in this period that staff redeemed."
    )
  })
})
