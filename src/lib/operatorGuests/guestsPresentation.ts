import type {
  OperatorGuestOverviewKpiId,
  OperatorGuestSmartGroupId,
  OperatorGuestSortId,
  OperatorGuestsTableEmptyStateKind,
} from "@/types/operatorGuests"

/** Figma Guests page — nodes 3388:14344 (light) / 3373:76903 (dark). */

export const GUESTS_PAGE_STACK_CLASS = "flex flex-col gap-5"

export const GUESTS_PAGE_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-start"

export const GUESTS_PAGE_HEADER_COPY_CLASS = "flex flex-col gap-3.5 leading-[0]"

export const GUESTS_PAGE_TITLE_CLASS =
  "m-0 text-2xl font-bold leading-10 text-foreground sm:text-[32px]"

export const GUESTS_PAGE_SUBTITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_PAGE_ACTION_BUTTON_CLASS =
  "h-auto min-h-0 rounded-[2px] border-transparent px-4 py-2.5 text-sm font-medium leading-5 disabled:opacity-50"

export const GUESTS_PAGE_PRIMARY_BUTTON_CLASS = `${GUESTS_PAGE_ACTION_BUTTON_CLASS} bg-primary text-primary-foreground hover:bg-primary/90`

export const GUESTS_PAGE_SECONDARY_BUTTON_CLASS = `${GUESTS_PAGE_ACTION_BUTTON_CLASS} bg-[#e8e8e8] text-foreground hover:bg-[#dedede] dark:bg-[#333] dark:text-white dark:hover:bg-[#3d3d3d]`

export const GUESTS_SECTION_CLASS =
  "flex flex-col gap-6 overflow-clip rounded-md border border-[#dcdcdc] bg-[var(--operator-card)] p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6 dark:border-[#262626] dark:shadow-none"

export const GUESTS_SECTION_HEADER_ROW_CLASS =
  "flex items-center justify-between gap-4"

export const GUESTS_SECTION_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-foreground sm:text-xl"

export const GUESTS_SECTION_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Guest Profile detail grids (profile summary / overview details) — Figma 3388:12918 */
export const GUESTS_DETAIL_ROWS_STACK_CLASS = "flex flex-col gap-5"

export const GUESTS_DETAIL_ROW_PAIR_CLASS =
  "grid grid-cols-1 gap-3 sm:grid-cols-2 sm:gap-10"

export const GUESTS_DETAIL_FIELD_CLASS =
  "flex min-w-0 items-baseline justify-between gap-4"

export const GUESTS_DETAIL_FIELD_LABEL_CLASS =
  "m-0 text-base font-semibold leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_DETAIL_FIELD_VALUE_CLASS =
  "m-0 text-right text-base font-medium leading-normal text-foreground"

export const GUESTS_DETAIL_DIVIDER_CLASS =
  "m-0 h-px w-full shrink-0 border-0 bg-[#e5e5e5] dark:bg-[#262626]"

export const GUESTS_KPI_GRID_CLASS =
  "grid grid-cols-1 gap-2.5 sm:grid-cols-2 lg:grid-cols-4"

/** Nested KPI tiles — Figma Main Bg/Bg-colour (#ebebeb light / #202020 dark). */
export const GUESTS_KPI_CARD_CLASS =
  "rounded-[4px] bg-[#ebebeb] p-5 dark:bg-[#202020]"

export const GUESTS_KPI_LABEL_CLASS =
  "m-0 text-sm font-medium leading-normal text-[#707070]"

export const GUESTS_KPI_VALUE_CLASS =
  "m-0 text-[30px] font-extrabold leading-9 text-foreground"

export const GUESTS_KPI_DESCRIPTION_CLASS =
  "m-0 pt-0.5 text-xs font-normal leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_SMART_GROUPS_STACK_CLASS = "flex flex-col gap-[22px]"

export const GUESTS_TABLIST_SCROLL_CLASS = "overflow-x-auto"

