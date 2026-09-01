import { describe, expect, it, vi } from "vitest"

import { createOperatorLocationDetailPageModule } from "@/lib/operatorLocations/createOperatorLocationDetailPageModule"
import type { LocationsListResponse } from "@/lib/operatorLocations/locationsListQueryParams"
import {
  formatLocationDetailHeaderMeta,
  formatLocationDetailMonthMetric,
  resolveLocationDetailTabId,
} from "@/lib/operatorLocations/locationDetailPresentation"

function listResponse(
  overrides: Partial<LocationsListResponse> = {}
): LocationsListResponse {
  return {
    success: true,
    rows: [
      {
        id: 42,
        name: "KFC Chicken — Camden",
        lifecycleStatus: "active",
        setupStatus: "ready",
        managerName: "Aisha",
        city: "Camden",
        postcode: "NW1 1AA",
        cityId: "camden",
        cityPostcode: "Camden, NW1 1AA",
        lastActivityAt: null,
      },
    ],
    totalCount: 1,
    page: 1,
    pageSize: 100,
    kpis: {
      active: 1,
      draft: 0,
      paused: 0,
      setupNeedsAttention: 0,
    },
    cityFacets: [],
    ...overrides,
  }
}

describe("resolveLocationDetailTabId", () => {
  it("accepts known tabs and defaults to overview", () => {
    expect(resolveLocationDetailTabId("setup-details")).toBe("setup-details")
    expect(resolveLocationDetailTabId("guest-loop")).toBe("guest-loop")
    expect(resolveLocationDetailTabId("nope")).toBe("overview")
    expect(resolveLocationDetailTabId(null)).toBe("overview")
  })
})

describe("formatLocationDetailHeaderMeta", () => {
  it("formats city, QR count and guest count", () => {
    expect(
      formatLocationDetailHeaderMeta({
        city: "Camden",
        qrCount: 6,
        guestCount: 842,
      })
    ).toBe("Camden · 6 QR codes · 842 guests captured")
  })

  it("uses singular labels for one", () => {
    expect(
      formatLocationDetailHeaderMeta({
        city: "Soho",
        qrCount: 1,
        guestCount: 1,
      })
    ).toBe("Soho · 1 QR code · 1 guest captured")
  })
})

describe("formatLocationDetailMonthMetric", () => {
  it("appends this month with grouped thousands", () => {
    expect(formatLocationDetailMonthMetric(1204)).toBe("1,204 this month")
  })
})

describe("createOperatorLocationDetailPageModule", () => {
  it("keeps getSnapshot identity until emit", async () => {
    const getList = vi.fn().mockResolvedValue(listResponse())
    const pageModule = createOperatorLocationDetailPageModule(42, { getList })

    expect(pageModule.getSnapshot()).toBe(pageModule.getSnapshot())

    await pageModule.load()

    expect(pageModule.getSnapshot()).toBe(pageModule.getSnapshot())
    expect(pageModule.getSnapshot().name).toBe("KFC Chicken — Camden")
    expect(pageModule.getSnapshot().city).toBe("Camden")
    expect(pageModule.getSnapshot().setupChecklist.locationDetailsAdded).toBe(
      "complete"
    )
    expect(pageModule.getSnapshot().setupChecklist.teamAccessAssigned).toBe(
      "complete"
    )
    expect(pageModule.getSnapshot().teamAccessRows).toEqual([
      {
        id: "location-manager",
        name: "Aisha",
        role: "Manager",
        accessLabel: "This location only",
        lastActiveLabel: "—",
      },
    ])
    expect(pageModule.getSnapshot().locationControlsStatus.locationStatus).toBe(
      "Active"
    )
    expect(pageModule.getSnapshot().locationControlsActions[0]?.id).toBe(
      "pause"
    )
    expect(pageModule.getSnapshot().loadStatus).toBe("loaded")
  })

  it("marks not-found when the list has no matching row", async () => {
    const getList = vi.fn().mockResolvedValue(listResponse({ rows: [] }))
    const pageModule = createOperatorLocationDetailPageModule(99, { getList })

    await pageModule.load()

    expect(pageModule.getSnapshot().loadStatus).toBe("not-found")
  })

  it("changes tabs through requestTabChange", () => {
    const pageModule = createOperatorLocationDetailPageModule(42, {
      getList: vi.fn(),
    })

    pageModule.requestTabChange("team-access")
    expect(pageModule.getSnapshot().activeTabId).toBe("team-access")
  })
})
