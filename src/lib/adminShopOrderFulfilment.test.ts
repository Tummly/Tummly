import { describe, expect, it } from "vitest"

import {
  adminShopFulfilmentLabel,
  adminShopOrderReviewNotice,
  adminShopPaymentSourceLabel,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  formatAdminShopGbpOrDash,
  nextAdminShopFulfilmentAction,
  normaliseAdminShopTransitionNote,
} from "@/lib/adminShopOrderFulfilment"

describe("adminShopOrderFulfilment", () => {
  it("maps API statuses to operator-facing labels", () => {
    expect(adminShopFulfilmentLabel("processing")).toBe("Processing")
    expect(adminShopFulfilmentLabel("in_transit")).toBe("Dispatched")
    expect(adminShopFulfilmentLabel("delivered")).toBe("Delivered")
  })

  it("exposes the next legal fulfilment action", () => {
    expect(nextAdminShopFulfilmentAction("processing")).toEqual({
      status: "in_transit",
      label: "Mark as Dispatched",
    })
    expect(nextAdminShopFulfilmentAction("in_transit")).toEqual({
      status: "delivered",
      label: "Mark as Delivered",
    })
    expect(nextAdminShopFulfilmentAction("delivered")).toBeNull()
    expect(nextAdminShopFulfilmentAction("cancelled")).toBeNull()
  })

  it("limits tracking and notes edits by status", () => {
    expect(canEditAdminShopTrackingUrl("processing")).toBe(true)
    expect(canEditAdminShopTrackingUrl("in_transit")).toBe(true)
    expect(canEditAdminShopTrackingUrl("delivered")).toBe(false)
    expect(canEditAdminShopOpsNotes("cancelled")).toBe(false)
    expect(canEditAdminShopOpsNotes("delivered")).toBe(true)
  })
})

describe("adminShopOrderReviewNotice", () => {
  it("returns null when the order needs no review", () => {
    expect(adminShopOrderReviewNotice({})).toBeNull()
    expect(
      adminShopOrderReviewNotice({
        reviewReason: null,
        reviewQrReservations: [],
      })
    ).toBeNull()
  })

  it("explains the capacity-after-payment review reason", () => {
    expect(
      adminShopOrderReviewNotice({
        reviewReason: "qr_capacity_unavailable_after_payment",
      })
    ).toEqual({
      title:
        "Needs review: paid, but the plan had no room for a new QR code.",
      lines: [],
    })
  })

  it("shows an unknown reason code as is", () => {
    expect(
      adminShopOrderReviewNotice({ reviewReason: "some_new_reason" })?.title
    ).toBe("Needs review: some_new_reason")
  })

  it("lists each held review reservation with its last error", () => {
    expect(
      adminShopOrderReviewNotice({
        reviewReason: null,
        reviewQrReservations: [
          {
            status: "review",
            qrType: "GuestCard",
            lastReconcileError: "revolut_state:processing",
            createdAtUtc: "2026-09-20T09:30:00Z",
          },
          {
            status: "review",
            qrType: "TableTalker",
            lastReconcileError: null,
            createdAtUtc: "2026-09-20T10:00:00Z",
          },
        ],
      })
    ).toEqual({
      title: "Needs review: a QR slot has been held for over 24 hours.",
      lines: [
        "GuestCard held since 2026-09-20 09:30 UTC (revolut_state:processing)",
        "TableTalker held since 2026-09-20 10:00 UTC (no error recorded)",
      ],
    })
  })

  it("trims a transition note and sends null when it is blank", () => {
    expect(normaliseAdminShopTransitionNote("  Courier collected  ")).toBe(
      "Courier collected"
    )
    expect(normaliseAdminShopTransitionNote("   ")).toBeNull()
    expect(normaliseAdminShopTransitionNote("")).toBeNull()
  })

  it("labels the payment source and shows a dash when it is unknown", () => {
    expect(adminShopPaymentSourceLabel("revolut")).toBe("Revolut")
    expect(adminShopPaymentSourceLabel("complimentary")).toBe(
      "Complimentary (£0)"
    )
    expect(adminShopPaymentSourceLabel(undefined)).toBe("—")
  })

  it("formats pence as pounds and shows a dash for a missing amount", () => {
    expect(formatAdminShopGbpOrDash(2880)).toBe("£28.80")
    expect(formatAdminShopGbpOrDash(0)).toBe("£0.00")
    expect(formatAdminShopGbpOrDash(undefined)).toBe("—")
    expect(formatAdminShopGbpOrDash(null)).toBe("—")
  })
})
