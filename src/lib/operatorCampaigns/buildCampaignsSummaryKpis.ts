import { CAMPAIGNS_PAGE_COPY } from "@/lib/operatorCampaigns/campaignsPresentation"

export type CampaignsSummaryFacts = {
  marketingEligible: number
  /** Campaigns with Scheduled status — ignores overview date window. */
  scheduledCount: number
  /** Campaigns with Sending status — ignores overview date window. */
  sendingCount: number
  /** Submitted/accepted outbound messages in the overview window (Email first). */
  messagesSentAccepted: number
  /**
   * False until Offers mark-complete + Campaign offer attach facts exist.
   * When false, redemptions stay honest 0 with empty description.
   */
  redemptionsHasRealData: boolean
  /** Only used when redemptionsHasRealData is true. */
  redemptions?: number
}

export type OperatorCampaignsSummaryKpiId =
  | "marketing-eligible"
  | "campaigns-in-flight"
  | "messages-sent"
  | "campaign-attributed-redemptions"

export type OperatorCampaignsSummaryKpi = {
  id: OperatorCampaignsSummaryKpiId
  label: string
  description: string
  value: number
}

export type CampaignsSummaryKpisResult = {
  kpis: OperatorCampaignsSummaryKpi[]
}

function formatCount(value: number): string {
  return value.toLocaleString("en-GB")
}

/** Build Campaign overview summary KPI cards from live facts (ticket 29). */
export function buildCampaignsSummaryKpis(
  facts: CampaignsSummaryFacts
): CampaignsSummaryKpisResult {
  const scheduled = facts.scheduledCount
  const sending = facts.sendingCount
  const inFlightValue = scheduled + sending

  const redemptionsValue = facts.redemptionsHasRealData
    ? (facts.redemptions ?? 0)
    : 0

  const kpis: OperatorCampaignsSummaryKpi[] = [
    {
      id: "marketing-eligible",
      label: CAMPAIGNS_PAGE_COPY.marketingEligibleLabel,
      description: CAMPAIGNS_PAGE_COPY.marketingEligibleDescription,
      value: facts.marketingEligible,
    },
    {
      id: "campaigns-in-flight",
      label: CAMPAIGNS_PAGE_COPY.campaignsInFlightLabel,
      description: `${scheduled} scheduled · ${sending} sending`,
      value: inFlightValue,
    },
    {
      id: "messages-sent",
      label: CAMPAIGNS_PAGE_COPY.messagesSentLabel,
      description: `${formatCount(facts.messagesSentAccepted)} email`,
      value: facts.messagesSentAccepted,
    },
    {
      id: "campaign-attributed-redemptions",
      label: CAMPAIGNS_PAGE_COPY.campaignAttributedRedemptionsLabel,
      // Honest empty until Offers mark-complete + Campaign offer attach exist.
      description: "",
      value: redemptionsValue,
    },
  ]

  return { kpis }
}
