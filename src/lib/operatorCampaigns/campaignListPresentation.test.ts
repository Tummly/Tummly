import { describe, expect, it } from "vitest"

import {
  buildCampaignRowActions,
  mapCampaignListItemToTableRow,
} from "@/lib/operatorCampaigns/campaignListPresentation"
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
      status: "draft",
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
    })
  })

  it("keeps non-Draft status on the row for Preview actions", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "scheduled",
        name: "Weekend brunch push",
      })
    )

    expect(row).toMatchObject({
      status: "scheduled",
      statusLabel: "Scheduled",
    })
  })
})

describe("buildCampaignRowActions", () => {
  it("returns Preview and Continue editing for Draft", () => {
    expect(buildCampaignRowActions("draft")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "continue-editing", label: "Continue editing" },
    ])
  })

  it("returns Preview only for non-Draft statuses", () => {
    expect(buildCampaignRowActions("scheduled")).toEqual([
      { id: "preview", label: "Preview" },
    ])
    expect(buildCampaignRowActions("sent")).toEqual([
      { id: "preview", label: "Preview" },
    ])
  })
})
