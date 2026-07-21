import { describe, expect, it } from "vitest"

import {
  HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL,
  inclusiveLocalDateSpanDays,
  isHomePerformanceCustomSpanAllowed,
  labelForHomePerformanceDateRange,
  resolveHomePerformanceWindow,
  toLocalDateKey,
} from "./homePerformanceDateRange"

describe("homePerformanceDateRange custom", () => {
  it("exposes the default preset label for shared unwired chrome", () => {
    expect(HOME_PERFORMANCE_DEFAULT_DATE_RANGE_LABEL).toBe("Last 7 days")
  })

  it("labels a multi-day Custom range compactly", () => {
    expect(
      labelForHomePerformanceDateRange({
        kind: "custom",
        startDate: "2026-07-12",
        endDate: "2026-07-18",
      })
    ).toBe("12–18 Jul 2026")
  })

  it("labels a same-day Custom range as a single date", () => {
    expect(
      labelForHomePerformanceDateRange({
        kind: "custom",
        startDate: "2026-07-12",
        endDate: "2026-07-12",
      })
    ).toBe("12 Jul 2026")
  })

  it("resolves Custom bounds as inclusive local end day via exclusive to", () => {
    const { from, to } = resolveHomePerformanceWindow({
      kind: "custom",
      startDate: "2026-07-12",
      endDate: "2026-07-18",
    })
    expect(toLocalDateKey(from)).toBe("2026-07-12")
    expect(toLocalDateKey(to)).toBe("2026-07-19")
    expect(from.getHours()).toBe(0)
    expect(to.getHours()).toBe(0)
  })

  it("allows an inclusive 180-day Custom span and rejects 181", () => {
    expect(inclusiveLocalDateSpanDays("2026-01-01", "2026-06-29")).toBe(180)
    expect(
      isHomePerformanceCustomSpanAllowed("2026-01-01", "2026-06-29")
    ).toBe(true)
    expect(
      isHomePerformanceCustomSpanAllowed("2026-01-01", "2026-06-30")
    ).toBe(false)
  })
})
