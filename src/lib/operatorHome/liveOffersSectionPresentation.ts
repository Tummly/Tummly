/** Figma Live offers and campaigns empty — node 3360:66306 (light). */

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

export const LIVE_OFFERS_EMPTY_TITLE_CLASS = `${OPERATOR_HOME_EMPTY_TITLE_CLASS} text-foreground`

export const LIVE_OFFERS_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-sm font-medium leading-[18px] text-muted-foreground dark:text-[#7c7c7c] text-center"

export const LIVE_OFFERS_EMPTY_ACTIONS_CLASS =
  "flex flex-wrap items-center justify-center gap-3"

export const LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS =
  "h-auto min-h-11 min-w-11 disabled:opacity-50 md:min-h-0 md:min-w-0"

export const LIVE_OFFERS_EMPTY_COPY = "No live offers or campaigns"

export const LIVE_OFFERS_EMPTY_HELPER =
  "Create a return-visit offer or start from a campaign template."

export type LiveOffersEmptyActionId = "create-offer" | "create-campaign"

export function resolveLiveOffersEmptyActionVariant(
  actionId: LiveOffersEmptyActionId
): "operator-secondary" | "operator-tertiary" {
  return actionId === "create-offer" ? "operator-secondary" : "operator-tertiary"
}

export const LIVE_OFFERS_EMPTY_ACTIONS: ReadonlyArray<{
  id: LiveOffersEmptyActionId
  label: string
}> = [
  { id: "create-offer", label: "Create offer" },
  { id: "create-campaign", label: "Create campaign" },
]
