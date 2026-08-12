import { describe, expect, it } from "vitest"

import {
  GUEST_PREVIEW_CLOSE_LABEL,
  GUEST_PREVIEW_CONTROL_LABEL,
  GUEST_PREVIEW_DESKTOP_LABEL,
  GUEST_PREVIEW_EDIT_TEXT_LABEL,
  GUEST_PREVIEW_EMPTY_VALUE,
  GUEST_PREVIEW_FOOTER_COOKIE,
  GUEST_PREVIEW_FOOTER_PRIVACY,
  GUEST_PREVIEW_FOOTER_TERMS,
  GUEST_PREVIEW_FOOTER_UNSUBSCRIBE,
  GUEST_PREVIEW_HEADING,
  GUEST_PREVIEW_MOBILE_LABEL,
  GUEST_PREVIEW_OFFER_COPY_LABEL,
  GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
  GUEST_PREVIEW_OVERLAY_BODY_CLASS,
  GUEST_PREVIEW_OVERLAY_CLASS,
  GUEST_PREVIEW_POWERED_BY_LABEL,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  buildGuestPreviewOfferCoupon,
  toIssuedGuestOfferCoupon,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
  guestPreviewFooterAddress,
  guestPreviewFooterDisclaimer,
} from "./guestPreviewPresentation"
import type { ConfirmedRecoveryOfferPayload } from "./recoveryOfferPresentation"

