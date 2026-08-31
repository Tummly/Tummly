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
    // Setup/Activity remain UI seeds until tickets 02 / 06.
    expect(snap.setupAttentionItems.length).toBeGreaterThan(0)
    expect(snap.activityItems.length).toBeGreaterThan(0)
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

  it("refreshes the list after createDraft succeeds", async () => {
    const getList = vi.fn(async () => apiResponse())
    const createDraft = vi.fn(async () => undefined)
    const module = createOperatorLocationsPageModule({
      getList,
      createDraft,
      debounceMs: 0,
    })
    await module.load()
    getList.mockClear()

    await module.createDraft({
      locationName: "New Draft",
      address: "1 High Street",
      city: "Leeds",
      postcode: "LS1 1AA",
    })

    expect(createDraft).toHaveBeenCalledTimes(1)
    expect(getList).toHaveBeenCalledTimes(1)
  })

  it("refreshes the list after activateDraft and deleteDraft", async () => {
    const getList = vi.fn(async () => apiResponse())
    const activateDraft = vi.fn(async () => undefined)
    const deleteDraft = vi.fn(async () => undefined)
    const module = createOperatorLocationsPageModule({
      getList,
      activateDraft,
      deleteDraft,
      debounceMs: 0,
    })
    await module.load()
    getList.mockClear()

    await module.activateDraft("11")
    await module.deleteDraft("11")

    expect(activateDraft).toHaveBeenCalledWith("11")
    expect(deleteDraft).toHaveBeenCalledWith("11")
    expect(getList).toHaveBeenCalledTimes(2)
  })

  it("refreshes the list after setManager", async () => {
    const getList = vi.fn(async () => apiResponse())
    const setManager = vi.fn(async () => undefined)
    const module = createOperatorLocationsPageModule({
      getList,
      setManager,
      debounceMs: 0,
    })
    await module.load()
    getList.mockClear()

    await module.setManager("10", 42)
    await module.setManager("10", null)

    expect(setManager).toHaveBeenNthCalledWith(1, "10", 42)
    expect(setManager).toHaveBeenNthCalledWith(2, "10", null)
    expect(getList).toHaveBeenCalledTimes(2)
  })

  it.each([
    ["pause-location", "pause", "paused"] as const,
    ["resume-location", "resume", "active"] as const,
    ["archive-location", "archive", "archived"] as const,
    ["restore-location", "restore", "paused"] as const,
  ])(
    "%s mutates lifecycle then refreshes the snapshot",
    async (actionId, apiAction, nextStatus) => {
      const getList = vi
        .fn()
        .mockResolvedValueOnce(apiResponse())
        .mockResolvedValueOnce(
          apiResponse({
            rows: [
              {
                id: 10,
                name: "Alpha Venue",
                lifecycleStatus: nextStatus,
                setupStatus: "ready",
                managerName: "Aisha Khan",
                city: "Camden",
                postcode: "NW1 1AA",
                cityId: "camden",
                cityPostcode: "Camden, NW1 1AA",
                lastActivityAt: "2026-08-31T12:42:00.000Z",
                searchText: "alpha venue camden nw1 1aa",
              },
            ],
            kpis: {
              active: nextStatus === "active" ? 1 : 0,
              draft: 1,
              paused: nextStatus === "paused" ? 1 : 0,
              setupNeedsAttention: 0,
            },
          })
        )
      const mutateLifecycle = vi.fn(async () => undefined)
      const module = createOperatorLocationsPageModule({
        getList,
        mutateLifecycle,
        debounceMs: 0,
      })
      await module.load()

      await module.onRowAction("10", actionId)
      await Promise.resolve()
      await Promise.resolve()

      expect(mutateLifecycle).toHaveBeenCalledWith(10, apiAction)
      expect(getList).toHaveBeenCalledTimes(2)
      expect(module.getSnapshot().rows[0]?.lifecycleStatus).toBe(nextStatus)
    }
  )
})
