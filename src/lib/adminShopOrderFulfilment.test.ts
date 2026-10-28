import { describe, expect, it } from "vitest"

import {
  adminShopFulfilmentLabel,
  adminShopOrderFulfilmentLabel,
  adminShopOrderReviewNotice,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  canSubmitAdminShopFulfilmentAction,
  nextAdminShopFulfilmentAction,
} from "@/lib/adminShopOrderFulfilment"

type FulfilmentFields = Parameters<typeof adminShopOrderFulfilmentLabel>[0]

const EVIDENCE = {
  preparationStartedAtUtc: "2026-10-27T08:00:00Z",
  productionStartedAtUtc: "2026-10-27T09:00:00Z",
  dispatchedAtUtc: "2026-10-27T10:00:00Z",
  deliveredAtUtc: "2026-10-28T10:00:00Z",
}

function order(overrides: Partial<FulfilmentFields>): FulfilmentFields {
  return {
    fulfilmentStatus: "processing",
    preparationStartedAtUtc: null,
    productionStartedAtUtc: null,
    dispatchedAtUtc: null,
    deliveredAtUtc: null,
    ...overrides,
  }
}

describe("adminShopOrderFulfilment", () => {
  it("maps stored statuses used by the filters to operator-facing labels", () => {
    expect(adminShopFulfilmentLabel("processing")).toBe("Confirmed")
    expect(adminShopFulfilmentLabel("in_transit")).toBe("Dispatched")
    expect(adminShopFulfilmentLabel("delivered")).toBe("Delivered")
    expect(adminShopFulfilmentLabel("cancelled")).toBe("Cancelled")
  })

  it("reads Confirmed for a paid order with no preparation evidence", () => {
    expect(adminShopOrderFulfilmentLabel(order({}))).toBe("Confirmed")
  })

  it("reads Preparing from preparation evidence only", () => {
    expect(
      adminShopOrderFulfilmentLabel(
        order({ preparationStartedAtUtc: EVIDENCE.preparationStartedAtUtc })
      )
    ).toBe("Preparing")
  })

  it("reads In production from production evidence", () => {
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          preparationStartedAtUtc: EVIDENCE.preparationStartedAtUtc,
          productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
        })
      )
    ).toBe("In production")
  })

  it("reads Dispatched only with dispatch evidence, never from a tracking URL", () => {
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "in_transit",
          productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
        })
      )
    ).toBe("In production")
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "in_transit",
          productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
        })
      )
    ).toBe("Dispatched")
  })

  it("reads Delivered only with delivery evidence", () => {
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "delivered",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
        })
      )
    ).toBe("Dispatched")
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "delivered",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          deliveredAtUtc: EVIDENCE.deliveredAtUtc,
        })
      )
    ).toBe("Delivered")
  })

  it("reads Cancelled for a cancelled order", () => {
    expect(
      adminShopOrderFulfilmentLabel(order({ fulfilmentStatus: "cancelled" }))
    ).toBe("Cancelled")
  })

  it("walks the fulfilment actions in order: prepare, produce, dispatch, deliver", () => {
    expect(nextAdminShopFulfilmentAction(order({}))).toEqual({
      kind: "preparation",
      label: "Start preparing",
      resultLabel: "Preparing",
    })
    expect(
      nextAdminShopFulfilmentAction(
        order({ preparationStartedAtUtc: EVIDENCE.preparationStartedAtUtc })
      )
    ).toEqual({
      kind: "production",
      label: "Start production",
      resultLabel: "In production",
    })
    expect(
      nextAdminShopFulfilmentAction(
        order({
          preparationStartedAtUtc: EVIDENCE.preparationStartedAtUtc,
          productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
        })
      )
    ).toEqual({
      kind: "dispatch",
      label: "Mark as Dispatched",
      resultLabel: "Dispatched",
    })
    expect(
      nextAdminShopFulfilmentAction(
        order({
          fulfilmentStatus: "in_transit",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
        })
      )
    ).toEqual({
      kind: "delivery",
      label: "Mark as Delivered",
      resultLabel: "Delivered",
    })
  })

  it("skips preparation for an order whose production was stamped before preparation existed", () => {
    expect(
      nextAdminShopFulfilmentAction(
        order({ productionStartedAtUtc: EVIDENCE.productionStartedAtUtc })
      )
    ).toMatchObject({ kind: "dispatch" })
  })

  it("offers no action once delivered or cancelled", () => {
    expect(
      nextAdminShopFulfilmentAction(
        order({ fulfilmentStatus: "delivered", dispatchedAtUtc: EVIDENCE.dispatchedAtUtc })
      )
    ).toBeNull()
    expect(
      nextAdminShopFulfilmentAction(order({ fulfilmentStatus: "cancelled" }))
    ).toBeNull()
  })

  it("needs a carrier name and reference to dispatch; the tracking URL is optional", () => {
    const dispatch = { kind: "dispatch" as const }
    expect(
      canSubmitAdminShopFulfilmentAction(dispatch, {
        carrierName: "DPD",
        carrierReference: "",
      })
    ).toBe(false)
    expect(
      canSubmitAdminShopFulfilmentAction(dispatch, {
        carrierName: "   ",
        carrierReference: "DPD-123",
      })
    ).toBe(false)
    expect(
      canSubmitAdminShopFulfilmentAction(dispatch, {
        carrierName: "DPD",
        carrierReference: "DPD-123",
      })
    ).toBe(true)
    expect(
      canSubmitAdminShopFulfilmentAction(
        { kind: "preparation" },
        { carrierName: "", carrierReference: "" }
      )
    ).toBe(true)
  })

  it("limits tracking and notes edits by status", () => {
    expect(canEditAdminShopTrackingUrl("processing")).toBe(false)
    expect(canEditAdminShopTrackingUrl("in_transit")).toBe(true)
    expect(canEditAdminShopTrackingUrl("delivered")).toBe(false)
    expect(canEditAdminShopOpsNotes("processing")).toBe(true)
    expect(canEditAdminShopOpsNotes("cancelled")).toBe(false)
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
