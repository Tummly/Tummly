/**
 * Load recovery credit chrome from Billing usage + plan snapshot.
 * Ticket 24 — omit adapter keeps burn controls unlocked (rollout-safe).
 */

import {
  getBillingCreditsPage,
  getBillingCreditsUsage,
} from "@/api/billingCreditsApi"
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { CreditsUsageSnapshot } from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import {
  billingStatusImpliesPaidActionsLocked,
  restorationCauseFromBilling,
  type RecoveryCreditChromeContext,
} from "@/lib/operatorFeedback/recoveryCreditChromePresentation"

function remainingForChannel(
  usage: CreditsUsageSnapshot,
  channel: "sms" | "ai"
): number {
  const record = usage.channels.find((row) => row.channel === channel)
  return record?.combinedRemaining ?? 0
}

export type LoadRecoveryCreditChromeInput = {
  mode: OperatorDashboardMode
  locationId: number
  /** Omit defaults to manage so Account-owner burn CTAs stay visible during rollout. */
  accessLevel?: BillingCreditsAccessLevel
  permissionRole?: string
}

export async function loadRecoveryCreditChrome(
  input: LoadRecoveryCreditChromeInput
): Promise<RecoveryCreditChromeContext> {
  const [usage, page] = await Promise.all([
    getBillingCreditsUsage(),
    getBillingCreditsPage(),
  ])

  const billingStatus = page.planSubscription.billingStatus
  const isPilot = usage.isPilot || page.planSubscription.isPilot
  const paidActionsLocked = billingStatusImpliesPaidActionsLocked(billingStatus)

  return {
    smsRemaining: remainingForChannel(usage, "sms"),
    aiRemaining: remainingForChannel(usage, "ai"),
    isPilot,
    paidActionsLocked,
    restorationCause: restorationCauseFromBilling({
      billingStatus,
      isPilot,
    }),
    accessLevel: input.accessLevel ?? "manage",
    permissionRole:
      input.permissionRole
      ?? page.actorPermissionRole
      ?? "",
    mode: input.mode,
    locationId: input.locationId,
  }
}
