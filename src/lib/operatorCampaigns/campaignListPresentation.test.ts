import { describe, expect, it } from "vitest"

import {
  buildCampaignRowActions,
  formatCampaignListChannelDetail,
  formatCampaignListDeliveryLabel,
  formatCampaignListRedemptionsLabel,
  formatCampaignListSendDate,
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
        delivery: "98",
        recipientCount: 100,
      })
    )
    expect(row.engagementLabel).toBe("—")
    expect(row.deliveryLabel).toBe("98 of 100 processed")
  })

  it("prefers catalog Offer title and expiry over stance label", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "sent",
        offerStance: "existing-offer",
        offerTitle: "10% off next visit",
        offerValidity: "choose_expiry_date",
        offerExpiryDate: "2026-08-31",
      })
    )
    expect(row.offerTitle).toBe("10% off next visit")
    expect(row.offerDetail).toMatch(/^Expires:/)
    expect(row.offerDetail).toContain("31")
  })

  it("maps Redemptions from the list API when present", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "sent",
        redemptions: "2",
      })
    )
    expect(row.redemptionsLabel).toBe("2 redeemed")
  })

  it("formats Channel detail with recipients and SMS parts", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "sent",
        recipientCount: 154,
        smsPartsPerMessage: 2,
      })
    )
    expect(row.channelDetail).toBe("154 recipients · 2 parts each")
  })

  it("formats Send date as day month · time", () => {
    const row = mapCampaignListItemToTableRow(
      sampleItem({
        status: "scheduled",
        sendDate: "2026-08-18T09:00:00.000Z",
      })
    )
    expect(row.sendDateLabel).not.toBe("—")
    expect(row.sendDateLabel).toMatch(/ · /)
    expect(row.sendDateLabel).toMatch(/AM|PM/)
  })
})

describe("campaign list column formatters", () => {
  it("formatCampaignListChannelDetail omits parts for email", () => {
    expect(
      formatCampaignListChannelDetail(
        sampleItem({
          channel: "email",
          recipientCount: 40,
          smsPartsPerMessage: null,
        })
      )
    ).toBe("40 recipients")
  })

  it("formatCampaignListSendDate dashes invalid input", () => {
    expect(formatCampaignListSendDate(null)).toBe("—")
    expect(formatCampaignListSendDate("not-a-date")).toBe("—")
  })

  it("formatCampaignListDeliveryLabel falls back to raw delivery", () => {
    expect(
      formatCampaignListDeliveryLabel(sampleItem({ delivery: "12" }))
    ).toBe("12")
  })

  it("formatCampaignListRedemptionsLabel dashes empty", () => {
    expect(formatCampaignListRedemptionsLabel(null)).toBe("—")
  })
})

describe("buildCampaignRowActions", () => {
  it("returns Preview, Continue editing, and Delete draft for Draft", () => {
    expect(buildCampaignRowActions("draft")).toEqual([
      { id: "preview", label: "Preview" },
      { id: "continue-editing", label: "Continue editing" },
      { id: "delete-draft", label: "Delete draft" },
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
