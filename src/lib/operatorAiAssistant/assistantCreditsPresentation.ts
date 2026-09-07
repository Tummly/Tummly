/**
 * AI Assistant composer credits bar (Figma 3454:56050; lock 09 / ticket 25).
 * Remaining and allowance come from Billing **Credits & usage** (AI channel).
 */
import type { BillingCreditsAccessLevel } from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  operatorDashboardBillingCreditsManagePlanPath,
  operatorDashboardBillingCreditsPath,
} from "@/lib/operatorBillingCredits/billingCreditsPresentation"
import {
  creditChannelFillRatio,
  formatCreditCount,
} from "@/lib/operatorBillingCredits/creditsUsagePresentation"
import type { OperatorDashboardMode } from "@/lib/operatorHome/operatorDashboardPaths"

export const ASSISTANT_CREDITS_STUB_REMAINING = 20
export const ASSISTANT_CREDITS_STUB_ALLOWANCE = 20
export const ASSISTANT_CREDITS_STUB_USED = 0
export const ASSISTANT_VIEW_USAGE_LABEL = "View usage"
export const ASSISTANT_ADD_CREDITS_LABEL = "Add credits"
export const ASSISTANT_CHOOSE_PLAN_LABEL = "Choose a plan"
export const ASSISTANT_UPDATE_PAYMENT_LABEL = "Update payment method"

/** Shell navbar AI credits popover — Figma 5216:26967. */
export const SHELL_AI_CREDITS_TITLE = "AI credit usage"
export const SHELL_AI_VIEW_USAGE_LABEL = "View AI usage"
export const SHELL_AI_ADD_CREDITS_LABEL = "Add AI credits"

export function shellAiCreditsButtonLabel(remaining: number): string {
  return `${formatCreditCount(remaining)} AI credits`
}

export function shellAiCreditsUsedLine(used: number, allowance: number): string {
  return `${formatCreditCount(used)} of ${formatCreditCount(allowance)} AI credits used`
}

export function shellAiCreditsLeftLine(remaining: number): string {
  return `${formatCreditCount(remaining)} AI credits left`
}

export function shellAiCreditsFillRatio(
  remaining: number,
  used: number
): number {
  return creditChannelFillRatio(remaining, used)
}

export function resolveShellAiCreditsUsed(options: {
  remaining: number
  allowance: number
  usedThisCycle?: number
}): number {
  if (options.usedThisCycle != null) {
    return Math.max(0, options.usedThisCycle)
  }
  return Math.max(0, options.allowance - options.remaining)
}

export type AssistantAccountLockCause = "unpaid-pilot" | "dunning"

export type AssistantCreditsRestorationHelper = {
  label: string
  href: string
}

export function assistantCreditsRemainingLine(
  remaining: number,
  allowance: number
): string {
  return `${formatCreditCount(remaining)} of ${formatCreditCount(allowance)} monthly AI credits remaining`
}

export const ASSISTANT_CREDITS_STUB_REMAINING_LINE =
  assistantCreditsRemainingLine(
    ASSISTANT_CREDITS_STUB_REMAINING,
    ASSISTANT_CREDITS_STUB_ALLOWANCE
  )

export function assistantCreditsDepleted(remaining: number): boolean {
  return remaining <= 0
}

export function isAssistantAccountLocked(billingStatus: string): boolean {
  return billingStatus === "Soft lock" || billingStatus === "Dormant"
}

export function resolveAssistantAccountLockCause(options: {
  billingStatus: string
  isPilot: boolean
}): AssistantAccountLockCause | null {
  if (!isAssistantAccountLocked(options.billingStatus)) {
    return null
  }
  return options.isPilot ? "unpaid-pilot" : "dunning"
}

export function assistantCreditsShowViewUsage(
  accessLevel: BillingCreditsAccessLevel
): boolean {
  return accessLevel === "view" || accessLevel === "manage"
}

export function assistantCreditsShowAddCredits(options: {
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
}): boolean {
  if (options.accessLevel !== "manage") {
    return false
  }
  // Omit role must not hide Manage writers (CODING_STANDARDS chrome access).
  if (options.permissionRole.trim() === "") {
    return true
  }
  return (
    options.permissionRole === "Owner"
    || options.permissionRole === "Billing Admin"
    || options.permissionRole === "Admin"
  )
}