export const GUESTS_TABLIST_CLASS =
  "flex w-max min-w-full flex-nowrap items-center gap-5 border-b border-[#e5e5e5] dark:border-[#262626]"

export const GUESTS_TAB_BUTTON_CLASS =
  "inline-flex min-h-11 shrink-0 items-center gap-2.5 rounded-none border-b-2 border-transparent px-[18px] py-3.5 text-base font-medium tracking-[-0.4px] shadow-none hover:bg-transparent focus-visible:ring-0 md:min-h-0"

export const GUESTS_TAB_BUTTON_ACTIVE_CLASS =
  "border-b-primary bg-[#edefee] text-[#1B1B1B] dark:bg-[#1B1B1B] dark:text-white"

export const GUESTS_TAB_BUTTON_INACTIVE_CLASS =
  "text-[#a6a6a6] dark:text-[#7c7c7c]"

export const GUESTS_TAB_COUNT_ACTIVE_CLASS = "text-primary"

export const GUESTS_TOOLBAR_ROW_CLASS = "flex flex-col gap-3 sm:flex-row sm:items-center"

export const GUESTS_SEARCH_WRAP_CLASS = "relative min-w-0 flex-1"

export const GUESTS_SEARCH_FIELD_CLASS =
  "h-10 rounded-[2px] border-0 bg-[#ebebeb] pl-9 text-sm text-foreground shadow-none transition-colors placeholder:text-[#707070] hover:bg-[#dddddd] focus-visible:bg-[#ebebeb] focus-visible:ring-0 disabled:opacity-100 dark:bg-[#212121] dark:hover:bg-[#2a2a2a] dark:focus-visible:bg-[#212121]"

export const GUESTS_TOOLBAR_ACTIONS_CLASS = "flex shrink-0 flex-wrap items-center gap-3"

export const GUESTS_BULK_BAR_ROW_CLASS =
  "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"

export const GUESTS_BULK_BAR_LABEL_CLASS =
  "m-0 text-sm font-semibold leading-normal text-foreground"

export const GUESTS_BULK_BAR_ACTIONS_CLASS = "flex flex-wrap items-center gap-3"

/** Figma 3388:14443 — primary Create campaign (disabled chrome for now). */
export const GUESTS_BULK_BAR_PRIMARY_BUTTON_CLASS =
  GUESTS_PAGE_PRIMARY_BUTTON_CLASS

/** Figma 3388:14444 / 3388:14445 — tertiary Add tag / Export selected. */
export const GUESTS_BULK_BAR_TERTIARY_BUTTON_CLASS =
  "h-auto min-h-0 rounded-[2px] px-[17px] py-[11px] text-sm font-medium leading-5"

/** Figma 3388:14446 — link Clear selection (no underline). */
export const GUESTS_BULK_BAR_CLEAR_BUTTON_CLASS =
  "h-auto min-h-0 rounded-none border-0 bg-transparent p-0 text-sm font-medium leading-5 text-foreground no-underline shadow-none hover:bg-transparent hover:text-foreground/80 hover:no-underline"

export const GUESTS_SORT_BUTTON_CLASS =
  "h-auto min-h-0 shrink-0 gap-1.5 rounded border-[#dcdcdc] bg-transparent px-[17px] py-[11px] text-xs font-medium leading-[18px] text-[#171717] opacity-100 shadow-none hover:bg-transparent aria-expanded:bg-transparent disabled:opacity-100 dark:border-[#393939] dark:bg-transparent dark:text-[#a6a6a6] dark:hover:bg-transparent dark:aria-expanded:bg-transparent"

export const GUESTS_TABLE_FRAME_CLASS =
  "overflow-x-auto rounded-[2px]"

export const GUESTS_TABLE_EMPTY_SHELL_CLASS =
  "flex min-h-[291px] flex-col items-center justify-center"

export const GUESTS_TABLE_EMPTY_COPY_STACK_CLASS =
  "flex flex-col items-center gap-2.5 text-center"

