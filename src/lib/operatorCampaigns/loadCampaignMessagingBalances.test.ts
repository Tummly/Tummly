import { describe, expect, it } from "vitest"

import {
  mapCreditsUsageToCampaignBillingBalances,
  resolveCampaignMessagingChromeAccessFromBillingPage,
  resolveCampaignMessagingLockFromBillingStatus,
} from "@/lib/operatorCampaigns/loadCampaignMessagingBalances"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"

const SAMPLE_USAGE: CreditsUsageSnapshot = {
  periodLabel: "August 2026",
  starterKitState: "unused",
  isPilot: false,
  channels: [
    {
      channel: "email",
      combinedRemaining: 400,
      usedThisCycle: 100,
      includedThisPeriod: 500,
      purchasedRemaining: 0,
      purchasedExpiryLabel: null,
    },
    {
      channel: "sms",
      combinedRemaining: 40,
      usedThisCycle: 10,
      includedThisPeriod: 50,
      purchasedRemaining: 0,
      purchasedExpiryLabel: null,
    },
    {
      channel: "ai",
      combinedRemaining: 12,
      usedThisCycle: 0,
      includedThisPeriod: 20,
      purchasedRemaining: 0,
      purchasedExpiryLabel: null,
    },
  ],
}

describe("resolveCampaignMessagingLockFromBillingStatus", () => {
  it("maps Soft lock Pilot to unpaid-pilot cause", () => {
    expect(
      resolveCampaignMessagingLockFromBillingStatus({
        billingStatus: "Soft lock",
        isPilot: true,
      })
    ).toEqual({ softLocked: true, lockCause: "unpaid-pilot" })
  })

  it("maps Dormant paid to dunning cause", () => {
    expect(
      resolveCampaignMessagingLockFromBillingStatus({
        billingStatus: "Dormant",
        isPilot: false,
      })
    ).toEqual({ softLocked: true, lockCause: "dunning" })
  })

  it("leaves Active unlocked", () => {
    expect(
      resolveCampaignMessagingLockFromBillingStatus({
        billingStatus: "Active",
        isPilot: false,
      })
    ).toEqual({ softLocked: false, lockCause: null })
  })
})

describe("resolveCampaignMessagingChromeAccessFromBillingPage", () => {
  it("maps actorCanManage to manage access", () => {
    expect(
      resolveCampaignMessagingChromeAccessFromBillingPage({
        actorPermissionRole: "Billing Admin",
        actorCanManage: true,
      })
    ).toEqual({
      accessLevel: "manage",
      permissionRole: "Billing Admin",
    })
  })

  it("maps non-manage actor to view access", () => {
    expect(
      resolveCampaignMessagingChromeAccessFromBillingPage({
        actorPermissionRole: "Marketing",
        actorCanManage: false,
      })
    ).toEqual({
      accessLevel: "view",
      permissionRole: "Marketing",
    })
  })
})

describe("mapCreditsUsageToCampaignBillingBalances", () => {
  it("carries chrome access and soft lock onto the Campaigns payload", () => {
    const payload = mapCreditsUsageToCampaignBillingBalances(SAMPLE_USAGE, {
      softLocked: true,
      lockCause: "unpaid-pilot",
      chromeAccess: {
        accessLevel: "manage",
        permissionRole: "Owner",
      },
    })

    expect(payload.softLocked).toBe(true)
    expect(payload.lockCause).toBe("unpaid-pilot")
    expect(payload.chromeAccess).toEqual({
      accessLevel: "manage",
      permissionRole: "Owner",
    })
    expect(payload.email.combinedRemaining).toBe(400)
    expect(payload.ai.available).toBe(12)
  })
})
