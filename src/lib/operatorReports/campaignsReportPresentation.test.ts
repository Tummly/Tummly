import { describe, it, expect } from "vitest"
import {
  CAMPAIGNS_REPORT_PAGE_COPY,
  mockCampaignsReportData,
} from "./campaignsReportPresentation"

describe("campaignsReportPresentation", () => {
  it("exports complete copy constants for Campaigns Report page", () => {
    expect(CAMPAIGNS_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.breadcrumbCampaignsReport).toBe(
      "Campaigns report"
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.pageTitle).toBe("Campaigns report")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.emptyTitle).toBe(
      "No campaign reports yet"
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.createCampaign).toBe("Create campaign")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.performanceSectionTitle).toBe(
      "Campaign performance"
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionTitle).toBe(
      "Needs attention"
    )
  })

  it("provides structured mock data with 6 KPIs, performance list, and attention items", () => {
    expect(Object.keys(mockCampaignsReportData.kpis)).toHaveLength(6)
    expect(mockCampaignsReportData.kpis.campaignsSent.label).toBe(
      "Campaigns sent"
    )
    expect(mockCampaignsReportData.kpis.guestsMessaged.label).toBe(
      "Guests messaged"
    )
    expect(mockCampaignsReportData.kpis.offerClaims.label).toBe("Offer claims")
    expect(mockCampaignsReportData.kpis.offerRedemptions.label).toBe(
      "Offer redemptions"
    )
    expect(mockCampaignsReportData.kpis.unsubscribes.label).toBe("Unsubscribes")
    expect(mockCampaignsReportData.kpis.failedSends.label).toBe("Failed sends")

    expect(mockCampaignsReportData.performance.length).toBeGreaterThanOrEqual(5)
    expect(mockCampaignsReportData.performance[0].campaign).toBe(
      "Quiet Tuesday offer"
    )

    expect(mockCampaignsReportData.attentionItems).toHaveLength(3)
    expect(mockCampaignsReportData.attentionItems[0].title).toBe(
      "3 feedback items need attention"
    )
    expect(mockCampaignsReportData.attentionItems[1].title).toBe(
      "SMS credits are running low"
    )
    expect(mockCampaignsReportData.attentionItems[2].title).toBe(
      "Offer expires in 2 days"
    )
  })
})
