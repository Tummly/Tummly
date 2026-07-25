import type { ActivationPeriodBadgeCopy } from "./activationPeriod"

/** Figma hero card chrome — surface + border from operator tokens (not hardcoded white). */
export const OPERATOR_HOME_HERO_CARD_CLASS =
  "relative overflow-hidden rounded-op-lg border border-op-card-border bg-op-card-background"

/** Soften accent art into the card fill (token-aware for light/dark). */
export const OPERATOR_HOME_HERO_ART_FADE_CLASS =
  "absolute inset-0 bg-[linear-gradient(4deg,var(--op-card-background)_17%,color-mix(in_srgb,var(--op-card-background)_20%,transparent)_66%)]"

export const OPERATOR_HOME_HERO_ART_EDGE_FADE_CLASS =
  "absolute inset-y-0 left-0 w-[45%] bg-[linear-gradient(90deg,var(--op-card-background)_0%,transparent_100%)]"

export const OPERATOR_HOME_HERO_PHONE_FADE_CLASS =
  "absolute inset-x-0 bottom-0 hidden h-[18%] bg-[linear-gradient(180deg,transparent_0%,var(--op-card-background)_95%)] dark:block"

/** Figma hero inner copy block — PRD §4.1 responsive padding steps. */
export const OPERATOR_HOME_HERO_INNER_CLASS =
  "relative z-20 flex flex-col items-stretch px-4 py-8 sm:px-6 sm:py-10 md:px-8 lg:flex-row lg:items-center lg:px-[55px] lg:py-[71px]"

export const OPERATOR_HOME_HERO_TITLE_CLASS =
  "text-2xl leading-10 font-bold text-op-card-title-color sm:text-[32px]"

export const OPERATOR_HOME_HERO_SUBTITLE_CLASS =
  "max-w-[555px] text-sm leading-6 text-op-card-subtitle-color"

export const OPERATOR_HOME_HERO_CTA_ROW_CLASS =
  "flex flex-wrap items-center gap-3"

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
