import { describe, expect, it } from "vitest"

import {
  buildPlacementDetailDrawer,
  PLACEMENT_DETAIL_CONNECTED_GUEST_FORM,
  placementDetailKindForQrType,
} from "./buildPlacementDetailDrawer"
import { CAPTURE_CONNECTED_OFFERS_NONE } from "./captureThankYouOfferPresentation"
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

describe("buildPlacementDetailDrawer", () => {
  it("maps catalog kinds with Placement details and QR assets", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 1,
        qrType: "DeliveryInsert",
        status: "Active",
        qrScans: 8,
        feedbackSubmitted: 2,
      }),
      locationName: "Camden",
      descriptionDraft: "",
    })

    expect(placementDetailKindForQrType("DeliveryInsert")).toBe("catalog")
    expect(view).toMatchObject({
      kind: "catalog",
      title: "Delivery insert",
      detailsSectionTitle: "Placement details",
      canRotate: true,
      showOrderPrintMaterials: true,
      connectedGuestForm: PLACEMENT_DETAIL_CONNECTED_GUEST_FORM,
      connectedOfferText: CAPTURE_CONNECTED_OFFERS_NONE,
      editConnectedOfferEnabled: true,
      editGuestFormEnabled: true,
      submissionRateText: "25%",
      channelLabel: null,
    })
    expect(view.orderPrintMaterialsEnabled).toBe(true)
  })

  it("enables Edit guest form when placement is not archived", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 1,
        qrType: "TableTent",
        status: "Active",
      }),
      locationName: "Camden",
      descriptionDraft: "",
    })

    expect(view.editGuestFormEnabled).toBe(true)
  })

  it("shows live thank-you offer title as Connected offer", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 1,
        qrType: "TableTent",
        status: "Active",
      }),
      locationName: "Camden",
      descriptionDraft: "",
      thankYouOffer: {
        offerId: 88,
        title: "Free dessert",
        live: true,
      },
    })

    expect(view.connectedOfferText).toBe("Free dessert")
    expect(view.editConnectedOfferEnabled).toBe(true)
  })

  it("shows No active offers when thank-you attach is not live", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 1,
        qrType: "TableTent",
        status: "Active",
      }),
      locationName: "Camden",
      descriptionDraft: "",
      thankYouOffer: {
        offerId: 88,
        title: "Draft treat",
        live: false,
      },
    })

    expect(view.connectedOfferText).toBe(CAPTURE_CONNECTED_OFFERS_NONE)
  })

  it("disables edit connected offer when placement is archived", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 1,
        qrType: "TableTent",
        status: "Archived",
      }),
      locationName: "Camden",
      descriptionDraft: "",
      thankYouOffer: {
        offerId: 88,
        title: "Free dessert",
        live: true,
      },
    })

    expect(view.connectedOfferText).toBe("Free dessert")
    expect(view.editConnectedOfferEnabled).toBe(false)
    expect(view.editGuestFormEnabled).toBe(false)
  })

  it("maps digital links with Link details and no Rotate / print materials", () => {
    const view = buildPlacementDetailDrawer({
      fact: fact({
        qrCodeId: 2,
        qrType: "DigitalGuestLink",
        status: "Paused",
        linkName: "WhatsApp blast",
        channelLabel: "WhatsApp",
      }),
      locationName: "Camden",
      descriptionDraft: "note",
    })

    expect(view).toMatchObject({
      kind: "digital",
      title: "WhatsApp blast",
      detailsSectionTitle: "Link details",
      typeFieldLabel: "Link type",
      typeValue: "Digital guest link",
      channelLabel: "WhatsApp",
      canRotate: false,
      showOrderPrintMaterials: false,
      assetsSectionTitle: "Link assets",
      pauseActivateLabel: "Activate link",
      descriptionDraft: "note",
      editConnectedOfferEnabled: true,
    })
    expect(view.orderPrintMaterialsEnabled).toBe(false)
  })
})
