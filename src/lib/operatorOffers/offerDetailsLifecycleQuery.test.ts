import { describe, expect, it } from "vitest"

import {
  loadOfferDetailsClaims,
  loadOfferDetailsRedemptions,
  mapOfferDetailsClaimListItem,
  mapOfferDetailsRedemptionListItem,
} from "@/lib/operatorOffers/offerDetailsLifecycleQuery"

describe("mapOfferDetailsClaimListItem", () => {
  it("maps API claim row to Details Claims table view-model", () => {
    const row = mapOfferDetailsClaimListItem({
      id: "12",
      guestName: "Maya Guest",
      guestId: 42,
      claimCode: "TUM-CLM001",
      claimedAtUtc: "2026-08-01T12:00:00.000Z",
      issuedAtUtc: "2026-07-30T12:00:00.000Z",
      source: "campaign",
      sourceLabel: "Summer thank-you",
      campaignName: "Summer thank-you",
      locationName: "Camden",
      expiryAtUtc: "2026-08-15T23:59:59.000Z",
      status: "open",
      statusLabel: "Open",
      passCodeMasked: "•••• L001",
      offerTitle: "10% off next visit",
      linkedCampaignText: "Summer thank-you",
    })

    expect(row).toMatchObject({
      id: "12",
      guestName: "Maya Guest",
      guestId: "42",
      claimCode: "TUM-CLM001",
      sourceText: "Summer thank-you",
      locationName: "Camden",
      statusText: "Open",
    })
    expect(row.claimedText).toContain("Aug")
    expect(row.expiryText).toContain("Aug")
  })
})

describe("mapOfferDetailsRedemptionListItem", () => {
  it("maps redeemed API row including void-create fields", () => {
    const row = mapOfferDetailsRedemptionListItem({
      id: "redeemed-12",
      kind: "redeemed",
      dateTimeUtc: "2026-08-02T14:30:00.000Z",
      guestName: "Alex Guest",
      guestId: 7,
      passReferenceText: "TUM-RED001",
      passId: "12",
      passCodeMasked: "•••• D001",
      locationName: "Camden",
      staffMemberText: null,
      outcome: "redeemed",
      outcomeLabel: "Redeemed",
      reason: null,
      reasonLabel: null,
      offerVersionLabel: "1 Aug 2026",
      expiresAtUtc: "2026-08-15T23:59:59.000Z",
      linkedCampaignText: "Summer thank-you",
      offerTitle: "10% off next visit",
    })

    expect(row).toMatchObject({
      id: "redeemed-12",
      guestName: "Alex Guest",
      guestId: "7",
      passReferenceText: "TUM-RED001",
      passId: "12",
      passCodeMasked: "•••• D001",
      outcomeText: "Redeemed",
      reasonText: "—",
      staffMemberText: "—",
      offerTitle: "10% off next visit",
    })
    expect(row.dateTimeText).toContain("Aug")
  })

  it("maps failed attempt reason label", () => {
    const row = mapOfferDetailsRedemptionListItem({
      id: "failed-3",
      kind: "failed",
      dateTimeUtc: "2026-08-02T15:00:00.000Z",
      guestName: "Alex Guest",
      guestId: 7,
      passReferenceText: "TUM-RED001",
      passId: "12",
      passCodeMasked: "•••• D001",
      locationName: "Camden",
      staffMemberText: null,
      outcome: "already_used",
      outcomeLabel: "Already used",
      reason: "already_used",
      reasonLabel: "Already redeemed",
      offerVersionLabel: "1 Aug 2026",
      expiresAtUtc: null,
      linkedCampaignText: null,
      offerTitle: "10% off next visit",
    })

    expect(row.outcomeText).toBe("Already used")
    expect(row.reasonText).toBe("Already redeemed")
  })
})

describe("loadOfferDetailsClaims", () => {
  it("throws when API success is false", async () => {
    await expect(
      loadOfferDetailsClaims(10, {
        fetchClaims: async () => ({ success: false, items: [] }),
      })
    ).rejects.toThrow("Offer claims list failed.")
  })

  it("returns mapped rows when API succeeds", async () => {
    const rows = await loadOfferDetailsClaims(10, {
      fetchClaims: async () => ({
        success: true,
        items: [
          {
            id: "1",
            guestName: "Sam",
            guestId: 1,
            claimCode: "TUM-001",
            claimedAtUtc: "2026-08-01T12:00:00.000Z",
            issuedAtUtc: "2026-08-01T12:00:00.000Z",
            source: "campaign",
            sourceLabel: "Campaign",
            campaignName: null,
            locationName: "Camden",
            expiryAtUtc: "2026-08-15T23:59:59.000Z",
            status: "open",
            statusLabel: "Open",
            passCodeMasked: "•••• 0001",
            offerTitle: "Offer",
            linkedCampaignText: null,
          },
        ],
      }),
    })

    expect(rows).toHaveLength(1)
    expect(rows[0]?.guestName).toBe("Sam")
  })
})

describe("loadOfferDetailsRedemptions", () => {
  it("throws when API success is false", async () => {
    await expect(
      loadOfferDetailsRedemptions(10, {
        fetchRedemptions: async () => ({ success: false, items: [] }),
      })
    ).rejects.toThrow("Offer redemptions list failed.")
  })
})
