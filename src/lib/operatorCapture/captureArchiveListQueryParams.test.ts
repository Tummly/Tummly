import { describe, expect, it } from "vitest"

import { DEFAULT_CAPTURE_ARCHIVE_FILTERS } from "@/lib/operatorCapture/buildCaptureArchive"
import { buildCaptureArchiveListQueryParams } from "@/lib/operatorCapture/captureArchiveListQueryParams"

describe("buildCaptureArchiveListQueryParams", () => {
  it("sends page defaults and omits empty optional filters", () => {
    expect(
      buildCaptureArchiveListQueryParams({
        q: "  ",
        filters: DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        sort: "recently-archived",
        page: 1,
      })
    ).toEqual({
      sort: "recently-archived",
      page: 1,
      pageSize: 25,
    })
  })

  it("maps filters to qrTypes, archivedBy, locationIds, and trimmed q", () => {
    expect(
      buildCaptureArchiveListQueryParams({
        q: "  counter  ",
        filters: {
          locationIds: [42, 99],
          placementTypes: ["CounterCard", "SmartGuest"],
          archivedDate: { preset: "any-time" },
          archivedByDisplayNames: ["Ada", "Mohamed"],
        },
        sort: "highest-qr-scans",
        page: 2,
        pageSize: 25,
      })
    ).toEqual({
      q: "counter",
      locationIds: [42, 99],
      qrTypes: ["CounterCard", "SmartGuest"],
      archivedBy: ["Ada", "Mohamed"],
      sort: "highest-qr-scans",
      page: 2,
      pageSize: 25,
    })
  })

  it("sends datePreset with utcOffsetMinutes for operator-local presets", () => {
    const now = new Date("2026-07-30T12:00:00.000Z")
    const params = buildCaptureArchiveListQueryParams({
      q: "",
      filters: {
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: { preset: "today" },
      },
      sort: "recently-archived",
      page: 1,
      now,
    })

    expect(params.datePreset).toBe("today")
    expect(params.utcOffsetMinutes).toBe(-now.getTimezoneOffset())
    expect(params.dateFrom).toBeUndefined()
    expect(params.dateTo).toBeUndefined()
  })

  it("sends custom date bounds as UTC ISO window for inclusive local days", () => {
    const params = buildCaptureArchiveListQueryParams({
      q: "",
      filters: {
        ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
        archivedDate: {
          preset: "custom",
          dateFrom: "2026-07-01",
          dateTo: "2026-07-15",
        },
      },
      sort: "oldest-archived",
      page: 1,
      now: new Date("2026-07-30T12:00:00.000Z"),
    })

    expect(params.datePreset).toBe("custom")
    expect(params.dateFrom).toBe(
      new Date(2026, 6, 1).toISOString()
    )
    expect(params.dateTo).toBe(
      new Date(2026, 6, 16).toISOString()
    )
    expect(params.utcOffsetMinutes).toBeUndefined()
  })

  it("omits incomplete custom date ranges", () => {
    expect(
      buildCaptureArchiveListQueryParams({
        q: "",
        filters: {
          ...DEFAULT_CAPTURE_ARCHIVE_FILTERS,
          archivedDate: {
            preset: "custom",
            dateFrom: "2026-07-01",
          },
        },
        sort: "recently-archived",
        page: 1,
      })
    ).toEqual({
      sort: "recently-archived",
      page: 1,
      pageSize: 25,
    })
  })
})