export function assistantCreditsViewUsageHref(
  mode: OperatorDashboardMode,
  locationId: number
): string {
  return operatorDashboardBillingCreditsPath(mode, locationId, {
    tab: "credits-usage",
  })
}

export function assistantCreditsAddCreditsHref(
  mode: OperatorDashboardMode,
  locationId: number
): string {
  return operatorDashboardBillingCreditsManagePlanPath(mode, locationId, {
    section: "credit-top-ups",
    channel: "ai",
  })
}

export function assistantCreditsRestorationHelper(options: {
  lockCause: AssistantAccountLockCause | null
  accessLevel: BillingCreditsAccessLevel
  permissionRole: string
  mode: OperatorDashboardMode
  locationId: number
}): AssistantCreditsRestorationHelper | null {
  if (options.lockCause == null || options.accessLevel !== "manage") {
    return null
  }

  const isOwner = options.permissionRole === "Owner"
  const isBillingAdminWriter =
    options.permissionRole === "Billing Admin"
    || options.permissionRole === "Admin"

  if (options.lockCause === "unpaid-pilot") {
    if (!isOwner) {
      return null
    }
    return {
      label: ASSISTANT_CHOOSE_PLAN_LABEL,
      href: operatorDashboardBillingCreditsManagePlanPath(
        options.mode,
        options.locationId
      ),
    }
  }

  if (!isOwner && !isBillingAdminWriter) {
    return null
  }

  return {
    label: ASSISTANT_UPDATE_PAYMENT_LABEL,
    href: operatorDashboardBillingCreditsPath(options.mode, options.locationId, {
      tab: "payment-invoices",
    }),
  }
}

/** Shared Mic / Send circle fill — Figma Main Bg/Colour, primary glyph. */
const ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS = [
  "shrink-0 rounded-full shadow-none",
  "bg-op-assistant-credits-background text-op-text-primary",
  "hover:bg-op-assistant-credits-background hover:text-op-text-primary",
].join(" ")

/** Mic circle — 40px, 44px hit below md. */
export const ASSISTANT_COMPOSER_CIRCLE_CLASS = [
  ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS,
  "size-10 min-h-11 min-w-11 p-2 md:min-h-10 md:min-w-10",
].join(" ")

/** Send circle — smaller than mic. */
export const ASSISTANT_COMPOSER_SEND_CIRCLE_CLASS = [
  ASSISTANT_COMPOSER_CIRCLE_CHROME_CLASS,
  "size-8 min-h-8 min-w-8 p-2",
].join(" ")

export const ASSISTANT_COMPOSER_SEND_ICON_CLASS = "size-4"

const ASSISTANT_COMPOSER_FIELD_BASE_CLASS = [
  "flex min-h-[112px] flex-col justify-between border-0 p-4",
  "md:min-h-[144px] md:p-[21px]",
].join(" ")

/**
 * Outer credits + field shell — rest vs focus.
 * Focus keeps the rest border. Do not paint a ring.
 */
export function assistantComposerBorderClass(_focused: boolean): string {
  return "border-op-assistant-composer-border"
}

export function assistantComposerShellClass(focused: boolean): string {
  return [
    "rounded-[8px] border transition-colors",
    assistantComposerBorderClass(focused),
  ].join(" ")
}

export function assistantComposerTextareaClass(): string {
  return [
    "min-h-0 flex-1 resize-none rounded-none border-0 bg-transparent p-0",
    "text-sm text-op-text-primary shadow-none",
    "placeholder:text-[var(--op-color-gray-550)]",
    "focus-visible:border-0 focus-visible:ring-0",
    "disabled:bg-transparent disabled:opacity-100 dark:bg-transparent dark:disabled:bg-transparent",
  ].join(" ")
}

/** True while the mic is recording or transcribing. */
export function assistantComposerMicActive(
  chrome: "mic" | "tick_cancel" | "loader"
): boolean {
  return chrome !== "mic"
}

/** Idle uses Side-nav fill. Mic active lifts to Main Bg/Colour. No field border — shell owns it. */
export function assistantComposerFieldClass(
  chrome: "mic" | "tick_cancel" | "loader"
): string {
  return [
    ASSISTANT_COMPOSER_FIELD_BASE_CLASS,
    assistantComposerMicActive(chrome)
      ? "bg-op-assistant-composer-recording-background"
      : "bg-op-assistant-composer-background",
  ].join(" ")
}