export const GUESTS_TABLE_EMPTY_TITLE_CLASS =
  "m-0 text-base font-medium leading-normal text-foreground"

export const GUESTS_TABLE_EMPTY_HELPER_CLASS =
  "m-0 max-w-[450px] text-sm font-medium leading-[18px] text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_TABLE_EMPTY_ACTIONS_CLASS =
  "mt-[30px] flex items-center justify-center"

export const GUESTS_TABLE_EMPTY_CLEAR_BUTTON_CLASS =
  "h-auto min-h-0 rounded-[2px] border-[#d5d5d5] px-[17px] py-[11px] text-sm font-medium leading-5 text-foreground shadow-none dark:border-[#393939]"

export const GUESTS_TABLE_CLASS = "w-full border-collapse text-sm"

export const GUESTS_TABLE_HEAD_ROW_CLASS = "border-0 hover:bg-transparent"

/** Figma Header/Search/Bg-colour (#ebebeb light / #212121 dark) — shared by table headings. */
export const GUESTS_TABLE_HEAD_CELL_CLASS =
  "h-[43px] border border-[#e5e5e5] bg-[#ebebeb] px-[18px] py-3 text-left align-middle text-sm font-bold leading-[19px] whitespace-nowrap text-foreground dark:border-[#262626] dark:bg-[#212121]"

export const GUESTS_TABLE_BODY_ROW_CLASS = "border-0 hover:bg-transparent"

export const GUESTS_TABLE_BODY_CELL_CLASS =
  "border border-[#e5e5e5] px-[18px] py-3 align-middle dark:border-[#262626]"

export const GUESTS_TABLE_CHECKBOX_CELL_CLASS =
  "w-[66px] border border-[#e5e5e5] py-3 pl-6 pr-6 align-middle dark:border-[#262626] [&:has([role=checkbox])]:pr-6"

export const GUESTS_TABLE_HEAD_CHECKBOX_CELL_CLASS =
  "h-[43px] w-[66px] border border-[#e5e5e5] bg-[#ebebeb] py-3 pl-6 pr-6 text-left align-middle text-sm font-bold leading-[19px] whitespace-nowrap text-foreground dark:border-[#262626] dark:bg-[#212121] [&:has([role=checkbox])]:pr-6"

export const GUESTS_TABLE_HEAD_ACTIONS_CELL_CLASS =
  "h-[43px] border border-[#e5e5e5] bg-[#ebebeb] px-[18px] py-3 text-center align-middle text-sm font-bold leading-[19px] whitespace-nowrap text-foreground dark:border-[#262626] dark:bg-[#212121]"

export const GUESTS_TABLE_ACTIONS_CELL_CLASS =
  "border border-[#e5e5e5] px-[18px] py-3 text-center align-middle dark:border-[#262626]"

export const GUESTS_TABLE_ICON_CELL_INNER_CLASS =
  "flex items-center justify-center"

export const GUESTS_TABLE_CHECKBOX_CELL_INNER_CLASS =
  "flex items-center justify-start"

export const GUESTS_MARKETING_STATUS_BADGE_CLASS = "px-2 py-1.5"

export const GUESTS_TABLE_GUEST_NAME_CLASS =
  "text-sm font-semibold leading-[19px] text-foreground"

export const GUESTS_TABLE_LOCATION_CLASS =
  "text-sm font-normal leading-[19px] text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_TABLE_FEEDBACK_COUNT_CLASS =
  "text-xs font-normal leading-normal text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_TABLE_INTERACTION_LABEL_CLASS =
  "text-sm font-normal leading-4 text-foreground"

export const GUESTS_TABLE_INTERACTION_TIME_CLASS =
  "text-[10px] font-normal leading-4 text-foreground"

export const GUESTS_PAGINATION_ROW_CLASS =
  "flex flex-col gap-3 sm:flex-row sm:items-center sm:justify-between"

export const GUESTS_PAGINATION_LABEL_CLASS =
  "m-0 text-sm font-medium text-muted-foreground dark:text-[#7c7c7c]"

