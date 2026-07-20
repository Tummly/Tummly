/** Shared Figma operator home card chrome — light empty sections (3360:66297+). */

export const OPERATOR_HOME_CARD_CLASS =
  "overflow-clip rounded-md border border-[#e5e5e5] bg-[#f8f8f8] dark:border-[#262626] dark:bg-[#171717]"

export const OPERATOR_HOME_CARD_PADDED_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-10 p-6`

export const OPERATOR_HOME_CARD_STACK_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-6 py-[25px] px-px`

export const OPERATOR_HOME_HEADER_ROW_CLASS =
  "flex w-full items-center justify-between gap-4"

export const OPERATOR_HOME_HEADER_COPY_CLASS = "flex flex-col gap-2 leading-[0]"

export const OPERATOR_HOME_TITLE_CLASS =
  "m-0 text-xl font-bold leading-normal text-foreground"

export const OPERATOR_HOME_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Collapse / refresh chrome — Figma Button Collaps (node 3360:66302). */
export const OPERATOR_HOME_CHROME_BUTTON_CLASS =
  "flex size-[42px] shrink-0 items-center justify-center rounded-sm bg-[#ebebeb] p-3 dark:bg-[#333]"

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

/** Latest activity empty — node 3360:66317. */
export const LATEST_ACTIVITY_HEADER_CLASS = `${OPERATOR_HOME_HEADER_ROW_CLASS} px-6 pb-5`

/** Weekly brief — node 3360:66328. */
export const WEEKLY_BRIEF_HEADER_CLASS =
  "border-b border-[#e5e5e5] px-6 pb-6 dark:border-[#262626]"

export const WEEKLY_BRIEF_SUBTITLE =
  "A weekly summary of guest capture, feedback, offers and campaign performance."

export const WEEKLY_BRIEF_EMPTY_TITLE =
  "Your first weekly brief will be ready on Monday"

export const WEEKLY_BRIEF_EMPTY_HELPER =
  "It will summarise guest activity, feedback themes, offers and campaigns."

export const WEEKLY_BRIEF_EMPTY_COPY_CLASS =
  "flex flex-col gap-2.5 px-6 text-muted-foreground dark:text-[#7c7c7c]"

/** Recommended next step — node 3353:42550 (header + inner panel). */
export const RECOMMENDED_SECTION_CLASS = `${OPERATOR_HOME_CARD_CLASS} flex flex-col gap-5 py-[25px] px-px`

export const RECOMMENDED_HEADER_CLASS = "flex items-start gap-3 px-6 pb-6"

export const RECOMMENDED_INNER_PANEL_CLASS =
  "mx-6 rounded-sm bg-white p-5 dark:bg-[#202020]"

export const RECOMMENDED_EMPTY_COPY =
  "A recommended action will appear once there is enough guest activity."

export const RECOMMENDED_EMPTY_COPY_CLASS =
  "m-0 text-sm font-normal leading-5 text-muted-foreground dark:text-[#7c7c7c]"

/** Needs attention — node 3360:66297. */
export const NEEDS_ATTENTION_EMPTY_COPY = "Nothing needs attention right now."
