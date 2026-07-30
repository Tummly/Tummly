import { OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS } from "@/lib/operatorHome/shellResponsivePresentation"

/** Figma Performance overview section — node 3353:42472 (light) / 3062:5973 (dark). */

export const PERFORMANCE_SECTION_CLASS =
  "flex flex-col gap-6 overflow-clip rounded-op-lg border border-op-card-border bg-op-card-background p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6 dark:shadow-none"

export const PERFORMANCE_HEADER_ROW_CLASS =
  "flex items-center justify-between gap-4"

export const PERFORMANCE_HEADER_COPY_CLASS = "flex flex-col gap-2 leading-[0]"

export const PERFORMANCE_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-op-card-title-color sm:text-xl"

export const PERFORMANCE_SUBTITLE_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-op-card-subtitle-color"

/** Date-range trigger — same outlined chrome as Guests Sort. */
export const PERFORMANCE_DATE_BUTTON_CLASS = OPERATOR_OUTLINE_TOOLBAR_BUTTON_CLASS

/** Disabled date-range colours — matches Figma Button Date (unavailable). */
export const PERFORMANCE_DATE_BUTTON_DISABLED_CLASS =
  "text-[#171717] opacity-60 dark:text-[#a6a6a6]"

/** Enabled date-range colours for the live Performance overview control. */
export const PERFORMANCE_DATE_BUTTON_ENABLED_CLASS = ""

export const PERFORMANCE_DATE_PRESET_LIST_CLASS = "flex flex-col"

/**
 * Date preset rows — layout hooks on top of shell menu item chrome
 * (`OPERATOR_SHELL_MENU_ITEM_CLASS` / `OPERATOR_SHELL_MENU_ITEM_SELECTED_CLASS`).
 */
export const PERFORMANCE_DATE_PRESET_ITEM_CLASS =
  "h-auto w-full justify-start text-sm font-normal shadow-none hover:text-inherit"

export const PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS =
  "flex items-center justify-between gap-2"

export const PERFORMANCE_DATE_CUSTOM_HINT_CLASS =
  "m-0 px-1 text-xs text-muted-foreground"

export const PERFORMANCE_DATE_ICON_CLASS =
  "size-3.5 shrink-0 text-current"

export const PERFORMANCE_KPI_STRIP_CLASS = "rounded-sm px-5 py-[30px]"

export const PERFORMANCE_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:flex lg:flex-row lg:items-center lg:gap-[30px]"

export const PERFORMANCE_KPI_CELL_CLASS =
  "flex min-w-0 flex-1 flex-col"

export const PERFORMANCE_KPI_DIVIDER_CLASS =
  "hidden h-[76px] w-[2px] shrink-0 self-center bg-op-card-border lg:block"

export const PERFORMANCE_KPI_CONTENT_CLASS =
  "flex min-w-0 w-full flex-col items-stretch gap-0.5 pb-[4.25px]"

export const PERFORMANCE_KPI_LABEL_CLASS =
  "m-0 text-op-sm font-medium leading-normal text-op-kpi-label-color"

/** Big value + metric icon — space-between on one row. */
export const PERFORMANCE_KPI_VALUE_ROW_CLASS =
  "flex w-full items-center justify-between gap-3"

export const PERFORMANCE_KPI_VALUE_CLASS =
  "m-0 text-op-xl font-extrabold leading-9 text-op-kpi-value-color"

export type KpiTrendTone = "positive" | "negative" | "neutral" | "unknown"

/**
 * Rounded percent change vs the equal-length previous period.
 * When previous is 0 and current has activity, treat as +100% (new growth).
 * Null only when both periods are empty (undefined %).
 */
export function computeKpiTrendPercent(
  current: number,
  previous: number
): number | null {
  if (previous === 0) {
    return current > 0 ? 100 : null
  }
  return Math.round(((current - previous) / previous) * 100)
}

/** Maps API trendPercent to display tone; null → placeholder row. */
export function resolveKpiTrendTone(trendPercent: number | null): KpiTrendTone {
  if (trendPercent == null) {
    return "unknown"
  }
  if (trendPercent > 0) {
    return "positive"
  }
  if (trendPercent < 0) {
    return "negative"
  }
  return "neutral"
}

/**
 * Trend percent for the label, or em dash when unknown.
 * Positive keeps `+`; negative is unsigned (tone/color + arrow carry the sign).
 */
export function formatKpiTrendPercentValue(trendPercent: number | null): string {
  if (trendPercent == null) {
    return "—"
  }
  if (trendPercent > 0) {
    return `+${trendPercent}`
  }
  return `${Math.abs(trendPercent)}`
}

export const PERFORMANCE_KPI_TREND_SUFFIX = "vs previous period"

export const PERFORMANCE_KPI_TREND_ROW_CLASS =
  "flex items-baseline gap-0.5 pt-[1.5px]"

export const PERFORMANCE_KPI_TREND_ICON_CLASS = "size-3 shrink-0"

export const PERFORMANCE_KPI_TREND_TEXT_CLASS =
  "m-0 text-op-kpi-info-size font-semibold leading-normal"

/** Figma KPI trend — success up; shared destructive down; muted when unknown/neutral. */
export function resolveKpiTrendTextClass(tone: KpiTrendTone): string {
  switch (tone) {
    case "positive":
      return "text-op-kpi-info-color"
    case "negative":
      return "text-destructive"
    default:
      return "text-op-kpi-label-color"
  }
}

export const PERFORMANCE_KPI_ICON_CLASS =
  "size-op-icon-md shrink-0 text-op-kpi-icon-color"
