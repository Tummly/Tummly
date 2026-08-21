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

export const LIVE_OFFERS_EMPTY_TITLE_CLASS = OPERATOR_HOME_EMPTY_TITLE_CLASS

export const LIVE_OFFERS_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-op-sm font-medium leading-[18px] text-op-card-subtitle-color text-center"

export const LIVE_OFFERS_EMPTY_ACTIONS_CLASS =
  "flex flex-wrap items-center justify-center gap-3"

export const LIVE_OFFERS_EMPTY_ACTION_BUTTON_CLASS =
  "h-auto min-h-11 min-w-11 disabled:opacity-50 md:min-h-0 md:min-w-0"

export const LIVE_OFFERS_EMPTY_COPY = "No live offers or campaigns"

export const LIVE_OFFERS_EMPTY_HELPER =
  "Create a return-visit offer or start from a campaign template."

export const LIVE_OFFERS_LOAD_ERROR =
  "Could not load live offers and campaigns. Please try again."

export const LIVE_OFFERS_CARDS_STACK_CLASS =
  "flex flex-col gap-4 lg:flex-row lg:gap-5"

export const LIVE_OFFERS_CARD_CLASS =
  "flex min-h-[257px] w-full flex-col overflow-hidden rounded-op-lg border border-op-card-border bg-op-card-background sm:flex-row"

export const LIVE_OFFERS_CARD_PREVIEW_CLASS =
  "relative flex min-h-[200px] w-full items-center justify-center overflow-hidden border-b border-op-card-border bg-op-background-secondary sm:min-h-0 sm:w-[min(50%,360px)] sm:shrink-0 sm:border-b-0 sm:border-r"

export const LIVE_OFFERS_CARD_PREVIEW_SCALE_CLASS =
  "pointer-events-none origin-top scale-[0.42] sm:scale-[0.48]"

export const LIVE_OFFERS_CARD_PREVIEW_OVERLAY_CLASS =
  "absolute inset-0 flex items-center justify-center bg-[color-mix(in_srgb,var(--op-color-black)_35%,transparent)]"

export const LIVE_OFFERS_CARD_META_CLASS =
  "flex min-w-0 flex-1 flex-col justify-between gap-6 p-5 sm:p-6"

export const LIVE_OFFERS_CARD_META_TOP_CLASS = "flex flex-col items-start gap-3"

export const LIVE_OFFERS_CARD_TITLE_CLASS =
  "m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-op-card-title-color"

export const LIVE_OFFERS_CARD_METRICS_CLASS =
  "m-0 flex flex-wrap items-start gap-2 text-op-xs font-normal text-op-card-subtitle-color"

export const LIVE_OFFERS_CARD_ACTIONS_CLASS =
  "flex flex-wrap items-center gap-3"

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
