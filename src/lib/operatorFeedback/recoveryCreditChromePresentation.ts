/**
 * Recovery Email/SMS remaining chrome and shared Uses 1 AI action chip.
 * Locks 09 / 11 / 12 — ticket 24.
 */

import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"
import type { RespondToGuestChannel } from "@/lib/operatorFeedback/respondToGuestPresentation"

export const RECOVERY_EMAIL_AVAILABILITY_LINE =
  "Available · No email credits required"

export const RECOVERY_SMS_AVAILABILITY_FLOOR_LINE =
  "Available · Estimated usage: at least 1 SMS credit"

export const RECOVERY_AI_NO_CREDITS_REMAINING = "No AI credits remaining"

export const RECOVERY_BUY_SMS_CREDITS_LABEL = "Buy SMS credits"
export const RECOVERY_BUY_AI_CREDITS_LABEL = "Buy AI credits"
export const RECOVERY_CHANGE_PLAN_LABEL = "Change plan"
export const RECOVERY_CHOOSE_PLAN_LABEL = "Choose a plan"
export const RECOVERY_UPDATE_PAYMENT_LABEL = "Update payment method"

export type RecoveryRestorationCause = "unpaid-pilot" | "dunning"

export type RecoveryCreditChromeContext = {
  smsRemaining: number | null
  aiRemaining: number | null
  isPilot: boolean
  paidActionsLocked: boolean
  restorationCause: RecoveryRestorationCause | null
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
}

export type RecoveryChannelCreditCta = {
  label: string
  href: string
}

export type RecoverySmsShortfallChrome = {
  blocked: boolean
  buyCta: RecoveryChannelCreditCta | null
  changePlanCta: RecoveryChannelCreditCta | null
}

export type RecoveryAiActionChipChrome = {
  /** Prepare / Rewrite with AI may run. */
  prepareAllowed: boolean
  /** Chip only when remaining ≥ 1 and not paid-locked. */
  showMeteringChip: boolean
  depletedMessage: string | null
  buyCta: RecoveryChannelCreditCta | null
  changePlanCta: RecoveryChannelCreditCta | null
  /** Write manually stays unless the session locks unrelated chrome. */
  writeManuallyAllowed: boolean
}

export type RecoveryPaidWriteChrome = {
  burnDisabled: boolean
  helperCta: RecoveryChannelCreditCta | null
}

function formatCreditCount(count: number): string {
  return count.toLocaleString("en-GB")
}

function canBuyTopUp(context: RecoveryCreditChromeContext): boolean {
  if (context.accessLevel !== "manage" || context.isPilot) {
    return false
  }
  const role = context.permissionRole
  return role === "Owner" || role === "Billing Admin" || role === "Admin"
}

function canChangePlan(context: RecoveryCreditChromeContext): boolean {
  return (
    context.accessLevel === "manage" && context.permissionRole === "Owner"
  )
}

function buyChannelHref(
  context: RecoveryCreditChromeContext,
  channel: "sms" | "ai"
): string {
  return operatorDashboardBillingCreditsManagePlanPath(
    context.mode,
    context.locationId,
    { section: "credit-top-ups", channel }
  )
}

function changePlanHref(context: RecoveryCreditChromeContext): string {
  return operatorDashboardBillingCreditsManagePlanPath(
    context.mode,
    context.locationId
  )
}

/**
 * Paint-only SMS credit estimate for one recovery recipient until Reserve
 * estimate exists. Empty body → floor of 1. Body → segments of 160 chars.
 */
export function resolveRecoverySmsEstimateCredits(messageBody: string): number {
  const trimmed = messageBody.trim()
  if (trimmed === "") {
    return 1
  }
  return Math.max(1, Math.ceil(trimmed.length / 160))
}

export function recoverySmsAvailabilityLine(input: {
  messageBody: string
  estimateCredits?: number
}): string {
  const trimmed = input.messageBody.trim()
  if (trimmed === "") {
    return RECOVERY_SMS_AVAILABILITY_FLOOR_LINE
  }
  const estimate =
    input.estimateCredits ?? resolveRecoverySmsEstimateCredits(trimmed)
  return `Estimated usage: ${formatCreditCount(estimate)} SMS credits`
}

export function recoveryChannelAvailabilityLine(input: {
  channel: RespondToGuestChannel
  messageBody: string
}): string {
  if (input.channel === "email") {
    return RECOVERY_EMAIL_AVAILABILITY_LINE
  }
  return recoverySmsAvailabilityLine({ messageBody: input.messageBody })
}

/** Email recovery is never credit-blocked. */
export function isRecoveryEmailCreditBlocked(): boolean {
  return false
}

