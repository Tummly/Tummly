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
  showViewUsage: boolean
  buyLabel: string
  primaryActionLabel: string
}

export type CreditsUsageTableRowViewModel = {
  channelLabel: string
  usedThisCycle: string
  includedThisPeriod: string
  extraUsed: string
  estimatedCharge: string
}

const CHANNEL_LABELS: Record<CreditChannelId, string> = {
  sms: "SMS credits",
  email: "Email credits",
  ai: "AI credits",
}

/** Billing usage card titles — Figma 5746:96470. */
const BILLING_CARD_TITLES: Record<CreditChannelId, string> = {
  sms: "SMS credits",
  email: "Email sends",
  ai: "AI credits",
}

const TABLE_CHANNEL_LABELS: Record<CreditChannelId, string> = {
  sms: "SMS sent",
  email: "Email sent",
  ai: "AI credits used",
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
  // Fill tracks usage (matches backend UsedShare), not remaining.
  return usedThisCycle / total
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

/** Figma 5746:96470 card status line under the channel title. */
export function creditChannelCardHeadline(
  record: CreditChannelUsageRecord
): string {
  if (record.combinedRemaining <= 0) {
    return `No ${creditChannelLabel(record.channel)} remaining.`
  }
  if (record.channel === "email") {
    return `${formatCreditCount(record.usedThisCycle)} of ${formatCreditCount(record.includedThisPeriod)} used`
  }
  return `${formatCreditCount(record.combinedRemaining)} of ${formatCreditCount(record.includedThisPeriod)} remaining`
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
  channel: CreditChannelId
}): { showBuy: boolean; showChangePlan: boolean; showViewUsage: boolean } {
  const showViewUsage = options.channel === "email"

  if (options.accessLevel !== "manage") {
    return { showBuy: false, showChangePlan: false, showViewUsage }
  }

  const isOwner = options.permissionRole === "Owner"
  const canBuyTopUp =
    isOwner
    || options.permissionRole === "Billing Admin"
    || options.permissionRole === "Admin"

  if (options.channel === "email") {
    return {
      showBuy: false,
      showChangePlan: options.isDepleted && isOwner,
      showViewUsage: true,
    }
  }

  if (options.isDepleted) {
    return {
      showBuy: canBuyTopUp && !options.isPilot,
      showChangePlan: isOwner,
      showViewUsage: false,
    }
  }

  return {
    showBuy: canBuyTopUp && !options.isPilot,
    showChangePlan: false,
    showViewUsage: false,
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
    channel: record.channel,
  })
  const title = BILLING_CARD_TITLES[record.channel]
  const buyLabel =
    record.channel === "email"
      ? "Buy Email credits"
      : `Buy ${title}`
  const primaryActionLabel = actions.showViewUsage
    ? "View usage"
    : buyLabel

  return {
    channel: record.channel,
    title,
    headline: creditChannelCardHeadline(record),
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
      Math.max(
        record.includedThisPeriod,
        record.combinedRemaining + record.usedThisCycle
      )
    ),
    isDepleted,
    showBuy: actions.showBuy,
    showChangePlan: actions.showChangePlan,
    showViewUsage: actions.showViewUsage,
    buyLabel,
    primaryActionLabel,
  }
}

export function buildCreditsUsageTableRows(
  channels: CreditChannelUsageRecord[]
): CreditsUsageTableRowViewModel[] {
  return channels.map((record) => ({
    channelLabel: TABLE_CHANNEL_LABELS[record.channel],
    usedThisCycle: formatCreditCount(record.usedThisCycle),
    includedThisPeriod: formatCreditCount(record.includedThisPeriod),
    // Extra used = one-time purchased credits still applied (PurchasedRemaining).
    extraUsed: formatCreditCount(record.purchasedRemaining),
    // Backend has no estimated overage/top-up charge on the usage DTO yet.
    estimatedCharge: "—",
  }))
}
