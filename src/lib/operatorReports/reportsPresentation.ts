/**
 * Shared Operator Reports chrome + body tokens.
 * Aligns with Capture / Guests / Home Performance for consistent Operator shell.
 */

import {
  CAPTURE_EMPTY_HELPER_CLASS,
  CAPTURE_EMPTY_SHELL_CLASS,
  CAPTURE_EMPTY_TITLE_CLASS,
  CAPTURE_KPI_CELL_CLASS,
  CAPTURE_KPI_CONTENT_CLASS,
  CAPTURE_KPI_DIVIDER_CLASS,
  CAPTURE_KPI_ROW_CLASS,
  CAPTURE_KPI_STRIP_CLASS,
  CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_CELL_CLASS,
  CAPTURE_PLACEMENTS_BODY_ROW_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_CELL_CLASS,
  CAPTURE_PLACEMENTS_HEAD_ROW_CLASS,
  CAPTURE_PLACEMENTS_NAME_CELL_CLASS,
  CAPTURE_PLACEMENTS_TABLE_CLASS,
  CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_SEPARATOR_CLASS,
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS,
  CAPTURE_SECTION_CLASS,
} from "@/lib/operatorCapture/capturePresentation"
import {
  PERFORMANCE_SUBTITLE_CLASS,
  PERFORMANCE_TITLE_CLASS,
} from "@/lib/operatorHome/performanceOverviewPresentation"
import type { VariantProps } from "class-variance-authority"
import type { badgeVariants } from "@/components/ui/badge"

export const REPORTS_PAGE_STACK_CLASS = "flex w-full flex-col gap-5"

export const REPORTS_PAGE_HEADER_ROW_CLASS =
  "flex flex-col items-start justify-between gap-4 sm:flex-row sm:items-start"

export const REPORTS_PAGE_HEADER_COPY_CLASS =
  "flex min-w-0 flex-1 flex-col gap-3.5 leading-[0]"

export const REPORTS_PAGE_TITLE_CLASS =
  "m-0 text-2xl font-bold leading-10 text-op-card-title-color sm:text-[32px]"

export const REPORTS_PAGE_SUBTITLE_CLASS =
  "m-0 text-base font-medium leading-5 text-op-text-muted"

export const REPORTS_PAGE_ACTIONS_CLASS =
  "flex shrink-0 flex-wrap items-center gap-3"

export const REPORTS_PAGE_ACTION_BUTTON_CLASS = "shrink-0 disabled:opacity-50"

export const REPORTS_BREADCRUMB_NAV_CLASS =
  "flex flex-wrap items-center gap-2.5 text-base font-medium"

export const REPORTS_BREADCRUMB_LINK_CLASS =
  "text-op-text-primary hover:text-op-text-primary/90"

export const REPORTS_BREADCRUMB_CURRENT_CLASS = "text-op-text-muted"

export const REPORTS_BODY_STACK_CLASS = "flex flex-col gap-6"

/** Section cards — same chrome as Capture / Home Performance. */
export const REPORTS_SECTION_CLASS = CAPTURE_SECTION_CLASS

export const REPORTS_SECTION_TITLE_CLASS = PERFORMANCE_TITLE_CLASS

export const REPORTS_SECTION_SUBTITLE_CLASS = PERFORMANCE_SUBTITLE_CLASS

export const REPORTS_SECTION_HEADER_CLASS = "flex flex-col gap-1"

/** KPI strip — Capture performance divider row. */
export const REPORTS_KPI_STRIP_CLASS = CAPTURE_KPI_STRIP_CLASS

export const REPORTS_KPI_ROW_CLASS = CAPTURE_KPI_ROW_CLASS

export const REPORTS_KPI_DIVIDER_CLASS = CAPTURE_KPI_DIVIDER_CLASS

export const REPORTS_KPI_CELL_CLASS = CAPTURE_KPI_CELL_CLASS

export const REPORTS_KPI_CONTENT_CLASS = CAPTURE_KPI_CONTENT_CLASS

/** Tables — Capture placements bordered frame. */
export const REPORTS_TABLE_FRAME_CLASS = CAPTURE_PLACEMENTS_TABLE_FRAME_CLASS

export const REPORTS_TABLE_CLASS = CAPTURE_PLACEMENTS_TABLE_CLASS

