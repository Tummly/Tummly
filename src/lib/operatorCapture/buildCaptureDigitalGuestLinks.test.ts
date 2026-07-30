import { describe, expect, it } from "vitest"

import {
  buildCaptureDigitalGuestLinks,
  type CaptureDigitalGuestLinkFact,
} from "./buildCaptureDigitalGuestLinks"

const NOW_MS = Date.parse("2026-07-16T12:00:00.000Z")

function fact(
  overrides: Partial<CaptureDigitalGuestLinkFact> &
    Pick<CaptureDigitalGuestLinkFact, "qrCodeId" | "status">
): CaptureDigitalGuestLinkFact {
  return {
    qrType: "DigitalGuestLink",
    qrLinkUrl: `https://tummly.example/scan/token-${overrides.qrCodeId}`,
    qrScans: 0,
    feedbackSubmitted: 0,
    marketingOptIns: 0,
    offerClaims: 0,
    lastScanAt: null,
    linkName: "Link",
    channel: "SocialMedia",
    ...overrides,
  }
}

describe("buildCaptureDigitalGuestLinks", () => {
  it("maps Digital guest links with Link name and N opens cells", () => {
    const result = buildCaptureDigitalGuestLinks(
      [
        fact({
          qrCodeId: 11,
          status: "Active",
          linkName: "Instagram bio",
          qrScans: 45,
          feedbackSubmitted: 12,
          marketingOptIns: 8,
          offerClaims: 0,
          lastScanAt: "2026-07-07T12:00:00.000Z",
        }),
        fact({
          qrCodeId: 12,
          status: "Paused",
          linkName: "Email blast",
          qrScans: 3,
          feedbackSubmitted: 1,
        }),
      ],
      NOW_MS
    )

    expect(result.isEmpty).toBe(false)
    expect(result.rows).toEqual([
      {
        qrCodeId: 11,
        guestLinkLabel: "Instagram bio",
        status: "Active",
        qrLinkUrl: "https://tummly.example/scan/token-11",
        qrScansText: "45 opens",
        feedbackSubmittedText: "12 feedback",
        marketingOptInsText: "8 opt-ins",
        offerClaimsText: "0 claims",
        lastScanText: "9 days ago",
      },
      {
        qrCodeId: 12,
        guestLinkLabel: "Email blast",
        status: "Paused",
        qrLinkUrl: "https://tummly.example/scan/token-12",
        qrScansText: "3 opens",
        feedbackSubmittedText: "1 feedback",
        marketingOptInsText: "0 opt-ins",
        offerClaimsText: "0 claims",
        lastScanText: "—",
      },
    ])
  })

  it("returns empty chrome facts when there are no Digital guest links", () => {
    const result = buildCaptureDigitalGuestLinks(
      [
        fact({
          qrCodeId: 1,
          status: "Active",
          qrType: "SmartGuest",
          linkName: null,
        }),
        fact({
          qrCodeId: 2,
          status: "Active",
          qrType: "CounterCard",
          linkName: null,
        }),
      ],
      NOW_MS
    )

    expect(result.isEmpty).toBe(true)
    expect(result.rows).toEqual([])
  })
})
