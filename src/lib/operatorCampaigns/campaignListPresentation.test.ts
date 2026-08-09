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
    rowVersion: "AAAAAAAAB9E=",
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
      rowVersion: "AAAAAAAAB9E=",
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

  it("labels partially-sent as Partially sent", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({ status: "partially-sent" })
    )
    expect(row.statusLabel).toBe("Partially sent")
  })

  it("always dashes Engagement even when the API sends a value", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "sent",
        engagement: "12%",
        delivery: "98%",
      })
    )
    expect(row.engagementLabel).toBe("—")
    expect(row.deliveryLabel).toBe("98%")
  })
})

describe("buildCampaignRowActions", () => {
  it("returns Preview and Continue editing for Draft", () => {
    expect(buildCampaignRowActions("draft")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "continue-editing", label: "Continue editing" },
    ])
  })

  it("returns Scheduled lifecycle actions", () => {
    expect(buildCampaignRowActions("scheduled")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "unschedule", label: "Unschedule" },
      { id: "pause", label: "Pause" },
      { id: "cancel", label: "Cancel" },
    ])
  })

  it("returns Sending lifecycle actions", () => {
    expect(buildCampaignRowActions("sending")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "pause", label: "Pause" },
      { id: "cancel-remaining", label: "Cancel remaining" },
    ])
  })

  it("returns Paused lifecycle actions", () => {
    expect(buildCampaignRowActions("paused")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "resume", label: "Resume" },
      { id: "cancel", label: "Cancel" },
    ])
  })

  it("returns Retry remaining only for Partially sent", () => {
    expect(buildCampaignRowActions("partially-sent")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "retry-remaining", label: "Retry remaining" },
    ])
  })

  it("returns Preview only for Sent and Cancelled", () => {
    expect(buildCampaignRowActions("sent")).toEqual([
      { id: "preview", label: "Preview" },
    ])
    expect(buildCampaignRowActions("cancelled")).toEqual([
      { id: "preview", label: "Preview" },
    ])
  })

  it("returns Duplicate for Failed", () => {
    expect(buildCampaignRowActions("failed")).toEqual([
      { id: "preview", label: "Preview" },
      {
        id: "duplicate",
        label: "Duplicate / retry as new Draft",
      },
    ])
  })
})
