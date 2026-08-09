import { describe, expect, it } from "vitest"

import {
  buildCampaignsSummaryKpis,
  type CampaignsSummaryFacts,
} from "./buildCampaignsSummaryKpis"
import { CAMPAIGNS_PAGE_COPY } from "./campaignsPresentation"

const baseFacts: CampaignsSummaryFacts = {
  marketingEligible: 12,
  scheduledCount: 0,
  sendingCount: 0,
  messagesSentAccepted: 0,
  redemptionsHasRealData: false,
}

describe("buildCampaignsSummaryKpis", () => {
  it("sets in-flight value to scheduled + sending and excludes paused from membership", () => {
    const { kpis } = buildCampaignsSummaryKpis({
      ...baseFacts,
      scheduledCount: 2,
      sendingCount: 1,
    })

    const inFlight = kpis.find((kpi) => kpi.id === "campaigns-in-flight")
    expect(inFlight).toMatchObject({
      label: CAMPAIGNS_PAGE_COPY.campaignsInFlightLabel,
      value: 3,
      description: "2 scheduled · 1 sending",
    })
  })

  it("formats messages-sent as email-first accepted count without Delivered", () => {
    const { kpis } = buildCampaignsSummaryKpis({
      ...baseFacts,
      messagesSentAccepted: 1510,
    })

    const messages = kpis.find((kpi) => kpi.id === "messages-sent")
    expect(messages).toMatchObject({
      label: CAMPAIGNS_PAGE_COPY.messagesSentLabel,
      value: 1510,
      description: "1,510 email",
    })
    expect(messages?.description.toLowerCase()).not.toContain("delivered")
    expect(messages?.description.toLowerCase()).not.toContain("sms")
  })

  it("shows honest redemptions zero with empty description when attach facts are absent", () => {
    const { kpis } = buildCampaignsSummaryKpis({
      ...baseFacts,
      redemptionsHasRealData: false,
    })

    expect(kpis.find((kpi) => kpi.id === "campaign-attributed-redemptions")).toEqual(
      {
        id: "campaign-attributed-redemptions",
        label: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsLabel,
        description: "",
        value: 0,
      }
    )
  })

  it("returns four display KPIs including live marketing eligible", () => {
    const { kpis } = buildCampaignsSummaryKpis(baseFacts)

    expect(kpis.map((kpi) => kpi.id)).toEqual([
      "marketing-eligible",
      "campaigns-in-flight",
      "messages-sent",
      "campaign-attributed-redemptions",
    ])
    expect(kpis[0]).toMatchObject({
      id: "marketing-eligible",
      value: 12,
      description: CAMPAIGNS_PAGE_COPY.marketingEligibleDescription,
    })
  })
})
