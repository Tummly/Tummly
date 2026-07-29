import { describe, expect, it } from "vitest"

import {
  buildCaptureLocationPerformanceRows,
  formatCaptureLocationPageRangeLabel,
  formatCaptureLocationSubmissionRate,
} from "./buildCaptureLocationPerformance"
import type { CaptureLocationItem } from "@/types/dashboard"

const NOW_MS = Date.parse("2026-07-16T12:00:00.000Z")

function item(
  overrides: Partial<CaptureLocationItem> &
    Pick<CaptureLocationItem, "locationId" | "locationName">
): CaptureLocationItem {
  return {
    status: "Active",
    activePlacementsCount: 0,
    qrScans: 0,
    feedbackSubmitted: 0,
    marketingOptIns: 0,
    offerClaims: 0,
    lastActivityAt: null,
    ...overrides,
  }
}

describe("buildCaptureLocationPerformanceRows", () => {
  it("derives submission rate and formats Figma count suffixes", () => {
    const rows = buildCaptureLocationPerformanceRows(
      [
        item({
          locationId: 1,
          locationName: "Camden",
          activePlacementsCount: 5,
          qrScans: 10,
          feedbackSubmitted: 4,
          marketingOptIns: 2,
          offerClaims: 0,
          lastActivityAt: "2026-07-16T11:48:00.000Z",
        }),
        item({
          locationId: 2,
          locationName: "Soho",
          qrScans: 0,
          feedbackSubmitted: 0,
        }),
      ],
      NOW_MS
    )

    expect(rows[0]).toMatchObject({
      locationName: "Camden",
      activePlacementsText: "5 placements",
      qrScansText: "10 scans",
      feedbackSubmittedText: "4 feedback",
      submissionRateText: "40%",
      marketingOptInsText: "2 opt-ins",
      offerClaimsText: "0 claims",
      lastActivityText: "12 minutes ago",
    })
    expect(rows[1]?.submissionRateText).toBe("—")
    expect(rows[1]?.lastActivityText).toBe("—")
  })
})

describe("formatCaptureLocationSubmissionRate", () => {
  it("returns em dash when scans are zero", () => {
    expect(formatCaptureLocationSubmissionRate(3, 0)).toBe("—")
  })
})

describe("formatCaptureLocationPageRangeLabel", () => {
  it("matches Showing a–b of N locations", () => {
    expect(formatCaptureLocationPageRangeLabel(1, 20, 45)).toBe(
      "Showing 1–20 of 45 locations"
    )
    expect(formatCaptureLocationPageRangeLabel(1, 20, 0)).toBe(
      "Showing 0 of 0 locations"
    )
  })
})
