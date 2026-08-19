/**
 * Shared Operator wizard shell chrome helpers (Last saved footer, etc.).
 * Domain-neutral — not Feedback-owned.
 */

/**
 * Header / body / footer fill — Figma `#fff` / `--op-color-white` light,
 * `#141414` / `--op-color-black` dark via `--op-shell-chrome`.
 */
export const OPERATOR_WIZARD_SHELL_SURFACE_CLASS = "bg-op-shell-chrome"

/**
 * Full-screen wizard / Start recovery entry dialog.
 * Shared by Campaign and Recovery wizards via OperatorWizardShell, and by
 * StartRecoveryEntryShell.
 */
export const OPERATOR_WIZARD_SHELL_DIALOG_CLASS =
  `fixed inset-0 top-0 left-0 z-[130] flex h-dvh max-h-dvh w-screen max-w-none translate-x-0 translate-y-0 flex-col gap-0 overflow-hidden rounded-none border-0 ${OPERATOR_WIZARD_SHELL_SURFACE_CLASS} p-0 text-op-text-primary shadow-none sm:max-w-none data-open:zoom-in-100 data-closed:zoom-out-100`

/** Close-control row above the rounded body. */
export const OPERATOR_WIZARD_SHELL_HEADER_CLASS =
  `flex w-full shrink-0 items-center justify-end p-6 ${OPERATOR_WIZARD_SHELL_SURFACE_CLASS}`

/**
 * Rounded body under the close header.
 * Header / footer borders use `--op-divider` (`#e5e5e5` light / `#262626` dark).
 */
export const OPERATOR_WIZARD_SHELL_BODY_CLASS =
  `min-h-0 flex-1 overflow-y-auto rounded-t-[20px] border-t border-op-divider ${OPERATOR_WIZARD_SHELL_SURFACE_CLASS}`

/** Sticky-looking footer strip inside the body scroll track. */
export const OPERATOR_WIZARD_SHELL_FOOTER_CLASS =
  `border-t border-op-divider ${OPERATOR_WIZARD_SHELL_SURFACE_CLASS} px-4 py-6 sm:px-6 md:px-[100px] min-[1728px]:px-[200px]`

/**
 * Wizard body copy inset. Figma 1728 frame uses 200px side inset; scale down
 * on narrower viewports.
 */
export const OPERATOR_WIZARD_SHELL_INSET_CLASS =
  "px-4 pb-24 pt-10 sm:px-6 sm:pt-[60px] md:px-[100px] min-[1728px]:px-[200px]"

/**
 * Success step column — Figma `5337:43934`. Center a 600px stack (title,
 * subtitle, status rows) inside the inset frame.
 */
export const OPERATOR_WIZARD_SUCCESS_COLUMN_CLASS =
  "flex w-full max-w-[600px] flex-col"

/** Figma success subtitle measure (`w-[425px]`). */
export const OPERATOR_WIZARD_SUCCESS_DESCRIPTION_CLASS = "max-w-[425px]"

/** Figma gap from title block to status list (`gap-[52px]`). */
export const OPERATOR_WIZARD_SUCCESS_BODY_GAP_CLASS = "mt-[52px]"

/**
 * Selectable wizard cards (intent, Offer details, Response setup).
 * Fill `#f5f5f5` / `--op-color-gray-60` light, `#171717` / `--op-color-gray-1000`
 * dark. Idle border `--op-divider` (`#e5e5e5` light / `#262626` dark).
 * Hover and selected `#7c7c7c` / `--op-color-gray-550`.
 */
export const OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS =
  "border-op-divider bg-op-color-gray-60 dark:bg-[var(--op-color-gray-1000)]"

/** Hover and selected — `#7c7c7c` / `--op-color-gray-550` in light and dark. */
export const OPERATOR_WIZARD_SELECTABLE_CARD_SELECTED_CLASS =
  "border-[var(--op-color-gray-550)]"

export const OPERATOR_WIZARD_SELECTABLE_CARD_IDLE_CLASS =
  "hover:border-[var(--op-color-gray-550)]"

export const OPERATOR_WIZARD_SELECTABLE_CARD_DISABLED_CLASS =
  "cursor-not-allowed opacity-50 hover:border-op-divider"

/**
 * Shared layout for selectable wizard cards (intent, Offer stance, Offer type).
 */
export const OPERATOR_WIZARD_SELECTABLE_CARD_CLASS =
  `h-auto w-full items-center justify-start rounded-[4px] border px-[18px] py-4 text-left whitespace-normal hover:bg-transparent ${OPERATOR_WIZARD_SELECTABLE_CARD_SURFACE_CLASS}`

/**
 * Preparing draft overlay (Guest response / Campaign Message).
 * Fill `#f5f5f5` / `--op-color-gray-60` light, `#171717` / `--op-color-gray-1000`
 * dark.
 */
export const OPERATOR_WIZARD_PREPARING_OVERLAY_CLASS =
  "z-[150] w-full max-w-[min(100%-2rem,520px)] gap-0 rounded-[4px] border-op-card-border bg-op-color-gray-60 p-0 text-op-text-primary shadow-none sm:max-w-[520px] dark:bg-[var(--op-color-gray-1000)]"

export const OPERATOR_WIZARD_PREPARING_OVERLAY_BODY_CLASS =
  "flex flex-col items-center pt-[22px] pb-[42px]"

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
