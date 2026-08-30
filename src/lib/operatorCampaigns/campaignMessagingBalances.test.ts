import { describe, expect, it, vi } from "vitest"

import {
  CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
  CAMPAIGN_AI_PREPARE_BLOCKED_SOFT_LOCK,
  CAMPAIGN_AI_PREPARE_BLOCKED_NO_CREDITS,
  mapBillingBalancesToMessagingFixture,
  resolveCampaignAiPrepareGate,
  resolveCampaignMessagingUsage,
  maybeConsumeDirectAiOnUsableDraft,
  type CampaignBillingBalancesPayload,
} from "@/lib/operatorCampaigns/campaignMessagingBalances"
import { MESSAGING_USAGE_FIXTURE } from "@/lib/operatorCampaigns/messagingUsageFixtures"

const SAMPLE_LIVE_BALANCES: CampaignBillingBalancesPayload = {
  email: {
    combinedRemaining: 400,
    usedThisCycle: 100,
    includedThisPeriod: 500,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  sms: {
    combinedRemaining: 40,
    usedThisCycle: 10,
    includedThisPeriod: 50,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  ai: {
    available: 12,
  },
  isPilot: false,
  softLocked: false,
  lockCause: null,
}

describe("resolveCampaignMessagingUsage", () => {
  it("uses fixtures before Billing cutover", () => {
    const resolved = resolveCampaignMessagingUsage({ cutover: "fixtures" })

    expect(resolved).toEqual({
      status: "ready",
      source: "fixtures",
      fixture: MESSAGING_USAGE_FIXTURE,
      viewModel: expect.objectContaining({
        email: expect.objectContaining({
          title: "Email credits",
          headline: "6,760 remaining",
        }),
        sms: expect.objectContaining({
          title: "SMS credits",
          headline: "300 remaining",
        }),
      }),
      aiAvailable: null,
      softLocked: false,
      lockCause: null,
      isPilot: false,
    })
    if (resolved.status === "ready") {
      expect(resolved.viewModel).not.toHaveProperty("plan")
    }
  })

  it("maps the same live Billing balances payload for overview and Channel", () => {
    const resolved = resolveCampaignMessagingUsage({
      cutover: "live",
      balances: SAMPLE_LIVE_BALANCES,
    })

    expect(resolved.status).toBe("ready")
    if (resolved.status !== "ready") {
      return
    }
    expect(resolved.source).toBe("live")
    expect(resolved.fixture).toEqual(
      mapBillingBalancesToMessagingFixture(SAMPLE_LIVE_BALANCES)
    )
    expect(resolved.viewModel.email.headline).toBe("400 remaining")
    expect(resolved.viewModel.sms.headline).toBe("40 remaining")
    expect(resolved.aiAvailable).toBe(12)
    expect(resolved.softLocked).toBe(false)
    expect(resolved.lockCause).toBeNull()
    expect(resolved.isPilot).toBe(false)
  })

  it("after live cutover, balances failure does not fall back to fixtures", () => {
    const resolved = resolveCampaignMessagingUsage({
      cutover: "live",
      failed: true,
    })

    expect(resolved).toEqual({
      status: "load-failed",
      source: "live",
      errorMessage: CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
    })
  })
})

describe("resolveCampaignAiPrepareGate", () => {
  it("allows Prepare before cutover (display-only AI)", () => {
    expect(
      resolveCampaignAiPrepareGate({
        cutover: "fixtures",
        softLocked: true,
        aiAvailable: 0,
      })
    ).toEqual({ allowed: true, blockReason: null })
  })

  it("disables Prepare on Soft-lock after live cutover", () => {
    expect(
      resolveCampaignAiPrepareGate({
        cutover: "live",
        softLocked: true,
        aiAvailable: 5,
      })
    ).toEqual({
      allowed: false,
      blockReason: CAMPAIGN_AI_PREPARE_BLOCKED_SOFT_LOCK,
    })
  })

  it("disables Prepare when AI available is 0 after live cutover", () => {
    expect(
      resolveCampaignAiPrepareGate({
        cutover: "live",
        softLocked: false,
        aiAvailable: 0,
      })
    ).toEqual({
      allowed: false,
      blockReason: CAMPAIGN_AI_PREPARE_BLOCKED_NO_CREDITS,
    })
  })

  it("disables Prepare when live balances failed to load", () => {
    expect(
      resolveCampaignAiPrepareGate({
        cutover: "live",
        softLocked: false,
        aiAvailable: null,
        balancesStatus: "load-failed",
      })
    ).toEqual({
      allowed: false,
      blockReason:
        "Messaging usage is unavailable. Try again before using AI.",
    })
  })
})

describe("maybeConsumeDirectAiOnUsableDraft", () => {
  it("debits 1 AI via ConsumeDirect on usable prepare success after cutover", async () => {
    const consumeDirectAi = vi.fn(async () => {})

    const outcome = await maybeConsumeDirectAiOnUsableDraft({
      cutover: "live",
      usableSuccess: true,
      locationId: 42,
      consumeDirectAi,
    })

    expect(outcome).toBe("debited")
    expect(consumeDirectAi).toHaveBeenCalledTimes(1)
    expect(consumeDirectAi).toHaveBeenCalledWith({
      locationId: 42,
      units: 1,
    })
  })

  it("debits 0 when prepare fails or times out", async () => {
    const consumeDirectAi = vi.fn(async () => {})

    const outcome = await maybeConsumeDirectAiOnUsableDraft({
      cutover: "live",
      usableSuccess: false,
      locationId: 42,
      consumeDirectAi,
    })

    expect(outcome).toBe("skipped")
    expect(consumeDirectAi).not.toHaveBeenCalled()
  })

  it("debits 0 before cutover even on usable success", async () => {
    const consumeDirectAi = vi.fn(async () => {})

    const outcome = await maybeConsumeDirectAiOnUsableDraft({
      cutover: "fixtures",
      usableSuccess: true,
      locationId: 42,
      consumeDirectAi,
    })

    expect(outcome).toBe("skipped")
    expect(consumeDirectAi).not.toHaveBeenCalled()
  })
})
