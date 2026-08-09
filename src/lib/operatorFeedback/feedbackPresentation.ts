/** Feedback page presentation tokens and copy — Guests / Capture section chrome. */

import {
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  OPERATOR_SHELL_MENU_PANEL_CLASS,
} from "@/lib/operatorHome/shellResponsivePresentation"
import type {
  OperatorFeedbackInboxEmptyStateKind,
  OperatorFeedbackInboxSortId,
  OperatorFeedbackInboxTabId,
} from "@/types/operatorFeedback"

export const OPERATOR_FEEDBACK_INBOX_TAB_LABELS: Record<
  OperatorFeedbackInboxTabId,
  string
> = {
  all: "All",
  "needs-attention": "Needs attention",
  new: "New",
  "in-progress": "In progress",
  resolved: "Resolved",
}

export const OPERATOR_FEEDBACK_INBOX_SORT_LABELS: Record<
  OperatorFeedbackInboxSortId,
  string
> = {
  "newest-submitted": "Newest submitted",
  "oldest-submitted": "Oldest submitted",
  "needs-attention-first": "Needs attention first",
  "oldest-unresolved": "Oldest unresolved",
  "recently-updated": "Recently updated",
  "negative-first": "Negative first",
  "positive-first": "Positive first",
  "guest-name-az": "Guest name A–Z",
}

export const OPERATOR_FEEDBACK_INBOX_EMPTY_COPY: Record<
  OperatorFeedbackInboxEmptyStateKind,
  { title: string; helper: string; actionLabel?: string }
> = {
  "no-match": {
    title: "No feedback matches these filters",
    helper: "Try removing a filter or changing your search.",
    actionLabel: "Clear all filters",
  },
  "true-empty": {
    title: "No feedback in this period",
    helper: "Try a wider date range, or check another tab.",
    actionLabel: "Change period",
  },
}

export function feedbackInboxPageRangeLabel(
  from: number,
  to: number,
  total: number
): string {
  return `Showing ${from}–${to} of ${total} feedback items`
}

/** Feedback summary KPI strip — Figma 3539:59355 (flat cells + dividers, not nested wash tiles). */
export const FEEDBACK_KPI_STRIP_CLASS = "w-full"

export const FEEDBACK_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:flex lg:flex-row lg:items-stretch lg:gap-[30px]"

export const FEEDBACK_KPI_DIVIDER_CLASS =
  "hidden w-px shrink-0 self-stretch bg-op-card-border lg:block"

export const FEEDBACK_KPI_CELL_CLASS = "flex min-w-0 flex-1 flex-col"

export const FEEDBACK_KPI_CONTENT_CLASS =
  "flex min-w-0 w-full flex-col items-stretch gap-0.5 pb-[4.25px]"

/** Share of total — Figma KPIs/Info (#14a946). */
export const FEEDBACK_KPI_SHARE_CLASS =
  "m-0 text-op-kpi-info-size font-normal leading-normal text-op-kpi-info-color"

/** Invisible share stub — reserves trend-line height on Total (no share %). */
export const FEEDBACK_KPI_SHARE_STUB_CLASS =
  "m-0 text-op-kpi-info-size font-normal leading-normal invisible select-none"

/** PoP helper — Figma Main Bg/Subtitle (#7c7c7c). */
export const FEEDBACK_KPI_COMPARISON_CLASS =
  "m-0 text-op-kpi-info-size font-normal leading-normal text-op-card-subtitle-color"

export const FEEDBACK_KPI_META_STACK_CLASS =
  "flex flex-col gap-0.5 pt-[1.5px]"

export const FEEDBACK_SUMMARY_SUBTITLE_CLASS =
  "m-0 max-w-[449px] text-op-sm font-medium leading-normal text-op-card-subtitle-color"

