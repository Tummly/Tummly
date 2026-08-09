/**
 * Shared Operator wizard shell chrome helpers (Last saved footer, etc.).
 * Domain-neutral — not Feedback-owned.
 */

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
