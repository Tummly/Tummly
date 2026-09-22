import { describe, expect, it } from "vitest"

import { resolveGuestPrimaryCta } from "@/lib/operatorGuests/guestPrimaryCta"

describe("resolveGuestPrimaryCta", () => {
  it("swaps to Start recovery when not eligible and needs recovery with id", () => {
    expect(
      resolveGuestPrimaryCta({
        marketingEligible: false,
        needsRecovery: true,
        recoveryFeedbackId: 77,
      })
    ).toEqual({
      kind: "start-recovery",
      feedbackId: 77,
      label: "Start recovery",
    })
  })

  it("keeps Create campaign enabled when marketing eligible", () => {
    expect(
      resolveGuestPrimaryCta({
        marketingEligible: true,
        needsRecovery: true,
        recoveryFeedbackId: 77,
      })
    ).toEqual({
      kind: "create-campaign",
      enabled: true,
      label: "Create campaign",
    })
  })

  it("keeps Create campaign disabled when not eligible and no recovery", () => {
    expect(
      resolveGuestPrimaryCta({
        marketingEligible: false,
        needsRecovery: false,
        recoveryFeedbackId: null,
      })
    ).toEqual({
      kind: "create-campaign",
      enabled: false,
      label: "Create campaign",
    })
  })

  it("falls back to disabled Create campaign when recovery id is missing", () => {
    expect(
      resolveGuestPrimaryCta({
        marketingEligible: false,
        needsRecovery: true,
        recoveryFeedbackId: null,
      })
    ).toEqual({
      kind: "create-campaign",
      enabled: false,
      label: "Create campaign",
    })
  })

  it("supports Create campaign with guest label for Actions menu", () => {
    expect(
      resolveGuestPrimaryCta(
        {
          marketingEligible: true,
          needsRecovery: false,
          recoveryFeedbackId: null,
        },
        { createCampaignLabel: "Create campaign with guest" }
      )
    ).toEqual({
      kind: "create-campaign",
      enabled: true,
      label: "Create campaign with guest",
    })
  })
})
