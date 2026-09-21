import type { ManagePlanId } from "@/lib/operatorBillingCredits/managePlanPresentation"
import {
  creditChannelLabel,
  formatCreditCount,
  type CreditChannelId,
  type CreditChannelUsageRecord,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"

export type CreditTopUpPack = {
  channel: CreditChannelId
  quantity: number
  netPounds: number
}

export const CREDIT_TOP_UP_PACKS: readonly CreditTopUpPack[] = [
  { channel: "sms", quantity: 100, netPounds: 12 },
  { channel: "sms", quantity: 500, netPounds: 55 },
  { channel: "sms", quantity: 1000, netPounds: 100 },
  { channel: "sms", quantity: 5000, netPounds: 450 },
  { channel: "ai", quantity: 100, netPounds: 5 },
  { channel: "ai", quantity: 500, netPounds: 15 },
  { channel: "ai", quantity: 2000, netPounds: 39 },
  { channel: "email", quantity: 5000, netPounds: 10 },
  { channel: "email", quantity: 20000, netPounds: 30 },
  { channel: "email", quantity: 50000, netPounds: 60 },
] as const

export type CreditTopUpPackChipViewModel = {
  quantity: number
  label: string
  selected: boolean
}

export type CreditTopUpCardViewModel = {
  channel: CreditChannelId
  title: string
  remainingHeadline: string
  detailLine: string
  packs: CreditTopUpPackChipViewModel[]
  selectedNetLabel: string | null
  buyLabel: string
  buyDisabled: boolean
  chipsDisabled: boolean
}

export type CreditTopUpConfirmViewModel = {
  open: boolean
  title: string
  body: string
  primaryLabel: string
  busy: boolean
  channel: CreditChannelId
  quantity: number
}

export function formatTopUpPounds(amount: number): string {
  const formatted = Number.isInteger(amount)
    ? amount.toString()
    : amount.toFixed(2).replace(/\.?0+$/, "")
  return `£${formatted}`
}

/** Derive fractional VAT rate from catalog basis points; missing → 0 (launch OFF). */
export function vatRateFromBps(vatRateBps: number | null | undefined): number {
  return (vatRateBps ?? 0) / 10_000
}

export function grossTopUpPounds(
  netPounds: number,
  vatRateBps: number | null | undefined = 0
): number {
  const rate = vatRateFromBps(vatRateBps)
  return Math.round(netPounds * (1 + rate) * 100) / 100
}

export function isSms5000TopUpAllowed(options: {
  subscriptionPlan: string
  allowSms5000TopUp: boolean
}): boolean {
  if (options.subscriptionPlan === "Group") {
    return true
  }
  return options.allowSms5000TopUp
}

export function isTopUpPackVisible(
  pack: CreditTopUpPack,
  options: {
    subscriptionPlan: string
    allowSms5000TopUp: boolean
  }
): boolean {
  if (pack.channel === "sms" && pack.quantity === 5000) {
    return isSms5000TopUpAllowed(options)
  }
  return true
}

export function visibleTopUpPacksForChannel(
  channel: CreditChannelId,
  options: {
    subscriptionPlan: string
    allowSms5000TopUp: boolean
  }
): CreditTopUpPack[] {
  return CREDIT_TOP_UP_PACKS.filter(
    (pack) =>
      pack.channel === channel
      && isTopUpPackVisible(pack, options)
  )
}

export function findTopUpPack(
  channel: CreditChannelId,
  quantity: number
): CreditTopUpPack | undefined {
  return CREDIT_TOP_UP_PACKS.find(
    (pack) => pack.channel === channel && pack.quantity === quantity
  )
}

export function buildCreditTopUpCards(options: {
  channels: CreditChannelUsageRecord[]
  subscriptionPlan: ManagePlanId | string
  allowSms5000TopUp: boolean
  isPilot: boolean
  canBuy: boolean
  selectedPackByChannel: Partial<Record<CreditChannelId, number>>
  focusedChannel: CreditChannelId | null
  /** Catalog / API rate in basis points; 0 omits “+ VAT”. */
  vatRateBps?: number
}): CreditTopUpCardViewModel[] {
  // Pilot cannot buy top-ups — hide purchase cards entirely (no disabled Buy UI).
  if (options.isPilot) {
    return []
  }

  const channelOrder: CreditChannelId[] = ["sms", "ai", "email"]
  const visibility = {
    subscriptionPlan: options.subscriptionPlan,
    allowSms5000TopUp: options.allowSms5000TopUp,
  }
  const vatRateBps = options.vatRateBps ?? 0
  const netLabelSuffix = vatRateBps > 0 ? " + VAT" : ""

  return channelOrder.map((channel) => {
    const usage = options.channels.find((row) => row.channel === channel)
    const remaining = usage?.combinedRemaining ?? 0
    const selectedQuantity = options.selectedPackByChannel[channel]
    const packs = visibleTopUpPacksForChannel(channel, visibility)
    const selectedPack =
      selectedQuantity != null
        ? packs.find((pack) => pack.quantity === selectedQuantity)
        : undefined

    const chipsDisabled = !options.canBuy
    const buyDisabled = chipsDisabled || selectedPack == null
    const unit = channel === "email" ? "sends" : "credits"

    return {
      channel,
      title: creditChannelLabel(channel),
      remainingHeadline: `${formatCreditCount(remaining)} ${unit}`,
      detailLine: creditTopUpDetailLine(channel),
      packs: packs.map((pack) => ({
        quantity: pack.quantity,
        label: `${formatCreditCount(pack.quantity)} ${unit}`,
        selected: selectedQuantity === pack.quantity,
      })),
      selectedNetLabel:
        selectedPack != null
          ? `${formatTopUpPounds(selectedPack.netPounds)}${netLabelSuffix}`
          : null,
      buyLabel: `Buy ${creditChannelLabel(channel)}`,
      buyDisabled,
      chipsDisabled,
    }
  })
}

function creditTopUpDetailLine(channel: CreditChannelId): string {
  switch (channel) {
    case "sms":
      return "Use SMS credits for eligible guest campaigns, reminders and offer messages."
    case "ai":
      return "Use AI credits for operator-triggered briefs, drafts and regenerations."
    case "email":
      return "Use email sends for eligible guest campaigns and recovery messages."
  }
}

export function buildCreditTopUpConfirmCopy(options: {
  channelLabel: string
  quantity: number
  netLabel: string
  grossLabel: string
  vatRateBps?: number
}): { title: string; body: string; primaryLabel: string } {
  const vatRateBps = options.vatRateBps ?? 0
  const priceSegment =
    vatRateBps > 0
      ? `${options.netLabel} + VAT · ${options.grossLabel} total incl. VAT`
      : `${options.netLabel} · ${options.grossLabel} total`
  return {
    title: "Confirm credit top-up",
    body: `${options.channelLabel} · ${formatCreditCount(options.quantity)} credits · ${priceSegment}`,
    primaryLabel: "Continue to payment",
  }
}
