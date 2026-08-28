import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

export type LockAlertTitle = "Soft lock" | "Dormant"

export type LockAlertPresentation = {
  title: LockAlertTitle
  body: string
  buttonLabel: string | null
  buttonHref: string | null
}

export type LockRestorationCause = "pilot" | "dunning"

const LOCK_ALERT_BODIES: Record<
  LockAlertTitle,
  Record<LockRestorationCause, string>
> = {
  "Soft lock": {
    pilot:
      "Your Pilot period has ended. Paid actions are paused. Existing Feedback links stay live.",
    dunning:
      "Payment failed. Paid actions are paused. Existing Feedback links stay live.",
  },
  Dormant: {
    pilot: "Your account is Dormant. Guest QR links show as unavailable.",
    dunning:
      "Payment failed. Your account is Dormant. Guest QR links show as unavailable.",
  },
}

export function isAccountLockedBillingStatus(billingStatus: string): boolean {
  return billingStatus === "Soft lock" || billingStatus === "Dormant"
}

export function resolveLockRestorationCause(options: {
  billingStatus: string
  subscriptionPlan: string
  isPilot?: boolean
}): LockRestorationCause | null {
  if (!isAccountLockedBillingStatus(options.billingStatus)) {
    return null
  }
  if (options.isPilot === true || options.subscriptionPlan === "Pilot") {
    return "pilot"
  }
  return "dunning"
}

/**
 * Dashboard-shell Lock Alert for Soft lock / Dormant.
 * Restoration Button follows billing-credits Manage writes (ticket 11 + 12).
 */
export function resolveLockAlertPresentation(options: {
  billingStatus: string
  subscriptionPlan: string
  isPilot?: boolean
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
}): LockAlertPresentation | null {
  const title =
    options.billingStatus === "Soft lock" || options.billingStatus === "Dormant"
      ? (options.billingStatus as LockAlertTitle)
      : null
  if (title == null) {
    return null
  }

  const cause = resolveLockRestorationCause(options)
  if (cause == null) {
    return null
  }

  const body = LOCK_ALERT_BODIES[title][cause]
  const button = resolveLockAlertButton({
    cause,
    accessLevel: options.accessLevel,
    permissionRole: options.permissionRole,
    mode: options.mode,
    locationId: options.locationId,
  })

  return {
    title,
    body,
    buttonLabel: button.label,
    buttonHref: button.href,
  }
}

function resolveLockAlertButton(options: {
  cause: LockRestorationCause
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
}): { label: string | null; href: string | null } {
  if (options.accessLevel !== "manage") {
    return { label: null, href: null }
  }

  const isOwner = options.permissionRole === "Owner"
  const isBillingAdminOrAdminManage =
    options.permissionRole === "Billing Admin"
    || options.permissionRole === "Admin"

  if (options.cause === "pilot") {
    if (!isOwner) {
      return { label: null, href: null }
    }
    return {
      label: "Choose a plan",
      href: `${operatorDashboardBillingCreditsManagePlanPath(
        options.mode,
        options.locationId
      )}#plan-cards`,
    }
  }

  if (!isOwner && !isBillingAdminOrAdminManage) {
    return { label: null, href: null }
  }

  return {
    label: "Update payment method",
    href: `${operatorDashboardBillingCreditsPath(options.mode, options.locationId, {
      tab: "payment-invoices",
    })}#update-payment-method`,
  }
}
