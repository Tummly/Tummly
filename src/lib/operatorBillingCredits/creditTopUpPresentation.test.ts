import { describe, expect, it } from "vitest"

import {
  buildCreditTopUpCards,
  buildCreditTopUpConfirmCopy,
  isSms5000TopUpAllowed,
  isTopUpPackVisible,
  visibleTopUpPacksForChannel,
  type CreditTopUpPack,
} from "@/lib/operatorBillingCredits/creditTopUpPresentation"
import type { CreditChannelUsageRecord } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

/** Server catalogue packs as the Billing page returns them (pence). */
const SERVER_TOP_UP_PACKS: CreditTopUpPack[] = [
  { channel: "sms", quantity: 100, netPence: 1200 },
  { channel: "sms", quantity: 500, netPence: 5500 },
  { channel: "sms", quantity: 1000, netPence: 10000 },
  { channel: "sms", quantity: 5000, netPence: 45000 },
  { channel: "ai", quantity: 100, netPence: 500 },
  { channel: "ai", quantity: 500, netPence: 1500 },
  { channel: "ai", quantity: 2000, netPence: 3900 },
  { channel: "email", quantity: 5000, netPence: 1000 },
  { channel: "email", quantity: 20000, netPence: 3000 },
  { channel: "email", quantity: 50000, netPence: 6000 },
]

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
        { channel: "sms", quantity: 5000, netPence: 45000 },
        { subscriptionPlan: "Starter", allowSms5000TopUp: false }
      )
    ).toBe(false)
    expect(
      visibleTopUpPacksForChannel("sms", SERVER_TOP_UP_PACKS, {
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

  it("offers no chips and no Buy when the catalogue has no packs", () => {
    const cards = buildCreditTopUpCards({
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: "sms",
      topUpPacks: [],
      vatRateBps: 2000,
    })

    expect(cards.every((card) => card.packs.length === 0)).toBe(true)
    expect(cards.every((card) => card.buyDisabled)).toBe(true)
    expect(cards.every((card) => card.selectedNetLabel == null)).toBe(true)
  })

  it("prices the selected chip from the catalogue netPence", () => {
    const cards = buildCreditTopUpCards({
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: "sms",
      topUpPacks: [{ channel: "sms", quantity: 500, netPence: 5550 }],
      vatRateBps: 2000,
    })

    expect(cards.find((card) => card.channel === "sms")?.selectedNetLabel).toBe(
      "£55.50 + VAT"
    )
  })

  it("shows operator-triggered detail on the AI top-up card", () => {
    const cards = buildCreditTopUpCards({
      topUpPacks: SERVER_TOP_UP_PACKS,
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: {},
      focusedChannel: null,
      vatRateBps: 2000,
    })

    const ai = cards.find((card) => card.channel === "ai")
    expect(ai?.detailLine).toBe(
      "Use AI credits for operator-triggered briefs, drafts and regenerations."
    )
  })

  it("disables Buy until a chip is selected", () => {
    const cards = buildCreditTopUpCards({
      topUpPacks: SERVER_TOP_UP_PACKS,
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: {},
      focusedChannel: null,
      vatRateBps: 2000,
    })

    expect(cards.every((card) => card.buyDisabled)).toBe(true)
    expect(cards.every((card) => card.selectedNetLabel == null)).toBe(true)
  })

  it("enables Buy when a chip is selected on a paid plan", () => {
    const cards = buildCreditTopUpCards({
      topUpPacks: SERVER_TOP_UP_PACKS,
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: "sms",
      vatRateBps: 2000,
    })

    const sms = cards.find((card) => card.channel === "sms")
    expect(sms?.buyDisabled).toBe(false)
    expect(sms?.selectedNetLabel).toBe("£55 + VAT")
  })

  it("omits + VAT from selectedNetLabel when vatRateBps is 0", () => {
    const cards = buildCreditTopUpCards({
      topUpPacks: SERVER_TOP_UP_PACKS,
      channels: sampleChannels,
      subscriptionPlan: "Growth",
      allowSms5000TopUp: false,
      isPilot: false,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: "sms",
      vatRateBps: 0,
    })

    const sms = cards.find((card) => card.channel === "sms")
    expect(sms?.selectedNetLabel).toBe("£55")
  })

  it("omits VAT phrases from confirm copy when vatRateBps is 0", () => {
    expect(
      buildCreditTopUpConfirmCopy({
        channelLabel: "SMS",
        quantity: 500,
        netLabel: "£55",
        grossLabel: "£55",
        vatRateBps: 0,
      }).body
    ).toBe("SMS · 500 credits · £55 · £55 total")
  })

  it("keeps VAT phrases on confirm copy when vatRateBps is active", () => {
    expect(
      buildCreditTopUpConfirmCopy({
        channelLabel: "SMS",
        quantity: 500,
        netLabel: "£55",
        grossLabel: "£66",
        vatRateBps: 2000,
      }).body
    ).toBe("SMS · 500 credits · £55 + VAT · £66 total incl. VAT")
  })

  it("hides purchase cards on Pilot", () => {
    const cards = buildCreditTopUpCards({
      topUpPacks: SERVER_TOP_UP_PACKS,
      channels: sampleChannels,
      subscriptionPlan: "Pilot",
      allowSms5000TopUp: false,
      isPilot: true,
      canBuy: true,
      selectedPackByChannel: { sms: 500 },
      focusedChannel: null,
      vatRateBps: 2000,
    })

    expect(cards).toEqual([])
  })
})
