import type { ActivationPeriodBadgeCopy } from "./activationPeriod"

/** Figma hero inner copy block — PRD §4.1 responsive padding steps. */
export const OPERATOR_HOME_HERO_INNER_CLASS =
  "relative z-20 flex flex-col items-stretch px-4 py-8 sm:px-6 sm:py-10 md:px-8 lg:flex-row lg:items-center lg:px-[55px] lg:py-[71px]"

export const OPERATOR_HOME_HERO_TITLE_CLASS =
  "text-2xl leading-10 font-bold text-foreground sm:text-[32px]"

export const OPERATOR_HOME_HERO_CTA_ROW_CLASS =
  "flex flex-wrap items-center gap-3"

const heroButtonBaseClass =
  "h-auto min-h-0 max-md:min-h-11 max-md:min-w-11 rounded-[2px] border-transparent px-4 py-2.5 text-sm font-medium leading-5 disabled:opacity-50"

export const OPERATOR_HOME_HERO_PRIMARY_BUTTON_CLASS = `${heroButtonBaseClass} bg-primary text-primary-foreground hover:bg-primary/90`

export const OPERATOR_HOME_HERO_SECONDARY_BUTTON_CLASS = `${heroButtonBaseClass} bg-[#e8e8e8] text-foreground hover:bg-[#dedede] dark:bg-[#333] dark:text-white dark:hover:bg-[#3d3d3d]`

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
