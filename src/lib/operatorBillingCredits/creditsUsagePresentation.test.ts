import { describe, expect, it } from "vitest"

import {
  billingCreditsChannelCardActions,
  buildCreditChannelCardViewModel,
  creditChannelFillRatio,
  creditChannelHeadline,
  creditChannelPurchasedLine,
  creditChannelSubline,
  type CreditChannelUsageRecord,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"

function channelRecord(
  overrides: Partial<CreditChannelUsageRecord> = {}
): CreditChannelUsageRecord {
  return {
    channel: "sms",
    combinedRemaining: 428,
    usedThisCycle: 72,
    includedThisPeriod: 500,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
    ...overrides,
  }
}

describe("creditChannelFillRatio", () => {
  it("derives bar width from remaining over remaining plus used", () => {
    expect(creditChannelFillRatio(428, 72)).toBeCloseTo(428 / 500)
    expect(creditChannelFillRatio(0, 500)).toBe(1)
  })
})

describe("creditChannelHeadline", () => {
  it("shows combined remaining for live channels", () => {
    expect(creditChannelHeadline(428, "sms")).toBe("428 remaining")
  })

  it("shows 100% copy when combined remaining is zero", () => {
    expect(creditChannelHeadline(0, "email")).toBe(
      "No Email credits remaining."
    )
  })
})

describe("creditChannelSubline", () => {
  it("formats included used against included allowance", () => {
    expect(creditChannelSubline(72, 500)).toBe("72 of 500 included used")
  })
})

describe("creditChannelPurchasedLine", () => {
  it("is omitted when purchased remaining is zero", () => {
    expect(creditChannelPurchasedLine(0, "31 Dec 2026")).toBeNull()
  })

  it("includes earliest expiry when purchased remaining is positive", () => {
    expect(creditChannelPurchasedLine(120, "31 Dec 2026")).toBe(
      "120 purchased remaining · use by 31 Dec 2026"
    )
  })
})

describe("billingCreditsChannelCardActions", () => {
  it("hides write CTAs for View", () => {
    expect(
      billingCreditsChannelCardActions({
        accessLevel: "view",
        permissionRole: "Marketing",
        isPilot: false,
        isDepleted: false,
      })
    ).toEqual({ showBuy: false, showChangePlan: false })
  })

  it("hides Buy on Pilot while keeping Change plan at 100% for Owner", () => {
    expect(
      billingCreditsChannelCardActions({
        accessLevel: "manage",
        permissionRole: "Owner",
        isPilot: true,
        isDepleted: true,
      })
    ).toEqual({ showBuy: false, showChangePlan: true })
  })

  it("keeps Buy for Billing Admin but hides Change plan at 100%", () => {
    expect(
      billingCreditsChannelCardActions({
        accessLevel: "manage",
        permissionRole: "Billing Admin",
        isPilot: false,
        isDepleted: true,
      })
    ).toEqual({ showBuy: true, showChangePlan: false })
  })
})

describe("buildCreditChannelCardViewModel", () => {
  it("maps combined remaining math onto card copy", () => {
    const card = buildCreditChannelCardViewModel(channelRecord(), {
      accessLevel: "manage",
      permissionRole: "Owner",
      isPilot: false,
    })

    expect(card.headline).toBe("428 remaining")
    expect(card.subline).toBe("72 of 500 included used")
    expect(card.fillRatio).toBeCloseTo(428 / 500)
    expect(card.meterMaxLabel).toBe("500")
    expect(card.showBuy).toBe(true)
    expect(card.buyLabel).toBe("Buy SMS credits")
  })

  it("shows 100% copy and Change plan for depleted paid Owner", () => {
    const card = buildCreditChannelCardViewModel(
      channelRecord({ combinedRemaining: 0, usedThisCycle: 500 }),
      {
        accessLevel: "manage",
        permissionRole: "Owner",
        isPilot: false,
      }
    )

    expect(card.headline).toBe("No SMS credits remaining.")
    expect(card.fillRatio).toBe(1)
    expect(card.showBuy).toBe(true)
    expect(card.showChangePlan).toBe(true)
  })
})
