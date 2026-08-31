import { describe, expect, it } from "vitest"

import {
  buildLocationsKpis,
  formatLocationsLastActivityAt,
  formatLocationsPageRange,
  formatNeedsAttentionSubtitle,
  locationLifecycleBadgeVariant,
  locationRowActionsForLifecycle,
  resolveLocationsTabId,
} from "@/lib/operatorLocations/locationsPresentation"

describe("locationRowActionsForLifecycle", () => {
  it("returns Active location actions (Archive only from Paused)", () => {
    expect(
      locationRowActionsForLifecycle("active").map((action) => action.id)
    ).toEqual([
      "view-location",
      "edit-location",
      "set-manager",
      "view-qr-placements",
      "view-feedback",
      "view-reports",
      "pause-location",
    ])
  })

  it("returns Draft location actions from Figma", () => {
    expect(
      locationRowActionsForLifecycle("draft").map((action) => action.id)
    ).toEqual([
      "continue-setup",
      "edit-location",
      "set-manager",
      "delete-draft",
    ])
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

describe("formatLocationsLastActivityAt", () => {
  const now = new Date("2026-08-31T15:00:00.000Z")

  it("formats Today and Yesterday and empty as em dash", () => {
    expect(formatLocationsLastActivityAt(null, now)).toBe("—")
    expect(
      formatLocationsLastActivityAt("2026-08-31T12:42:00.000Z", now)
    ).toMatch(/^Today,/)
    expect(
      formatLocationsLastActivityAt("2026-08-30T15:05:00.000Z", now)
    ).toMatch(/^Yesterday,/)
  })
})
