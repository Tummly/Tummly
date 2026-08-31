import { describe, expect, it } from "vitest"

import { createOperatorLocationsPageModule } from "@/lib/operatorLocations/createOperatorLocationsPageModule"
import {
  buildLocationsKpis,
  formatLocationsPageRange,
  formatNeedsAttentionSubtitle,
  locationLifecycleBadgeVariant,
  locationRowActionsForLifecycle,
  resolveLocationsTabId,
} from "@/lib/operatorLocations/locationsPresentation"

describe("locationRowActionsForLifecycle", () => {
  it("returns Active location actions from Figma", () => {
    expect(
      locationRowActionsForLifecycle("active").map((action) => action.id)
    ).toEqual([
      "view-location",
      "edit-location",
      "view-qr-placements",
      "view-feedback",
      "view-reports",
      "pause-location",
      "archive-location",
    ])
  })

  it("returns Draft location actions from Figma", () => {
    expect(
      locationRowActionsForLifecycle("draft").map((action) => action.id)
    ).toEqual(["continue-setup", "edit-location", "delete-draft"])
  })

  it("returns Paused location actions from Figma", () => {
    expect(
      locationRowActionsForLifecycle("paused").map((action) => action.id)
    ).toEqual([
      "view-location",
      "edit-location",
      "view-historical-activity",
      "resume-location",
      "archive-location",
    ])
  })

  it("returns Archived location actions from Figma", () => {
    expect(
      locationRowActionsForLifecycle("archived").map((action) => action.id)
    ).toEqual([
      "view-historical-record",
      "restore-location",
      "export-location-history",
    ])
  })
})

describe("locationLifecycleBadgeVariant", () => {
  it("uses chips for Active Draft Paused and plain text for Archived", () => {
    expect(locationLifecycleBadgeVariant("active")).toBe("positive")
    expect(locationLifecycleBadgeVariant("draft")).toBe("soft")
    expect(locationLifecycleBadgeVariant("paused")).toBe("neutral")
    expect(locationLifecycleBadgeVariant("archived")).toBeNull()
  })
})

describe("buildLocationsKpis", () => {
  it("maps summary counts into KPI cells", () => {
    expect(
      buildLocationsKpis({
        active: 0,
        draft: 3,
        paused: 1,
        setupNeedsAttention: 0,
      })
    ).toEqual([
      {
        id: "active-locations",
        label: "Active locations",
        primaryText: "0",
      },
      {
        id: "draft-locations",
        label: "Draft locations",
        primaryText: "3",
      },
      {
        id: "paused-locations",
        label: "Paused locations",
        primaryText: "1",
      },
      {
        id: "setup-needs-attention",
        label: "Setup needs attention",
        primaryText: "0",
      },
    ])
  })
})

describe("formatLocationsPageRange", () => {
  it("formats the pagination label", () => {
    expect(
      formatLocationsPageRange({ page: 1, pageSize: 10, totalCount: 5 })
    ).toBe("Showing 1–5 of 5 locations")
  })
})

describe("resolveLocationsTabId", () => {
  it("defaults unknown tabs to locations", () => {
    expect(resolveLocationsTabId(null)).toBe("locations")
    expect(resolveLocationsTabId("setup-readiness")).toBe("setup-readiness")
  })
})

describe("formatNeedsAttentionSubtitle", () => {
  it("uses singular copy for one location", () => {
    expect(formatNeedsAttentionSubtitle(1)).toBe("1 location needs attention")
  })

  it("uses plural copy for other counts", () => {
    expect(formatNeedsAttentionSubtitle(2)).toBe(
      "2 locations need attention"
    )
  })
})

describe("createOperatorLocationsPageModule", () => {
  it("sorts by location name A–Z by default", () => {
    const module = createOperatorLocationsPageModule({
      rows: [
        {
          id: "b",
          name: "Beta",
          lifecycleStatus: "active",
          setupStatus: "ready",
          managerName: "A",
          cityPostcode: "Camden, London",
          cityId: "camden",
          lastActivityLabel: "Today",
          searchText: "beta camden",
        },
        {
          id: "a",
          name: "Alpha",
          lifecycleStatus: "draft",
          setupStatus: "ready",
          managerName: "A",
          cityPostcode: "Soho, London",
          cityId: "soho",
          lastActivityLabel: "Today",
          searchText: "alpha soho",
        },
      ],
      setupNeedsAttentionCount: 0,
    })

    expect(module.getSnapshot().rows.map((row) => row.name)).toEqual([
      "Alpha",
      "Beta",
    ])

    module.setSortId("name-desc")
    expect(module.getSnapshot().rows.map((row) => row.name)).toEqual([
      "Beta",
      "Alpha",
    ])
  })

  it("filters by search and lifecycle chip selection", () => {
    const module = createOperatorLocationsPageModule({
      setupNeedsAttentionCount: 2,
    })

    module.setSearchQuery("Soho")
    expect(module.getSnapshot().rows).toHaveLength(1)
    expect(module.getSnapshot().rows[0]?.name).toContain("Soho")

    module.clearSearchAndFilters()
    module.openFilters()
    const session = module.getSnapshot().filtersSession
    expect(session).not.toBeNull()
    if (session == null) {
      return
    }
    module.setFiltersSession({
      ...session,
      pending: {
        ...session.pending,
        lifecycle: { kind: "multi-select", ids: ["paused"] },
      },
    })
    module.applyFilters()
    expect(module.getSnapshot().rows).toHaveLength(1)
    expect(module.getSnapshot().rows[0]?.lifecycleStatus).toBe("paused")
    expect(module.getSnapshot().filterChipCount).toBe(1)
  })

  it("exposes Setup & readiness tab count", () => {
    const module = createOperatorLocationsPageModule({
      setupNeedsAttentionCount: 2,
    })
    const setupTab = module
      .getSnapshot()
      .tabs.find((tab) => tab.id === "setup-readiness")
    expect(setupTab?.count).toBe(2)
  })

  it("exposes Needs attention and Activity seed rows", () => {
    const module = createOperatorLocationsPageModule()
    const snap = module.getSnapshot()
    expect(snap.setupAttentionItems).toHaveLength(2)
    expect(snap.setupAttentionItems[0]?.message).toContain("privacy review")
    expect(snap.activityItems.length).toBeGreaterThan(0)
    expect(snap.activityItems[0]?.timeLabel).toBe("Today, 10:42")
  })
})