describe("guestPreviewPresentation", () => {
  it("keeps Guest preview chrome copy as in Figma", () => {
    expect(GUEST_PREVIEW_HEADING).toBe("Guest preview")
    expect(GUEST_PREVIEW_CONTROL_LABEL).toBe("Preview")
    expect(GUEST_PREVIEW_EDIT_TEXT_LABEL).toBe("Edit text")
    expect(GUEST_PREVIEW_SEND_TEST_LABEL).toBe("Send test")
    expect(GUEST_PREVIEW_DESKTOP_LABEL).toBe("Desktop")
    expect(GUEST_PREVIEW_MOBILE_LABEL).toBe("Mobile")
    expect(GUEST_PREVIEW_CLOSE_LABEL).toBe("Close")
    expect(GUEST_PREVIEW_FOOTER_UNSUBSCRIBE).toBe("Unsubscribe")
    expect(GUEST_PREVIEW_FOOTER_TERMS).toBe("Terms")
    expect(GUEST_PREVIEW_FOOTER_PRIVACY).toBe("Privacy")
    expect(GUEST_PREVIEW_FOOTER_COOKIE).toBe("Cookie settings")
    expect(GUEST_PREVIEW_POWERED_BY_LABEL).toBe("Powered by")
  })

  it("matches Operator wizard body border and sits above wizard shell", () => {
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).toContain("border-op-card-border")
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).toContain("border-t")
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).toContain("rounded-t-[20px]")
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).not.toContain(
      "border-[var(--op-color-gray-200)]"
    )
    expect(GUEST_PREVIEW_OVERLAY_CLASS).toContain("z-[135]")
    expect(GUEST_PREVIEW_OVERLAY_CLASS).toContain("fixed")
  })

  it("scrolls the preview body under a fixed header", () => {
    expect(GUEST_PREVIEW_OVERLAY_CLASS).toContain("overflow-hidden")
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).toContain("min-h-0")
    expect(GUEST_PREVIEW_OVERLAY_BODY_CLASS).toContain("overflow-y-auto")
  })

  it("keeps pointer events above an open Operator wizard Dialog", () => {
    expect(GUEST_PREVIEW_OVERLAY_CLASS).toContain("pointer-events-auto")
  })

  it("uses brand name, then location name, then em dash for header title", () => {
    expect(guestPreviewBrandTitle("KFC", "Camden")).toBe("KFC")
    expect(guestPreviewBrandTitle(null, "Camden")).toBe("Camden")
    expect(guestPreviewBrandTitle("  ", "Camden")).toBe("Camden")
    expect(guestPreviewBrandTitle(null, null)).toBe(GUEST_PREVIEW_EMPTY_VALUE)
    expect(guestPreviewBrandTitle(null, "  ")).toBe(GUEST_PREVIEW_EMPTY_VALUE)
  })

  it("shows location subtitle only when brand title is distinct", () => {
    expect(guestPreviewBrandSubtitle("KFC", "Camden High Street")).toBe(
      "Camden High Street"
    )
    expect(guestPreviewBrandSubtitle(null, "Camden")).toBeNull()
    expect(guestPreviewBrandSubtitle("KFC", null)).toBeNull()
  })

  it("formats footer address with em dash when address is missing", () => {
    expect(guestPreviewFooterAddress("Camden", "12 High Street")).toBe(
      "Camden, 12 High Street"
    )
    expect(guestPreviewFooterAddress("Camden", null)).toBe(
      `Camden, ${GUEST_PREVIEW_EMPTY_VALUE}`
    )
    expect(guestPreviewFooterAddress("Camden", "  ")).toBe(
      `Camden, ${GUEST_PREVIEW_EMPTY_VALUE}`
    )
  })

  it("builds footer disclaimer with display name", () => {
    expect(guestPreviewFooterDisclaimer("Camden")).toBe(
      "You're receiving this because you joined Camden customer club after visiting or giving feedback."
    )
  })

  it("builds email offer coupon from confirmed offer with placeholder code for claim QR", () => {
    const offer: Pick<
      ConfirmedRecoveryOfferPayload,
      "title" | "description" | "validity" | "expiryDate"
    > = {
      title: "15% off your next order",
      description:
        "Show this code to the team on your next visit. This offer is from Camden and is subject to the terms below.",
      validity: "choose_expiry_date",
      expiryDate: "2026-07-31",
    }

    const coupon = buildGuestPreviewOfferCoupon(offer)
    expect(coupon).toEqual({
      title: "15% off your next order",
      description:
        "Show this code to the team on your next visit. This offer is from Camden and is subject to the terms below.",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
      expiryLabel: "Expires: 31 July 2026",
      copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
      copyEnabled: false,
    })
    // Offer claim QR payload is the same sample Claim code (no Issue).
    expect(coupon?.redemptionCode).toBe("PREVIEW-CODE")
    expect(GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER).not.toMatch(
      /^TUM-/
    )
  })

  it("formats relative offer expiry without inventing a calendar date", () => {
    expect(
      buildGuestPreviewOfferCoupon({
        title: "20% off",
        description: "Thanks for your feedback.",
        validity: "30_days_after_issue",
        expiryDate: null,
      })
    ).toMatchObject({
      expiryLabel: "Expires: 30 days after issue",
      redemptionCode: GUEST_PREVIEW_OFFER_REDEMPTION_CODE_PLACEHOLDER,
    })
  })

  it("returns null when offer title is missing", () => {
    expect(
      buildGuestPreviewOfferCoupon({
        title: "  ",
        description: "Thanks",
        validity: "7_days_after_issue",
        expiryDate: null,
      })
    ).toBeNull()
  })

  it("maps an issued thank-you offer to a live coupon with Copy enabled", () => {
    expect(
      toIssuedGuestOfferCoupon({
        title: "Thanks for visiting",
        description: "Guest form thank-you",
        claimCode: "TUM-ABC234",
        expiryLabel: "Expires: 26 August 2026",
      })
    ).toEqual({
      title: "Thanks for visiting",
      description: "Guest form thank-you",
      redemptionCode: "TUM-ABC234",
      expiryLabel: "Expires: 26 August 2026",
      copyLabel: GUEST_PREVIEW_OFFER_COPY_LABEL,
      copyEnabled: true,
    })
  })

  it("returns null when issued offer title or claim code is missing", () => {
    expect(
      toIssuedGuestOfferCoupon({
        title: "  ",
        description: "Guest form thank-you",
        claimCode: "TUM-ABC234",
        expiryLabel: "Expires: 26 August 2026",
      })
    ).toBeNull()
    expect(
      toIssuedGuestOfferCoupon({
        title: "Thanks for visiting",
        description: "Guest form thank-you",
        claimCode: "  ",
        expiryLabel: "Expires: 26 August 2026",
      })
    ).toBeNull()
  })
})
