import { afterEach, describe, expect, it, vi } from "vitest"

import { createOperatorLocationsPageModule } from "@/lib/operatorLocations/createOperatorLocationsPageModule"
import type { LocationsListResponse } from "@/lib/operatorLocations/locationsListQueryParams"

function apiResponse(
  overrides: Partial<LocationsListResponse> = {}
): LocationsListResponse {
  return {
    success: true,
    rows: [
      {
        id: 10,
        name: "Alpha Venue",
        lifecycleStatus: "active",
        setupStatus: "ready",
        managerName: "Aisha Khan",
        city: "Camden",
        postcode: "NW1 1AA",
        cityId: "camden",
        cityPostcode: "Camden, NW1 1AA",
        lastActivityAt: "2026-08-31T12:42:00.000Z",
        searchText: "alpha venue camden nw1 1aa",
      },
      {
        id: 11,
        name: "Beta Venue",
        lifecycleStatus: "draft",
        setupStatus: "not-started",
        managerName: null,
        city: "Soho",
        postcode: "W1D 1AA",
        cityId: "soho",
        cityPostcode: "Soho, W1D 1AA",
        lastActivityAt: null,
        searchText: "beta venue soho w1d 1aa",
      },
    ],
    totalCount: 2,
    page: 1,
    pageSize: 10,
    kpis: {
      active: 1,
      draft: 1,
      paused: 0,
      setupNeedsAttention: 0,
    },
    cityFacets: [
      { id: "camden", label: "Camden" },
      { id: "soho", label: "Soho" },
    ],
    ...overrides,
  }
}

describe("createOperatorLocationsPageModule", () => {
  afterEach(() => {
    vi.useRealTimers()
  })

  it("maps API rows and KPIs into the snapshot without demo names", async () => {
    const getList = vi.fn(async () => apiResponse())
    const module = createOperatorLocationsPageModule(
      {
        getList,
        debounceMs: 0,
        getNow: () => new Date("2026-08-31T15:00:00.000Z"),
      },
      {}
    )

    await module.load()

    const snap = module.getSnapshot()
    expect(snap.rows.map((row) => row.name)).toEqual([
      "Alpha Venue",
      "Beta Venue",
    ])
    expect(snap.rows.some((row) => row.name.includes("Mehmet"))).toBe(false)
    expect(snap.rows[0]?.managerName).toBe("Aisha Khan")
    expect(snap.rows[1]?.managerName).toBe("—")
    expect(snap.rows[1]?.lastActivityLabel).toBe("—")
    expect(snap.kpis.map((kpi) => kpi.primaryText)).toEqual([
      "1",
      "1",
      "0",
      "0",
    ])
    expect(snap.cityFilterOptions).toEqual([
      { id: "camden", label: "Camden" },
      { id: "soho", label: "Soho" },
    ])
    expect(snap.totalFilteredCount).toBe(2)
    expect(snap.setupAttentionItems).toEqual([])
    expect(snap.activityItems).toEqual([])
    expect(getList).toHaveBeenCalledTimes(1)
  })

  it("keeps getSnapshot identity until emit", async () => {
    const getList = vi.fn(async () => apiResponse())
    const module = createOperatorLocationsPageModule({
      getList,
      debounceMs: 0,
    })
    const before = module.getSnapshot()
    expect(module.getSnapshot()).toBe(before)
    await module.load()
    const after = module.getSnapshot()
    expect(after).not.toBe(before)
    expect(module.getSnapshot()).toBe(after)
  })

  it("refetches with query params when search changes", async () => {
    vi.useFakeTimers()
    const getList = vi.fn(async () => apiResponse({ totalCount: 0, rows: [] }))
    const module = createOperatorLocationsPageModule({
      getList,
      debounceMs: 50,
    })
    await module.load()
    getList.mockClear()

    module.setSearchQuery("Soho")
    await vi.advanceTimersByTimeAsync(50)
    await Promise.resolve()

    expect(getList).toHaveBeenCalledWith(
      expect.objectContaining({ q: "Soho", page: 1 })
    )
  })

  it("refetches when sort changes", async () => {
    const getList = vi.fn(async () => apiResponse())
    const module = createOperatorLocationsPageModule({
      getList,
      debounceMs: 0,
    })
    await module.load()
    getList.mockClear()

    module.setSortId("name-desc")
    await Promise.resolve()

    expect(getList).toHaveBeenCalledWith(
      expect.objectContaining({ sort: "name-desc", page: 1 })
    )
  })

  it("sets error loadStatus when getList fails", async () => {
    const getList = vi.fn(async () => {
      throw new Error("network")
    })
    const module = createOperatorLocationsPageModule({
      getList,
      debounceMs: 0,
    })
    await module.load()
    expect(module.getSnapshot().loadStatus).toBe("error")
    expect(module.getSnapshot().rows).toEqual([])
  })
})
