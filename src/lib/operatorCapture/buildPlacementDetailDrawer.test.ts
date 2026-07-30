import { describe, expect, it } from "vitest"

import {
  buildPlacementDetailDrawer,
  PLACEMENT_DETAIL_CONNECTED_GUEST_FORM,
  PLACEMENT_DETAIL_CONNECTED_OFFER_STUB,
  placementDetailKindForQrType,
} from "./buildPlacementDetailDrawer"
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
      connectedOfferText: PLACEMENT_DETAIL_CONNECTED_OFFER_STUB,
      submissionRateText: "25%",
      channelLabel: null,
    })
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
      pauseActivateLabel: "Activate placement",
      descriptionDraft: "note",
    })
  })
})