export function resolveRecoverySmsShortfall(input: {
  channel: RespondToGuestChannel | null
  messageBody: string
  smsRemaining: number | null
  context: RecoveryCreditChromeContext | null
}): RecoverySmsShortfallChrome {
  if (input.channel !== "sms") {
    return { blocked: false, buyCta: null, changePlanCta: null }
  }

  const estimate = resolveRecoverySmsEstimateCredits(input.messageBody)
  const remaining = input.smsRemaining
  const blocked = remaining != null && remaining < estimate

  if (!blocked || input.context == null) {
    return {
      blocked,
      buyCta: null,
      changePlanCta: null,
    }
  }

  return {
    blocked: true,
    buyCta: canBuyTopUp(input.context)
      ? {
          label: RECOVERY_BUY_SMS_CREDITS_LABEL,
          href: buyChannelHref(input.context, "sms"),
        }
      : null,
    changePlanCta: canChangePlan(input.context)
      ? {
          label: RECOVERY_CHANGE_PLAN_LABEL,
          href: changePlanHref(input.context),
        }
      : null,
  }
}

export function resolveRecoveryAiActionChipChrome(input: {
  context: RecoveryCreditChromeContext | null
}): RecoveryAiActionChipChrome {
  const writeManuallyAllowed = true

  if (input.context == null) {
    return {
      prepareAllowed: true,
      showMeteringChip: true,
      depletedMessage: null,
      buyCta: null,
      changePlanCta: null,
      writeManuallyAllowed,
    }
  }

  const context = input.context
  if (context.paidActionsLocked) {
    return {
      prepareAllowed: false,
      showMeteringChip: true,
      depletedMessage: null,
      buyCta: null,
      changePlanCta: null,
      writeManuallyAllowed,
    }
  }

  const aiRemaining = context.aiRemaining
  if (aiRemaining != null && aiRemaining <= 0) {
    return {
      prepareAllowed: false,
      showMeteringChip: true,
      depletedMessage: RECOVERY_AI_NO_CREDITS_REMAINING,
      buyCta: canBuyTopUp(context)
        ? {
            label: RECOVERY_BUY_AI_CREDITS_LABEL,
            href: buyChannelHref(context, "ai"),
          }
        : null,
      changePlanCta: canChangePlan(context)
        ? {
            label: RECOVERY_CHANGE_PLAN_LABEL,
            href: changePlanHref(context),
          }
        : null,
      writeManuallyAllowed,
    }
  }

  return {
    prepareAllowed: true,
    showMeteringChip: true,
    depletedMessage: null,
    buyCta: null,
    changePlanCta: null,
    writeManuallyAllowed,
  }
}

export function resolveRecoveryPaidWriteChrome(input: {
  context: RecoveryCreditChromeContext | null
}): RecoveryPaidWriteChrome {
  if (input.context == null || !input.context.paidActionsLocked) {
    return { burnDisabled: false, helperCta: null }
  }

  const context = input.context
  const cause = context.restorationCause
  if (cause == null || context.accessLevel !== "manage") {
    return { burnDisabled: true, helperCta: null }
  }

  if (cause === "unpaid-pilot") {
    if (context.permissionRole !== "Owner") {
      return { burnDisabled: true, helperCta: null }
    }
    return {
      burnDisabled: true,
      helperCta: {
        label: RECOVERY_CHOOSE_PLAN_LABEL,
        href: changePlanHref(context),
      },
    }
  }

  return {
    burnDisabled: true,
    helperCta: {
      label: RECOVERY_UPDATE_PAYMENT_LABEL,
      href: operatorDashboardBillingCreditsPath(context.mode, context.locationId, {
        tab: "payment-invoices",
      }),
    },
  }
}

export function isRecoverySendBlocked(input: {
  channel: RespondToGuestChannel | null
  messageBody: string
  context: RecoveryCreditChromeContext | null
}): boolean {
  const paid = resolveRecoveryPaidWriteChrome({ context: input.context })
  if (paid.burnDisabled) {
    return true
  }
  if (input.channel === "email") {
    return isRecoveryEmailCreditBlocked()
  }
  return resolveRecoverySmsShortfall({
    channel: input.channel,
    messageBody: input.messageBody,
    smsRemaining: input.context?.smsRemaining ?? null,
    context: input.context,
  }).blocked
}

export function billingStatusImpliesPaidActionsLocked(
  billingStatus: string | null | undefined
): boolean {
  return billingStatus === "Soft lock" || billingStatus === "Dormant"
}

export function restorationCauseFromBilling(input: {
  billingStatus: string | null | undefined
  isPilot: boolean
  hasFailedPayment?: boolean
}): RecoveryRestorationCause | null {
  if (!billingStatusImpliesPaidActionsLocked(input.billingStatus)) {
    return null
  }
  if (input.hasFailedPayment) {
    return "dunning"
  }
  if (input.isPilot) {
    return "unpaid-pilot"
  }
  return "dunning"
}
