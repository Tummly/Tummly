import { describe, expect, it } from "vitest"
import { mapShopOrdersListResponse } from "@/lib/operatorShop/mapShopOrdersApiResponse"
import { SHOP_ORDERS_FULFILMENT_STATUS_LABELS } from "@/lib/operatorShop/shopOrdersFilterSheetSchema"
import {
  SHOP_ORDERS_SURFACE_TABS,
  SHOP_TOOLBAR_PRIMARY_ACTIONS,
} from "@/lib/operatorShop/shopOutOfScopeChrome"

describe("shop out-of-scope chrome (ticket 21)", () => {
  it("collapses fulfilment filter ids to the four stored values", () => {
    expect(Object.keys(SHOP_ORDERS_FULFILMENT_STATUS_LABELS)).toEqual([
      "processing",
      "in_transit",
      "delivered",
      "cancelled",
    ])
  })

  it("omits Create QR from toolbar primary actions", () => {
    expect(SHOP_TOOLBAR_PRIMARY_ACTIONS.map((action) => action.id)).toEqual([
      "view-orders",
    ])
    expect(
      SHOP_TOOLBAR_PRIMARY_ACTIONS.some((action) =>
        /create qr/i.test(action.label)
      )
    ).toBe(false)
  })

  it("orders surface has no drafts tab or draft routes", () => {
    expect(SHOP_ORDERS_SURFACE_TABS).toEqual(["orders"])
    expect(SHOP_ORDERS_SURFACE_TABS).not.toContain("drafts")
  })

  it("maps orders list rows from the API adapter", () => {
    const mapped = mapShopOrdersListResponse({
      items: [
        {
          id: "ord-1",
          orderNumber: "ORD-10428",
          orderDate: "29 Jul 2026",
          locationId: 1,
          locationName: "Padella",
          materialsSummary: "Table tents",
          materialTypes: ["table-tents"],
          placedBy: "Owner",
          totalFormatted: "£82.80",
          totalGrossPence: 8280,
          paymentStatus: "Paid",
          fulfilmentStatus: "Processing",
          updatedAtUtc: "2026-07-30T12:00:00Z",
        },
      ],
      totalCount: 1,
      page: 1,
      pageSize: 25,
      aggregates: {
        inProgress: 1,
        dispatched: 0,
        deliveredLast90Days: 0,
      },
    })

    expect(mapped.orders).toHaveLength(1)
    expect(mapped.orders[0]?.orderNumber).toBe("ORD-10428")
    expect(mapped.orders[0]?.fulfilmentStatus).toBe("Processing")
    expect(mapped.totalCount).toBe(1)
    expect(mapped.aggregates.inProgress).toBe(1)
  })
})
