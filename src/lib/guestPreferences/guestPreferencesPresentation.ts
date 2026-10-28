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

/**
 * Pill button on the dark surface. Figma fill is #262626; the nearest token is
 * `guest-feedback-secondary` (#232323, difference 3), so no new token is added.
 */
export const GUEST_PREFERENCES_PILL_BUTTON_CLASS =
  "h-auto min-h-0 w-full rounded-[45px] border-transparent bg-guest-feedback-secondary px-4 py-3 font-sans text-sm font-medium text-guest-feedback-text shadow-none hover:bg-guest-feedback-secondary/90"

/**
 * Verify dialog surface, shared by the email and mobile dialogs. Figma 7196:1226
 * and 7198:1284: 353px wide, 20px radius, 40px top padding.
 */
export const GUEST_PREFERENCES_DIALOG_CLASS =
  "w-full max-w-[calc(100%-2rem)] gap-[38px] rounded-[20px] bg-guest-feedback-card p-5 pt-10 text-guest-feedback-text sm:max-w-[353px]"

/** Dialog title — *headline*, centred. */
export const GUEST_PREFERENCES_DIALOG_TITLE_CLASS =
  "pr-0 text-center font-heading text-[20px] leading-6 font-bold tracking-normal text-guest-feedback-text"

/** Dialog body copy — *functional*, centred. */
export const GUEST_PREFERENCES_DIALOG_DESCRIPTION_CLASS =
  "max-w-none text-center font-sans text-sm leading-5 text-guest-feedback-muted-soft tracking-normal"

/** Plain bottom action (Stop all Tummly marketing). */
export const GUEST_PREFERENCES_TEXT_ACTION_CLASS =
  "font-sans text-sm font-medium text-guest-feedback-text hover:text-guest-feedback-text"

/** Footer legal links. */
export const GUEST_PREFERENCES_LEGAL_LINK_CLASS =
  "rounded-sm transition-colors hover:text-guest-feedback-text focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-guest-feedback-accent/40"