export const FEEDBACK_PAGE_COPY = {
  title: "Feedback",
  subtitle:
    "Review private guest feedback, identify recurring issues and manage follow-up actions.",
  summariseWithAi: "Summarise with AI",
  reviewNeedsAttention: (n: number) => `Review needs attention (${n})`,
  summary: {
    title: "Feedback summary",
    subtitle:
      "See how guests responded during the selected period and compare it with the previous equivalent period.",
    emptyTitle: "No feedback received during this period",
    emptyHelper:
      "Try a wider date range or check that your QR placements are active.",
    changePeriod: "Change period",
    viewCapture: "View Capture",
  },
  inbox: {
    title: "Feedback inbox",
    subtitle:
      "Review and manage follow-up for private guest feedback at this location.",
    searchPlaceholder: "Search comments, guest names or issue tags.",
  },
  overflow: {
    exportFeedback: "Export feedback",
    manageSettings: "Manage feedback settings",
    viewHelp: "View feedback help",
  },
  exportDialog: {
    title: "Export feedback",
    subtitle: "Choose which feedback records and fields to include.",
    scopeCurrentTitle: "Current results",
    scopeCurrentHelper: (n: number) =>
      `Download the ${n} feedback items matching your current search and filters.`,
    scopeAllTitle: "All feedback in this period",
    scopeAllHelper: (m: number, location: string, period: string) =>
      `Download all ${m} feedback items from ${location} for ${period}.`,
    fileFormatLabel: "File format",
    formatExcel: "Excel (.xlsx)",
    formatCsv: "CSV (.csv)",
    includeContactLabel: "Include guest contact details",
    includeContactHelper:
      "Includes available guest names, email addresses and mobile numbers.",
    summaryLocation: "Location",
    summaryPeriod: "Period",
    summaryItems: "Feedback items",
    summaryFormat: "Format",
    summaryContact: "Guest contact details",
    contactNotIncluded: "Not included",
    contactIncluded: "Included",
    formatExcelShort: "Excel",
    formatCsvShort: "CSV",
    download: "Download export",
    preparing: "Preparing download…",
    cancel: "Cancel",
    softMaxError:
      "Export exceeds 10,000 rows. Narrow filters and try again.",
    genericError: "Could not export feedback. Please try again.",
  },
} as const

export const FEEDBACK_HEADER_OVERFLOW_ACTIONS = [
  { id: "export-feedback", label: FEEDBACK_PAGE_COPY.overflow.exportFeedback },
  {
    id: "manage-feedback-settings",
    label: FEEDBACK_PAGE_COPY.overflow.manageSettings,
  },
  { id: "view-feedback-help", label: FEEDBACK_PAGE_COPY.overflow.viewHelp },
] as const

export const FEEDBACK_PAGE_META_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-muted-foreground"

/**
 * Portaled Select menus inside Feedback dialogs — same shell chrome as Account /
 * Capture dialog selects; `z-[130]` sits above Dialog (`z-[120]`).
 */
export const FEEDBACK_DIALOG_SELECT_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1 z-[130] p-0`

/**
 * Portaled Select menus inside Start Recovery wizards — same shell chrome as
 * Feedback dialog selects; `z-[140]` sits above OperatorWizardShell (`z-[130]`).
 */
export const FEEDBACK_RECOVERY_SELECT_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1 z-[140] p-0`

/**
 * Portaled Select menus inside Feedback details drawer — same shell chrome;
 * `z-[120]` sits above Drawer content (`z-[115]`).
 */
export const FEEDBACK_DRAWER_SELECT_MENU_CLASS = `${OPERATOR_SHELL_MENU_PANEL_CLASS} min-w-40 gap-0 px-0 py-1 z-[120] p-0`

/** Select group — flush with shell panel (override default SelectGroup padding). */
export const FEEDBACK_DIALOG_SELECT_GROUP_CLASS = "p-0"

/**
 * Select option row — shell item chrome; hide check indicator; selected uses
 * primary text like Account / Capture dialog selects.
 */
export const FEEDBACK_DIALOG_SELECT_ITEM_CLASS = [
  OPERATOR_SHELL_MENU_ITEM_CLASS,
  "pr-3 focus:bg-black/5 focus:text-inherit dark:focus:bg-white/5",
  "data-[state=checked]:bg-transparent data-[state=checked]:font-medium data-[state=checked]:text-primary",
  "data-[state=checked]:focus:bg-transparent data-[state=checked]:focus:text-primary",
  "data-[state=checked]:hover:bg-transparent data-[state=checked]:hover:text-primary",
  "[&>span.absolute]:hidden",
].join(" ")

/**
 * External field label — Figma Container/Input 4574:38058 (Main Bg/Title,
 * semibold 14/20).
 */
export const FEEDBACK_FIELD_LABEL_CLASS =
  "text-sm font-semibold leading-5 text-op-text-primary"

/**
 * Shared Input chrome — Figma Container/Input: transparent fill, op-input-border
 * (dark rgba(74,74,76,0.4)), 4px radius, placeholder #7d7d7d.
 */
export const FEEDBACK_INPUT_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"

/**
 * Shared Textarea chrome — same field tokens as input, with Figma padding 15px.
 */
export const FEEDBACK_TEXTAREA_CLASS =
  "rounded-[4px] border-op-input-border bg-transparent px-[15px] py-[15px] text-sm text-op-text-primary placeholder:text-op-input-placeholder dark:bg-transparent dark:disabled:bg-transparent"
