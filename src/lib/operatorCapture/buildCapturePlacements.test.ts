import { describe, expect, it } from "vitest"

import {
  buildCapturePlacements,
  type CapturePlacementFact,
} from "./buildCapturePlacements"

const NOW_MS = Date.parse("2026-07-16T12:00:00.000Z")

function fact(
  overrides: Partial<CapturePlacementFact> &
    Pick<CapturePlacementFact, "qrCodeId" | "qrType" | "status">
): CapturePlacementFact {
  return {
    qrLinkUrl: `https://tummly.example/scan/token-${overrides.qrCodeId}`,
    qrScans: 0,
    feedbackSubmitted: 0,
    marketingOptIns: 0,
    offerClaims: 0,
    lastScanAt: null,
    ...overrides,
  }
}

describe("buildCapturePlacements", () => {
  it("maps Active and Paused rows with Figma labels, count suffixes, and relative Last scan", () => {
    const result = buildCapturePlacements(
      [
        fact({
          qrCodeId: 1,
          qrType: "CounterCard",
          status: "Active",
          qrScans: 45,
          feedbackSubmitted: 12,
          marketingOptIns: 8,
          offerClaims: 0,
          lastScanAt: "2026-07-16T11:00:00.000Z",
        }),
        fact({
          qrCodeId: 2,
          qrType: "SmartGuest",
          status: "Paused",
          qrScans: 3,
          feedbackSubmitted: 1,
          marketingOptIns: 0,
          lastScanAt: "2026-07-07T12:00:00.000Z",
        }),
      ],
      NOW_MS
    )

    expect(result.isEmpty).toBe(false)
        expect(result.rows).toEqual([
      {
        qrCodeId: 1,
        qrType: "CounterCard",
        placementLabel: "Counter card",
        status: "Active",
        qrLinkUrl: "https://tummly.example/scan/token-1",
        qrScansText: "45 scans",
        feedbackSubmittedText: "12 feedback",
        marketingOptInsText: "8 opt-ins",
        offerClaimsText: "0 claims",
        lastScanText: "1 hour ago",
      },
      {
        qrCodeId: 2,
        qrType: "SmartGuest",
        placementLabel: "Smart Guest",
        status: "Paused",
        qrLinkUrl: "https://tummly.example/scan/token-2",
        qrScansText: "3 scans",
        feedbackSubmittedText: "1 feedback",
        marketingOptInsText: "0 opt-ins",
        offerClaimsText: "0 claims",
        lastScanText: "9 days ago",
      },
    ])
  })

  it("shows — for Last scan when lastScanAt is null", () => {
    const result = buildCapturePlacements(
      [
        fact({
          qrCodeId: 1,
          qrType: "DeliveryInsert",
          status: "Active",
          lastScanAt: null,
        }),
      ],
      NOW_MS
    )

    expect(result.rows[0]?.lastScanText).toBe("—")
  })

  it("is empty when there are no Active or Paused placements", () => {
    const result = buildCapturePlacements([], NOW_MS)
    expect(result.isEmpty).toBe(true)
    expect(result.rows).toEqual([])
  })
})
