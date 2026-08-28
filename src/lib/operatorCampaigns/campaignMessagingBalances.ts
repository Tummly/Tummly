/**
 * Campaigns Messaging usage + AI debit cutover against Billing balances.
 * Prefer live Billing payload when available; fixtures only pre-cutover.
 * Ticket 23 / 25 — no Campaigns-owned ledger.
 */

import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  buildCampaignsMessagingUsageViewModel,
  CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
  type CampaignsMessagingBalancesFixture,
  type CampaignsMessagingLockCause,
  type CampaignsMessagingUsageViewModel,
} from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"

export const CAMPAIGN_MESSAGING_BALANCES_LOAD_ERROR =
  "Could not load messaging usage. Please try again."

export const CAMPAIGN_AI_PREPARE_BLOCKED_SOFT_LOCK =
  "AI drafting is unavailable while this account has limited access."

export const CAMPAIGN_AI_PREPARE_BLOCKED_NO_CREDITS =
  "No AI credits remaining"

export const CAMPAIGN_AI_PREPARE_BLOCKED_BALANCES =
  "Messaging usage is unavailable. Try again before using AI."

/** Billing usage (+ lock) payload Campaigns maps for overview + wizard. */
export type CampaignBillingBalancesPayload = {
  email: CampaignsMessagingBalancesFixture["email"]
  sms: CampaignsMessagingBalancesFixture["sms"]
  ai: {
    available: number
  }
  isPilot: boolean
  softLocked: boolean
  lockCause: CampaignsMessagingLockCause | null
  /**
   * When present, drives Buy / Change plan / restoration CTAs (lock 11).
   * Prefer this over adapter `messagingChromeAccess` on live loads.
   */
  chromeAccess?: CampaignMessagingChromeAccess
}

export type CampaignMessagingUsageCutover = "fixtures" | "live"

export type CampaignMessagingUsageResolution =
  | {
      status: "ready"
      source: CampaignMessagingUsageCutover
      fixture: CampaignsMessagingBalancesFixture
      viewModel: CampaignsMessagingUsageViewModel
      /** null before cutover — display-only AI chrome. */
      aiAvailable: number | null
      softLocked: boolean
      lockCause: CampaignsMessagingLockCause | null
      isPilot: boolean
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

/**
 * Billing chrome access for Campaigns CTAs (lock 11).
 * Omit / undefined: treat as Owner + manage so Account-owner chrome stays
 * visible until `/auth/me` and Billing page fields are live. Explicit
 * `accessLevel: "none"` or `"view"` still hides write CTAs.
 */
export type CampaignMessagingChromeAccess = {
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
}

export const DEFAULT_CAMPAIGN_MESSAGING_CHROME_ACCESS: CampaignMessagingChromeAccess =
  {
    accessLevel: "manage",
    permissionRole: "Owner",
  }

export function mapBillingBalancesToMessagingFixture(
  balances: CampaignBillingBalancesPayload
): CampaignsMessagingBalancesFixture {
  return {
    email: balances.email,
    sms: balances.sms,
    isPilot: balances.isPilot,
    softLocked: balances.softLocked,
    lockCause: balances.lockCause,
  }
}

export function resolveCampaignMessagingUsage(
  input:
    | {
        cutover: "fixtures"
        access?: CampaignMessagingChromeAccess
      }
    | {
        cutover: "live"
        balances: CampaignBillingBalancesPayload
        access?: CampaignMessagingChromeAccess
      }
    | { cutover: "live"; failed: true }
): CampaignMessagingUsageResolution {
  if (input.cutover === "fixtures") {
    const access =
      input.access ?? DEFAULT_CAMPAIGN_MESSAGING_CHROME_ACCESS
    return {
      status: "ready",
      source: "fixtures",
      fixture: CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
      viewModel: buildCampaignsMessagingUsageViewModel(
        CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
        access
      ),
      aiAvailable: null,
      softLocked: false,
      lockCause: null,
      isPilot: CAMPAIGNS_MESSAGING_BALANCES_FIXTURE.isPilot,
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
  const access =
    balances.chromeAccess
    ?? input.access
    ?? DEFAULT_CAMPAIGN_MESSAGING_CHROME_ACCESS
  const fixture = mapBillingBalancesToMessagingFixture(balances)
  return {
    status: "ready",
    source: "live",
    fixture,
    viewModel: buildCampaignsMessagingUsageViewModel(fixture, access),
    aiAvailable: balances.ai.available,
    softLocked: balances.softLocked,
    lockCause: balances.lockCause,
    isPilot: balances.isPilot,
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
