/**
 * Preference centre class constants. Colours use the `guest-feedback-*`
 * tokens (`src/index.css`); the card surface uses `guest-feedback-card`.
 * Headings use *headline* (`font-heading`); controls and body copy use
 * *functional* (`font-sans`).
 */

/** Section card — Figma 7191:821. */
export const GUEST_PREFERENCES_CARD_CLASS =
  "flex w-full flex-col gap-6 overflow-clip rounded-[28px] bg-guest-feedback-card p-5"

/** Card heading — *headline*. */
export const GUEST_PREFERENCES_CARD_TITLE_CLASS =
  "m-0 font-heading text-sm font-bold leading-normal text-guest-feedback-text"

/** Field label above a value. */
export const GUEST_PREFERENCES_FIELD_LABEL_CLASS =
  "m-0 font-sans text-sm font-semibold leading-[19px] text-guest-feedback-muted-soft"

/** Field value. */
export const GUEST_PREFERENCES_FIELD_VALUE_CLASS =
  "m-0 font-sans text-sm font-semibold leading-[19px] text-guest-feedback-text"

/** Link-style action (Edit, Add another contact method, Manage restaurants). */
export const GUEST_PREFERENCES_LINK_CLASS =
  "font-sans font-medium text-guest-feedback-accent hover:text-guest-feedback-accent"

/** Pill button on the dark surface — Figma #262626 (`guest-feedback-secondary`). */
export const GUEST_PREFERENCES_PILL_BUTTON_CLASS =
  "h-auto min-h-0 w-full rounded-[45px] border-transparent bg-guest-feedback-secondary px-4 py-3 font-sans text-sm font-medium text-white shadow-none hover:bg-guest-feedback-secondary/90"

/** Plain bottom action (Stop all Tummly marketing). */
export const GUEST_PREFERENCES_TEXT_ACTION_CLASS =
  "font-sans text-sm font-medium text-guest-feedback-text hover:text-guest-feedback-text"

/** Footer legal links. */
export const GUEST_PREFERENCES_LEGAL_LINK_CLASS =
  "rounded-sm transition-colors hover:text-guest-feedback-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-guest-feedback-accent/40"
