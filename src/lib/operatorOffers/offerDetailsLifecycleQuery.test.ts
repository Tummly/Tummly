import { describe, expect, it } from "vitest"

import {
  loadOfferDetailsClaims,
  loadOfferDetailsLinkedCampaigns,
  loadOfferDetailsRedemptions,
  loadOfferDetailsVoidRequests,
  mapOfferDetailsClaimListItem,
  mapOfferDetailsIssuanceSourceListItem,
  mapOfferDetailsLinkedCampaignListItem,
  mapOfferDetailsRedemptionListItem,
  mapOfferDetailsVoidRequestListItem,
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

describe("mapOfferDetailsLinkedCampaignListItem", () => {
  it("maps linked campaign API row to Campaigns table view-model", () => {
    const row = mapOfferDetailsLinkedCampaignListItem({
      id: "9",
      campaignName: "Summer thank-you",
      status: "sent",
      statusLabel: "Sent",
      locationName: "Camden",
      channelLabel: "EMAIL",
      audienceLabel: "All eligible guests",
      offerVersionLabel: "1 Aug 2026",
      passesIssued: "12",
      claims: "8",
      redemptions: "3",
      sendDateUtc: "2026-08-01T12:00:00.000Z",
      sendDateLabel: "1 Aug 2026",
    })

    expect(row).toEqual({
      id: "9",
      campaignName: "Summer thank-you",
      statusText: "Sent",
      locationName: "Camden",
      channelText: "EMAIL",
      audienceText: "All eligible guests",
      offerVersionText: "1 Aug 2026",
      passesIssuedText: "12",
      claimsText: "8",
      redemptionsText: "3",
      sendDateText: "1 Aug 2026",
    })
  })
})

describe("mapOfferDetailsIssuanceSourceListItem", () => {
  it("maps issuance source API row with distinct source and path", () => {
    const row = mapOfferDetailsIssuanceSourceListItem({
      id: "campaign:9",
      sourceLabel: "Campaign",
      pathLabel: "Welcome blast",
      passesIssued: "4",
      lastIssuedAtUtc: "2026-08-02T10:00:00.000Z",
      lastIssuedLabel: "2 Aug 2026",
    })

    expect(row).toEqual({
      id: "campaign:9",
      sourceText: "Campaign",
      pathText: "Welcome blast",
      passesIssuedText: "4",
      lastIssuedText: "2 Aug 2026",
    })
  })
})

describe("mapOfferDetailsVoidRequestListItem", () => {
  it("maps void request API row including Review dialogue fields", () => {
    const row = mapOfferDetailsVoidRequestListItem({
      requestId: "44",
      requestedAtUtc: "2026-08-03T11:00:00.000Z",
      requestedAtText: "3 Aug 2026, 11:00",
      requestedByText: "Sam Operator",
      guestName: "Maya Guest",
      offerPassText: "•••• 0001",
      reasonId: "redeemed_by_mistake",
      reasonText: "Redeemed by mistake",
      explanation: null,
      locationName: "Camden",
      currentStateText: "Redeemed",
      correctionId: "keep_unusable",
      correctionText: "Keep pass unusable",
      status: "pending",
      statusLabel: "Pending",
      passId: "12",
      passCodeMasked: "•••• 0001",
      expiresText: "Expires 15 Aug 2026",
      linkedCampaignText: "Summer thank-you",
      offerTitle: "10% off next visit",
    })

    expect(row).toMatchObject({
      id: "44",
      dateTimeText: "3 Aug 2026, 11:00",
      requestedByText: "Sam Operator",
      guestName: "Maya Guest",
      offerPassText: "•••• 0001",
      reasonText: "Redeemed by mistake",
      statusText: "Pending",
      passId: "12",
      reasonId: "redeemed_by_mistake",
      correctionId: "keep_unusable",
      offerTitle: "10% off next visit",
    })
  })
})

describe("loadOfferDetailsVoidRequests", () => {
  it("throws when API success is false", async () => {
    await expect(
      loadOfferDetailsVoidRequests(10, {
        fetchVoidRequests: async () => ({ success: false, items: [] }),
      })
    ).rejects.toThrow("Offer void requests list failed.")
  })

  it("returns mapped rows when API succeeds", async () => {
    const rows = await loadOfferDetailsVoidRequests(10, {
      fetchVoidRequests: async () => ({
        success: true,
        items: [
          {
            requestId: "1",
            requestedAtUtc: "2026-08-03T11:00:00.000Z",
            requestedAtText: "3 Aug 2026, 11:00",
            requestedByText: "Sam",
            guestName: "Maya",
            offerPassText: "•••• 0001",
            reasonId: "redeemed_by_mistake",
            reasonText: "Redeemed by mistake",
            explanation: null,
            locationName: "Camden",
            currentStateText: "Redeemed",
            correctionId: "keep_unusable",
            correctionText: "Keep pass unusable",
            status: "pending",
            statusLabel: "Pending",
            passId: "12",
            passCodeMasked: "•••• 0001",
            expiresText: "Expires 15 Aug 2026",
            linkedCampaignText: "Not issued through a campaign",
            offerTitle: "Offer",
          },
        ],
      }),
    })

    expect(rows).toHaveLength(1)
    expect(rows[0]?.statusText).toBe("Pending")
  })
})

describe("loadOfferDetailsLinkedCampaigns", () => {
  it("throws when API success is false", async () => {
    await expect(
      loadOfferDetailsLinkedCampaigns(10, {
        fetchLinkedCampaigns: async () => ({ success: false, items: [] }),
      })
    ).rejects.toThrow("Offer linked campaigns list failed.")
  })
})
