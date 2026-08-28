import { getBillingCreditsUsage } from "@/api/billingCreditsApi"
import type { CampaignBillingBalancesPayload } from "@/lib/operatorCampaigns/campaignMessagingBalances"
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

/** Map Credits & usage snapshot (+ optional soft-lock defaults) for Campaigns. */
export function mapCreditsUsageToCampaignBillingBalances(
  usage: CreditsUsageSnapshot,
  lock: {
    softLocked?: boolean
    lockCause?: CampaignBillingBalancesPayload["lockCause"]
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
  }
}

export const loadCampaignMessagingBalances: LoadCampaignMessagingBalances =
  async () => {
    const usage = await getBillingCreditsUsage()
    // Soft lock / Dormant fields are not on usage yet — default unlocked.
    return mapCreditsUsageToCampaignBillingBalances(usage)
  }
