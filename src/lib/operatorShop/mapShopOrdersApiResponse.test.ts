import { describe, expect, it } from "vitest"

import type { ShopOrderListItemWire } from "@/api/shopOrdersApi"
import { mapShopOrderListItemToRow } from "@/lib/operatorShop/mapShopOrdersApiResponse"

describe("mapShopOrderListItemToRow", () => {
  it("passes API status labels and the order kind label through", () => {
    const item: ShopOrderListItemWire = {
      id: "ord-2",
      orderNumber: "ORD-10431",
      orderDate: "30 Oct 2026",
      locationId: 1,
      locationName: "Padella",
      materialsSummary: "Starter kit",
      materialTypes: ["qr-guest-cards"],
      placedBy: "Owner",
      totalFormatted: "£0.00",
      totalGrossPence: 0,
      paymentStatus: "Free",
      fulfilmentStatus: "Processing",
      paymentStatusLabel: "Free",
      fulfilmentStatusLabel: "Processing",
      orderKindLabel: "Complimentary Starter Kit",
      isComplimentary: true,
      updatedAtUtc: "2026-10-30T12:00:00Z",
    }

    const row = mapShopOrderListItemToRow(item)

    expect(row.paymentStatusLabel).toBe("Free")
    expect(row.fulfilmentStatusLabel).toBe("Processing")
    expect(row.orderKindLabel).toBe("Complimentary Starter Kit")
    expect(row.isComplimentary).toBe(true)
  })

  it("keeps a null order kind for regular orders", () => {
    const row = mapShopOrderListItemToRow({
      id: "ord-3",
      orderNumber: "ORD-10432",
      orderDate: "30 Oct 2026",
      locationId: 1,
      locationName: "Padella",
      materialsSummary: "Table tents",
      materialTypes: ["table-tents"],
      placedBy: "Owner",
      totalFormatted: "£82.80",
      totalGrossPence: 8280,
      paymentStatus: "Awaiting payment",
      fulfilmentStatus: "Awaiting payment",
      paymentStatusLabel: "Awaiting payment",
      fulfilmentStatusLabel: "Awaiting payment",
      orderKindLabel: null,
      updatedAtUtc: "2026-10-30T12:00:00Z",
    })

    expect(row.orderKindLabel).toBeNull()
    expect(row.paymentStatusLabel).toBe("Awaiting payment")
  })
})
