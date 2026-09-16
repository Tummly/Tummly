import type { ActivationPeriodBadgeCopy } from "./activationPeriod"

/**
 * Full-bleed hero wash — Figma #f6f6f6 light (`--op-color-gray-55`).
 * Negative margins cancel shell gutters so the wash reaches the pane edge;
 * matching padding keeps hero copy on the same inset as the rest of Home.
 */
export const OPERATOR_HOME_HERO_BAND_CLASS =
  "bg-op-color-gray-55 -mx-4 -mt-6 px-4 pt-6 sm:-mx-6 sm:px-6 md:-mx-8 md:-mt-8 md:px-8 md:pt-8 lg:-mx-[70px] lg:-mt-[70px] lg:px-[70px] lg:pt-[70px] dark:bg-transparent"

/**
 * Figma hero row — not a card; sits on the shell pane (70px gutters).
 * Copy left, CTAs right at lg (`items-end`). Node 5693:73314.
 * Bottom padding = 40px (`pb-10`).
 */
export const OPERATOR_HOME_HERO_INNER_CLASS =
  "relative flex flex-col gap-[26px] pb-10 lg:flex-row lg:items-end"

export const OPERATOR_HOME_HERO_COPY_CLASS =
  "flex min-w-0 flex-1 flex-col items-start gap-[26px]"

export const OPERATOR_HOME_HERO_TITLE_CLASS =
  "font-serif text-2xl leading-10 font-semibold text-op-card-title-color sm:text-[36px]"

/** Figma Main Bg/Title on body copy (same as headline). */
export const OPERATOR_HOME_HERO_SUBTITLE_CLASS =
  "max-w-[555px] text-sm leading-6 text-op-card-title-color"

export const OPERATOR_HOME_HERO_CTA_ROW_CLASS =
  "flex shrink-0 flex-wrap items-center gap-3"

/** Touch + layout only — paint comes from `variant="op-primary"` / `op-secondary`. */
export const OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS =
  "max-md:min-h-11 max-md:min-w-11 !text-white disabled:!text-white"

export const OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS =
  "max-md:min-h-11 max-md:min-w-11"

/** Single-line badge; compact copy below md is handled in the component. */
export const OPERATOR_HOME_HERO_BADGE_CLASS =
  "h-auto max-w-full justify-start gap-3 rounded px-3 py-2.5 font-medium whitespace-nowrap"

/** Full trial + end date visible from md up. */
export function formatActivationPeriodBadgeFullVisibleText(
  badge: ActivationPeriodBadgeCopy
): string {
  return `${badge.remaining} in your free trial · Ends ${badge.endsOn}`
}

/** Always expose full meaning to assistive tech. */
export function formatActivationPeriodBadgeAriaLabel(
  badge: ActivationPeriodBadgeCopy
): string {
  return `${badge.remaining} in your free trial. Ends ${badge.endsOn}`
}
