/**
 * Shared Operator wizard shell chrome helpers (Last saved footer, etc.).
 * Domain-neutral — not Feedback-owned.
 */

/**
 * Rounded body under the close header — Figma `#141414` / `--op-color-black`
 * via `bg-op-surface-secondary` (dark).
 * Shared by Campaign and Recovery wizards via OperatorWizardShell.
 */
export const OPERATOR_WIZARD_SHELL_BODY_CLASS =
  "min-h-0 flex-1 overflow-y-auto rounded-t-[20px] border-t border-op-card-border bg-op-surface-secondary"

/**
 * Figma footer: "Last saved 14 August 2026 at 2:18 PM."
 * Uses en-GB day+month+year and a 12-hour clock with AM/PM.
 */
export function formatOperatorWizardLastSavedLabel(at: Date): string {
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
