import {
  getBillingCreditsPage,
  getBillingCreditsUsage,
} from "@/api/billingCreditsApi"
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type {
  CampaignBillingBalancesPayload,
  CampaignMessagingChromeAccess,
} from "@/lib/operatorCampaigns/campaignMessagingBalances"
import type { CampaignsMessagingLockCause } from "@/lib/operatorCampaigns/campaignsMessagingCreditChrome"
import type {
  CreditChannelId,
  CreditChannelUsageRecord,
  CreditsUsageSnapshot,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"

/**
 * Shared Billing balances adapter for overview Messaging usage and wizard
 * Channel meters. Pass the same value from CampaignsPageModuleProvider and
 * CampaignsPage wizard so cutover stays in lockstep.
 */
export type LoadCampaignMessagingBalances =
  () => Promise<CampaignBillingBalancesPayload>

function emptyChannelRecord(
  _channel: Exclude<CreditChannelId, "ai">
): Omit<CreditChannelUsageRecord, "channel"> {
  return {
    combinedRemaining: 0,
    usedThisCycle: 0,
    includedThisPeriod: 0,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  }
}

function channelFromUsage(
  usage: CreditsUsageSnapshot,
  channel: "email" | "sms"
): Omit<CreditChannelUsageRecord, "channel"> {
  const record = usage.channels.find((item) => item.channel === channel)
  if (record == null) {
    return emptyChannelRecord(channel)
  }
  return {
    combinedRemaining: record.combinedRemaining,
    usedThisCycle: record.usedThisCycle,
    includedThisPeriod: record.includedThisPeriod,
    purchasedRemaining: record.purchasedRemaining,
    purchasedExpiryLabel: record.purchasedExpiryLabel,
  }
}

export function resolveCampaignMessagingLockFromBillingStatus(input: {
  billingStatus: string
  isPilot: boolean
}): {
  softLocked: boolean
  lockCause: CampaignsMessagingLockCause | null
} {
  const status = input.billingStatus.trim()
  const softLocked = status === "Soft lock" || status === "Dormant"
  if (!softLocked) {
    return { softLocked: false, lockCause: null }
  }
  return {
    softLocked: true,
    lockCause: input.isPilot ? "unpaid-pilot" : "dunning",
  }
}

export function resolveCampaignMessagingChromeAccessFromBillingPage(input: {
  actorPermissionRole: string
  actorCanManage: boolean
}): CampaignMessagingChromeAccess {
  const accessLevel: BillingCreditsAccessLevel = input.actorCanManage
    ? "manage"
    : "view"
  return {
    accessLevel,
    permissionRole: input.actorPermissionRole,
  }
}

/** Map Credits & usage snapshot (+ optional soft-lock / chrome) for Campaigns. */
export function mapCreditsUsageToCampaignBillingBalances(
  usage: CreditsUsageSnapshot,
  lock: {
    softLocked?: boolean
    lockCause?: CampaignBillingBalancesPayload["lockCause"]
    chromeAccess?: CampaignMessagingChromeAccess
  } = {}
): CampaignBillingBalancesPayload {
  const ai = usage.channels.find((item) => item.channel === "ai")
  return {
    email: channelFromUsage(usage, "email"),
    sms: channelFromUsage(usage, "sms"),
    ai: {
      available: ai?.combinedRemaining ?? 0,
    },
    isPilot: usage.isPilot,
    softLocked: lock.softLocked ?? false,
    lockCause: lock.lockCause ?? null,
    chromeAccess: lock.chromeAccess,
  }
}

export const loadCampaignMessagingBalances: LoadCampaignMessagingBalances =
  async () => {
    const [usage, page] = await Promise.all([
      getBillingCreditsUsage(),
      getBillingCreditsPage().catch(() => null),
    ])

    if (page == null) {
      return mapCreditsUsageToCampaignBillingBalances(usage)
    }

    const lock = resolveCampaignMessagingLockFromBillingStatus({
      billingStatus: page.planSubscription.billingStatus,
      isPilot: page.planSubscription.isPilot || usage.isPilot,
    })
    const chromeAccess = resolveCampaignMessagingChromeAccessFromBillingPage({
      actorPermissionRole: page.actorPermissionRole,
      actorCanManage: page.actorCanManage,
    })

    return mapCreditsUsageToCampaignBillingBalances(usage, {
      ...lock,
      chromeAccess,
    })
  }