export const REPORTS_TABLE_HEAD_ROW_CLASS = CAPTURE_PLACEMENTS_HEAD_ROW_CLASS

export const REPORTS_TABLE_HEAD_CELL_CLASS = CAPTURE_PLACEMENTS_HEAD_CELL_CLASS

export const REPORTS_TABLE_HEAD_ACTIONS_CELL_CLASS =
  CAPTURE_PLACEMENTS_HEAD_ACTIONS_CELL_CLASS

export const REPORTS_TABLE_BODY_ROW_CLASS = CAPTURE_PLACEMENTS_BODY_ROW_CLASS

export const REPORTS_TABLE_BODY_CELL_CLASS = CAPTURE_PLACEMENTS_BODY_CELL_CLASS

export const REPORTS_TABLE_NAME_CELL_CLASS = CAPTURE_PLACEMENTS_NAME_CELL_CLASS

export const REPORTS_TABLE_ACTIONS_CELL_CLASS =
  CAPTURE_PLACEMENTS_ACTIONS_CELL_CLASS

export const REPORTS_ROW_ACTIONS_TRIGGER_CLASS =
  CAPTURE_PLACEMENT_ROW_ACTIONS_TRIGGER_CLASS

export const REPORTS_ROW_ACTIONS_MENU_CLASS =
  CAPTURE_PLACEMENT_ROW_ACTIONS_MENU_CLASS

export const REPORTS_ROW_ACTIONS_ITEM_CLASS =
  CAPTURE_PLACEMENT_ROW_ACTIONS_ITEM_CLASS

export const REPORTS_ROW_ACTIONS_SEPARATOR_CLASS =
  CAPTURE_PLACEMENT_ROW_ACTIONS_SEPARATOR_CLASS

/** Page-level empty — Capture empty shell. */
export const REPORTS_EMPTY_SHELL_CLASS = CAPTURE_EMPTY_SHELL_CLASS

export const REPORTS_EMPTY_TITLE_CLASS = CAPTURE_EMPTY_TITLE_CLASS

export const REPORTS_EMPTY_HELPER_CLASS = CAPTURE_EMPTY_HELPER_CLASS

export const REPORTS_EMPTY_ACTIONS_CLASS =
  "flex flex-wrap items-center justify-center gap-3"

/** AI / insight callout inside a section. */
export const REPORTS_INSIGHT_BANNER_CLASS =
  "flex items-start gap-3 rounded-sm border border-op-border-default/60 bg-op-background-primary/80 p-4"

export const REPORTS_INSIGHT_TITLE_CLASS =
  "m-0 text-sm font-semibold leading-normal text-op-text-primary"

export const REPORTS_INSIGHT_BODY_CLASS =
  "m-0 text-sm font-medium leading-relaxed text-op-text-muted"

export const REPORTS_HUB_PAGE_COPY = {
  title: "Reports",
  subtitle:
    "See what is working across guest capture, private feedback, offers and campaigns.",
  generateBrief: "Generate brief",
  export: "Export",
  emptyTitle: "No report data yet.",
  emptySubtitle:
    "Create your first QR code and start collecting private guest feedback.",
} as const

export const REPORTS_BREADCRUMB_COPY = {
  reports: "Reports",
} as const

export const REPORTS_STANDARD_ACTIONS_COPY = {
  generateBrief: "Generate brief",
  export: "Export",
} as const

export type ReportsStatusBadgeVariant = NonNullable<
  VariantProps<typeof badgeVariants>["variant"]
>

/**
 * Map report status labels to Operator Badge variants.
 * Unknown labels use the muted soft chip.
 */
export function resolveReportsStatusBadgeVariant(
  status: string
): ReportsStatusBadgeVariant {
  const normalized = status.trim().toLowerCase()

  switch (normalized) {
    case "active":
    case "redeemed":
    case "resolved":
    case "followed up":
      return "ready"
    case "paused":
    case "archived":
    case "draft":
    case "sent":
    case "reviewed":
    case "new":
    case "expired":
      return "soft"
    case "follow-up needed":
    case "needs follow-up":
    case "attention":
      return "neutral"
    case "invalid":
    case "failed":
    case "unsubscribed":
      return "negative"
    default:
      return "soft"
  }
}
