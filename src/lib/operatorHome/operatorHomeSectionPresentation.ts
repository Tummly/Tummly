/** Shared Figma operator home card chrome — light empty sections (3360:66297+). */

export const OPERATOR_HOME_CARD_CLASS =
  "overflow-clip rounded-op-lg border border-op-card-border bg-op-card-background"

export const OPERATOR_HOME_CARD_PADDED_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-6 p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6`

export const OPERATOR_HOME_CARD_STACK_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-6 py-[25px] px-px`

export const OPERATOR_HOME_WHITE_CARD_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col px-4 py-5 sm:px-5 sm:py-6 md:px-6 md:py-[25px]`

export const OPERATOR_HOME_HEADER_ROW_CLASS =
  "flex w-full items-center justify-between gap-4"

export const OPERATOR_HOME_HEADER_COPY_CLASS =
  "flex min-w-0 flex-col gap-op-2 leading-[0]"

export const OPERATOR_HOME_TITLE_CLASS =
  "m-0 text-op-lg font-bold leading-normal text-op-card-title-color"

export const OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-op-card-title-color sm:text-xl"

export const OPERATOR_HOME_WHITE_CARD_TITLE_CLASS =
  "m-0 text-lg font-semibold leading-normal text-op-card-title-color"

export const OPERATOR_HOME_SUBTITLE_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-op-card-subtitle-color"

/** Collapse / refresh chrome — Figma Button Collaps (node 3360:66302). */
export const OPERATOR_HOME_CHROME_BUTTON_CLASS =
  "flex size-[42px] shrink-0 cursor-pointer items-center justify-center rounded-op-sm bg-op-button-collapse-background p-3 hover:bg-op-button-collapse-hover"

export const OPERATOR_HOME_CHROME_ICON_CLASS = "size-[18px] text-foreground"

export const OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS =
  "flex min-h-[291px] flex-col items-center justify-center"

export const OPERATOR_HOME_EMPTY_COPY_STACK_CLASS =
  "flex flex-col items-center gap-2.5 text-center"

export const OPERATOR_HOME_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-op-empty-title-color"

export const OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS =
  "m-0 text-base font-semibold leading-normal text-op-empty-title-color"

export const OPERATOR_HOME_EMPTY_HELPER_CLASS =
  "m-0 max-w-[324px] text-op-sm font-medium leading-[18px] text-op-card-subtitle-color"

/** Latest activity — responsive stepped gutters (ticket 16). */
export const LATEST_ACTIVITY_STEPPED_PADDING_CLASS =
  "px-4 sm:px-5 md:px-6"

export const LATEST_ACTIVITY_TITLE_CLASS =
  "m-0 text-lg sm:text-xl font-bold leading-normal text-op-card-title-color"

export const LATEST_ACTIVITY_HEADER_CLASS = `flex w-full flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS} pb-5`

export const LATEST_ACTIVITY_TABLIST_SCROLL_CLASS =
  "overflow-x-auto md:overflow-x-visible"

export const LATEST_ACTIVITY_TABLIST_CLASS = `flex w-max min-w-full flex-nowrap items-end gap-2.5 ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS} md:w-full`

export const LATEST_ACTIVITY_TAB_TOUCH_CLASS =
  "min-h-11 shrink-0 md:min-h-0"

export const LATEST_ACTIVITY_ROW_CLASS = `flex flex-col gap-3 border-b border-op-card-border py-6 sm:flex-row sm:items-start sm:justify-between sm:gap-4 ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS}`

export const LATEST_ACTIVITY_EMPTY_SHELL_CLASS = `flex min-h-[220px] flex-col items-center justify-center text-center ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS}`

/** Figma Latest activity footer — View all activity (node 3353:40926). */
export const LATEST_ACTIVITY_FOOTER_CLASS = LATEST_ACTIVITY_STEPPED_PADDING_CLASS

export const LATEST_ACTIVITY_VIEW_ALL_LABEL = "View all activity"

/** Weekly brief empty — node 3360:66112. */
export const WEEKLY_BRIEF_SECTION_CLASS = OPERATOR_HOME_CARD_STACK_CLASS

export const WEEKLY_BRIEF_HEADER_CLASS =
  "border-b border-op-card-border px-4 pb-6 sm:px-5 md:px-6"

export const WEEKLY_BRIEF_SUBTITLE =
  "A weekly summary of guest capture, feedback, offers and campaign performance."

export const WEEKLY_BRIEF_EMPTY_TITLE =
  "Your first weekly brief will be ready on Monday"

export const WEEKLY_BRIEF_EMPTY_HELPER =
  "It will summarise guest activity, feedback themes, offers and campaigns."

export const WEEKLY_BRIEF_EMPTY_COPY_CLASS =
  "flex flex-col gap-2.5 px-4 sm:px-5 md:px-6 text-op-card-subtitle-color"

export const WEEKLY_BRIEF_EMPTY_TITLE_CLASS =
  "m-0 text-base font-semibold leading-normal text-op-empty-title-color"

export const WEEKLY_BRIEF_EMPTY_HELPER_CLASS =
  "m-0 text-op-sm font-normal leading-normal text-op-card-subtitle-color"

export const WEEKLY_BRIEF_BODY_CLASS =
  "flex flex-col gap-5 px-4 sm:px-5 md:px-6"

export const WEEKLY_BRIEF_HEADLINE_CLASS =
  "m-0 text-base font-semibold leading-normal text-op-card-title-color"

export const WEEKLY_BRIEF_DOMAIN_BLOCK_CLASS = "flex flex-col gap-1.5"

export const WEEKLY_BRIEF_DOMAIN_LABEL_CLASS =
  "m-0 text-sm font-medium leading-5 text-op-card-title-color"

export const WEEKLY_BRIEF_DOMAIN_SUMMARY_CLASS =
  "m-0 text-op-sm font-normal leading-5 text-op-card-subtitle-color"

export const WEEKLY_BRIEF_WATCH_LIST_CLASS =
  "m-0 list-disc space-y-0.5 pl-[21px] text-op-sm font-normal leading-5 text-op-card-subtitle-color"

export const WEEKLY_BRIEF_STATUS_SHELL_CLASS =
  "flex min-h-[120px] flex-col items-center justify-center gap-3 px-4 sm:px-5 md:px-6"

export const WEEKLY_BRIEF_ERROR_COPY_CLASS =
  "m-0 text-center text-op-sm font-normal leading-5 text-op-card-subtitle-color"

export const WEEKLY_BRIEF_RETRY_LABEL = "Retry"

/** Recommended next step — node 3353:42550 (header + inner panel). */
export const RECOMMENDED_SECTION_CLASS = `${OPERATOR_HOME_WHITE_CARD_CLASS} gap-5`

export const RECOMMENDED_HEADER_CLASS = "flex items-start gap-3 pb-6"

/** AI response body — Figma #ebebeb → op-background-secondary (gray-150). */
export const RECOMMENDED_INNER_PANEL_CLASS =
  "rounded-sm bg-op-background-secondary p-4 sm:p-5"

export const RECOMMENDED_EMPTY_COPY =
  "A recommended action will appear once there is enough guest activity."

export const RECOMMENDED_EMPTY_COPY_CLASS =
  "m-0 text-op-sm font-normal leading-5 text-op-card-subtitle-color"

/** Needs attention — node 3360:66297. */
export const NEEDS_ATTENTION_EMPTY_COPY = "Nothing needs attention right now."

export const NEEDS_ATTENTION_LOAD_ERROR =
  "Could not load Needs attention. Please try again."

export const NEEDS_ATTENTION_DUPLICATE_DRAFT_TOAST =
  "Duplicate Draft has been created."

export const NEEDS_ATTENTION_DUPLICATE_DRAFT_ERROR =
  "Could not duplicate this campaign. Try again."

/**
 * Figma Needs attention row (3344:39087) — #1d1d1d wash, 14px gap,
 * 30px / 20px padding.
 */
export const WARNING_ROW_CLASS =
  "flex w-full flex-wrap items-center gap-x-op-3-5 gap-y-3 overflow-clip rounded-op-md bg-op-needs-attention-row-background py-5 pl-[30px] pr-5"

export const NEEDS_ATTENTION_ROW_LIST_CLASS = "flex flex-col gap-3"

export const NEEDS_ATTENTION_ROW_COPY_CLASS =
  "flex min-w-0 flex-1 flex-col gap-[6px] leading-[0]"

export const NEEDS_ATTENTION_ROW_TITLE_CLASS =
  "m-0 text-base font-semibold leading-6 tracking-[-0.4px] text-op-card-title-color"

export const NEEDS_ATTENTION_ROW_BODY_CLASS =
  "m-0 text-sm font-medium leading-[17px] text-op-card-title-color"

export const NEEDS_ATTENTION_ROW_META_CLASS =
  "m-0 text-xs font-medium leading-[15px] tracking-[-0.4px] text-op-needs-attention-row-meta"

export const NEEDS_ATTENTION_ROW_ICON_CLASS =
  "size-4 shrink-0 text-op-action-primary"

export const NEEDS_ATTENTION_ROW_ACTIONS_CLASS =
  "ml-auto flex shrink-0 flex-wrap items-center gap-2"

export const NEEDS_ATTENTION_VIEW_ALL_ROW_CLASS = "flex justify-end pt-1"

export const NEEDS_ATTENTION_VIEW_ALL_LABEL = "View all"
