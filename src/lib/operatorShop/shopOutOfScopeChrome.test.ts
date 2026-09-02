import { readFileSync, existsSync } from "node:fs"
import { dirname, join } from "node:path"
import { fileURLToPath } from "node:url"
import { describe, expect, it } from "vitest"
import { mapShopOrdersListResponse } from "@/lib/operatorShop/mapShopOrdersApiResponse"

const shopDir = join(
  dirname(fileURLToPath(import.meta.url)),
  "../../components/dashboard/operator/Shop"
)

function readShopSource(fileName: string): string {
  return readFileSync(join(shopDir, fileName), "utf8")
}

describe("shop out-of-scope chrome (ticket 21)", () => {
  it("omits Create QR control and dialog", () => {
    expect(existsSync(join(shopDir, "ShopCreateQrAssetDialog.tsx"))).toBe(
      false
    )
    const toolbar = readShopSource("ShopToolbar.tsx")
    expect(toolbar).not.toMatch(/Create QR|onCreateQrAsset/i)
    expect(toolbar).toMatch(/View orders/)
  })

  it("removes draft routes and draft chrome from orders and checkout", () => {
    const orders = readShopSource("ShopOrdersScreen.tsx")
    const checkout = readShopSource("ShopCheckoutScreen.tsx")
    const page = readShopSource("ShopPage.tsx")

    expect(orders).not.toMatch(
      /DEFAULT_DRAFTS|DetailedShopDraft|activeTab|"drafts"/
    )
    expect(orders).not.toMatch(/>\s*Drafts\s*</)
    expect(checkout).not.toMatch(/onSaveDraft|handleSaveDraft|Save at Drafts/)
    expect(page).not.toMatch(/onContinueCheckoutDraft|onSaveDraft/)
  })

  it("removes seeded local orders dialog; list mapping uses the API adapter", () => {
    expect(existsSync(join(shopDir, "ShopOrdersDialog.tsx"))).toBe(false)
    expect(readShopSource("ShopPage.tsx")).not.toMatch(/ShopOrdersDialog/)

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

    const ordersScreen = readShopSource("ShopOrdersScreen.tsx")
    expect(ordersScreen).toMatch(/fetchShopOrdersList/)
    expect(ordersScreen).toMatch(/mapShopOrdersListResponse/)
  })
})
