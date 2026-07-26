import { describe, expect, it } from "vitest"

import {
  computeKpiTrendPercent,
  formatKpiTrendPercentValue,
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_TREND_SUFFIX,
  PERFORMANCE_KPI_TREND_TEXT_CLASS,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_SECTION_CLASS,
  PERFORMANCE_SUBTITLE_CLASS,
  PERFORMANCE_TITLE_CLASS,
  resolveKpiTrendTextClass,
  resolveKpiTrendTone,
} from "./performanceOverviewPresentation"

describe("performanceOverviewPresentation", () => {
  it("uses responsive section chrome (stepped padding/gap, card tokens)", () => {
    expect(PERFORMANCE_SECTION_CLASS).toContain("p-4")
    expect(PERFORMANCE_SECTION_CLASS).toContain("sm:p-5")
    expect(PERFORMANCE_SECTION_CLASS).toContain("md:p-6")
    expect(PERFORMANCE_SECTION_CLASS).toContain("gap-6")
    expect(PERFORMANCE_SECTION_CLASS).toContain("sm:gap-8")
    expect(PERFORMANCE_SECTION_CLASS).toContain("md:gap-10")
    expect(PERFORMANCE_SECTION_CLASS).toContain("rounded-op-lg")
    expect(PERFORMANCE_SECTION_CLASS).toContain("bg-op-card-background")
    expect(PERFORMANCE_SECTION_CLASS).toContain("border-op-card-border")
  })

  it("uses card title/subtitle tokens", () => {
    expect(PERFORMANCE_TITLE_CLASS).toContain("text-lg")
    expect(PERFORMANCE_TITLE_CLASS).toContain("sm:text-xl")
    expect(PERFORMANCE_TITLE_CLASS).toContain("text-op-card-title-color")
    expect(PERFORMANCE_SUBTITLE_CLASS).toContain("text-op-sm")
    expect(PERFORMANCE_SUBTITLE_CLASS).toContain("text-op-card-subtitle-color")
  })

  it("keeps date button layout hooks matching Sort outline chrome", () => {
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("shrink-0")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("gap-1.5")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("border-[#dcdcdc]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("text-[#171717]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("dark:text-[#a6a6a6]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("dark:border-[#393939]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("dark:hover:text-white")
  })

  it("uses responsive KPI layout (stack → 2×2 → row; lg+ dividers only)", () => {
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("grid-cols-1")
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("sm:grid-cols-2")
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("lg:flex")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("lg:block")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).not.toContain("sm:block")
  })

  it("uses KPI component tokens (label, value, divider, icon)", () => {
    expect(PERFORMANCE_KPI_LABEL_CLASS).toContain("text-op-sm")
    expect(PERFORMANCE_KPI_LABEL_CLASS).toContain("text-op-kpi-label-color")
    expect(PERFORMANCE_KPI_VALUE_CLASS).toContain("text-op-xl")
    expect(PERFORMANCE_KPI_VALUE_CLASS).toContain("text-op-kpi-value-color")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("w-[2px]")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("h-[76px]")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("bg-op-card-border")
    expect(PERFORMANCE_KPI_ICON_CLASS).toContain("size-op-icon-md")
    expect(PERFORMANCE_KPI_ICON_CLASS).toContain("text-op-kpi-icon-color")
  })

  it("computes rounded percent change vs previous period", () => {
    expect(computeKpiTrendPercent(12, 10)).toBe(20)
    expect(computeKpiTrendPercent(5, 10)).toBe(-50)
    expect(computeKpiTrendPercent(10, 10)).toBe(0)
    expect(computeKpiTrendPercent(1, 3)).toBe(-67)
    expect(computeKpiTrendPercent(8, 0)).toBe(100)
    expect(computeKpiTrendPercent(0, 0)).toBeNull()
  })

  it("resolves KPI trend tone and signed percent labels", () => {
    expect(resolveKpiTrendTone(12)).toBe("positive")
    expect(resolveKpiTrendTone(-8)).toBe("negative")
    expect(resolveKpiTrendTone(0)).toBe("neutral")
    expect(resolveKpiTrendTone(null)).toBe("unknown")
    expect(formatKpiTrendPercentValue(12)).toBe("+12")
    expect(formatKpiTrendPercentValue(-8)).toBe("-8")
    expect(formatKpiTrendPercentValue(0)).toBe("0")
    expect(formatKpiTrendPercentValue(null)).toBe("—")
    expect(PERFORMANCE_KPI_TREND_SUFFIX).toBe("vs previous period")
  })

  it("uses tokenised trend colours per tone", () => {
    expect(resolveKpiTrendTextClass("positive")).toContain("text-op-kpi-info-color")
    expect(resolveKpiTrendTextClass("negative")).toContain("text-destructive")
    expect(resolveKpiTrendTextClass("unknown")).toContain("text-op-kpi-label-color")
  })

  it("uses KPI info size token for trend % copy (Figma 12px)", () => {
    expect(PERFORMANCE_KPI_TREND_TEXT_CLASS).toContain("text-op-kpi-info-size")
  })
})
