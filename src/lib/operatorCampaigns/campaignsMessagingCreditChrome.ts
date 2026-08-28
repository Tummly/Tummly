/**
 * Campaigns overview Messaging usage + wizard estimate chrome (ticket 23 / lock 09).
 * Numbers follow Credits & usage combined remaining (ticket 18 / lock 05).
 */

import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  billingCreditsChannelCardActions,
  creditChannelFillRatio,
  creditChannelHeadline,
  creditChannelLabel,
  creditChannelPurchasedLine,
  creditChannelSubline,
  formatCreditCount,
  type CreditChannelId,
  type CreditChannelUsageRecord,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"

export type CampaignsMessagingLockCause = "unpaid-pilot" | "dunning"

export type CampaignsMessagingChannelBalance = CreditChannelUsageRecord

export type CampaignsMessagingBalancesFixture = {
  email: Omit<CampaignsMessagingChannelBalance, "channel">
  sms: Omit<CampaignsMessagingChannelBalance, "channel">
  isPilot: boolean
  softLocked: boolean
  /** Soft lock / Dormant restoration cause — null when unlocked. */
  lockCause: CampaignsMessagingLockCause | null
}

export type CampaignsMessagingChromeActionKind =
  | "view-usage"
  | "buy-sms-credits"
  | "buy-email-credits"
  | "buy-ai-credits"
  | "change-plan"
  | "choose-a-plan"
  | "update-payment-method"

export type CampaignsMessagingChromeAction = {
  kind: CampaignsMessagingChromeActionKind
  label: string
}

export type CampaignsMessagingChannelCardViewModel = {
  channel: "email" | "sms"
  title: string
  headline: string
  subline: string
  purchasedLine: string | null
  fillRatio: number
  meterMaxLabel: string
  isDepleted: boolean
  actions: CampaignsMessagingChromeAction[]
}

export type CampaignsMessagingUsageViewModel = {
  title: string
  subtitle: string
  email: CampaignsMessagingChannelCardViewModel
  sms: CampaignsMessagingChannelCardViewModel
  /** Section-level CTAs (View usage always when billing is visible). */
  sectionActions: CampaignsMessagingChromeAction[]
}

export const CAMPAIGNS_MESSAGING_USAGE_COPY = {
  title: "Messaging usage",
  subtitle:
    "Review the Email credits and SMS credits available to this operator account.",
  viewUsage: "View usage",
  buySmsCredits: "Buy SMS credits",
  buyEmailCredits: "Buy Email credits",
  buyAiCredits: "Buy AI credits",
  changePlan: "Change plan",
  chooseAPlan: "Choose a plan",
  updatePaymentMethod: "Update payment method",
} as const

