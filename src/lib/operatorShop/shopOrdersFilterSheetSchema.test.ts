import { describe, expect, it } from "vitest"
import {
  shopOrdersFilterSheetSchema,
  matchesShopOrderFilters,
  sortShopOrders,
  getShopOrdersSortId,
  normalizeFulfilmentStatusToId,
  normalizePaymentStatusToId,
  type DetailedShopOrder,
} from "@/lib/operatorShop/shopOrdersFilterSheetSchema"
import {
  emptySelection,
} from "@/lib/operatorFilterSheet"

const MOCK_ORDERS: DetailedShopOrder[] = [
  {
    id: "ord-1",
    orderNumber: "ORD-10428",
    orderDate: "29 Jul 2026",
    isoDate: "2026-07-29",
    locationId: "1",
    locationName: "Padella · Borough Market",
    materials: "Table tents · Pack of 20",
    materialTypes: ["table-tents"],
    placedBy: "Mohamed Mahmoud",
    total: "£82.80",
    totalNumeric: 82.8,
    paymentStatus: "Paid",
    fulfilmentStatus: "Processing",
    updatedDate: "30 Jul 2026",
  },
  {
    id: "ord-2",
    orderNumber: "ORD-10425",
    orderDate: "25 Jul 2026",
    isoDate: "2026-07-25",
    locationId: "2",
    locationName: "The Ivy Soho Brasserie",
    materials: "Window stickers · Pack of 10",
    materialTypes: ["window-stickers"],
    placedBy: "Sarah Jenkins",
    total: "£45.00",
    totalNumeric: 45.0,
    paymentStatus: "Paid",
    fulfilmentStatus: "Dispatched",
    updatedDate: "28 Jul 2026",
  },
  {
    id: "ord-3",
    orderNumber: "ORD-10350",
    orderDate: "04 May 2026",
    isoDate: "2026-05-04",
    locationId: "3",
    locationName: "Camden Market Kitchen",
    materials: "Receipt stickers · Roll of 1000",
    materialTypes: ["receipt-stickers"],
    placedBy: "Liam O'Connor",
    total: "£112.00",
    totalNumeric: 112.0,
    paymentStatus: "Refunded",
    fulfilmentStatus: "Cancelled",
    updatedDate: "05 May 2026",
  },
]

describe("shopOrdersFilterSheetSchema", () => {
  it("builds the schema with all 6 filter fields including Sort", () => {
    const schema = shopOrdersFilterSheetSchema({
      locations: [
        { id: "1", label: "Padella" },
        { id: "2", label: "The Ivy" },
      ],
    })
    expect(schema.fields).toHaveLength(6)
    const fieldIds = schema.fields.map((f) => f.id)
    expect(fieldIds).toEqual([
      "location",
      "fulfilmentStatus",
      "paymentStatus",
      "orderDate",
      "materialType",
      "sort",
    ])
  })

  it("maps fulfilment display labels to stored filter ids", () => {
    expect(normalizeFulfilmentStatusToId("Dispatched")).toBe("in_transit")
    expect(normalizePaymentStatusToId("Refunded")).toBe("refunded")
  })

  it("exposes four stored fulfilment filter options", () => {
    const schema = shopOrdersFilterSheetSchema()
    const fulfilment = schema.fields.find((field) => field.id === "fulfilmentStatus")
    expect(fulfilment?.kind).toBe("multi-select")
    if (fulfilment?.kind !== "multi-select") {
      throw new Error("expected multi-select fulfilment field")
    }
    expect(fulfilment.options.map((option) => option.id)).toEqual([
      "processing",
      "in_transit",
      "delivered",
      "cancelled",
    ])
  })

  it("filters by fulfilment status id", () => {
    const schema = shopOrdersFilterSheetSchema()
    const selection: typeof emptySelection = {
      ...emptySelection(schema),
      fulfilmentStatus: {
        kind: "multi-select",
        ids: ["in_transit"],
      },
    }

    const matched = MOCK_ORDERS.filter((order) =>
      matchesShopOrderFilters(order, selection)
    )
    expect(matched).toHaveLength(1)
    expect(matched[0]?.orderNumber).toBe("ORD-10425")
  })

  it("sorts by highest total", () => {
    const sorted = sortShopOrders(MOCK_ORDERS, "highest-total")
    expect(sorted[0]?.orderNumber).toBe("ORD-10350")
  })

  it("defaults sort id to newest", () => {
    const schema = shopOrdersFilterSheetSchema()
    expect(getShopOrdersSortId(emptySelection(schema))).toBe("newest")
  })
})
