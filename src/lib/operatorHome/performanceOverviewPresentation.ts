/** Figma Performance overview section — node 3353:42472 (light) / 3062:5973 (dark). */

export const PERFORMANCE_SECTION_CLASS =
  "flex flex-col gap-10 overflow-clip rounded-md border border-[#dcdcdc] bg-white p-6 dark:border-[#262626] dark:bg-[#171717] dark:shadow-none"

export const PERFORMANCE_HEADER_ROW_CLASS =
  "flex items-center justify-between gap-4"

export const PERFORMANCE_HEADER_COPY_CLASS = "flex flex-col gap-2 leading-[0]"

export const PERFORMANCE_TITLE_CLASS =
  "m-0 text-xl font-bold leading-normal text-foreground"

export const PERFORMANCE_SUBTITLE_CLASS =
  "m-0 text-sm font-medium leading-normal text-muted-foreground dark:text-[#7c7c7c]"

/** Disabled date-range chrome — matches Figma Button Date (no picker wired). */
export const PERFORMANCE_DATE_BUTTON_CLASS =
  "h-auto min-h-0 shrink-0 gap-1.5 rounded border-[#dcdcdc] bg-transparent px-[17px] py-[11px] text-xs font-medium leading-[18px] text-[#a6a6a6] opacity-100 disabled:opacity-100 dark:border-[#393939]"

export const PERFORMANCE_DATE_ICON_CLASS = "size-3.5 shrink-0"

export const PERFORMANCE_KPI_STRIP_CLASS = "rounded-sm px-5 py-[30px]"

export const PERFORMANCE_KPI_ROW_CLASS =
  "flex flex-col gap-6 sm:flex-row sm:items-center sm:gap-[30px]"

export const PERFORMANCE_KPI_CELL_CLASS =
  "flex min-w-0 flex-1 items-center justify-between gap-3"

export const PERFORMANCE_KPI_DIVIDER_CLASS =
  "hidden h-[76px] w-[2px] shrink-0 self-center bg-[#dcdcdc] sm:block dark:bg-[#262626]"

export const PERFORMANCE_KPI_CONTENT_CLASS =
  "flex flex-col items-start gap-0.5 pb-[4.25px]"

export const PERFORMANCE_KPI_LABEL_CLASS =
  "m-0 text-sm font-medium leading-normal text-[#707070]"

export const PERFORMANCE_KPI_VALUE_CLASS =
  "m-0 text-[30px] font-extrabold leading-9 text-foreground"

export type KpiTrendTone = "positive" | "negative" | "neutral" | "unknown"

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
