import { describe, expect, it, vi } from "vitest"

import {
  loadOfferDetailsOverviewMetrics,
  resolveOfferDetailsWindow,
} from "@/lib/operatorOffers/offerDetailsMetricsQuery"
import { toLocalDateKey } from "@/lib/operatorHome/homePerformanceDateRange"

describe("resolveOfferDetailsWindow", () => {
  const now = new Date(2026, 7, 12, 15, 30, 0)

  it("resolves Last 7 days as local start 6 days ago through now", () => {
    const { from, to } = resolveOfferDetailsWindow(
      { kind: "preset", presetId: "last7" },
      now
    )
    expect(toLocalDateKey(from)).toBe("2026-08-06")
    expect(from.getHours()).toBe(0)
    expect(to).toEqual(now)
  })

  it("resolves Last 30 days as local start 29 days ago through now", () => {
    const { from, to } = resolveOfferDetailsWindow(
      { kind: "preset", presetId: "last30" },
      now
    )
    expect(toLocalDateKey(from)).toBe("2026-07-14")
    expect(to).toEqual(now)
  })

  it("resolves Last 90 days as local start 89 days ago through now", () => {
    const { from, to } = resolveOfferDetailsWindow(
      { kind: "preset", presetId: "last90" },
      now
    )
    expect(toLocalDateKey(from)).toBe("2026-05-15")
    expect(to).toEqual(now)
  })

  it("resolves Custom as inclusive local end day via exclusive to", () => {
    const { from, to } = resolveOfferDetailsWindow({
      kind: "custom",
      startDate: "2026-07-12",
      endDate: "2026-07-18",
    })
    expect(toLocalDateKey(from)).toBe("2026-07-12")
    expect(toLocalDateKey(to)).toBe("2026-07-19")
    expect(from.getHours()).toBe(0)
    expect(to.getHours()).toBe(0)
  })
})

describe("loadOfferDetailsOverviewMetrics", () => {
  it("calls metrics API with ISO [from, to) and maps counts for Overview KPIs", async () => {
    const now = new Date(2026, 7, 12, 15, 30, 0)
    const fetchMetrics = vi.fn().mockResolvedValue({
      success: true,
      claims: 40,
      redemptions: 10,
      redemptionRate: 0.25,
      expiredUnused: 3,
      failedAttempts: 2,
    })

    const metrics = await loadOfferDetailsOverviewMetrics(
      10,
      { kind: "preset", presetId: "last7" },
      { fetchMetrics, now }
    )

    expect(fetchMetrics).toHaveBeenCalledWith(10, {
      from: new Date(2026, 7, 6).toISOString(),
      to: now.toISOString(),
    })
    expect(metrics).toEqual({
      claims: 40,
      redemptions: 10,
      expiredUnused: 3,
      failedAttempts: 2,
    })
  })

  it("throws when metrics response is not successful", async () => {
    const fetchMetrics = vi.fn().mockResolvedValue({
      success: false,
      claims: 0,
      redemptions: 0,
      redemptionRate: null,
      expiredUnused: 0,
      failedAttempts: 0,
    })

    await expect(
      loadOfferDetailsOverviewMetrics(
        10,
        { kind: "preset", presetId: "last7" },
        { fetchMetrics }
      )
    ).rejects.toThrow("Offer metrics get failed.")
  })
})
