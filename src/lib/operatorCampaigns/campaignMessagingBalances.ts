/**
 * Campaigns Messaging usage + AI debit cutover against Billing balances.
 * Prefer live Billing payload when available; fixtures only pre-cutover.
 * Ticket 25 / grilling 14 — no Campaigns-owned ledger.
 */

import {
  MESSAGING_USAGE_FIXTURE,
  messagingUsageViewModelFromFixture,
  type MessagingUsageFixture,
  type OperatorCampaignsMessagingUsageViewModel,
} from "@/lib/operatorCampaigns/messagingUsageFixtures"

export const CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR =
  "Could not load messaging usage. Please try again."

export const CAMPAIGN_AI_PREPARE_BLOCKED_SOFT_LOCK =
  "AI drafting is unavailable while this account has limited access."

export const CAMPAIGN_AI_PREPARE_BLOCKED_NO_CREDITS =
  "No AI credits remaining. Prepare with AI is unavailable."

export const CAMPAIGN_AI_PREPARE_BLOCKED_BALANCES =
  "Messaging usage is unavailable. Try again before using AI."

/** Billing balances (+ plan) payload Campaigns maps for overview + Channel. */
export type CampaignBillingBalancesPayload = {
  email: {
    used: number
    allowance: number
    remaining: number
    refreshLabel: string
  }
  sms: {
    total: number
    reserved: number
    available: number
  }
  plan: {
    name: string
    locationCount: number
    billingLine: string
  }
  ai: {
    available: number
  }
  softLocked: boolean
}

export type CampaignMessagingUsageCutover = "fixtures" | "live"

export type CampaignMessagingUsageResolution =
  | {
      status: "ready"
      source: CampaignMessagingUsageCutover
      fixture: MessagingUsageFixture
      viewModel: OperatorCampaignsMessagingUsageViewModel
      /** null before cutover — display-only AI chrome. */
      aiAvailable: number | null
      softLocked: boolean
    }
  | {
      status: "load-failed"
      source: "live"
      errorMessage: string
    }

export type CampaignAiPrepareGate = {
  allowed: boolean
  blockReason: string | null
}

export type ConsumeDirectAiInput = {
  locationId: number
  units: 1
}

export function mapBillingBalancesToMessagingFixture(
  balances: CampaignBillingBalancesPayload
): MessagingUsageFixture {
  return {
    email: {
      used: balances.email.used,
      allowance: balances.email.allowance,
      remaining: balances.email.remaining,
      refreshLabel: balances.email.refreshLabel,
    },
    sms: {
      total: balances.sms.total,
      reserved: balances.sms.reserved,
      available: balances.sms.available,
    },
    plan: {
      name: balances.plan.name,
      locationCount: balances.plan.locationCount,
      billingLine: balances.plan.billingLine,
    },
  }
}

export function resolveCampaignMessagingUsage(
  input:
    | { cutover: "fixtures" }
    | { cutover: "live"; balances: CampaignBillingBalancesPayload }
    | { cutover: "live"; failed: true }
): CampaignMessagingUsageResolution {
  if (input.cutover === "fixtures") {
    return {
      status: "ready",
      source: "fixtures",
      fixture: MESSAGING_USAGE_FIXTURE,
      viewModel: messagingUsageViewModelFromFixture(),
      aiAvailable: null,
      softLocked: false,
    }
  }

  if (input.cutover === "live" && "failed" in input && input.failed) {
    return {
      status: "load-failed",
      source: "live",
      errorMessage: CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
    }
  }

  if (input.cutover !== "live" || !("balances" in input)) {
    return {
      status: "load-failed",
      source: "live",
      errorMessage: CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR,
    }
  }

  const balances = input.balances
  const fixture = mapBillingBalancesToMessagingFixture(balances)
  return {
    status: "ready",
    source: "live",
    fixture,
    viewModel: messagingUsageViewModelFromFixture(fixture),
    aiAvailable: balances.ai.available,
    softLocked: balances.softLocked,
  }
}

export function resolveCampaignAiPrepareGate(input: {
  cutover: CampaignMessagingUsageCutover
  softLocked: boolean
  aiAvailable: number | null
  /** After live cutover, balances must be ready before Prepare. */
  balancesStatus?: "ready" | "load-failed"
}): CampaignAiPrepareGate {
  if (input.cutover === "fixtures") {
    return { allowed: true, blockReason: null }
  }

  if (input.balancesStatus === "load-failed") {
    return {
      allowed: false,
      blockReason: CAMPAIGN_AI_PREPARE_BLOCKED_BALANCES,
    }
  }

  if (input.softLocked) {
    return {
      allowed: false,
      blockReason: CAMPAIGN_AI_PREPARE_BLOCKED_SOFT_LOCK,
    }
  }

  if (input.aiAvailable != null && input.aiAvailable <= 0) {
    return {
      allowed: false,
      blockReason: CAMPAIGN_AI_PREPARE_BLOCKED_NO_CREDITS,
    }
  }

  return { allowed: true, blockReason: null }
}

export async function maybeConsumeDirectAiOnUsableDraft(input: {
  cutover: CampaignMessagingUsageCutover
  usableSuccess: boolean
  locationId: number
  consumeDirectAi?: (body: ConsumeDirectAiInput) => Promise<void>
}): Promise<"debited" | "skipped"> {
  if (
    input.cutover !== "live"
    || !input.usableSuccess
    || input.consumeDirectAi == null
  ) {
    return "skipped"
  }

  await input.consumeDirectAi({
    locationId: input.locationId,
    units: 1,
  })
  return "debited"
}
