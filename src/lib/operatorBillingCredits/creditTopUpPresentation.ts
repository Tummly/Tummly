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

const VAT_RATE = 0.2

export type CreditTopUpPackChipViewModel = {
  quantity: number
  label: string
  selected: boolean
}

export type CreditTopUpCardViewModel = {
  channel: CreditChannelId
  title: string
  remainingHeadline: string
  packs: CreditTopUpPackChipViewModel[]
  selectedNetLabel: string | null
  buyLabel: string
  buyDisabled: boolean
  chipsDisabled: boolean
  showPilotNotice: boolean
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

export function grossTopUpPounds(netPounds: number): number {
  return Math.round(netPounds * (1 + VAT_RATE) * 100) / 100
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
}): CreditTopUpCardViewModel[] {
  const channelOrder: CreditChannelId[] = ["sms", "ai", "email"]
  const visibility = {
    subscriptionPlan: options.subscriptionPlan,
    allowSms5000TopUp: options.allowSms5000TopUp,
  }

  return channelOrder.map((channel) => {
    const usage = options.channels.find((row) => row.channel === channel)
    const remaining = usage?.combinedRemaining ?? 0
    const selectedQuantity = options.selectedPackByChannel[channel]
    const packs = visibleTopUpPacksForChannel(channel, visibility)
    const selectedPack =
      selectedQuantity != null
        ? packs.find((pack) => pack.quantity === selectedQuantity)
        : undefined

    const chipsDisabled = options.isPilot || !options.canBuy
    const buyDisabled =
      chipsDisabled || selectedPack == null

    return {
      channel,
      title: creditChannelLabel(channel),
      remainingHeadline: `${formatCreditCount(remaining)} remaining`,
      packs: packs.map((pack) => ({
        quantity: pack.quantity,
        label: `${formatCreditCount(pack.quantity)} credits`,
        selected: selectedQuantity === pack.quantity,
      })),
      selectedNetLabel:
        selectedPack != null
          ? `${formatTopUpPounds(selectedPack.netPounds)} + VAT`
          : null,
      buyLabel: `Buy ${creditChannelLabel(channel)}`,
      buyDisabled,
      chipsDisabled,
      showPilotNotice: options.isPilot,
    }
  })
}

export function buildCreditTopUpConfirmCopy(options: {
  channelLabel: string
  quantity: number
  netLabel: string
  grossLabel: string
}): { title: string; body: string; primaryLabel: string } {
  return {
    title: "Confirm credit top-up",
    body: `${options.channelLabel} · ${formatCreditCount(options.quantity)} credits · ${options.netLabel} + VAT · ${options.grossLabel} total incl. VAT`,
    primaryLabel: "Continue to payment",
  }
}
