import { describe, expect, it } from "vitest"

import { mapCampaignListItemToTableRow } from "@/lib/operatorCampaigns/campaignListPresentation"
import type { CampaignsListItem } from "@/types/operatorCampaigns"

function sampleItem(
  overrides: Partial<CampaignsListItem> = {}
): CampaignsListItem {
  return {
    id: 9,
    name: "Tuesday lunch reminder",
    status: "draft",
    goalId: "boost-quieter-time",
    locationId: 42,
    locationName: "Camden",
    channel: "sms",
    audienceKey: "all-eligible-guests",
    offerStance: "no-offer",
    updatedAt: "2026-08-08T10:00:00.000Z",
    sendDate: null,
    delivery: null,
    engagement: null,
    redemptions: null,
    ...overrides,
  }
}

describe("mapCampaignListItemToTableRow", () => {
  it("maps a Draft list item to Figma row projection with metric dashes", () => {
    const nowMs = Date.parse("2026-08-08T12:00:00.000Z")
    const row = mapCampaignListItemToTableRow(sampleItem(), nowMs)

    expect(row).toMatchObject({
      id: 9,
      name: "Tuesday lunch reminder",
      metaLine: "Boost a quieter time · Updated 2 hours ago",
      statusLabel: "Draft",
      locationName: "Camden",
      channelLabel: "SMS",
      channelDetail: null,
      offerTitle: "No offer",
      offerDetail: null,
      sendDateLabel: "—",
      deliveryLabel: "—",
      engagementLabel: "—",
      redemptionsLabel: "—",
      continueEditingLabel: "Continue editing",
    })
  })
})
