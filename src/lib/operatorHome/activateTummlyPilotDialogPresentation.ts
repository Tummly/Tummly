export const ACTIVATE_TUMMLY_PILOT_DIALOG_COPY = {
  title: "Activate Tummly to publish offers",
  body: "Start your 30-day Pilot or choose a plan to create live offers and make them available to guests.",
  startCta: "Start 30-day Pilot",
  viewPlansCta: "View plans",
} as const

/** Overrides default DialogContent marketing hex with Operator theme tokens.
 * Figma root uses 30px between the CTA block and the hero (`gap-[30px]`).
 */
export const ACTIVATE_TUMMLY_PILOT_DIALOG_CONTENT_CLASS =
  "gap-[30px] overflow-hidden border-0 bg-op-surface-secondary p-0 text-op-text-primary shadow-lg sm:max-w-[560px] dark:bg-op-surface-secondary"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_HEADER_CLASS =
  "flex w-full flex-col gap-7 px-8 pt-8"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_ROW_CLASS =
  "flex w-full items-start gap-[22px]"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_TITLE_CLASS =
  "pr-0 text-2xl font-bold tracking-normal text-op-text-primary"

/** Figma Main Bg/Title (#171717) — same token as the heading, not Subtitle. */
export const ACTIVATE_TUMMLY_PILOT_DIALOG_BODY_CLASS =
  "max-w-none text-sm font-semibold leading-[19px] text-op-text-primary"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

export const ACTIVATE_TUMMLY_PILOT_DIALOG_HERO_CLASS =
  "relative h-[384px] w-full overflow-hidden"
