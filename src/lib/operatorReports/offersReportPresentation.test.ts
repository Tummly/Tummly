import { describe, it, expect } from "vitest"
import {
  OFFERS_REPORT_PAGE_COPY,
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
    expect(OFFERS_REPORT_PAGE_COPY.controlSignalsEmpty).toBe(
      "No control signals for this period."
    )
    expect(OFFERS_REPORT_PAGE_COPY.loadError).toBe(
      "Could not load offers report. Please try again."
    )
  })

  it("keeps an empty controlSignals list when the API returns none", () => {
    const view = buildOffersReportViewModel({
      success: true,
      lifetimeEmpty: false,
      kpis: {
        activeOffers: { value: 1, valuePrevious: 1 },
        offerClaims: { value: 0, valuePrevious: 0 },
        redemptions: { value: 0, valuePrevious: 0 },
        redemptionRate: { value: null, valuePrevious: null },
        expiredClaims: { value: 0, valuePrevious: 0 },
        invalidAttempts: { value: 0, valuePrevious: 0 },
      },
      performance: [],
      recentRedemptions: [],
      controlSignals: [],
    })

    expect(view.controlSignals).toEqual([])
    expect(OFFERS_REPORT_PAGE_COPY.controlSignalsEmpty).toBe(
      "No control signals for this period."
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
