import { describe, expect, it } from "vitest"

import {
  formatKpiTrendPercentValue,
  PERFORMANCE_DATE_BUTTON_CLASS,
  PERFORMANCE_KPI_DIVIDER_CLASS,
  PERFORMANCE_KPI_ICON_CLASS,
  PERFORMANCE_KPI_LABEL_CLASS,
  PERFORMANCE_KPI_ROW_CLASS,
  PERFORMANCE_KPI_TREND_SUFFIX,
  PERFORMANCE_KPI_VALUE_CLASS,
  PERFORMANCE_SECTION_CLASS,
  PERFORMANCE_SUBTITLE_CLASS,
  PERFORMANCE_TITLE_CLASS,
  resolveKpiTrendTextClass,
  resolveKpiTrendTone,
} from "./performanceOverviewPresentation"

describe("performanceOverviewPresentation", () => {
  it("uses responsive section chrome (stepped padding/gap, 6px radius, themed border/bg)", () => {
    expect(PERFORMANCE_SECTION_CLASS).toContain("p-4")
    expect(PERFORMANCE_SECTION_CLASS).toContain("sm:p-5")
    expect(PERFORMANCE_SECTION_CLASS).toContain("md:p-6")
    expect(PERFORMANCE_SECTION_CLASS).toContain("gap-6")
    expect(PERFORMANCE_SECTION_CLASS).toContain("sm:gap-8")
    expect(PERFORMANCE_SECTION_CLASS).toContain("md:gap-10")
    expect(PERFORMANCE_SECTION_CLASS).toContain("rounded-md")
    expect(PERFORMANCE_SECTION_CLASS).toContain("dark:bg-[#171717]")
    expect(PERFORMANCE_SECTION_CLASS).toContain("dark:border-[#262626]")
  })

  it("uses responsive header typography (18px→20px title, 14px subtitle)", () => {
    expect(PERFORMANCE_TITLE_CLASS).toContain("text-lg")
    expect(PERFORMANCE_TITLE_CLASS).toContain("sm:text-xl")
    expect(PERFORMANCE_SUBTITLE_CLASS).toContain("text-sm")
    expect(PERFORMANCE_SUBTITLE_CLASS).toContain("dark:text-[#7c7c7c]")
  })

  it("uses Figma date button metrics (17/11 padding, 12px label)", () => {
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("px-[17px]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("py-[11px]")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("text-xs")
    expect(PERFORMANCE_DATE_BUTTON_CLASS).toContain("dark:border-[#393939]")
  })

  it("uses responsive KPI layout (stack → 2×2 → row; lg+ dividers only)", () => {
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("grid-cols-1")
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("sm:grid-cols-2")
    expect(PERFORMANCE_KPI_ROW_CLASS).toContain("lg:flex")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("lg:block")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).not.toContain("sm:block")
  })

  it("uses Figma KPI metrics (14px label, 30px value, 2px divider, 22px icon)", () => {
    expect(PERFORMANCE_KPI_LABEL_CLASS).toContain("text-sm")
    expect(PERFORMANCE_KPI_LABEL_CLASS).toContain("#707070")
    expect(PERFORMANCE_KPI_VALUE_CLASS).toContain("text-[30px]")
    expect(PERFORMANCE_KPI_VALUE_CLASS).toContain("leading-9")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("w-[2px]")
    expect(PERFORMANCE_KPI_DIVIDER_CLASS).toContain("h-[76px]")
    expect(PERFORMANCE_KPI_ICON_CLASS).toContain("size-[22px]")
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

  it("uses green/red/muted trend colours per tone", () => {
    expect(resolveKpiTrendTextClass("positive")).toContain("#14a946")
    expect(resolveKpiTrendTextClass("negative")).toContain("#e5484d")
    expect(resolveKpiTrendTextClass("unknown")).toContain("#707070")
  })
})
