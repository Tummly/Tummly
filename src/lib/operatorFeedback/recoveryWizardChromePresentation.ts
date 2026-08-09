/** Shared mid-flow chrome copy for Feedback recovery wizards (Figma U-03 / U-04). */

/**
 * Domain-neutral Operator wizard shell — Recovery intents and Campaign create
 * import this module for chrome (not Feedback-owned).
 */
export const OPERATOR_WIZARD_SHELL_MODULE =
  "@/components/dashboard/operator/OperatorWizardShell"

export const RECOVERY_WIZARD_PAGE_TITLE = "Start recovery"

/**
 * Figma footer: "Last saved 14 August 2026 at 2:18 PM."
 * Uses en-GB day+month+year and a 12-hour clock with AM/PM.
 */
export function formatRecoveryLastSavedLabel(at: Date): string {
  const datePart = new Intl.DateTimeFormat("en-GB", {
    day: "numeric",
    month: "long",
    year: "numeric",
  }).format(at)

  const timePart = new Intl.DateTimeFormat("en-GB", {
    hour: "numeric",
    minute: "2-digit",
    hour12: true,
  })
    .format(at)
    .replace(/\u202f/g, " ")
    .replace(/\s*(am|pm)$/i, (_, period: string) => ` ${period.toUpperCase()}`)

  return `Last saved ${datePart} at ${timePart}.`
}
