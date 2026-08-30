import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"

export type CreditChannelId = "sms" | "email" | "ai"

export type CreditChannelUsageRecord = {
  channel: CreditChannelId
  combinedRemaining: number
  usedThisCycle: number
  includedThisPeriod: number
  purchasedRemaining: number
  purchasedExpiryLabel: string | null
}

export type CreditsUsageSnapshot = {
  periodLabel: string
  starterKitState: string
  isPilot: boolean
  channels: CreditChannelUsageRecord[]
}

export type CreditChannelCardViewModel = {
  channel: CreditChannelId
  title: string
  headline: string
  subline: string
  purchasedLine: string | null
  fillRatio: number
  meterMaxLabel: string
  isDepleted: boolean
  showBuy: boolean
  showChangePlan: boolean
  buyLabel: string
}

export type CreditsUsageTableRowViewModel = {
  channelLabel: string
  usedThisCycle: string
  includedThisPeriod: string
  purchasedRemaining: string
}

const CHANNEL_LABELS: Record<CreditChannelId, string> = {
  sms: "SMS credits",
  email: "Email credits",
  ai: "AI credits",
}

export function creditChannelLabel(channel: CreditChannelId): string {
  return CHANNEL_LABELS[channel]
}

export function formatCreditCount(count: number): string {
  return count.toLocaleString("en-GB")
}

export function creditChannelFillRatio(
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
  return combinedRemaining / total
}

export function creditChannelHeadline(
  combinedRemaining: number,
  channel: CreditChannelId
): string {
  if (combinedRemaining <= 0) {
    return `No ${creditChannelLabel(channel)} remaining.`
  }
  return `${formatCreditCount(combinedRemaining)} remaining`
}

export function creditChannelSubline(
  usedThisCycle: number,
  includedThisPeriod: number
): string {
  return `${formatCreditCount(usedThisCycle)} of ${formatCreditCount(includedThisPeriod)} included used`
}

export function creditChannelPurchasedLine(
  purchasedRemaining: number,
  purchasedExpiryLabel: string | null
): string | null {
  if (purchasedRemaining <= 0) {
    return null
  }
  const expiry = purchasedExpiryLabel ?? "—"
  return `${formatCreditCount(purchasedRemaining)} purchased remaining · use by ${expiry}`
}

export function billingCreditsChannelCardActions(options: {
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  isPilot: boolean
  isDepleted: boolean
}): { showBuy: boolean; showChangePlan: boolean } {
  if (options.accessLevel !== "manage") {
    return { showBuy: false, showChangePlan: false }
  }

  const isOwner = options.permissionRole === "Owner"
  const canBuyTopUp =
    isOwner
    || options.permissionRole === "Billing Admin"
    || options.permissionRole === "Admin"

  if (options.isDepleted) {
    return {
      showBuy: canBuyTopUp && !options.isPilot,
      showChangePlan: isOwner,
    }
  }

  return {
    showBuy: canBuyTopUp && !options.isPilot,
    showChangePlan: false,
  }
}

export function buildCreditChannelCardViewModel(
  record: CreditChannelUsageRecord,
  options: {
    accessLevel: BillingCreditsAccessLevel
    permissionRole: string
    isPilot: boolean
  }
): CreditChannelCardViewModel {
  const isDepleted = record.combinedRemaining <= 0
  const actions = billingCreditsChannelCardActions({
    accessLevel: options.accessLevel,
    permissionRole: options.permissionRole,
    isPilot: options.isPilot,
    isDepleted,
  })

  return {
    channel: record.channel,
    title: creditChannelLabel(record.channel),
    headline: creditChannelHeadline(record.combinedRemaining, record.channel),
    subline: creditChannelSubline(
      record.usedThisCycle,
      record.includedThisPeriod
    ),
    purchasedLine: creditChannelPurchasedLine(
      record.purchasedRemaining,
      record.purchasedExpiryLabel
    ),
    fillRatio: creditChannelFillRatio(
      record.combinedRemaining,
      record.usedThisCycle
    ),
    meterMaxLabel: formatCreditCount(
      record.combinedRemaining + record.usedThisCycle
    ),
    isDepleted,
    showBuy: actions.showBuy,
    showChangePlan: actions.showChangePlan,
    buyLabel: `Buy ${creditChannelLabel(record.channel)}`,
  }
}

export function buildCreditsUsageTableRows(
  channels: CreditChannelUsageRecord[]
): CreditsUsageTableRowViewModel[] {
  return channels.map((record) => ({
    channelLabel: creditChannelLabel(record.channel),
    usedThisCycle: formatCreditCount(record.usedThisCycle),
    includedThisPeriod: formatCreditCount(record.includedThisPeriod),
    purchasedRemaining: formatCreditCount(record.purchasedRemaining),
  }))
}
