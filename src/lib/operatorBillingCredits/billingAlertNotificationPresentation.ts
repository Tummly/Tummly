import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { CreditChannelId } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"

export type BillingAlertEventKind =
  | "credit-threshold-80-or-90"
  | "credit-threshold-100-paid"
  | "credit-threshold-100-pilot"
  | "payment-failure-dunning"
  | "unpaid-pilot-lock"

export type BillingAlertNotificationCta = {
  label: string | null
  href: string | null
}

export function resolveBillingAlertNotificationCta(options: {
  eventKind: BillingAlertEventKind
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
  channel?: CreditChannelId
}): BillingAlertNotificationCta {
  const {
    eventKind,
    accessLevel,
    permissionRole,
    mode,
    locationId,
    channel,
  } = options

  if (accessLevel === "none") {
    return { label: null, href: null }
  }

  const isOwner = permissionRole === "Owner"
  const canManage = accessLevel === "manage"
  const canView = accessLevel === "view" || canManage

  switch (eventKind) {
    case "credit-threshold-80-or-90":
      if (!canView) {
        return { label: null, href: null }
      }
      return {
        label: "View usage",
        href: operatorDashboardBillingCreditsPath(mode, locationId, {
          tab: "credits-usage",
        }),
      }

    case "credit-threshold-100-paid":
      if (canManage) {
        return {
          label:
            channel == null
              ? "Buy credits"
              : `Buy ${channelLabel(channel)}`,
          href: managePlanHref(mode, locationId, channel),
        }
      }
      if (canView) {
        return {
          label: "View usage",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "credits-usage",
          }),
        }
      }
      return { label: null, href: null }

    case "credit-threshold-100-pilot":
      if (canManage && isOwner) {
        return {
          label: "Change plan",
          href: operatorDashboardBillingCreditsManagePlanPath(mode, locationId),
        }
      }
      if (canView) {
        return {
          label: "View usage",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "credits-usage",
          }),
        }
      }
      return { label: null, href: null }

    case "payment-failure-dunning":
      if (canManage) {
        return {
          label: "Update payment method",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "payment-invoices",
          }),
        }
      }
      if (canView) {
        return {
          label: "Payment & invoices",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "payment-invoices",
          }),
        }
      }
      return { label: null, href: null }

    case "unpaid-pilot-lock":
      if (canManage && isOwner) {
        return {
          label: "Choose a plan",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "plan-subscription",
          }),
        }
      }
      if (canView) {
        return {
          label: "Plan & subscription",
          href: operatorDashboardBillingCreditsPath(mode, locationId, {
            tab: "plan-subscription",
          }),
        }
      }
      return { label: null, href: null }

    default:
      return { label: null, href: null }
  }
}

function channelLabel(channel: CreditChannelId): string {
  switch (channel) {
    case "email":
      return "Email credits"
    case "sms":
      return "SMS credits"
    case "ai":
      return "AI credits"
  }
}

function managePlanHref(
  mode: OperatorDashboardMode,
  locationId: number,
  channel?: CreditChannelId
): string {
  const base = operatorDashboardBillingCreditsManagePlanPath(mode, locationId, {
    section: "credit-top-ups",
  })
  if (channel == null) {
    return base
  }
  const separator = base.includes("?") ? "&" : "?"
  return `${base}${separator}channel=${channel}`
}

export function billingAlertEventKindForCreditThreshold(
  thresholdBand: 80 | 90 | 100,
  isPilot: boolean
): BillingAlertEventKind {
  if (thresholdBand === 80 || thresholdBand === 90) {
    return "credit-threshold-80-or-90"
  }
  return isPilot
    ? "credit-threshold-100-pilot"
    : "credit-threshold-100-paid"
}
