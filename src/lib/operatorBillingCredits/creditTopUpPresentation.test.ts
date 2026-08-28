import { describe, expect, it } from "vitest"

import {
  buildCreditTopUpCards,
  grossTopUpPounds,
  isSms5000TopUpAllowed,
  isTopUpPackVisible,
  visibleTopUpPacksForChannel,
} from "@/lib/operatorBillingCredits/creditTopUpPresentation"
import type { CreditChannelUsageRecord } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

const sampleChannels: CreditChannelUsageRecord[] = [
  {
    channel: "sms",
    combinedRemaining: 428,
    usedThisCycle: 72,
    includedThisPeriod: 500,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  {
    channel: "email",
    combinedRemaining: 1200,
    usedThisCycle: 300,
    includedThisPeriod: 1500,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  {
    channel: "ai",
    combinedRemaining: 20,
    usedThisCycle: 0,
    includedThisPeriod: 20,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
]

describe("creditTopUpPresentation", () => {
  it("hides 5,000 SMS without Group or approval flag", () => {
    expect(
      isTopUpPackVisible(
        { channel: "sms", quantity: 5000, netPounds: 450 },
        { subscriptionPlan: "Starter", allowSms5000TopUp: false }
      )
    ).toBe(false)
    expect(
      visibleTopUpPacksForChannel("sms", {
        subscriptionPlan: "Growth",
        allowSms5000TopUp: false,
      }).map((pack) => pack.quantity)
    ).toEqual([100, 500, 1000])
  })

  it("shows 5,000 SMS on Group or approval flag", () => {
    expect(
      isSms5000TopUpAllowed({
        subscriptionPlan: "Group",
        allowSms5000TopUp: false,
      })
    ).toBe(true)
    expect(
      isSms5000TopUpAllowed({
        subscriptionPlan: "Starter",
        allowSms5000TopUp: true,
      })
    ).toBe(true)
  })

  it("computes VAT gross from net", () => {
    expect(grossTopUpPounds(12)).toBe(14.4)
    expect(grossTopUpPounds(450)).toBe(540)
  })

  it("disables Buy until a chip is selected", () => {
    const cards = buildCreditTopUpCards({
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: {},
      focusedChannel: null,
    })

    expect(cards.every((card) => card.buyDisabled)).toBe(true)
    expect(cards.every((card) => card.selectedNetLabel == null)).toBe(true)
  })

  it("enables Buy when a chip is selected on a paid plan", () => {
    const cards = buildCreditTopUpCards({
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: "sms",
    })

    const sms = cards.find((card) => card.channel === "sms")
    expect(sms?.buyDisabled).toBe(false)
    expect(sms?.selectedNetLabel).toBe("£55 + VAT")
  })

  it("keeps chips and Buy disabled on Pilot", () => {
    const cards = buildCreditTopUpCards({
      channels: sampleChannels,
      subscriptionPlan: "Pilot",
      allowSms5000TopUp: false,
      isPilot: true,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: null,
    })

    expect(cards.every((card) => card.buyDisabled)).toBe(true)
    expect(cards.every((card) => card.showPilotNotice)).toBe(true)
  })
})
