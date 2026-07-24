/** Figma Performance overview section — node 3353:42472 (light) / 3062:5973 (dark). */

export const PERFORMANCE_SECTION_CLASS =
  "flex flex-col gap-6 overflow-clip rounded-md border border-[#dcdcdc] bg-[var(--operator-card)] p-4 sm:gap-8 sm:p-5 md:gap-10 md:p-6 dark:border-[#262626] dark:shadow-none"

export const PERFORMANCE_HEADER_ROW_CLASS =
  "flex items-center justify-between gap-4"

export const PERFORMANCE_HEADER_COPY_CLASS = "flex flex-col gap-2 leading-[0]"

export const PERFORMANCE_TITLE_CLASS =
  "m-0 text-lg font-bold leading-normal text-foreground sm:text-xl"

export const PERFORMANCE_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Date-range chrome — shared sizing; pair with enabled/disabled colour classes. */
export const PERFORMANCE_DATE_BUTTON_CLASS =
  "h-auto min-h-0 shrink-0 gap-1.5 rounded border-[#dcdcdc] bg-transparent px-4 py-2.5 text-xs font-medium leading-[18px] opacity-100 disabled:opacity-100 dark:border-[#393939] dark:bg-transparent"

/** Disabled date-range colours — matches Figma Button Date (unavailable). */
export const PERFORMANCE_DATE_BUTTON_DISABLED_CLASS =
  "text-[#a6a6a6]"

/** Enabled date-range colours for the live Performance overview control. */
export const PERFORMANCE_DATE_BUTTON_ENABLED_CLASS =
  "text-foreground hover:bg-accent/50"

export const PERFORMANCE_DATE_PRESET_LIST_CLASS = "flex flex-col gap-0.5"

export const PERFORMANCE_DATE_PRESET_ITEM_CLASS =
  "h-auto w-full justify-start rounded-md px-2.5 py-1.5 text-sm font-normal text-foreground"

export const PERFORMANCE_DATE_PRESET_ITEM_ACTIVE_CLASS =
  "bg-accent font-medium"

export const PERFORMANCE_DATE_CUSTOM_ACTIONS_CLASS =
  "flex items-center justify-between gap-2"

export const PERFORMANCE_DATE_CUSTOM_HINT_CLASS =
  "m-0 px-1 text-xs text-muted-foreground"

export const PERFORMANCE_DATE_ICON_CLASS = "size-3.5 shrink-0"

export const PERFORMANCE_KPI_STRIP_CLASS = "rounded-sm px-5 py-[30px]"

export const PERFORMANCE_KPI_ROW_CLASS =
  "grid grid-cols-1 gap-6 sm:grid-cols-2 lg:flex lg:flex-row lg:items-center lg:gap-[30px]"

export const PERFORMANCE_KPI_CELL_CLASS =
  "flex min-w-0 flex-1 items-center justify-between gap-3"

export const PERFORMANCE_KPI_DIVIDER_CLASS =
  "hidden h-[76px] w-[2px] shrink-0 self-center bg-[#dcdcdc] lg:block dark:bg-[#262626]"

export const PERFORMANCE_KPI_CONTENT_CLASS =
  "flex min-w-0 flex-col items-start gap-0.5 pb-[4.25px]"

export const PERFORMANCE_KPI_LABEL_CLASS =
  "m-0 text-sm font-medium leading-normal text-[#707070]"

export const PERFORMANCE_KPI_VALUE_CLASS =
  "m-0 text-[30px] font-extrabold leading-9 text-foreground"

export type KpiTrendTone = "positive" | "negative" | "neutral" | "unknown"

/**
 * Rounded percent change vs the equal-length previous period.
 * Null when previous is 0 (undefined % — matches honest-empty KPI posture).
 */
export function computeKpiTrendPercent(
  current: number,
  previous: number
): number | null {
  if (previous === 0) {
    return null
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

/** Signed percent for the trend label, or em dash when unknown. */
export function formatKpiTrendPercentValue(trendPercent: number | null): string {
  if (trendPercent == null) {
    return "—"
  }
  if (trendPercent > 0) {
    return `+${trendPercent}`
  }
  return `${trendPercent}`
}

export const PERFORMANCE_KPI_TREND_SUFFIX = "vs previous period"

export const PERFORMANCE_KPI_TREND_ROW_CLASS =
  "flex items-center gap-0.5 pt-[1.5px]"

export const PERFORMANCE_KPI_TREND_ICON_CLASS = "size-3 shrink-0"

export const PERFORMANCE_KPI_TREND_TEXT_CLASS =
  "m-0 text-xs font-semibold leading-normal"

/** Figma KPI trend — green #14a946 up, red down, muted when unknown/neutral. */
export function resolveKpiTrendTextClass(tone: KpiTrendTone): string {
  switch (tone) {
    case "positive":
      return "text-[#14a946]"
    case "negative":
      return "text-[#e5484d]"
    default:
      return "text-[#707070]"
  }
}

export const PERFORMANCE_KPI_ICON_CLASS =
  "size-[22px] shrink-0 text-[#707070]"
