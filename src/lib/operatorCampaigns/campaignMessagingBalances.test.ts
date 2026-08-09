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
    used: 100,
    allowance: 500,
    remaining: 400,
    refreshLabel: "1 September",
  },
  sms: {
    total: 50,
    reserved: 10,
    available: 40,
  },
  plan: {
    name: "Starter",
    locationCount: 1,
    billingLine: "Billed monthly · Next refresh 1 September",
  },
  ai: {
    available: 12,
  },
  softLocked: false,
}

describe("resolveCampaignMessagingUsage", () => {
  it("uses fixtures before Billing cutover", () => {
    const resolved = resolveCampaignMessagingUsage({ cutover: "fixtures" })

    expect(resolved).toEqual({
      status: "ready",
      source: "fixtures",
      fixture: MESSAGING_USAGE_FIXTURE,
      viewModel: expect.objectContaining({
        plan: expect.objectContaining({ name: "Growth" }),
        email: expect.objectContaining({ remaining: 6760 }),
        sms: expect.objectContaining({ available: 300 }),
      }),
      aiAvailable: null,
      softLocked: false,
    })
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
    expect(resolved.viewModel.email.remaining).toBe(400)
    expect(resolved.viewModel.sms.available).toBe(40)
    expect(resolved.viewModel.plan.name).toBe("Starter")
    expect(resolved.aiAvailable).toBe(12)
    expect(resolved.softLocked).toBe(false)
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
