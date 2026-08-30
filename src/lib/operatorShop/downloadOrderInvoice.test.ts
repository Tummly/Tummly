import { describe, it, expect } from "vitest"
import { generateInvoiceHtml } from "./downloadOrderInvoice"
import type { DetailedShopOrder } from "./shopOrdersFilterSheetSchema"

describe("downloadOrderInvoice", () => {
  const sampleOrder: DetailedShopOrder = {
    id: "ord-10428",
    orderNumber: "#TM-10428",
    orderDate: "29 July 2026",
    isoDate: "2026-07-29",
    locationId: "1",
    locationName: "Padella Borough",
    materials: "Table tents · Pack of 20",
    materialTypes: ["table-tents"],
    placedBy: "Mohamed Mahmoud",
    total: "£82.80",
    totalNumeric: 82.8,
    paymentStatus: "Paid",
    fulfilmentStatus: "In production",
    updatedDate: "30 July 2026",
    items: ["20x Table tents (Matte Frosted Acrylic)"],
  }

  it("generates html containing invoice number, addresses, items and totals", () => {
    const html = generateInvoiceHtml(sampleOrder)

    expect(html).toContain("INV-TM-10428")
    expect(html).toContain("Invoice")
    expect(html).toContain("Tummly Ltd")
    expect(html).toContain("Padella Borough")
    expect(html).toContain("Mohamed Mahmoud")
    expect(html).toContain("£82.80 due 29 July 2026")
    expect(html).toContain("Table tents")
    expect(html).toContain("Standard delivery")
    expect(html).toContain("Tax (20%")
    expect(html).toContain("Page 1 of 1")
  })
})
