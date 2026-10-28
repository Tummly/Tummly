import { describe, expect, it } from "vitest"

import {
  adminShopFulfilmentLabel,
  adminShopOrderFulfilmentLabel,
  adminShopOrderReviewNotice,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  canSubmitAdminShopFulfilment,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"

describe("adminShopOrderFulfilment", () => {
  it("maps API statuses to operator-facing labels", () => {
    expect(adminShopFulfilmentLabel("processing")).toBe("Confirmed")
    expect(adminShopFulfilmentLabel("in_transit")).toBe("Dispatched")
    expect(adminShopFulfilmentLabel("delivered")).toBe("Delivered")
  })

  it("reads Preparing only for processing orders with production started", () => {
    expect(
      adminShopOrderFulfilmentLabel({
        fulfilmentStatus: "processing",
        productionStartedAtUtc: null,
      })
    ).toBe("Confirmed")
    expect(
      adminShopOrderFulfilmentLabel({
        fulfilmentStatus: "processing",
        productionStartedAtUtc: "2026-10-27T09:00:00Z",
      })
    ).toBe("Preparing")
    expect(
      adminShopOrderFulfilmentLabel({
        fulfilmentStatus: "in_transit",
        productionStartedAtUtc: "2026-10-27T09:00:00Z",
      })
    ).toBe("Dispatched")
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

  it("requires a tracking reference only to dispatch", () => {
    expect(
      canSubmitAdminShopFulfilment({ targetStatus: "in_transit", trackingUrl: "" })
    ).toBe(false)
    expect(
      canSubmitAdminShopFulfilment({
        targetStatus: "in_transit",
        trackingUrl: "   ",
      })
    ).toBe(false)
    expect(
      canSubmitAdminShopFulfilment({
        targetStatus: "in_transit",
        trackingUrl: "https://track.example/abc",
      })
    ).toBe(true)
    expect(
      canSubmitAdminShopFulfilment({ targetStatus: "delivered", trackingUrl: "" })
    ).toBe(true)
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
})
