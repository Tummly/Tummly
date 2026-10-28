import { describe, expect, it } from "vitest"

import type {
  AdminShopOrderListItem,
  AdminShopPrintAsset,
} from "@/api/adminShopOrdersApi"
import {
  adminShopOrderPaymentLabel,
  adminShopOrderRowActions,
  adminShopOrderTableLines,
  formatAdminShopOrderDate,
} from "./adminShopOrdersTablePresentation"

function asset(
  overrides: Partial<AdminShopPrintAsset> & Pick<AdminShopPrintAsset, "qrType">
): AdminShopPrintAsset {
  return {
    quantity: 10,
    status: "Ready",
    fileName: "asset.pdf",
    lastError: null,
    approved: false,
    printRoute: "Pdf",
    productTitle: "QR Counter Display",
    ...overrides,
  }
}

function order(
  overrides: Partial<AdminShopOrderListItem> = {}
): Pick<
  AdminShopOrderListItem,
  "fulfilmentStatus" | "paymentStatus" | "isComplimentary" | "printAssets"
> {
  return {
    fulfilmentStatus: "processing",
    paymentStatus: "paid",
    isComplimentary: false,
    printAssets: [],
    ...overrides,
  }
}

describe("formatAdminShopOrderDate", () => {
  it("formats UTC instants in Europe/London as dd MMM yyyy, HH:mm", () => {
    expect(formatAdminShopOrderDate("2026-07-01T13:05:00Z")).toBe(
      "01 Jul 2026, 14:05"
    )
  })

  it("uses London winter time (GMT) for January instants", () => {
    expect(formatAdminShopOrderDate("2026-01-15T23:30:00Z")).toBe(
      "15 Jan 2026, 23:30"
    )
  })

  it("returns the raw value when the timestamp cannot be parsed", () => {
    expect(formatAdminShopOrderDate("not-a-date")).toBe("not-a-date")
  })
})

describe("adminShopOrderPaymentLabel", () => {
  it("shows Free for complimentary orders regardless of payment status", () => {
    expect(
      adminShopOrderPaymentLabel({
        paymentStatus: "awaiting_payment",
        isComplimentary: true,
      })
    ).toBe("Free")
  })

  it("maps paid and refunded payments", () => {
    expect(
      adminShopOrderPaymentLabel({ paymentStatus: "paid", isComplimentary: false })
    ).toBe("Paid")
    expect(
      adminShopOrderPaymentLabel({
        paymentStatus: "refunded",
        isComplimentary: false,
      })
    ).toBe("Refunded")
  })

  it("maps every other payment status to Unpaid", () => {
    for (const paymentStatus of ["awaiting_payment", "payment_failed"]) {
      expect(
        adminShopOrderPaymentLabel({ paymentStatus, isComplimentary: false })
      ).toBe("Unpaid")
    }
  })
})

describe("adminShopOrderRowActions", () => {
  it("offers Mark as dispatched only while processing", () => {
    expect(adminShopOrderRowActions(order()).nextFulfilmentAction).toEqual({
      status: "in_transit",
      label: "Mark as dispatched",
    })
    expect(
      adminShopOrderRowActions(order({ fulfilmentStatus: "in_transit" }))
        .nextFulfilmentAction
    ).toEqual({ status: "delivered", label: "Mark as delivered" })
    expect(
      adminShopOrderRowActions(order({ fulfilmentStatus: "delivered" }))
        .nextFulfilmentAction
    ).toBeNull()
  })

  it("lists only Ready and approved assets as downloadable", () => {
    const actions = adminShopOrderRowActions(
      order({
        printAssets: [
          asset({ qrType: "CounterCard", approved: true }),
          asset({ qrType: "TableTent", approved: false }),
          asset({ qrType: "WindowSticker", approved: true, status: "Preparing" }),
          asset({ qrType: "OfferCard", approved: true, status: "Failed" }),
        ],
      })
    )

    expect(actions.downloadQrTypes).toEqual(["CounterCard"])
  })

  it("lists only Ready and unapproved assets as approvable", () => {
    const actions = adminShopOrderRowActions(
      order({
        printAssets: [
          asset({ qrType: "CounterCard", approved: true }),
          asset({ qrType: "TableTent", approved: false }),
          asset({ qrType: "WindowSticker", approved: false, status: "Failed" }),
        ],
      })
    )

    expect(actions.approveQrTypes).toEqual(["TableTent"])
    expect(actions.downloadQrTypes).toEqual(["CounterCard"])
  })

  it("returns no material actions for orders without print assets", () => {
    const actions = adminShopOrderRowActions(order())

    expect(actions.downloadQrTypes).toEqual([])
    expect(actions.approveQrTypes).toEqual([])
  })
})

describe("adminShopOrderTableLines", () => {
  it("gives one product and one quantity per order line", () => {
    expect(
      adminShopOrderTableLines({
        lines: [
          {
            catalogSkuId: "table-tent",
            titleSnapshot: "Table Tent",
            quantity: 2,
            unitNetPence: 0,
            lineNetPence: 0,
          },
          {
            catalogSkuId: "counter-card",
            titleSnapshot: "QR Counter Display",
            quantity: 1,
            unitNetPence: 0,
            lineNetPence: 0,
          },
        ],
      })
    ).toEqual([
      { key: "table-tent-0", product: "Table Tent", quantity: 2 },
      { key: "counter-card-1", product: "QR Counter Display", quantity: 1 },
    ])
  })

  it("returns no rows for an order without lines", () => {
    expect(adminShopOrderTableLines({ lines: [] })).toEqual([])
  })
})
