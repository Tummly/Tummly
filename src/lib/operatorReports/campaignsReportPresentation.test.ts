import { describe, it, expect } from "vitest"
import {
  buildCampaignsReportViewModel,
  CAMPAIGNS_REPORT_PAGE_COPY,
} from "./campaignsReportPresentation"
import type { ReportsCampaignsResponse } from "@/types/operatorReports"

describe("campaignsReportPresentation", () => {
  it("exports campaigns report copy constants", () => {
    expect(CAMPAIGNS_REPORT_PAGE_COPY.breadcrumbReports).toBe("Reports")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.breadcrumbCampaignsReport).toBe(
      "Campaigns report",
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.pageTitle).toBe("Campaigns report")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.emptyTitle).toBe(
      "No campaign reports yet",
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.createCampaign).toBe("Create campaign")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.claimsHeader).toBe("Claims")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.redemptionsHeader).toBe("Redemptions")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.unsubscribesHeader).toBe("Unsubscribes")
    expect(CAMPAIGNS_REPORT_PAGE_COPY.performanceSectionTitle).toBe(
      "Campaign performance",
    )
    expect(CAMPAIGNS_REPORT_PAGE_COPY.needsAttentionSectionTitle).toBe(
      "Needs attention",
    )
  })

  it("maps a ready campaigns payload into six KPIs and labelled rows", () => {
    const response: Extract<
      ReportsCampaignsResponse,
      { lifetimeEmpty: false }
    > = {
      success: true,
      lifetimeEmpty: false,
      campaignsSent: { value: 2, valuePrevious: 1 },
      guestsMessaged: { value: 4, valuePrevious: 2 },
      failedSends: { value: 1, valuePrevious: 0 },
      offerClaims: { value: 5, valuePrevious: 3 },
      offerRedemptions: { value: 2, valuePrevious: 1 },
      unsubscribes: { value: 1, valuePrevious: 0 },
      performance: [
        {
          campaignId: 9,
          name: "Quiet Tuesday",
          goal: "boost-quieter-time",
          channel: "sms",
          sent: 3,
          claims: 2,
          redemptions: 1,
          unsubscribes: 0,
          status: "sent",
        },
      ],
      needsAttention: [
        {
          campaignId: 11,
          name: "Failed blast",
          status: "failed",
        },
      ],
    }

    const view = buildCampaignsReportViewModel(response)
    expect(view.kpis).toHaveLength(6)
    expect(view.kpis.map((row) => row.label)).toEqual([
      "Campaigns sent",
      "Guests messaged",
      "Failed sends",
      "Offer claims",
      "Offer redemptions",
      "Unsubscribes",
    ])
    expect(view.kpis[0]?.value).toBe("2")
    expect(view.performance[0]).toMatchObject({
      campaignId: 9,
      name: "Quiet Tuesday",
      goal: "Boost a quieter time",
      channel: "SMS",
      sent: 3,
      claims: 2,
      redemptions: 1,
      unsubscribes: 0,
      statusLabel: "Sent",
    })
    expect(view.attentionItems[0]).toMatchObject({
      campaignId: 11,
      name: "Failed blast",
      statusLabel: "Failed",
    })
  })
})
