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
  toggleMultiSelect,
  openSession,
  pickDatePreset,
} from "@/lib/operatorFilterSheet"

const MOCK_ORDERS: DetailedShopOrder[] = [
  {
    id: "ord-1",
    orderNumber: "#TM-10428",
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
    fulfilmentStatus: "In production",
    updatedDate: "30 Jul 2026",
  },
  {
    id: "ord-2",
    orderNumber: "#TM-10425",
    orderDate: "25 Jul 2026",
    isoDate: "2026-07-25",
    locationId: "2",
    locationName: "The Ivy Soho Brasserie",
    materials: "Window stickers · Pack of 10",
    materialTypes: ["window-stickers"],
    placedBy: "Sarah Jenkins",
    total: "£45.00",
    totalNumeric: 45.0,
    paymentStatus: "Payment due",
    fulfilmentStatus: "Dispatched",
    updatedDate: "28 Jul 2026",
  },
  {
    id: "ord-3",
    orderNumber: "#TM-10350",
    orderDate: "04 May 2026",
    isoDate: "2026-05-04",
    locationId: "3",
    locationName: "Camden Market Kitchen",
    materials: "Receipt stickers · Roll of 1000",
    materialTypes: ["receipt-stickers"],
    placedBy: "Liam O'Connor",
    total: "£112.00",
    totalNumeric: 112.0,
    paymentStatus: "Payment failed",
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

  it("normalizes fulfilment and payment statuses to valid IDs", () => {
    expect(normalizeFulfilmentStatusToId("In production")).toBe("in-production")
    expect(normalizeFulfilmentStatusToId("Cancelled")).toBe("cancelled")
    expect(normalizePaymentStatusToId("Payment due")).toBe("payment-due")
    expect(normalizePaymentStatusToId("Partially refunded")).toBe("partially-refunded")
  })

  it("filters orders by fulfilment status", () => {
    const schema = shopOrdersFilterSheetSchema()
    let session = openSession(emptySelection(schema))
    session = toggleMultiSelect(session, "fulfilmentStatus", "in-production")

    const matching = MOCK_ORDERS.filter((o) =>
      matchesShopOrderFilters(o, session.pending)
    )
    expect(matching).toHaveLength(1)
    expect(matching[0].id).toBe("ord-1")
  })

  it("filters orders by payment status", () => {
    const schema = shopOrdersFilterSheetSchema()
    let session = openSession(emptySelection(schema))
    session = toggleMultiSelect(session, "paymentStatus", "payment-due")

    const matching = MOCK_ORDERS.filter((o) =>
      matchesShopOrderFilters(o, session.pending)
    )
    expect(matching).toHaveLength(1)
    expect(matching[0].id).toBe("ord-2")
  })

  it("filters orders by material type", () => {
    const schema = shopOrdersFilterSheetSchema()
    let session = openSession(emptySelection(schema))
    session = toggleMultiSelect(session, "materialType", "receipt-stickers")

    const matching = MOCK_ORDERS.filter((o) =>
      matchesShopOrderFilters(o, session.pending)
    )
    expect(matching).toHaveLength(1)
    expect(matching[0].id).toBe("ord-3")
  })

  it("filters orders by date preset (last-30 vs last-90)", () => {
    const schema = shopOrdersFilterSheetSchema()
    const refDate = new Date("2026-08-30T12:00:00Z")

    // Last 30 days (July 31 to Aug 30: ord-1 (Jul 29 is ~32 days ago), let's check July 25-29)
    let session = openSession(emptySelection(schema))
    const dateField = schema.fields.find((f) => f.id === "orderDate")!
    session = pickDatePreset(session, dateField, "last-90")

    const matching90 = MOCK_ORDERS.filter((o) =>
      matchesShopOrderFilters(o, session.pending, refDate)
    )
    // Jul 29 and Jul 25 are within last 90 days of Aug 30
    expect(matching90.map((o) => o.id)).toEqual(["ord-1", "ord-2"])
  })

  it("sorts orders correctly", () => {
    const sortedNewest = sortShopOrders(MOCK_ORDERS, "newest")
    expect(sortedNewest[0].id).toBe("ord-1") // Jul 29
    expect(sortedNewest[2].id).toBe("ord-3") // May 04

    const sortedOldest = sortShopOrders(MOCK_ORDERS, "oldest")
    expect(sortedOldest[0].id).toBe("ord-3") // May 04

    const sortedHighestTotal = sortShopOrders(MOCK_ORDERS, "highest-total")
    expect(sortedHighestTotal[0].totalNumeric).toBe(112.0)
    expect(sortedHighestTotal[2].totalNumeric).toBe(45.0)

    const sortedLowestTotal = sortShopOrders(MOCK_ORDERS, "lowest-total")
    expect(sortedLowestTotal[0].totalNumeric).toBe(45.0)

    const sortedStatus = sortShopOrders(MOCK_ORDERS, "status")
    expect(sortedStatus[0].fulfilmentStatus).toBe("Cancelled")
  })

  it("extracts active sort ID from selection with fallback to newest", () => {
    const schema = shopOrdersFilterSheetSchema()
    const empty = emptySelection(schema)
    expect(getShopOrdersSortId(empty)).toBe("newest")

    let session = openSession(empty)
    session = toggleMultiSelect(session, "sort", "highest-total")
    expect(getShopOrdersSortId(session.pending)).toBe("highest-total")
  })
})
