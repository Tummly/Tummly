import { describe, expect, it } from "vitest"

import {
  buildPauseActivateConfirm,
  PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
  PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
} from "./buildPauseActivateConfirm"
import type { CapturePlacementItem } from "@/types/dashboard"

function fact(
  overrides: Partial<CapturePlacementItem> &
    Pick<CapturePlacementItem, "qrCodeId" | "qrType" | "status">
): CapturePlacementItem {
  return {
    qrLinkUrl: "https://example.test/scan/x",
    qrScans: 0,
    feedbackSubmitted: 0,
    marketingOptIns: 0,
    offerClaims: 0,
    lastScanAt: null,
    ...overrides,
  }
}

describe("buildPauseActivateConfirm", () => {
  it("builds placement Pause copy with printed-materials warning", () => {
    const view = buildPauseActivateConfirm({
      fact: fact({
        qrCodeId: 9,
        qrType: "CounterCard",
        status: "Active",
        lastScanAt: "2026-07-16T11:00:00.000Z",
      }),
      action: "pause",
      locationName: "Camden",
      nowMs: Date.parse("2026-07-16T12:00:00.000Z"),
    })

    expect(view).toMatchObject({
      qrCodeId: 9,
      action: "pause",
      title: "Pause QR placement?",
      body: "Guests using this placement will not be able to open the guest form or submit feedback until it is activated again. Historical performance will remain available.",
      nameLabel: "Placement",
      nameValue: "Counter card",
      locationName: "Camden",
      currentStatus: "Active",
      warningText:
        "Any printed materials using this QR code will remain in circulation but will not work while the placement is paused.",
      primaryLabel: "Pause placement",
      cancelLabel: "Cancel",
      successToastMessage:
        "Counter card is now paused. You can activate it again at any time.",
    })
    expect(view.lastScanText).not.toBe("—")
    expect(view.connectedGuestForm).toBeNull()
    expect(view.connectedOfferText).toBeNull()
  })

  it("builds Smart Guest Pause with placement framing and print warning", () => {
    const view = buildPauseActivateConfirm({
      fact: fact({
        qrCodeId: 10,
        qrType: "SmartGuest",
        status: "Active",
      }),
      action: "pause",
      locationName: "Soho",
    })

    expect(view).toMatchObject({
      title: "Pause QR placement?",
      nameLabel: "Placement",
      nameValue: "Smart Guest",
      warningText:
        "Any printed materials using this QR code will remain in circulation but will not work while the placement is paused.",
      primaryLabel: "Pause placement",
      lastScanText: "—",
    })
  })

  it("builds digital Pause with link language and no print warning", () => {
    const view = buildPauseActivateConfirm({
      fact: fact({
        qrCodeId: 11,
        qrType: "DigitalGuestLink",
        status: "Active",
        linkName: "Instagram bio",
      }),
      action: "pause",
      locationName: "Camden",
    })

    expect(view).toMatchObject({
      title: "Pause digital guest link?",
      body: "Guests using this link will not be able to open the guest form or submit feedback until it is activated again. Historical performance will remain available.",
      nameLabel: "Link name",
      nameValue: "Instagram bio",
      warningText: null,
      primaryLabel: "Pause link",
      successToastMessage:
        "Instagram bio is now paused. You can activate it again at any time.",
    })
  })

  it("builds placement Activate copy with form and offer rows", () => {
    const view = buildPauseActivateConfirm({
      fact: fact({
        qrCodeId: 9,
        qrType: "WindowSticker",
        status: "Paused",
      }),
      action: "activate",
      locationName: "Camden",
    })

    expect(view).toMatchObject({
      action: "activate",
      title: "Activate QR placement?",
      body: "Activating this placement will allow guests to open the connected guest form using its QR code or Smart Guest Link.",
      nameLabel: "Placement",
      nameValue: "Window sticker",
      connectedGuestForm: PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
      connectedOfferText: PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
      warningText: null,
      currentStatus: null,
      lastScanText: null,
      primaryLabel: "Activate placement",
      successToastMessage:
        "Window sticker is now active. Guests can use it again.",
    })
  })

  it("builds digital Activate with link language", () => {
    const view = buildPauseActivateConfirm({
      fact: fact({
        qrCodeId: 12,
        qrType: "DigitalGuestLink",
        status: "Paused",
        linkName: "Email footer",
      }),
      action: "activate",
      locationName: "Camden",
    })

    expect(view).toMatchObject({
      title: "Activate digital guest link?",
      body: "Activating this link will allow guests to open the connected guest form using this digital guest link.",
      nameLabel: "Link name",
      nameValue: "Email footer",
      primaryLabel: "Activate link",
      connectedGuestForm: PAUSE_ACTIVATE_CONFIRM_CONNECTED_GUEST_FORM,
      connectedOfferText: PAUSE_ACTIVATE_CONFIRM_CONNECTED_OFFER,
      successToastMessage:
        "Email footer is now active. Guests can use it again.",
    })
  })
})
