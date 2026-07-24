/** Shared Figma operator home card chrome — light empty sections (3360:66297+). */

export const OPERATOR_HOME_CARD_CLASS =
  "overflow-clip rounded-md border border-[#e5e5e5] bg-[var(--operator-card)] dark:border-[#262626]"

export const OPERATOR_HOME_CARD_PADDED_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-6 p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6`

export const OPERATOR_HOME_CARD_STACK_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-6 py-[25px] px-px`

export const OPERATOR_HOME_WHITE_CARD_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col px-4 py-5 sm:px-5 sm:py-6 md:px-6 md:py-[25px]`

export const OPERATOR_HOME_HEADER_ROW_CLASS =
  "flex w-full items-center justify-between gap-4"

export const OPERATOR_HOME_HEADER_COPY_CLASS =
  "flex min-w-0 flex-col gap-2 leading-[0]"

export const OPERATOR_HOME_TITLE_CLASS =
  "m-0 text-xl font-bold leading-normal text-foreground"

export const OPERATOR_HOME_GRAY_SHELL_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-foreground sm:text-xl"

export const OPERATOR_HOME_WHITE_CARD_TITLE_CLASS =
  "m-0 text-lg font-semibold leading-normal text-foreground"

export const OPERATOR_HOME_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Collapse / refresh chrome — Figma Button Collaps (node 3360:66302). */
export const OPERATOR_HOME_CHROME_BUTTON_CLASS =
  "flex size-[42px] shrink-0 cursor-pointer items-center justify-center rounded-[2px] bg-[#ebebeb] p-3 dark:bg-[#333]"

export const OPERATOR_HOME_CHROME_ICON_CLASS = "size-[18px] text-foreground"

export const OPERATOR_HOME_EMPTY_SHELL_CENTERED_CLASS =
  "flex min-h-[291px] flex-col items-center justify-center"

export const OPERATOR_HOME_EMPTY_COPY_STACK_CLASS =
  "flex flex-col items-center gap-2.5 text-center"

export const OPERATOR_HOME_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const OPERATOR_HOME_EMPTY_TITLE_SEMIBOLD_CLASS =
  "m-0 text-base font-semibold leading-normal text-foreground"

export const OPERATOR_HOME_EMPTY_HELPER_CLASS =
  "m-0 max-w-[324px] text-sm font-medium leading-[18px] text-muted-foreground dark:text-[#7c7c7c]"

/** Latest activity — responsive stepped gutters (ticket 16). */
export const LATEST_ACTIVITY_STEPPED_PADDING_CLASS =
  "px-4 sm:px-5 md:px-6"

export const LATEST_ACTIVITY_TITLE_CLASS =
  "m-0 text-lg sm:text-xl font-bold leading-normal text-foreground"

export const LATEST_ACTIVITY_HEADER_CLASS = `flex w-full flex-col items-start gap-4 sm:flex-row sm:items-center sm:justify-between ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS} pb-5`

export const LATEST_ACTIVITY_TABLIST_SCROLL_CLASS =
  "overflow-x-auto md:overflow-x-visible"

export const LATEST_ACTIVITY_TABLIST_CLASS = `flex w-max min-w-full flex-nowrap items-start gap-2.5 ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS} md:h-[27px] md:w-full`

export const LATEST_ACTIVITY_TAB_TOUCH_CLASS =
  "min-h-11 shrink-0 md:min-h-0 md:h-full"

export const LATEST_ACTIVITY_ROW_CLASS = `flex flex-col gap-3 border-b border-[#e5e5e5] py-6 last:border-b-0 dark:border-[#262626] sm:flex-row sm:items-start sm:justify-between sm:gap-4 ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS}`

export const LATEST_ACTIVITY_EMPTY_SHELL_CLASS = `flex min-h-[220px] flex-col items-center justify-center text-center ${LATEST_ACTIVITY_STEPPED_PADDING_CLASS}`

/** Weekly brief empty — node 3360:66112. */
export const WEEKLY_BRIEF_SECTION_CLASS = OPERATOR_HOME_CARD_STACK_CLASS

export const WEEKLY_BRIEF_HEADER_CLASS =
  "border-b border-[#e5e5e5] px-4 pb-6 sm:px-5 md:px-6 dark:border-[#262626]"

export const WEEKLY_BRIEF_SUBTITLE =
  "A weekly summary of guest capture, feedback, offers and campaign performance."

export const WEEKLY_BRIEF_EMPTY_TITLE =
  "Your first weekly brief will be ready on Monday"

export const WEEKLY_BRIEF_EMPTY_HELPER =
  "It will summarise guest activity, feedback themes, offers and campaigns."

export const WEEKLY_BRIEF_EMPTY_COPY_CLASS =
  "flex flex-col gap-2.5 px-4 sm:px-5 md:px-6 text-muted-foreground dark:text-[#7c7c7c]"

export const WEEKLY_BRIEF_EMPTY_TITLE_CLASS =
  "m-0 text-base font-semibold leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const WEEKLY_BRIEF_EMPTY_HELPER_CLASS =
  "m-0 text-sm font-normal leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Recommended next step — node 3353:42550 (header + inner panel). */
export const RECOMMENDED_SECTION_CLASS = `${OPERATOR_HOME_WHITE_CARD_CLASS} gap-5`

export const RECOMMENDED_HEADER_CLASS = "flex items-start gap-3 pb-6"

export const RECOMMENDED_INNER_PANEL_CLASS =
  "rounded-sm bg-white p-4 sm:p-5 dark:bg-[#202020]"

export const RECOMMENDED_EMPTY_COPY =
  "A recommended action will appear once there is enough guest activity."

export const RECOMMENDED_EMPTY_COPY_CLASS =
  "m-0 text-sm font-normal leading-5 text-muted-foreground dark:text-[#7c7c7c]"

/** Needs attention — node 3360:66297. */
export const NEEDS_ATTENTION_EMPTY_COPY = "Nothing needs attention right now."