/** Shared sample figures — overview + wizard until live Billing usage is wired. */
export const CAMPAIGNS_MESSAGING_BALANCES_FIXTURE = {
  email: {
    combinedRemaining: 6760,
    usedThisCycle: 3240,
    includedThisPeriod: 10000,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  sms: {
    combinedRemaining: 300,
    usedThisCycle: 120,
    includedThisPeriod: 420,
    purchasedRemaining: 0,
    purchasedExpiryLabel: null,
  },
  isPilot: false,
  softLocked: false,
  lockCause: null,
} as const satisfies CampaignsMessagingBalancesFixture

export function messagingChannelCombinedRemaining(
  fixture: CampaignsMessagingBalancesFixture,
  channel: "email" | "sms"
): number {
  return fixture[channel].combinedRemaining
}

function overviewChannelActions(input: {
  channel: "email" | "sms"
  isDepleted: boolean
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  isPilot: boolean
}): CampaignsMessagingChromeAction[] {
  if (!input.isDepleted) {
    return []
  }

  const cardActions = billingCreditsChannelCardActions({
    accessLevel: input.accessLevel,
    permissionRole: input.permissionRole,
    isPilot: input.isPilot,
    isDepleted: true,
  })

  const actions: CampaignsMessagingChromeAction[] = []

  if (input.channel === "sms") {
    if (cardActions.showBuy) {
      actions.push({
        kind: "buy-sms-credits",
        label: CAMPAIGNS_MESSAGING_USAGE_COPY.buySmsCredits,
      })
    }
    if (cardActions.showChangePlan) {
      actions.push({
        kind: "change-plan",
        label: CAMPAIGNS_MESSAGING_USAGE_COPY.changePlan,
      })
    }
    if (actions.length === 0 && input.accessLevel !== "none") {
      actions.push({
        kind: "view-usage",
        label: CAMPAIGNS_MESSAGING_USAGE_COPY.viewUsage,
      })
    }
    return actions
  }

  // Email 100%: Change plan + View usage — no Buy Email credits on overview.
  if (cardActions.showChangePlan) {
    actions.push({
      kind: "change-plan",
      label: CAMPAIGNS_MESSAGING_USAGE_COPY.changePlan,
    })
  }
  if (input.accessLevel !== "none") {
    actions.push({
      kind: "view-usage",
      label: CAMPAIGNS_MESSAGING_USAGE_COPY.viewUsage,
    })
  }
  return actions
}

function buildChannelCard(
  channel: "email" | "sms",
  record: Omit<CampaignsMessagingChannelBalance, "channel">,
  options: {
    accessLevel: BillingCreditsAccessLevel
    permissionRole: string
    isPilot: boolean
  }
): CampaignsMessagingChannelCardViewModel {
  const channelId = channel as CreditChannelId
  const isDepleted = record.combinedRemaining <= 0
  return {
    channel,
    title: creditChannelLabel(channelId),
    headline: creditChannelHeadline(record.combinedRemaining, channelId),
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
    actions: overviewChannelActions({
      channel,
      isDepleted,
      accessLevel: options.accessLevel,
      permissionRole: options.permissionRole,
      isPilot: options.isPilot,
    }),
  }
}

export function buildCampaignsMessagingUsageViewModel(
  fixture: CampaignsMessagingBalancesFixture = CAMPAIGNS_MESSAGING_BALANCES_FIXTURE,
  options: {
    accessLevel: BillingCreditsAccessLevel
    permissionRole: string
  } = { accessLevel: "manage", permissionRole: "Owner" }
): CampaignsMessagingUsageViewModel {
  const email = buildChannelCard("email", fixture.email, {
    ...options,
    isPilot: fixture.isPilot,
  })
  const sms = buildChannelCard("sms", fixture.sms, {
    ...options,
    isPilot: fixture.isPilot,
  })

  const sectionActions: CampaignsMessagingChromeAction[] = []
  if (options.accessLevel !== "none") {
    sectionActions.push({
      kind: "view-usage",
      label: CAMPAIGNS_MESSAGING_USAGE_COPY.viewUsage,
    })
  }

  const smsBuy = billingCreditsChannelCardActions({
    accessLevel: options.accessLevel,
    permissionRole: options.permissionRole,
    isPilot: fixture.isPilot,
    isDepleted: sms.isDepleted,
  })
  if (smsBuy.showBuy && !sms.isDepleted) {
    sectionActions.push({
      kind: "buy-sms-credits",
      label: CAMPAIGNS_MESSAGING_USAGE_COPY.buySmsCredits,
    })
  }

  return {
    title: CAMPAIGNS_MESSAGING_USAGE_COPY.title,
    subtitle: CAMPAIGNS_MESSAGING_USAGE_COPY.subtitle,
    email,
    sms,
    sectionActions,
  }
}

export function campaignsMessagingSkippedCount(input: {
  matched: number | null
  channelEligible: number | null
}): number | null {
  if (input.matched == null || input.channelEligible == null) {
    return null
  }
  return Math.max(0, input.matched - input.channelEligible)
}

export function formatCampaignsMessagingSkipped(
  skipped: number | null
): string {
  if (skipped == null) {
    return "—"
  }
  return formatCreditCount(skipped)
}

export function formatCampaignsMessagingAfterSend(input: {
  remaining: number
  estimate: number | null
}): string {
  if (input.estimate == null) {
    return "—"
  }
  const after = input.remaining - input.estimate
  if (after < 0) {
    return `Shortfall ${formatCreditCount(Math.abs(after))}`
  }
  return formatCreditCount(after)
}

export function resolveCampaignsMessagingLockHelper(input: {
  softLocked: boolean
  lockCause: CampaignsMessagingLockCause | null
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
}): CampaignsMessagingChromeAction | null {
  if (!input.softLocked || input.lockCause == null) {
    return null
  }

  const isOwner = input.permissionRole === "Owner"
  const canManage = input.accessLevel === "manage"

  if (input.lockCause === "unpaid-pilot") {
    if (canManage && isOwner) {
      return {
        kind: "choose-a-plan",
        label: CAMPAIGNS_MESSAGING_USAGE_COPY.chooseAPlan,
      }
    }
    return null
  }

  // dunning
  if (
    canManage
    && (
      isOwner
      || input.permissionRole === "Billing Admin"
      || input.permissionRole === "Admin"
    )
  ) {
    return {
      kind: "update-payment-method",
      label: CAMPAIGNS_MESSAGING_USAGE_COPY.updatePaymentMethod,
    }
  }
  return null
}

export function resolveBillingReserveUnavailableCopy(input: {
  /** True when Billing Reserve adapter IsLive is true. */
  billingReserveLive: boolean
}): string {
  if (input.billingReserveLive) {
    return "Could not reserve credits for this campaign. Top up or reduce the audience, then try again."
  }
  return "Billing Reserve is not available yet. Schedule and send stay blocked. You can still Save draft and Send test."
}
