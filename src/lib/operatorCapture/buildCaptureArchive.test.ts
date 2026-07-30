import { describe, expect, it } from "vitest"

import {
  buildCaptureArchiveList,
  countActiveArchiveFilters,
  DEFAULT_CAPTURE_ARCHIVE_FILTERS,
  duplicateDigitalGuestLinkName,
  type CaptureArchiveFilters,
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

describe("buildCaptureArchiveList", () => {
  it("reports true empty when there are no archived codes", () => {
    const result = buildCaptureArchiveList({
      facts: [],
      searchQuery: "",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      sort: "recently-archived",
      nowMs: NOW,
    })

    expect(result).toMatchObject({
      rows: [],
      isTrueEmpty: true,
      isNoMatch: false,
      activeFilterCount: 0,
    })
  })

  it("filters by location, type, archived by, and search; sorts recently archived by default", () => {
    const facts = [
      archived({
        qrCodeId: 1,
        qrType: "DeliveryInsert",
        locationId: 1,
        locationName: "Camden",
        archivedAt: "2026-07-24T10:00:00.000Z",
        qrScans: 248,
        feedbackSubmitted: 176,
      }),
      archived({
        qrCodeId: 2,
        qrType: "DigitalGuestLink",
        linkName: "Summer promo",
        locationId: 2,
        locationName: "Shoreditch",
        archivedAt: "2026-07-10T10:00:00.000Z",
        archivedByDisplayName: "Ada Operator",
        qrScans: 50,
        feedbackSubmitted: 20,
        canRestore: false,
      }),
      archived({
        qrCodeId: 3,
        qrType: "SmartGuest",
        locationId: 1,
        locationName: "Camden",
        archivedAt: "2026-07-28T10:00:00.000Z",
        archivedByDisplayName: "Mohamed Mahmoud",
        qrScans: 3,
        feedbackSubmitted: 1,
      }),
    ]

    const filters: CaptureArchiveFilters = {
      locationIds: [1],
      placementTypes: ["DeliveryInsert", "SmartGuest"],
      archivedDate: { preset: "any-time" },
      archivedByDisplayNames: ["Mohamed Mahmoud"],
    }

    const result = buildCaptureArchiveList({
      facts,
      searchQuery: "camden",
      filters,
      sort: "recently-archived",
      nowMs: NOW,
      showLocationFilter: true,
    })

    expect(result.activeFilterCount).toBe(3)
    expect(result.isTrueEmpty).toBe(false)
    expect(result.isNoMatch).toBe(false)
    expect(result.rows.map((r) => r.qrCodeId)).toEqual([3, 1])
    expect(result.rows[0]).toMatchObject({
      placementLabel: "Smart Guest",
      qrScansText: "3 scans",
      feedbackSubmittedText: "1 feedback submissions",
      canDuplicateAsNew: false,
      canRestore: true,
    })
    expect(result.rows[1]).toMatchObject({
      placementLabel: "Delivery insert",
      archivedOnText: "24 July 2026",
      archivedByText: "Mohamed Mahmoud",
    })
  })

  it("reports no-match when filters exclude all rows", () => {
    const result = buildCaptureArchiveList({
      facts: [
        archived({ qrCodeId: 1, qrType: "CounterCard", locationId: 1 }),
      ],
      searchQuery: "zzz-no-match",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      sort: "recently-archived",
      nowMs: NOW,
    })

    expect(result).toMatchObject({
      rows: [],
      isTrueEmpty: false,
      isNoMatch: true,
    })
  })

  it("sorts by highest QR scans and placement name A–Z", () => {
    const facts = [
      archived({
        qrCodeId: 1,
        qrType: "WindowSticker",
        qrScans: 10,
      }),
      archived({
        qrCodeId: 2,
        qrType: "CounterCard",
        qrScans: 40,
      }),
      archived({
        qrCodeId: 3,
        qrType: "DigitalGuestLink",
        linkName: "Alpha link",
        qrScans: 40,
      }),
    ]

    const byScans = buildCaptureArchiveList({
      facts,
      searchQuery: "",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      sort: "highest-qr-scans",
      nowMs: NOW,
    })
    expect(byScans.rows.map((r) => r.qrCodeId)).toEqual([3, 2, 1])

    const byName = buildCaptureArchiveList({
      facts,
      searchQuery: "",
      filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
      sort: "placement-name-az",
      nowMs: NOW,
    })
    expect(byName.rows.map((r) => r.placementLabel)).toEqual([
      "Alpha link",
      "Counter card",
      "Window sticker",
    ])
  })

  it("filters archived date presets against now", () => {
    const facts = [
      archived({
        qrCodeId: 1,
        qrType: "CounterCard",
        archivedAt: "2026-07-28T10:00:00.000Z",
      }),
      archived({
        qrCodeId: 2,
        qrType: "PackagingSticker",
        archivedAt: "2026-06-01T10:00:00.000Z",
      }),
    ]

    const last7 = buildCaptureArchiveList({
      facts,
      searchQuery: "",
      filters: {
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: { preset: "last-7" },
      },
      sort: "recently-archived",
      nowMs: NOW,
    })
    expect(last7.rows.map((r) => r.qrCodeId)).toEqual([1])
    expect(last7.activeFilterCount).toBe(1)
  })
})

describe("countActiveArchiveFilters", () => {
  it("ignores location filter when showLocationFilter is false", () => {
    expect(
      countActiveArchiveFilters(
        {
          locationIds: [1],
          placementTypes: [],
          archivedDate: { preset: "any-time" },
          archivedByDisplayNames: [],
        },
        { showLocationFilter: false }
      )
    ).toBe(0)
  })

  it("does not count Custom archived-date until both bounds are set", () => {
    expect(
      countActiveArchiveFilters({
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: { preset: "custom" },
      })
    ).toBe(0)
    expect(
      countActiveArchiveFilters({
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: { preset: "custom", dateFrom: "2026-07-01" },
      })
    ).toBe(0)
    expect(
      countActiveArchiveFilters({
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: {
          preset: "custom",
          dateFrom: "2026-07-01",
          dateTo: "2026-07-31",
        },
      })
    ).toBe(1)
  })
})

describe("duplicateDigitalGuestLinkName", () => {
  it("appends (copy) to the archived link name", () => {
    expect(duplicateDigitalGuestLinkName("Summer promo")).toBe(
      "Summer promo (copy)"
    )
  })
})
