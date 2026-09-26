/** Figma Live offers and campaigns — empty + meta cards (5693:73433). */

import {
  OPERATOR_HOME_CARD_PADDED_CLASS,
  OPERATOR_HOME_EMPTY_COPY_STACK_CLASS,
  OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS,
  OPERATOR_HOME_EMPTY_TITLE_CLASS,
  OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS,
  OPERATOR_HOME_HEADER_COPY_CLASS,
  OPERATOR_HOME_SUBTITLE_CLASS,
} from "./operatorHomeSectionPresentation"

export const LIVE_OFFERS_SECTION_CLASS = OPERATOR_HOME_CARD_PADDED_CLASS

export const LIVE_OFFERS_HEADER_CLASS = OPERATOR_HOME_HEADER_COPY_CLASS

export const LIVE_OFFERS_TITLE_CLASS = OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS

export const LIVE_OFFERS_SUBTITLE_CLASS = OPERATOR_HOME_SUBTITLE_CLASS

export const LIVE_OFFERS_EMPTY_SHELL_CLASS = `${OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS} gap-[30px] py-10`

export const LIVE_OFFERS_EMPTY_COPY_CLASS = OPERATOR_HOME_EMPTY_COPY_STACK_CLASS

export const LIVE_OFFERS_EMPTY_TITLE_CLASS = OPERATOR_HOME_EMPTY_TITLE_CLASS

export const LIVE_OFFERS_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] font-sans text-op-sm font-medium leading-[18px] text-op-card-subtitle-color text-center"

export const LIVE_OFFERS_EMPTY_ACTIONS_CLASS =
  "flex flex-wrap items-center justify-center gap-3"

export const LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS =
  "h-auto min-h-11 min-w-11 disabled:opacity-50 md:min-h-0 md:min-w-0"

export const LIVE_OFFERS_EMPTY_COPY = "No live offers or campaigns"

export const LIVE_OFFERS_EMPTY_HELPER =
  "Create a return-visit offer or start from a campaign template."

export const LIVE_OFFERS_LOAD_ERROR =
  "Could not load live offers and campaigns. Please try again."

/** Figma 5693:73433 — two meta cards side by side (gap 30). */
export const LIVE_OFFERS_CARDS_STACK_CLASS =
  "flex flex-col gap-[30px] lg:flex-row lg:items-stretch"

export const LIVE_OFFERS_CARD_CLASS =
  "flex w-full min-w-0 flex-1 flex-col gap-4 overflow-clip bg-op-background-secondary p-3.5 sm:flex-row sm:items-end sm:justify-between sm:gap-6"

export const LIVE_OFFERS_CARD_META_CLASS = "flex min-w-0 flex-col items-start"

export const LIVE_OFFERS_CARD_META_TOP_CLASS = "flex flex-col items-start gap-3"

/** Status chip on nested card — Figma cards/bg on main-bg pane. */
export const LIVE_OFFERS_CARD_STATUS_BADGE_CLASS =
  "rounded-[2px] bg-op-card-background px-2.5 py-1.5 text-xs font-normal text-op-card-title-color dark:bg-op-card-background"

export const LIVE_OFFERS_CARD_TITLE_CLASS =
  "m-0 font-jakarta text-base font-semibold leading-6 tracking-[-0.4px] text-op-text-primary"

export const LIVE_OFFERS_CARD_METRICS_CLASS =
  "m-0 flex flex-wrap items-start gap-2 font-sans text-op-xs font-normal text-op-card-subtitle-color"

export const LIVE_OFFERS_CARD_ACTIONS_CLASS =
  "flex shrink-0 flex-wrap items-center gap-3"

export const LIVE_OFFERS_PAUSE_CONFIRM_TITLE = "Pause this campaign?"

export const LIVE_OFFERS_PAUSE_CONFIRM_DESCRIPTION =
  "Guests who did not receive this campaign will not receive it until you resume."

export const LIVE_OFFERS_PAUSE_CONFIRM_LABEL = "Pause campaign"

export type LiveOffersEmptyActionId = "create-offer" | "create-campaign"

export function resolveLiveOffersEmptyActionVariant(
  actionId: LiveOffersEmptyActionId
): "op-secondary" | "op-tertiary" {
  return actionId === "create-offer" ? "op-secondary" : "op-tertiary"
}

export const LIVE_OFFERS_EMPTY_ACTIONS: ReadonlyArray<{
  id: LiveOffersEmptyActionId
  label: string
}> = [
  { id: "create-offer", label: "Create offer" },
  { id: "create-campaign", label: "Create campaign" },
]
