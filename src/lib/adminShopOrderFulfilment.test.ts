import { describe, expect, it } from "vitest"

import {
  adminShopDispatchedAtToIso,
  adminShopFulfilmentLabel,
  adminShopOrderFulfilmentLabel,
  adminShopOrderReviewNotice,
  ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL,
  canEditAdminShopOpsNotes,
  canEditAdminShopTrackingUrl,
  canSubmitAdminShopDispatchEvidence,
  canSubmitAdminShopFulfilmentAction,
  canSubmitAdminShopStatusCorrection,
  hasAdminShopDispatchEvidence,
  isAdminShopDispatchEvidenceMissing,
  adminShopPaymentSourceLabel,
  formatAdminShopGbpOrDash,
  nextAdminShopFulfilmentAction,
  normaliseAdminShopTransitionNote,
} from "@/lib/adminShopOrderFulfilment"

type FulfilmentFields = Parameters<typeof adminShopOrderFulfilmentLabel>[0]

const EVIDENCE = {
  preparationStartedAtUtc: "2026-10-27T08:00:00Z",
  productionStartedAtUtc: "2026-10-27T09:00:00Z",
  dispatchedAtUtc: "2026-10-27T10:00:00Z",
  deliveredAtUtc: "2026-10-28T10:00:00Z",
}

/** Full carrier evidence, as written by Admin dispatch or a legacy backfill. */
const CARRIER_EVIDENCE = {
  dispatchCarrierName: "DPD",
  dispatchCarrierReference: "DPD-123",
  dispatchSource: "admin_manual",
  dispatchedByAdminUserId: 7,
}

function order(overrides: Partial<FulfilmentFields>): FulfilmentFields {
  return {
    fulfilmentStatus: "processing",
    preparationStartedAtUtc: null,
    productionStartedAtUtc: null,
    dispatchedAtUtc: null,
    dispatchCarrierName: null,
    dispatchCarrierReference: null,
    dispatchSource: null,
    dispatchedByAdminUserId: null,
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
          ...CARRIER_EVIDENCE,
        })
      )
    ).toBe("Dispatched")
  })

  it("reads a legacy dispatch without carrier evidence as its last substantiated state", () => {
    const legacy = order({
      fulfilmentStatus: "in_transit",
      preparationStartedAtUtc: EVIDENCE.preparationStartedAtUtc,
      productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
      dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
      dispatchCarrierName: "DPD",
    })
    expect(adminShopOrderFulfilmentLabel(legacy)).toBe("In production")
    expect(
      adminShopOrderFulfilmentLabel({ ...legacy, productionStartedAtUtc: null })
    ).toBe("Preparing")
  })

  it("reads Delivered only with delivery and dispatch evidence", () => {
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "delivered",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          ...CARRIER_EVIDENCE,
        })
      )
    ).toBe("Dispatched")
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "delivered",
          productionStartedAtUtc: EVIDENCE.productionStartedAtUtc,
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          deliveredAtUtc: EVIDENCE.deliveredAtUtc,
        })
      )
    ).toBe("In production")
    expect(
      adminShopOrderFulfilmentLabel(
        order({
          fulfilmentStatus: "delivered",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          deliveredAtUtc: EVIDENCE.deliveredAtUtc,
          ...CARRIER_EVIDENCE,
        })
      )
    ).toBe("Delivered")
  })

  it("flags a Dispatched or Delivered order that lacks carrier evidence", () => {
    const legacyInTransit = order({
      fulfilmentStatus: "in_transit",
      dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
    })
    expect(isAdminShopDispatchEvidenceMissing(legacyInTransit)).toBe(true)
    expect(
      isAdminShopDispatchEvidenceMissing({
        ...legacyInTransit,
        fulfilmentStatus: "delivered",
        deliveredAtUtc: EVIDENCE.deliveredAtUtc,
      })
    ).toBe(true)
    expect(
      isAdminShopDispatchEvidenceMissing({
        ...legacyInTransit,
        ...CARRIER_EVIDENCE,
      })
    ).toBe(false)
    expect(
      isAdminShopDispatchEvidenceMissing(
        order({ productionStartedAtUtc: EVIDENCE.productionStartedAtUtc })
      )
    ).toBe(false)
    expect(ADMIN_SHOP_DISPATCH_EVIDENCE_MISSING_LABEL).toBe(
      "Dispatch evidence missing"
    )
  })

  it("needs every evidence field, including the Admin actor, before a dispatch counts", () => {
    const base = order({
      fulfilmentStatus: "in_transit",
      dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
      ...CARRIER_EVIDENCE,
    })
    expect(hasAdminShopDispatchEvidence(base)).toBe(true)
    expect(hasAdminShopDispatchEvidence({ ...base, dispatchCarrierName: " " })).toBe(false)
    expect(hasAdminShopDispatchEvidence({ ...base, dispatchCarrierReference: null })).toBe(false)
    expect(hasAdminShopDispatchEvidence({ ...base, dispatchSource: null })).toBe(false)
    expect(hasAdminShopDispatchEvidence({ ...base, dispatchedByAdminUserId: null })).toBe(false)
    expect(hasAdminShopDispatchEvidence({ ...base, dispatchedAtUtc: null })).toBe(false)
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
          ...CARRIER_EVIDENCE,
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

  it("offers no action for a legacy dispatch until its evidence is recorded", () => {
    expect(
      nextAdminShopFulfilmentAction(
        order({
          fulfilmentStatus: "in_transit",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
        })
      )
    ).toBeNull()
    expect(
      nextAdminShopFulfilmentAction(
        order({
          fulfilmentStatus: "in_transit",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          ...CARRIER_EVIDENCE,
        })
      )
    ).toMatchObject({ kind: "delivery" })
  })

  it("offers no action once delivered or cancelled", () => {
    expect(
      nextAdminShopFulfilmentAction(
        order({
          fulfilmentStatus: "delivered",
          dispatchedAtUtc: EVIDENCE.dispatchedAtUtc,
          ...CARRIER_EVIDENCE,
        })
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

describe("admin dispatch evidence and status correction", () => {
  it("converts a datetime-local value to ISO UTC and rejects an empty or invalid one", () => {
    expect(adminShopDispatchedAtToIso("")).toBeNull()
    expect(adminShopDispatchedAtToIso("not a date")).toBeNull()
    const iso = adminShopDispatchedAtToIso("2026-10-27T10:30")
    expect(iso).not.toBeNull()
    expect(iso).toMatch(/Z$/)
    expect(new Date(iso as string).getTime()).toBe(new Date("2026-10-27T10:30").getTime())
  })

  it("needs a carrier name, reference and dispatch time to record evidence", () => {
    const complete = {
      carrierName: "DPD",
      carrierReference: "DPD-123",
      dispatchedAt: "2026-10-27T10:30",
    }
    expect(canSubmitAdminShopDispatchEvidence(complete)).toBe(true)
    expect(canSubmitAdminShopDispatchEvidence({ ...complete, carrierName: " " })).toBe(false)
    expect(canSubmitAdminShopDispatchEvidence({ ...complete, carrierReference: "" })).toBe(false)
    expect(canSubmitAdminShopDispatchEvidence({ ...complete, dispatchedAt: "" })).toBe(false)
  })

  it("needs a reason of at most 500 characters to correct the status", () => {
    expect(canSubmitAdminShopStatusCorrection("   ")).toBe(false)
    expect(canSubmitAdminShopStatusCorrection("No carrier reference.")).toBe(true)
    expect(canSubmitAdminShopStatusCorrection("x".repeat(501))).toBe(false)
  })
})

