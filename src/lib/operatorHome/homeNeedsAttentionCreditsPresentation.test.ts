import { describe, expect, it } from "vitest"

import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import {
  creditChannelQualifiesForHomeNeedsAttention,
  creditChannelUsedShare,
  homeNeedsAttentionCreditCtas,
  mapHomeNeedsAttentionCreditFacts,
} from "./homeNeedsAttentionCreditsPresentation"

function usage(
  channels: CreditsUsageSnapshot["channels"],
  isPilot = false
): CreditsUsageSnapshot {
  return {
    periodLabel: "Aug 2026",
    starterKitState: "unused",
    isPilot,
    channels,
  }
}

describe("homeNeedsAttentionCreditsPresentation", () => {
  it("computes used share as used / (remaining + used)", () => {
    expect(creditChannelUsedShare(20, 80)).toBe(0.8)
    expect(creditChannelUsedShare(0, 10)).toBe(1)
    expect(creditChannelUsedShare(100, 0)).toBe(0)
  })

  it("qualifies at 80% and leaves below 80%", () => {
    expect(creditChannelQualifiesForHomeNeedsAttention(20, 80)).toBe(true)
    expect(creditChannelQualifiesForHomeNeedsAttention(21, 79)).toBe(false)
  })

  it("maps one row per qualifying channel in SMS, Email, AI order", () => {
    const facts = mapHomeNeedsAttentionCreditFacts({
      usage: usage([
        {
          channel: "ai",
          combinedRemaining: 5,
          usedThisCycle: 45,
          includedThisPeriod: 50,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
        {
          channel: "email",
          combinedRemaining: 2,
          usedThisCycle: 18,
          includedThisPeriod: 20,
          purchasedRemaining: 0,
          purchasedExpiryLabel: null,
        },
      ]),
      accessLevel: "view",
      permissionRole: "Marketing",
      workspaceName: "Tummly Demo",
    })

    expect(facts.map((fact) => fact.channel)).toEqual(["email", "ai"])
    expect(facts[0]).toMatchObject({
      channel: "email",
      band: 90,
      title: "Email credits at 90% used",
      body: "Tummly Demo has used at least 90% of its Email credits this period.",
      ctas: [{ kind: "view-usage", label: "View usage" }],
    })
  })

  it("resolves 100% paid write CTA as Buy and View-only as View usage", () => {
    expect(
      homeNeedsAttentionCreditCtas({
        channel: "sms",
        band: 100,
        accessLevel: "manage",
        permissionRole: "Owner",
        isPilot: false,
      })
    ).toEqual([{ kind: "buy-channel-credits", label: "Buy SMS credits" }])

    expect(
      homeNeedsAttentionCreditCtas({
        channel: "sms",
        band: 100,
        accessLevel: "view",
        permissionRole: "Marketing",
        isPilot: false,
      })
    ).toEqual([{ kind: "view-usage", label: "View usage" }])
  })

  it("resolves 100% Pilot Owner to Change plan and others to View usage", () => {
    expect(
      homeNeedsAttentionCreditCtas({
        channel: "ai",
        band: 100,
        accessLevel: "manage",
        permissionRole: "Owner",
        isPilot: true,
      })
    ).toEqual([{ kind: "change-plan", label: "Change plan" }])

    expect(
      homeNeedsAttentionCreditCtas({
        channel: "ai",
        band: 100,
        accessLevel: "manage",
        permissionRole: "Billing Admin",
        isPilot: true,
      })
    ).toEqual([{ kind: "view-usage", label: "View usage" }])
  })
})
