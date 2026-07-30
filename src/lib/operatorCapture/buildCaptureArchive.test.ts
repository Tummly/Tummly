import { describe, expect, it } from "vitest"

import {
  buildCaptureArchiveList,
  countActiveArchiveFilters,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  duplicateDigitalGuestLinkName,
  formatCaptureArchivePageRangeLabel,
  mapCaptureArchiveRows,
  resolveCaptureArchiveEmptyState,
} from "@/lib/operatorCapture/buildCaptureArchive"
import type { CaptureArchivedPlacementItem } from "@/types/dashboard"

const NOW = Date.parse("2026-07-30T12:00:00.000Z")

function archived(
  overrides: Partial<CaptureArchivedPlacementItem> &
    Pick<CaptureArchivedPlacementItem, "qrCodeId" | "qrType">
): CaptureArchivedPlacementItem {
  return {
    locationId: 1,
    locationName: "Camden",
    status: "Archived",
    linkName: null,
    channel: null,
    internalDescription: null,
    qrLinkUrl: `https://example.test/scan/${overrides.qrCodeId}`,
    archivedAt: "2026-07-24T10:00:00.000Z",
    archivedByDisplayName: "Mohamed Mahmoud",
    qrScans: 10,
    feedbackSubmitted: 5,
    lastScanAt: "2026-07-20T10:00:00.000Z",
    canRestore: true,
    ...overrides,
  }
}

describe("mapCaptureArchiveRows", () => {
  it("maps placement labels and metric texts operators expect", () => {
    const rows = mapCaptureArchiveRows(
      [
        archived({
          qrCodeId: 1,
          qrType: "DeliveryInsert",
          qrScans: 248,
          feedbackSubmitted: 176,
        }),
        archived({
          qrCodeId: 2,
          qrType: "DigitalGuestLink",
          linkName: "Summer promo",
          channel: "Email",
          canRestore: false,
        }),
      ],
      NOW
    )

    expect(rows[0]).toMatchObject({
      placementLabel: "Delivery insert",
      archivedOnText: "24 July 2026",
      archivedByText: "Mohamed Mahmoud",
      qrScansText: "248 scans",
      feedbackSubmittedText: "176 feedback submissions",
      canDuplicateAsNew: false,
      canRestore: true,
    })
    expect(rows[1]).toMatchObject({
      placementLabel: "Summer promo",
      canDuplicateAsNew: true,
      canRestore: false,
    })
  })
})

describe("resolveCaptureArchiveEmptyState", () => {
  it("reports true empty when totalCount is 0 with no query signals", () => {
    expect(
      resolveCaptureArchiveEmptyState({
        totalCount: 0,
        searchQuery: "",
        filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      })
    ).toEqual({ isTrueEmpty: true, isNoMatch: false })
  })

  it("reports no-match when totalCount is 0 with search or filters", () => {
    expect(
      resolveCaptureArchiveEmptyState({
        totalCount: 0,
        searchQuery: "zzz",
        filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      })
    ).toEqual({ isTrueEmpty: false, isNoMatch: true })

    expect(
      resolveCaptureArchiveEmptyState({
        totalCount: 0,
        searchQuery: "",
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds: [42],
        },
        showLocationFilter: true,
      })
    ).toEqual({ isTrueEmpty: false, isNoMatch: true })
  })
})

describe("buildCaptureArchiveList", () => {
  it("projects page rows and pager chrome from the server page", () => {
    const result = buildCaptureArchiveList({
      placements: [
        archived({ qrCodeId: 7, qrType: "CounterCard" }),
      ],
      totalCount: 26,
      page: 1,
      pageSize: 25,
      searchQuery: "",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      nowMs: NOW,
    })

    expect(result.rows).toHaveLength(1)
    expect(result.totalCount).toBe(26)
    expect(result.pageRangeLabel).toBe(
      "Showing 1–25 of 26 archived placements"
    )
    expect(result.canGoPrevious).toBe(false)
    expect(result.canGoNext).toBe(true)
    expect(result.isTrueEmpty).toBe(false)
    expect(result.isNoMatch).toBe(false)
  })

  it("disables next on the last page", () => {
    const result = buildCaptureArchiveList({
      placements: [archived({ qrCodeId: 1, qrType: "SmartGuest" })],
      totalCount: 26,
      page: 2,
      pageSize: 25,
      searchQuery: "",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      nowMs: NOW,
    })

    expect(result.pageRangeLabel).toBe(
      "Showing 26–26 of 26 archived placements"
    )
    expect(result.canGoPrevious).toBe(true)
    expect(result.canGoNext).toBe(false)
  })
})

describe("formatCaptureArchivePageRangeLabel", () => {
  it("formats zero and ranged labels", () => {
    expect(formatCaptureArchivePageRangeLabel(1, 25, 0)).toBe(
      "Showing 0 of 0 archived placements"
    )
    expect(formatCaptureArchivePageRangeLabel(1, 25, 10)).toBe(
      "Showing 1–10 of 10 archived placements"
    )
  })
})

describe("countActiveArchiveFilters", () => {
  it("counts location, type, date, and archived-by independently", () => {
    expect(
      countActiveArchiveFilters({
        locationIds: [1],
        placementTypes: ["CounterCard"],
        archivedDate: { preset: "last-7" },
        archivedByDisplayNames: ["Ada"],
      })
    ).toBe(4)
  })

  it("ignores location when the location filter chrome is hidden", () => {
    expect(
      countActiveArchiveFilters(
        {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          locationIds: [1],
        },
        { showLocationFilter: false }
      )
    ).toBe(0)
  })
})

describe("duplicateDigitalGuestLinkName", () => {
  it("appends (copy) to trimmed link names", () => {
    expect(duplicateDigitalGuestLinkName("Summer promo")).toBe(
      "Summer promo (copy)"
    )
    expect(duplicateDigitalGuestLinkName("  ")).toBe("(copy)")
  })
})
