import { describe, it, expect } from "vitest"
import {
  OFFERS_REPORT_PAGE_COPY,
  mockOffersReportData,
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

    expect(mockOffersReportData.controlSignals).toHaveLength(3)
    expect(mockOffersReportData.controlSignals[0].title).toBe(
      "Repeated invalid attempts"
    )
  })
})
