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
  GUEST_PREVIEW_POWERED_BY_LABEL,
  GUEST_PREVIEW_SEND_TEST_LABEL,
  guestPreviewBrandSubtitle,
  guestPreviewBrandTitle,
  guestPreviewFooterAddress,
  guestPreviewFooterDisclaimer,
} from "./guestPreviewPresentation"

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
      "You're receiving this because you joined Camden guests list after visiting or giving feedback."
    )
  })
})
