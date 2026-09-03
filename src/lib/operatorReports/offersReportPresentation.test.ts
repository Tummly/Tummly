import { describe, it, expect } from "vitest"
import {
  OFFERS_REPORT_PAGE_COPY,
  mockOffersReportData,
  buildOffersReportViewModel,
} from "./offersReportPresentation"

describe("offersReportPresentation", () => {
  it("exports complete copy constants for Offers Report page", () => {
    expect(OFFERS_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(OFFERS_REPORT_PAGE_COPY.breadcrumbOffersReport).toBe("Offers report")
    expect(OFFERS_REPORT_PAGE_COPY.pageTitle).toBe("Offers report")
    expect(OFFERS_REPORT_PAGE_COPY.emptyTitle).toBe("No offer reports yet")
    expect(OFFERS_REPORT_PAGE_COPY.createOffer).toBe("Create offer")
    expect(OFFERS_REPORT_PAGE_COPY.performanceSectionTitle).toBe(
      "Offer performance"
    )
    expect(OFFERS_REPORT_PAGE_COPY.recentRedemptionsSectionTitle).toBe(
      "Recent redemptions"
    )
    expect(OFFERS_REPORT_PAGE_COPY.controlSignalsSectionTitle).toBe(
      "Offer control signals"
    )
  })

  it("provides structured mock data with 6 KPIs, performance list, redemptions and control signals", () => {
    expect(Object.keys(mockOffersReportData.kpis)).toHaveLength(6)
    expect(mockOffersReportData.kpis.activeOffers.label).toBe("Active offers")
    expect(mockOffersReportData.kpis.offerClaims.label).toBe("Offer claims")
    expect(mockOffersReportData.kpis.redemptions.label).toBe("Redemptions")
    expect(mockOffersReportData.kpis.redemptionRate.label).toBe("Redemption rate")
    expect(mockOffersReportData.kpis.expiredClaims.label).toBe("Expired claims")
    expect(mockOffersReportData.kpis.invalidAttempts.label).toBe("Invalid attempts")

    expect(mockOffersReportData.performance.length).toBeGreaterThanOrEqual(4)
    expect(mockOffersReportData.performance[0].offer).toBe("Free side next visit")

    expect(mockOffersReportData.redemptionsList.length).toBeGreaterThanOrEqual(5)
    expect(mockOffersReportData.redemptionsList[0].status).toBe("Redeemed")

    expect(mockOffersReportData.controlSignals).toHaveLength(2)
    expect(mockOffersReportData.controlSignals[0].title).toBe(
      "Repeated invalid attempts"
    )
  })

  it("maps a ready Offers API body into KPIs, tables, and control copy", () => {
    const view = buildOffersReportViewModel({
      success: true,
      lifetimeEmpty: false,
      kpis: {
        activeOffers: { value: 2, valuePrevious: 1 },
        offerClaims: { value: 5, valuePrevious: 0 },
        redemptions: { value: 2, valuePrevious: 1 },
        redemptionRate: { value: 0.4, valuePrevious: null },
        expiredClaims: { value: 1, valuePrevious: 1 },
        invalidAttempts: { value: 0, valuePrevious: 0 },
      },
      performance: [
        {
          offerId: 7,
          offer: "Free side",
          status: "archived",
          claims: 5,
          redemptions: 2,
          rate: 0.4,
          expired: 1,
          invalid: 0,
        },
      ],
      recentRedemptions: [
        {
          id: 3,
          dateTimeUtc: "2026-07-15T12:00:00.000Z",
          offerTitle: "Free side",
          guestName: "Maya",
          locationName: "Main",
          outcome: "redeemed",
        },
      ],
      controlSignals: [
        {
          kind: "low-redemption",
          offerId: 7,
          offerTitle: "Free side",
          claims: 5,
          redemptions: 2,
          rate: 0.4,
          target: "offers",
        },
      ],
    })

    expect(view.kpis.activeOffers.value).toBe("2")
    expect(view.kpis.redemptionRate.value).toBe("40%")
    expect(view.performance[0]?.status).toBe("Archived")
    expect(view.performance[0]).not.toHaveProperty("source")
    expect(view.redemptionsList[0]?.status).toBe("Redeemed")
    expect(view.controlSignals[0]?.cta).toBe("Review offer")
    expect(view.controlSignals[0]?.target).toBe("offers")
  })
})