export const GUESTS_PAGINATION_BUTTON_CLASS =
  "h-auto min-h-0 rounded-[2px] px-[17px] py-[11px] text-sm font-medium leading-5 disabled:opacity-50"

export const OPERATOR_GUEST_SMART_GROUP_TABS: Array<{
  id: OperatorGuestSmartGroupId
  label: string
}> = [
  { id: "all-guests", label: "All guests" },
  { id: "new-guests", label: "New guests" },
  { id: "needs-recovery", label: "Needs recovery" },
  { id: "positive-feedback", label: "Positive feedback" },
  { id: "offer-not-redeemed", label: "Offer not redeemed" },
  { id: "recent-redeemers", label: "Recent redeemers" },
  { id: "dormant-guests", label: "Dormant guests" },
]

export const OPERATOR_GUEST_OVERVIEW_KPIS: Array<{
  id: OperatorGuestOverviewKpiId
  label: string
  description: string
}> = [
  {
    id: "total-guests",
    label: "Total guests",
    description: "All guest profiles within the selected location scope.",
  },
  {
    id: "new-this-month",
    label: "New this month",
    description: "Guests first captured during the current month.",
  },
  {
    id: "marketing-eligible",
    label: "Marketing eligible",
    description:
      "Guests with valid permission, a reachable contact method and no suppression.",
  },
  {
    id: "needs-recovery",
    label: "Needs recovery",
    description:
      "Guests with unresolved negative feedback or an open recovery action.",
  },
]

export const OPERATOR_GUEST_SORT_LABELS: Record<OperatorGuestSortId, string> = {
  "recent-activity": "Recent activity",
  "newest-guests": "Newest guests",
  "oldest-guests": "Oldest guests",
  "guest-name-az": "Guest name A–Z",
  "guest-name-za": "Guest name Z–A",
  "most-feedback-submissions": "Most feedback submissions",
  "most-recent-redemption": "Most recent redemption",
}

export const OPERATOR_GUEST_DEFAULT_SORT_ID: OperatorGuestSortId =
  "recent-activity"

export const OPERATOR_GUEST_PAGE_SIZE = 25

export const OPERATOR_GUESTS_TABLE_EMPTY_COPY: Record<
  OperatorGuestsTableEmptyStateKind,
  { title: string; helper: string }
> = {
  "no-guests-found": {
    title: "No guests found",
    helper: "Try changing your search or removing some filters.",
  },
  "no-guests-yet": {
    title: "No guests yet",
    helper:
      "Guests will appear here after they submit feedback and provide their details through your Guest Loop.",
  },
}

export const OPERATOR_GUEST_CONTACT_LINK_CLASS =
  "text-sm font-normal leading-[19px] text-primary underline underline-offset-2 hover:text-primary/90"

/** Figma Guests row Actions menu — node 3388:14467 annotations. */
export const OPERATOR_GUEST_ROW_ACTIONS = [
  { id: "view-guest", label: "View guest" },
  { id: "edit-guest-details", label: "Edit guest details" },
  { id: "create-campaign-with-guest", label: "Create campaign with guest" },
  { id: "manage-tags", label: "Manage tags" },
  {
    id: "manage-marketing-permissions",
    label: "Manage marketing permissions",
  },
  { id: "export-guest-record", label: "Export guest record" },
  { id: "delete-guest-data", label: "Delete guest data — Admin only" },
] as const

export type OperatorGuestRowActionId =
  (typeof OPERATOR_GUEST_ROW_ACTIONS)[number]["id"]

export const GUESTS_ROW_ACTIONS_TRIGGER_CLASS =
  "size-8 text-foreground hover:bg-transparent"

/** Match Guests Sort dropdown content / items. */
export const GUESTS_ROW_ACTIONS_MENU_CLASS = "min-w-[240px]"

export const GUESTS_ROW_ACTIONS_ITEM_CLASS = "text-sm font-medium"

