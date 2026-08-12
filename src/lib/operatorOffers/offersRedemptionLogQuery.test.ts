import { describe, expect, it } from "vitest"

import {
  loadLocationRedemptionLogRows,
  mapLocationRedemptionLogRow,
} from "@/lib/operatorOffers/offersRedemptionLogQuery"

describe("mapLocationRedemptionLogRow", () => {
  it("maps API redemption row including Offer title for the location log", () => {
    const row = mapLocationRedemptionLogRow({
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
      offerTitle: "Free coffee",
    })

    expect(row).toEqual({
      id: "redeemed-12",
      dateTimeText: expect.stringContaining("Aug"),
      guestName: "Alex Guest",
      passReferenceText: "TUM-RED001",
      locationName: "Camden",
      staffMemberText: "—",
      outcomeText: "Redeemed",
      reasonText: "—",
      offerVersionText: "1 Aug 2026",
      offerTitle: "Free coffee",
    })
  })
})

describe("loadLocationRedemptionLogRows", () => {
  it("returns mapped rows when the location list succeeds", async () => {
    const rows = await loadLocationRedemptionLogRows(42, {
      fetchRedemptions: async (locationId) => {
        expect(locationId).toBe(42)
        return {
          success: true,
          items: [
            {
              id: "failed-9",
              kind: "failed",
              dateTimeUtc: "2026-08-02T16:00:00.000Z",
              guestName: "Sam",
              guestId: 3,
              passReferenceText: "TUM-FAIL1",
              passId: "8",
              passCodeMasked: "•••• AIL1",
              locationName: "Camden",
              staffMemberText: null,
              outcome: "expired",
              outcomeLabel: "Expired",
              reason: "expired",
              reasonLabel: "Pass expired",
              offerVersionLabel: "20 Jul 2026",
              expiresAtUtc: null,
              linkedCampaignText: null,
              offerTitle: "10% off next visit",
            },
          ],
        }
      },
    })

    expect(rows).toHaveLength(1)
    expect(rows[0]).toMatchObject({
      id: "failed-9",
      guestName: "Sam",
      outcomeText: "Expired",
      reasonText: "Pass expired",
      offerTitle: "10% off next visit",
    })
  })

  it("throws when the location list response is not successful", async () => {
    await expect(
      loadLocationRedemptionLogRows(42, {
        fetchRedemptions: async () => ({ success: false, items: [] }),
      })
    ).rejects.toThrow("Location redemption log list failed.")
  })
})
