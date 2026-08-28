/** Home Needs attention credit pool rows (ticket 27). */

import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  creditChannelLabel,
  type CreditChannelId,
  type CreditChannelUsageRecord,
  type CreditsUsageSnapshot,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import type {
  HomeNeedsAttentionCta,
  HomeNeedsAttentionCreditFact,
} from "@/lib/operatorHome/buildHomeNeedsAttention"

export const HOME_NEEDS_ATTENTION_ACCOUNT_WIDE_SCOPE = "Account-wide"

export const CREDIT_CHANNEL_ORDER: readonly CreditChannelId[] = [
  "sms",
  "email",
  "ai",
]

export type HomeNeedsAttentionCreditThresholdBand = 80 | 90 | 100

export function creditChannelUsedShare(
  combinedRemaining: number,
  usedThisCycle: number
): number {
  if (combinedRemaining <= 0) {
    return 1
  }
  const total = combinedRemaining + usedThisCycle
  if (total <= 0) {
    return 0
  }
  return usedThisCycle / total
}

export function creditChannelQualifiesForHomeNeedsAttention(
  combinedRemaining: number,
  usedThisCycle: number
): boolean {
  return creditChannelUsedShare(combinedRemaining, usedThisCycle) >= 0.8
}

export function homeNeedsAttentionCreditThresholdBand(
  combinedRemaining: number,
  usedThisCycle: number
): HomeNeedsAttentionCreditThresholdBand {
  if (combinedRemaining <= 0) {
    return 100
  }
  const usedShare = creditChannelUsedShare(combinedRemaining, usedThisCycle)
  if (usedShare >= 0.9) {
    return 90
  }
  return 80
}

export function homeNeedsAttentionCreditTitle(
  channel: CreditChannelId,
  band: HomeNeedsAttentionCreditThresholdBand
): string {
  const label = creditChannelLabel(channel)
  if (band === 100) {
    return `No ${label} remaining`
  }
  return `${label} at ${band}% used`
}

export function homeNeedsAttentionCreditBody(
  channel: CreditChannelId,
  band: HomeNeedsAttentionCreditThresholdBand,
  workspaceName: string
): string {
  const label = creditChannelLabel(channel)
  const scope = workspaceName.trim() === "" ? "Your account" : workspaceName
  if (band === 100) {
    return `${scope} has no ${label} remaining this period.`
  }
  return `${scope} has used at least ${band}% of its ${label} this period.`
}

export function homeNeedsAttentionCreditCtas(options: {
  channel: CreditChannelId
  band: HomeNeedsAttentionCreditThresholdBand
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  isPilot: boolean
}): readonly HomeNeedsAttentionCta[] {
  if (options.band === 80 || options.band === 90) {
    return [{ kind: "view-usage", label: "View usage" }]
  }

  const label = creditChannelLabel(options.channel)
  const isOwner = options.permissionRole === "Owner"
  const canBuyTopUp =
    options.accessLevel === "manage"
    && (
      isOwner
      || options.permissionRole === "Billing Admin"
      || options.permissionRole === "Admin"
    )

  if (options.isPilot) {
    if (isOwner && options.accessLevel === "manage") {
      return [{ kind: "change-plan", label: "Change plan" }]
    }
    return [{ kind: "view-usage", label: "View usage" }]
  }

  if (canBuyTopUp) {
    return [{ kind: "buy-channel-credits", label: `Buy ${label}` }]
  }

  return [{ kind: "view-usage", label: "View usage" }]
}

function mapCreditChannelFact(input: {
  record: CreditChannelUsageRecord
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  isPilot: boolean
  workspaceName: string
}): HomeNeedsAttentionCreditFact | null {
  if (
    !creditChannelQualifiesForHomeNeedsAttention(
      input.record.combinedRemaining,
      input.record.usedThisCycle
    )
  ) {
    return null
  }

  const band = homeNeedsAttentionCreditThresholdBand(
    input.record.combinedRemaining,
    input.record.usedThisCycle
  )

  return {
    channel: input.record.channel,
    band,
    title: homeNeedsAttentionCreditTitle(input.record.channel, band),
    body: homeNeedsAttentionCreditBody(
      input.record.channel,
      band,
      input.workspaceName
    ),
    ctas: homeNeedsAttentionCreditCtas({
      channel: input.record.channel,
      band,
      accessLevel: input.accessLevel,
      permissionRole: input.permissionRole,
      isPilot: input.isPilot,
    }),
  }
}

export function mapHomeNeedsAttentionCreditFacts(input: {
  usage: CreditsUsageSnapshot
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  workspaceName: string
}): HomeNeedsAttentionCreditFact[] {
  const byChannel = new Map(
    input.usage.channels.map((record) => [record.channel, record] as const)
  )

  return CREDIT_CHANNEL_ORDER.flatMap((channel) => {
    const record = byChannel.get(channel)
    if (record == null) {
      return []
    }
    const mapped = mapCreditChannelFact({
      record,
      accessLevel: input.accessLevel,
      permissionRole: input.permissionRole,
      isPilot: input.usage.isPilot,
      workspaceName: input.workspaceName,
    })
    return mapped == null ? [] : [mapped]
  })
}
